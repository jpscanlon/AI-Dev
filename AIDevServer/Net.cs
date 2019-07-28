using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace AIDevServer
{
    // Neural Network.
    [Serializable()]
    class Net
    {
        [NonSerialized()] private static readonly SqlConnection KBConnection = Knowledgebase.KBConnection;

        public int NetID { get; protected set; }
        public string Name { get; set; } = "";
        public NetType Type { get; set; }
        //public string ActivationFunction { get; set; } = "";
        public ActivationFunction ActivationFunction { get; set; } = ActivationFunction.tanh;
        public int NumInputs { get; protected set; } = 0;
        public int NumOutputs { get; protected set; } = 0;
        public int NumConvLayers { get; protected set; } = 0;
        public int NumFCLayers { get; protected set; } = 2;  // Default number of fully-connected layers.
        public bool IsGrayscale { get; set; } = false;
        private bool isBackpropTraining = false;

        // These declarations have the dots under them, suggesting make readonly, because they currently
        // aren't being modified.
        private int poolingStepSize = 2;  // 2 reduces map dimensions by 1/2.
        private int filterSize = 3;
        // Set to default number of filters for 1st conv layer.
        // 2nd layer gets 2 * numFilters, 3rd layer gets 3 * numFilters, etc.
        private int numFilters = 6;
        //private float numFiltersIncreaseFactor = 2;
        const float biasInput = 1.0F;

        private int numFCInputs;
        private float[] inputs;

        private ConvLayer[] convLayers;
        private FCLayer[] fcLayers;

        // For backprop training.
        [NonSerialized()] private ConvBackpropLayer[] convBackpropLayers;
        [NonSerialized()] private FCBackpropLayer[] fcBackpropLayers;

        [NonSerialized()] private ConvLayer[] savedConvLayers;
        [NonSerialized()] private FCLayer[] savedFCLayers;

        [NonSerialized()] private FeatureMapsLayer[] featureMapsLayers;

        public Net()
        {
            //File.AppendAllText(AppProperties.ServerLogPath, "\r\nConstructing Net ...\r\n\r\n");
        }

        public string Create(int netID, string name, NetType type, string activationFunction,
            int numInputs, int numOutputs, int numFCLayers, int numConvLayers, bool grayscale = false)
        {
            string result = "error";

            NetID = netID;
            Name = name;
            Type = type;
            Enum.TryParse(activationFunction, out ActivationFunction activationFunctionName);
            ActivationFunction = activationFunctionName;
            NumInputs = numInputs;
            NumOutputs = numOutputs;
            IsGrayscale = grayscale;
            int numInputMaps;

            Random random = new Random();

            //GetNetProperties(netID);

            File.AppendAllText(AppProperties.ServerLogPath, "Initializing net. Properties: " +
                Name + " " + ActivationFunction + " " + Convert.ToString(NumInputs) + " " +
                Convert.ToString(NumOutputs) + " " +
                Convert.ToString(numFCLayers) + " " + Convert.ToString(numConvLayers) + "\r\n");

            NumFCLayers = numFCLayers;
            NumConvLayers = numConvLayers;

            if (type == NetType.Convolutional)
            {
                if (NumConvLayers < 1) NumConvLayers = 1;  // Net must have at least one convolutional layer.

                // Create convolutional layers.
                convLayers = new ConvLayer[NumConvLayers];

                if (IsGrayscale)
                {
                    // Just one input image for grayscale.
                    numInputMaps = 1;
                }
                else
                {
                    // First conv layer has 3 input maps. One each for red, green, and blue.
                    numInputMaps = 3;
                }

                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    // If not the 1st layer, set # input maps to previous layer's # output maps.
                    //if (layer > 0) numInputMaps = convLayers[layer - 1].NumFilters;
                    //// Set # filters for each new layer to the same for now.
                    ////convLayers[layer] = new ConvLayer(numFilters, numInputMaps, filterSize);
                    //convLayers[layer] = new ConvLayer((layer + 1) * numFilters, numInputMaps, filterSize);
                    if (layer == 0)
                    {
                        convLayers[layer] = new ConvLayer(numFilters, numInputMaps, filterSize);
                    }
                    else
                    {
                        numInputMaps = convLayers[layer - 1].NumFilters;
                        convLayers[layer] = new ConvLayer((layer + 1) * numFilters, numInputMaps, filterSize);
                    }
                }

                //CreateFeatureMaps();

                // # Inputs reduced to 1/4 in each conv layer.
                //numFCInputs = (NumInputs / Common.NaturalPow(poolingStepSize * 2, NumConvLayers)) * numFilters;
                numFCInputs = (NumInputs / Common.NaturalPow(poolingStepSize * 2, NumConvLayers)) *
                    convLayers[NumConvLayers - 1].NumFilters;  // Num filters in last layer.
                if (numFCInputs < 1) numFCInputs = 1;

                //NumFCLayers = 2;
            }
            else if (type == NetType.FullyConnected)
            {
                numFCInputs = numInputs;
            }

            if (NumFCLayers < 1) NumFCLayers = 1;  // Net must have at least one fully connected layer.

            // Create fully-connected layers.
            fcLayers = new FCLayer[NumFCLayers];

            int numLayerInputs;
            int numLayerOutputs;

            int[] layersNumOutputs = new int[NumFCLayers];
            GetNumHidden(numFCInputs, NumOutputs, layersNumOutputs);

            for (int layer = 0; layer < NumFCLayers; layer++)
            {
                if (layer == 0)
                {
                    // Fully-connected input layer.
                    numLayerInputs = numFCInputs;
                    numLayerOutputs = layersNumOutputs[0];
                }
                else
                {
                    numLayerInputs = layersNumOutputs[layer - 1];
                    numLayerOutputs = layersNumOutputs[layer];
                }

                fcLayers[layer] = new FCLayer(numLayerOutputs, numLayerInputs);
            }

            //CreateBackpropLayers();
            InitializeWeights();

            //CreateSavedWeightsArrays();

            result = "success";

            return result;
        }

        public void InitializeWeights()
        {
            float weightValue;
            Random random = new Random();
            //Random random = new Random(1);  // Use same seed for testing identically initialized nets.

            if (Type == NetType.Convolutional)
            {
                // Initialize weights in conv layers.
                int numWeightsInFilter = filterSize * filterSize;

                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    for (int filter = 0; filter < convLayers[layer].NumFilters; filter++)
                    {
                        //for (int inputMap = 0; inputMap < convLayers[layer].Weights.GetLength(1); inputMap++)
                        for (int inputMap = 0; inputMap < convLayers[layer].NumInputMaps; inputMap++)
                        {
                            for (int x = 0; x < filterSize; x++)
                            {
                                for (int y = 0; y < filterSize; y++)
                                {
                                    //weightValue = Convert.ToSingle((random.NextDouble() * 2) - 1);
                                    weightValue = Convert.ToSingle(BoxMuller(random, 0, 0.05));
                                    convLayers[layer].Weights[filter, inputMap, x, y] = weightValue;
                                }
                            }
                        }
                        // Set bias weights for the filter to zero.
                        convLayers[layer].BiasWeights[filter] = 0.0F;
                    }
                }
            }

            // Initialize weights in FC layers.
            for (int layer = 0; layer < fcLayers.Length; layer++)
            {
                for (int output = 0; output < fcLayers[layer].Weights.GetLength(0); output++)
                {
                    for (int input = 0; input < (fcLayers[layer].Weights.GetLength(1) - 1); input++)
                    {
                        //weightValue = Convert.ToSingle((random.NextDouble() * 2) - 1);
                        //weightValue = Convert.ToSingle(NextGaussianDouble(random));
                        //weightValue = Convert.ToSingle((NextGaussianDouble(random) * 2) - 1);

                        // 0.05 performs better than any larger deviation.
                        weightValue = Convert.ToSingle(BoxMuller(random, 0, 0.05));

                        fcLayers[layer].Weights[output, input] = weightValue;
                    }
                    // Set bias weight to zero.
                    fcLayers[layer].Weights[output, fcLayers[layer].Weights.GetLength(1) - 1] = 0.0F;
                }
            }
        }

        // Generates random numbers in a normal distribution with a given standard deviation.
        // This can also be used to generate a 2-D Gaussian distribution.
        private static double BoxMuller(Random random, double mean, double stddev)
        {
            double u1 = 1.0 - random.NextDouble();  // These are uniform (0,1) random doubles
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2);  // Random normal (0,1)
            double randNormal = mean + (stddev * randStdNormal);  // Random normal (mean, stdDev^2)

            return randNormal;
        }

        // Returns a random double with a Gaussian distribution.
        // "Gauss" rhymes with "house".
        private static double NextGaussianDouble(Random random)
        {
            double u, v, S;

            do
            {
                u = 2.0 * random.NextDouble() - 1.0;
                v = 2.0 * random.NextDouble() - 1.0;
                S = u * u + v * v;
            }
            while (S >= 1.0);

            double fac = Math.Sqrt(-2.0 * Math.Log(S) / S);
            return u * fac;
        }

        // Scales a weight by #inputs to avoid over-saturation in weighted sums.
        private float ScaleWeight(double weightValue, int numInputs)
        {
            //weightValue = (weightValue / Convert.ToSingle(numInputs)); // * 2;

            // This seems to work to some degree.
            weightValue = weightValue / Math.Sqrt(Convert.ToDouble(numInputs));

            // Testing.
            //weightValue = weightValue / 4;

            return Convert.ToSingle(weightValue);
        }

        private void CreateSavedWeightsArrays()
        {
            if (Type == NetType.Convolutional)
            {
                // Create convolutional layers.
                savedConvLayers = new ConvLayer[NumConvLayers];

                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    //savedConvLayers[layer] = new ConvLayer(numFilters, convLayers[layer].Weights.GetLength(1), filterSize);
                    savedConvLayers[layer] = new ConvLayer(convLayers[layer].NumFilters, convLayers[layer].NumInputMaps, filterSize);
                }
            }

            // Create FC layers.
            savedFCLayers = new FCLayer[NumFCLayers];

            for (int layer = 0; layer < NumFCLayers; layer++)
            {
                savedFCLayers[layer] = new FCLayer(fcLayers[layer].NumOutputs, fcLayers[layer].NumInputs);
            }
        }

        public void SaveWeights()
        {
            if (Type == NetType.Convolutional)
            {
                if (savedConvLayers == null || savedFCLayers == null)
                {
                    CreateSavedWeightsArrays();
                }

                for (int layer = 0; layer < convLayers.Length; layer++)
                {
                    Array.Copy(convLayers[layer].Weights, 0, savedConvLayers[layer].Weights, 0, savedConvLayers[layer].Weights.Length);
                    Array.Copy(convLayers[layer].BiasWeights, 0, savedConvLayers[layer].BiasWeights, 0, savedConvLayers[layer].BiasWeights.Length);
                }
            }
            else if (Type == NetType.FullyConnected)
            {
                if (savedFCLayers == null)
                {
                    CreateSavedWeightsArrays();
                }
            }

            for (int layer = 0; layer < fcLayers.Length; layer++)
            {
                Array.Copy(fcLayers[layer].Weights, 0, savedFCLayers[layer].Weights, 0, savedFCLayers[layer].Weights.Length);
            }
        }

        public void RevertWeights()
        {
            if (Type == NetType.Convolutional)
            {
                for (int layer = 0; layer < convLayers.Length; layer++)
                {
                    Array.Copy(savedConvLayers[layer].Weights, 0, convLayers[layer].Weights, 0, convLayers[layer].Weights.Length);
                    Array.Copy(savedConvLayers[layer].BiasWeights, 0, convLayers[layer].BiasWeights, 0, convLayers[layer].BiasWeights.Length);
                }
            }

            for (int layer = 0; layer < fcLayers.Length; layer++)
            {
                Array.Copy(savedFCLayers[layer].Weights, 0, fcLayers[layer].Weights, 0, fcLayers[layer].Weights.Length);
            }
        }

        // Modifies all weights randomly according to parameters for training.
        public void ModifyWeights(Random random, float mu, float muVariance,
            float percentFCWeightsToChange = 1.00F, float percentConvWeightsToChange = 1.00F)
        {
            float weightValue;

            if (Type == NetType.Convolutional)
            {
                // Modify conv weights.
                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    for (int iFilter = 0; iFilter < convLayers[layer].NumFilters; iFilter++)
                    {
                        //for (int inputMap = 0; inputMap < convLayers[layer].Weights.GetLength(1); inputMap++)
                        for (int inputMap = 0; inputMap < convLayers[layer].NumInputMaps; inputMap++)
                        {
                            for (int x = 0; x < filterSize; x++)
                            {
                                for (int y = 0; y < filterSize; y++)
                                {
                                    // Modify a specified percentage of the weights.
                                    if (random.NextDouble() <= percentConvWeightsToChange)
                                    {
                                        // Modify filter input weight.
                                        weightValue = convLayers[layer].Weights[iFilter, inputMap, x, y];
                                        convLayers[layer].Weights[iFilter, inputMap, x, y] =
                                        GetNewWeightValue(random, weightValue, mu, muVariance);
                                    }
                                }
                            }
                        }

                        if (random.NextDouble() <= percentConvWeightsToChange)
                        {
                            // Modify filter bias weight.
                            weightValue = convLayers[layer].BiasWeights[iFilter];
                            convLayers[layer].BiasWeights[iFilter] = GetNewWeightValue(random, weightValue, mu, muVariance);
                        }
                    }
                }
            }

            // Modify FC weights.
            int numInputs;
            int numWeightsToChange;
            int numWeightsChanged;
            int weightToChange;
            List<int> changedWeights = new List<int>();
            for (int layer = 0; layer < fcLayers.Length; layer++)
            {
                for (int iOutput = 0; iOutput < fcLayers[layer].Weights.GetLength(0); iOutput++)
                {
                    if (percentFCWeightsToChange > 0.03F)
                        for (int input = 0; input < fcLayers[layer].Weights.GetLength(1); input++)
                        {
                            // Modify a specified percentage of the weights.
                            if (random.NextDouble() <= percentFCWeightsToChange)
                            {
                                weightValue = fcLayers[layer].Weights[iOutput, input];
                                fcLayers[layer].Weights[iOutput, input] =
                                    GetNewWeightValue(random, weightValue, mu, muVariance);
                            }
                        }
                    else
                    {
                        // At low percentage of weights being changed, this reduces execution time some.
                        // (to 89% at 0.02 weights being changed or 66% at 0.01.)
                        // May not be worth the complexity to add to convolutional nets.
                        changedWeights.Clear();
                        numInputs = fcLayers[layer].Weights.GetLength(1);
                        numWeightsToChange = Convert.ToInt32(numInputs * percentFCWeightsToChange);
                        numWeightsChanged = 0;
                        while (numWeightsChanged < 1 || numWeightsChanged < numWeightsToChange)
                        {
                            weightToChange = random.Next(0, numInputs);
                            while (changedWeights.Contains(weightToChange))
                            {
                                weightToChange = random.Next(0, numInputs);
                            }
                            weightValue = fcLayers[layer].Weights[iOutput, weightToChange];
                            fcLayers[layer].Weights[iOutput, weightToChange] =
                                GetNewWeightValue(random, weightValue, mu, muVariance);
                            changedWeights.Add(weightToChange);
                            numWeightsChanged++;
                        }
                    }
                }
            }
        }

        public void OutputWeights()
        {
            float weightValue;

            if (Type == NetType.Convolutional)
            {
                // Conv weights.
                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    for (int iFilter = 0; iFilter < convLayers[layer].NumFilters; iFilter++)
                    {
                        for (int inputMap = 0; inputMap < convLayers[layer].NumInputMaps; inputMap++)
                        {
                            for (int x = 0; x < filterSize; x++)
                            {
                                for (int y = 0; y < filterSize; y++)
                                {
                                    // Filter input weight.
                                    weightValue = convLayers[layer].Weights[iFilter, inputMap, x, y];
                                }
                            }
                        }

                        // Filter bias weight.
                        weightValue = convLayers[layer].BiasWeights[iFilter];

                        File.AppendAllText(AppProperties.ServerLogPath, "Filter# [" + iFilter +
                            "] Conv bias = " + convLayers[layer].BiasWeights[iFilter].ToString("+0.0000000;-0.0000000") +
                            ", Saved conv bias = " + savedConvLayers[layer].BiasWeights[iFilter].ToString("+0.0000000;-0.0000000") + "\r\n");
                    }
                }
            }

            // FC weights.
            for (int layer = 0; layer < fcLayers.Length; layer++)
            {
                for (int iOutput = 0; iOutput < fcLayers[layer].Weights.GetLength(0); iOutput++)
                {
                    for (int input = 0; input < fcLayers[layer].Weights.GetLength(1); input++)
                    {
                        // FC weight.
                        weightValue = fcLayers[layer].Weights[iOutput, input];
                    }
                }
            }
            File.AppendAllText(AppProperties.ServerLogPath, "[Add FC weights output here.]\r\n");
        }

        // Returns a weight value changed by a randomly positive or negative mu
        // +/- a random amount of variance.
        public float GetNewWeightValue(Random random, float weightValue, float mu, float muVariance)
        {
            double change;
            double variance;

            //// Change by fixed value of mu.
            //weightValue = weightValue + (mu * GetRandomNegativeOrPositive(random));

            // Change by mu +/- a random amount of variance.
            variance = (muVariance * mu) * GetRandomSymetrical(random);
            change = (mu + variance) * GetRandomNegativeOrPositive(random);

            //File.AppendAllText(AppProperties.ServerLogPath, "weightValue = " + weightValue.ToString("+0.0000000;-0.0000000") +
            //    ", variance = " + variance.ToString("+0.0000000;-0.0000000") +
            //    ", change = " + change.ToString("+0.0000000;-0.0000000") +
            //     ", new weightValue = " + (weightValue + change).ToString("+0.0000000;-0.0000000") + "\r\n");

            weightValue = weightValue + Convert.ToSingle(change);

            //if (weightValue > 2.0)
            //{
            //    weightValue = 2.0F;
            //}
            //else
            //{
            //    if (weightValue < -2.0) weightValue = -2.0F;
            //}

            return weightValue;
        }

        // Returns -1 or +1 with 50/50% chance.
        private int GetRandomNegativeOrPositive(Random random)
        {
            // Determine positive or negative sign with 50/50% chance.
            int value = -1;
            if (random.NextDouble() >= 0.5) value = 1;

            return value;
        }

        // NextDouble returns value >= 0 and < 1, so is slightly non-symetrical.
        // This method returns a symetrical float value > -1.0 and < +1.0.
        private double GetRandomSymetrical(Random random)
        {
            double value;

            if (random.Next(0, 2) == 1)
            {
                value = random.NextDouble();
            }
            else
            {
                value = 0.0 - random.NextDouble();
            }

            return value;
        }

        public float[] Run(NetMap[] inputMaps, bool isBackpropTraining, bool grayscale = false)
        {
            this.isBackpropTraining = isBackpropTraining;

            if (this.isBackpropTraining && (fcBackpropLayers == null || convBackpropLayers == null))
            {
                CreateBackpropLayers();
            }

            if (Type == NetType.Convolutional)
            {
                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    if (layer == 0)
                    {
                        RunConvLayer(layer, inputMaps);
                    }
                    else
                    {
                        RunConvLayer(layer);
                    }
                }
            }

            // Load input vector for FC layers.
            float[] fcValues;

            int i = 0;

            if (Type == NetType.Convolutional)
            {
                int width = featureMapsLayers[NumConvLayers - 1].PooledMaps[0].Width;
                int height = featureMapsLayers[NumConvLayers - 1].PooledMaps[0].Height;

                fcValues = new float[convLayers[NumConvLayers - 1].NumFilters * width * height];

                for (int inputMap = 0; inputMap < featureMapsLayers[NumConvLayers - 1].PooledMaps.Length; inputMap++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            fcValues[i] = featureMapsLayers[NumConvLayers - 1].PooledMaps[inputMap].Points[x, y];
                            i++;
                        }
                    }
                }
            }
            else
            {
                fcValues = new float[inputMaps.Length * inputMaps[0].Width * inputMaps[0].Height];

                for (int inputMap = 0; inputMap < inputMaps.Length; inputMap++)
                {
                    for (int x = 0; x < inputMaps[0].Width; x++)
                    {
                        for (int y = 0; y < inputMaps[0].Height; y++)
                        {
                            fcValues[i] = inputMaps[inputMap].Points[x, y];
                            i++;
                        }
                    }
                }
            }

            inputs = fcValues;

            // Run FC layers.
            for (int fcLayer = 0; fcLayer < NumFCLayers; fcLayer++)
            {
                fcValues = RunFCLayer(fcLayer, fcValues);
            }

            this.isBackpropTraining = false;

            return fcValues;
        }

        private void RunConvLayer(int layer, NetMap[] inputMaps = null)
        {
            if (layer == 0)
            {
                // This is the first convolutional layer (net-inputs).
                if (featureMapsLayers == null)
                {
                    CreateFeatureMaps();
                }

                for (int filter = 0; filter < convLayers[layer].NumFilters; filter++)
                {
                    for (int x = 0; x < inputMaps[0].Width; x++)
                    {
                        for (int y = 0; y < inputMaps[0].Height; y++)
                        {
                            featureMapsLayers[layer].FeatureMaps[filter].Points[x, y] =
                                RunFilter(layer, inputMaps, filter, x, y);
                        }
                    }
                    //if (isBackpropTraining)
                    //{
                    //    GetInputAverages(layer, filter, inputMaps.Length, inputMaps[0].Width, inputMaps[0].Height);
                    //}
                }
            }
            else
            {
                for (int filter = 0; filter < convLayers[layer].NumFilters; filter++)
                {
                    for (int x = 0; x < featureMapsLayers[layer - 1].PooledMaps[0].Width; x++)
                    {
                        for (int y = 0; y < featureMapsLayers[layer - 1].PooledMaps[0].Height; y++)
                        {
                            featureMapsLayers[layer].FeatureMaps[filter].Points[x, y] =
                                RunFilter(layer, featureMapsLayers[layer - 1].PooledMaps, filter, x, y);
                        }
                    }
                    //if (isBackpropTraining)
                    //{
                    //    GetInputAverages(layer, filter, featureMapsLayers[layer - 1].PooledMaps.Length,
                    //        featureMapsLayers[layer - 1].PooledMaps[0].Width,
                    //        featureMapsLayers[layer - 1].PooledMaps[0].Height);
                    //}
                }
            }

            PoolConvLayer(layer);
        }

        //private void GetInputAverages(int layer, int filter, int numInputMaps, int mapWidth, int mapHeight)
        //{
        //    int numPoints = mapWidth * mapHeight;

        //    for (int inputMap = 0; inputMap < numInputMaps; inputMap++)
        //    {
        //        for (int filterX = 0; filterX < filterSize; filterX++)
        //        {
        //            for (int filterY = 0; filterY < filterSize; filterY++)
        //            {
        //                convBackpropLayers[layer].FilterInputs[filter, inputMap, filterX, filterY] =
        //                    convBackpropLayers[layer].FilterInputs[filter, inputMap, filterX, filterY] / numPoints;
        //            }
        //        }
        //    }
        //}

        private float[] RunFCLayer(int fcLayerNum, float[] fcInputs)
        {
            int numInputs = fcLayers[fcLayerNum].NumInputs;
            int numOutputs = fcLayers[fcLayerNum].NumOutputs;
            float outputValue;

            float[] fcOutputs = new float[fcLayers[fcLayerNum].NumOutputs];
            float weightedSum;

            for (int output = 0; output < fcLayers[fcLayerNum].NumOutputs; output++)
            {
                weightedSum = 0;
                for (int input = 0; input < numInputs; input++)
                {
                    weightedSum = weightedSum + (fcLayers[fcLayerNum].Weights[output, input] * fcInputs[input]);

                    //File.AppendAllText(AppProperties.ServerLogPath, "Neuron value = " + fcInputs[input] + 
                    //    "Weight value = " + FCLayers[fcLayerNum].Weights[iOutput, input] + "\r\n");
                }
                weightedSum = weightedSum +
                    (fcLayers[fcLayerNum].Weights[output, numInputs] * 1);  // Add bias.

                // Scale the weighted sum proportional to the number of inputs to avoid oversaturation.
                weightedSum = (weightedSum / numInputs) * 80;
                //weightedSum = (weightedSum / numInputs) * 1;

                outputValue = ApplyActivationFunction(weightedSum);
                fcOutputs[output] = outputValue;

                if (isBackpropTraining)
                {
                    fcBackpropLayers[fcLayerNum].Outputs[output] = outputValue;
                }

                //File.AppendAllText(AppProperties.ServerLogPath, "Weighted sum of inputs to neuron = " + weightedSum + "\r\n");
            }

            return fcOutputs;
        }

        // Applies a filter to an area in an input map for a conv layer and returns its calculated output value.
        private float RunFilter(int layer, NetMap[] inputMaps, int filter, int mapPointX, int mapPointY)
        {
            float weightedSum = 0;
            float outputValue;
            int filterCenterPoint;
            int mapPointToReadX;
            int mapPointToReadY;
            const float paddingValue = 0.0F;
            int numFilterInputs = (filterSize * filterSize) + 1;

            filterCenterPoint = (filterSize - 1) / 2;  // filterSize must be an odd number for this to work.

            for (int inputMap = 0; inputMap < inputMaps.Length; inputMap++)
            {
                for (int filterX = 0; filterX < filterSize; filterX++)
                {
                    for (int filterY = 0; filterY < filterSize; filterY++)
                    {
                        mapPointToReadX = mapPointX + (filterX - filterCenterPoint);
                        mapPointToReadY = mapPointY + (filterY - filterCenterPoint);
                        if (mapPointToReadX < 0 || mapPointToReadX >= inputMaps[inputMap].Width ||
                            mapPointToReadY < 0 || mapPointToReadY >= inputMaps[inputMap].Height)
                        {
                            // Point is off the map, so use padding value.
                            weightedSum = weightedSum + (convLayers[layer].Weights[filter, inputMap, filterX, filterY] * paddingValue);
                            if (isBackpropTraining)
                            {
                                convBackpropLayers[layer].FilterInputs[filter, inputMap, filterX, filterY] += paddingValue;
                            }
                        }
                        else
                        {
                            weightedSum = weightedSum + (convLayers[layer].Weights[filter, inputMap, filterX, filterY] *
                                inputMaps[inputMap].Points[mapPointX, mapPointY]);
                            if (isBackpropTraining)
                            {
                                convBackpropLayers[layer].FilterInputs[filter, inputMap, filterX, filterY] +=
                                    inputMaps[inputMap].Points[mapPointX, mapPointY];
                            }
                        }
                    }
                }
            }

            // Apply bias weight for the filter.
            weightedSum = weightedSum + (convLayers[layer].BiasWeights[filter] * biasInput);

            weightedSum = (weightedSum / numFilterInputs) * 10;
            outputValue = ApplyActivationFunction(weightedSum);

            return outputValue;
        }

        private void PoolConvLayer(int layerNum)
        {
            int width = featureMapsLayers[layerNum].FeatureMaps[0].Width;
            int height = featureMapsLayers[layerNum].FeatureMaps[0].Height;
            float value;
            int pooledX;
            int pooledY;

            for (int filter = 0; filter < convLayers[layerNum].NumFilters; filter++)
            {
                for (int x = 0; x < width; x = x + poolingStepSize)
                {
                    for (int y = 0; y < height; y = y + poolingStepSize)
                    {
                        pooledX = 0;
                        pooledY = 0;
                        value = MaxPool(featureMapsLayers[layerNum].FeatureMaps[filter], x, y, poolingStepSize, 
                            ref pooledX, ref pooledY);
                        featureMapsLayers[layerNum].PooledMaps[filter].
                            Points[x / poolingStepSize, y / poolingStepSize] = value;

                        //if (isBackpropTraining)
                        //{
                        //    convBackpropLayers[layerNum].Maps[filter].
                        //        Outputs[x / poolingStepSize, y / poolingStepSize] = value;
                        //    // Save which point was chosen by MaxPool.
                        //    convBackpropLayers[layerNum].Maps[filter].
                        //        PooledX[x / poolingStepSize, y / poolingStepSize] = pooledX;
                        //    convBackpropLayers[layerNum].Maps[filter].
                        //        PooledY[x / poolingStepSize, y / poolingStepSize] = pooledY;
                        //}
                    }
                }
            }
        }

        private float MaxPool(NetMap map, int mapPointX, int mapPointY, int poolingStepSize, 
            ref int maxX, ref int maxY)
        {
            float highestValue = -2.0F;

            for (int filterPointX = 0; filterPointX < poolingStepSize; filterPointX++)
            {
                for (int filterPointY = 0; filterPointY < poolingStepSize; filterPointY++)
                {
                    if (map.Points[mapPointX + filterPointX, mapPointY + filterPointY] > highestValue)
                    {
                        highestValue = map.Points[mapPointX + filterPointX, mapPointY + filterPointY];
                        maxX = mapPointX + filterPointX;
                        maxY = mapPointY + filterPointY;
                    }
                }
            }

            return highestValue;
        }

        private float ApplyActivationFunction(float weightedSum)
        {
            float outputValue;

            // Case statement could choose activation function to apply.
            // enum ActivationFunctionName { tanh, ReLU, sigmoid }

            outputValue = Convert.ToSingle(Math.Tanh(weightedSum));

            //File.AppendAllText(AppProperties.ServerLogPath, "Weighted sum of inputs to neuron = " + weightedSum + "\r\n");

            return outputValue;
        }

        // Backpropagate error and store in backprop layers.
        public void BackpropagateError(float[] targets)
        {
            float sumErrors;
            //float reverseWeightedSum;

            for (int layer = NumFCLayers - 1; layer >= 0; layer--)
            {
                if (layer < NumFCLayers - 1)
                {
                    // This is a hidden FC layer.
                    //reverseWeightedSum = (1 / 80) * neuronLayers[layer].NumNeurons;
                    for (int neuron = 0; neuron < fcBackpropLayers[layer].NumNeurons; neuron++)
                    {
                        sumErrors = 0.0F;
                        for (int nextLayerNeuron = 0; nextLayerNeuron < fcBackpropLayers[layer + 1].NumNeurons;
                            nextLayerNeuron++)
                        {
                            sumErrors += fcLayers[layer + 1].Weights[nextLayerNeuron, neuron] *
                                fcBackpropLayers[layer + 1].Deltas[nextLayerNeuron];
                            // Use reverse weighted sum.
                            //error += fcLayers[layer].Weights[nextLayerNeuron, neuron] *
                            //    (neuronLayers[layer].Deltas[nextLayerNeuron] * reverseWeightedSum);
                        }
                        fcBackpropLayers[layer].Errors[neuron] = sumErrors;
                    }
                }
                else
                {
                    // This is the FC output layer.
                    for (int neuron = 0; neuron < fcBackpropLayers[layer].NumNeurons; neuron++)
                    {
                        fcBackpropLayers[layer].Errors[neuron] = targets[neuron] - fcBackpropLayers[layer].Outputs[neuron];
                    }
                }

                for (int neuron = 0; neuron < fcBackpropLayers[layer].NumNeurons; neuron++)
                {
                    fcBackpropLayers[layer].Deltas[neuron] = fcBackpropLayers[layer].Errors[neuron] *
                        ApplyActivationFunctionDerivatative(fcBackpropLayers[layer].Outputs[neuron]);
                }
            }

            if (Type == NetType.Convolutional)
            {
                float error;
                float sumDeltas;
                for (int layer = NumConvLayers - 1; layer >= 0; layer--)
                {
                    int width = featureMapsLayers[layer].PooledMaps[0].Width;
                    int height = featureMapsLayers[layer].PooledMaps[0].Height;
                    int mapSize = width * height;

                    if (layer < NumConvLayers - 1)
                    {
                        // This is a hidden convolutional layer.
                        for (int neuron = 0; neuron < convBackpropLayers[layer].NumNeurons; neuron++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    for (int nextLayerNeuron = 0;
                                    nextLayerNeuron < convBackpropLayers[layer + 1].NumNeurons;
                                    nextLayerNeuron++)
                                    {
                                        BackpropagateFilterError(layer, neuron, nextLayerNeuron, x, y);
                                    }
                                }
                            }
                            sumErrors = 0.0F;
                            sumDeltas = 0.0F;
                            for (int x = 0; x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    sumErrors =+ convBackpropLayers[layer].Maps[neuron].Errors[x, y];
                                    convBackpropLayers[layer].Maps[neuron].Deltas[x, y] =
                                        convBackpropLayers[layer].Maps[neuron].Errors[x, y] *
                                        ApplyActivationFunctionDerivatative(
                                        featureMapsLayers[layer].FeatureMaps[neuron].Points[x, y]);
                                    sumDeltas = + convBackpropLayers[layer].Maps[neuron].Deltas[x, y];
                                }
                            }
                            convBackpropLayers[layer].Errors[neuron] = sumErrors;
                            convBackpropLayers[layer].Deltas[neuron] = sumDeltas;
                        }
                    }
                    else
                    {
                        // This is the convolutional output layer.
                        for (int neuron = 0; neuron < convBackpropLayers[layer].NumNeurons; neuron++)
                        {
                            sumErrors = 0.0F;
                            sumDeltas = 0.0F;
                            for (int x = 0; x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    error = 0.0F;
                                    for (int nextLayerNeuron = 0; nextLayerNeuron < fcBackpropLayers[0].NumNeurons;
                                        nextLayerNeuron++)
                                    {
                                        error += fcLayers[0].Weights[nextLayerNeuron,
                                            (neuron * mapSize) + (x * width) + y] *
                                            fcBackpropLayers[0].Deltas[nextLayerNeuron];
                                    }
                                    sumErrors =+ error;

                                    convBackpropLayers[layer].Maps[neuron].Errors[x, y] = error;
                                    convBackpropLayers[layer].Maps[neuron].Deltas[x, y] =
                                        convBackpropLayers[layer].Maps[neuron].Errors[x, y] *
                                        ApplyActivationFunctionDerivatative(
                                        featureMapsLayers[layer].FeatureMaps[neuron].Points[x, y]);
                                    sumDeltas =+ convBackpropLayers[layer].Maps[neuron].Deltas[x, y];
                                }
                            }
                            convBackpropLayers[layer].Errors[neuron] = sumErrors;
                            convBackpropLayers[layer].Deltas[neuron] = sumDeltas;
                        }
                    }
                }
            }
        }

        private void BackpropagateFilterError(int layer, int inputMap, int nextLayerFilter, int mapPointX, int mapPointY)
        {
            int filterCenterPoint;
            int mapPointToReadX;
            int mapPointToReadY;
            //const float paddingValue = 0.0F;
            int numFilterInputs = (filterSize * filterSize) + 1;

            filterCenterPoint = (filterSize - 1) / 2;  // filterSize must be an odd number for this to work.

            for (int filterX = 0; filterX < filterSize; filterX++)
            {
                for (int filterY = 0; filterY < filterSize; filterY++)
                {
                    mapPointToReadX = mapPointX + (filterX - filterCenterPoint);
                    mapPointToReadY = mapPointY + (filterY - filterCenterPoint);
                    if (mapPointToReadX < 0 || mapPointToReadX >= 
                        convBackpropLayers[layer].Maps[inputMap].Width ||
                        mapPointToReadY < 0 || mapPointToReadY >= 
                        convBackpropLayers[layer].Maps[inputMap].Height)
                    {
                        // Point is off the map, so use padding value.
                        //convBackpropLayers[layer].Maps[inputMap].Errors[mapPointToReadX, mapPointToReadY] += 
                        //    (convLayers[layer + 1].Weights[nextLayerFilter, inputMap, filterX, filterY] * 
                        //    paddingValue);
                    }
                    else
                    {
                        convBackpropLayers[layer].Maps[inputMap].Errors[mapPointToReadX, mapPointToReadY] += 
                            (convLayers[layer + 1].Weights[nextLayerFilter, inputMap, filterX, filterY] *
                            convBackpropLayers[layer + 1].Deltas[nextLayerFilter]);
                    }
                }
            }

            // Apply bias weight for the filter.
            convBackpropLayers[layer].Maps[inputMap].Errors[mapPointX, mapPointY] +=
                (convLayers[layer + 1].BiasWeights[nextLayerFilter] *
                convBackpropLayers[layer + 1].Deltas[nextLayerFilter]);
        }

        // Update network weights with error
        public void UpdateWeightsBackprop(float learningRate)
        {
            // For reducing learning rate as gradients get larger to equalize learning rates between layers.
            float layerLearningRate = learningRate;
            //float reverseWeightedSum;

            for (int layer = 0; layer < NumFCLayers; layer++)
            {
                if (layer != 0)
                {
                    inputs = fcBackpropLayers[layer - 1].Outputs;
                }

                // Reduce learning rate for third layer or greater.
                //if (layer > 1)
                //{
                //    layerLearningRate = layerLearningRate / 2;
                //}

                // Reverse of: "weightedSum = (weightedSum / numInputs) * 80".
                //reverseWeightedSum = (1 / 80) * inputs.Length;

                for (int neuron = 0; neuron < fcBackpropLayers[layer].NumNeurons; neuron++)
                {
                    for (int input = 0; input < inputs.Length; input++)
                    {
                        //fcLayers[layer].Weights[neuron, input] += learningRate *
                        //    fcBackpropLayers[layer].Deltas[neuron] * inputs[input];
                        fcLayers[layer].Weights[neuron, input] += layerLearningRate *
                            fcBackpropLayers[layer].Deltas[neuron] * inputs[input];

                        // Use reverse weighted sum.
                        //fcLayers[layer].Weights[neuron, input] += learningRate *
                        //    (neuronLayers[layer].Deltas[neuron] * reverseWeightedSum) * inputs[input];
                    }

                    // Bias
                    //fcLayers[layer].Weights[neuron, inputs.Length] += learningRate * 
                    //    fcBackpropLayers[layer].Deltas[neuron];
                    fcLayers[layer].Weights[neuron, inputs.Length] += layerLearningRate *
                        fcBackpropLayers[layer].Deltas[neuron] * biasInput;

                    // Use reverse weighted sum.
                    //fcLayers[layer].Weights[neuron, inputs.Length] += learningRate *
                    //    (neuronLayers[layer].Deltas[neuron] * reverseWeightedSum) * biasInput;
                }
            }

            if (Type == NetType.Convolutional)
            {
                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    for (int neuron = 0; neuron < convBackpropLayers[layer].NumNeurons; neuron++)
                    {
                        for (int inputMap = 0; inputMap < convLayers[layer].NumInputMaps; inputMap++)
                        {
                            for (int filterX = 0; filterX < filterSize; filterX++)
                            {
                                for (int filterY = 0; filterY < filterSize; filterY++)
                                {
                                    // New weight = old weight + sum (map deltas)
                                    convLayers[layer].Weights[neuron, inputMap, filterX, filterY] +=
                                        layerLearningRate * convBackpropLayers[layer].Deltas[neuron] *
                                        convBackpropLayers[layer].FilterInputs[neuron, inputMap, filterX, filterY];
                                }
                            }
                        }
                        convLayers[layer].BiasWeights[neuron] += layerLearningRate * 
                            convBackpropLayers[layer].Deltas[neuron] * biasInput;
                    }
                }
            }
        }

        private void CreateFeatureMaps(int width = 28, int height = 28)
        {
            featureMapsLayers = new FeatureMapsLayer[NumConvLayers];
            for (int layer = 0; layer < NumConvLayers; layer++)
            {
                if (layer > 0)
                {
                    width = width / poolingStepSize;
                    height = height / poolingStepSize;
                }

                //featureMapsLayers[layerNum] = new FeatureMapsLayer(numFilters, width, height, poolingStepSize);
                featureMapsLayers[layer] = new FeatureMapsLayer(convLayers[layer].NumFilters, width, height, poolingStepSize);
            }
        }

        private void CreateBackpropLayers()
        {
            if (featureMapsLayers == null)
            {
                CreateFeatureMaps();
            }

            fcBackpropLayers = new FCBackpropLayer[NumFCLayers];

            for (int layer = 0; layer < NumFCLayers; layer++)
            {
                fcBackpropLayers[layer] = new FCBackpropLayer(fcLayers[layer].NumOutputs);
            }

            if (Type == NetType.Convolutional)
            {
                convBackpropLayers = new ConvBackpropLayer[NumConvLayers];

                for (int layer = 0; layer < NumConvLayers; layer++)
                {
                    convBackpropLayers[layer] = new ConvBackpropLayer(
                        convLayers[layer].NumFilters, convLayers[layer].NumInputMaps,
                        featureMapsLayers[layer].PooledMaps[0].Width, featureMapsLayers[layer].PooledMaps[0].Height,
                        filterSize);
                }
            }
        }

        // Applies the derivative of the activation function.
        private float ApplyActivationFunctionDerivatative(float value)
        {
            float derivativeValue;
            ActivationFunction activationFunction = ActivationFunction.tanh;

            if (activationFunction == ActivationFunction.tanh)
            {
                //derivedValue = Convert.ToSingle(1 - Math.Pow(value, 2));
                derivativeValue = 1 - (value * value);
            }
            else if (activationFunction == ActivationFunction.sigmoid)
            {
                derivativeValue = value * (1 - value);
            }
            else
            {
                derivativeValue = value;
            }

            return derivativeValue;
        }

        // Calculates the numbers of hidden units for fully-connected layers based on the numbers of inputs and outputs.
        //private int GetNumHidden(int numInputs, int numOutputs)
        void GetNumHidden(int numInputs, int numOutputs, int[] numLayerOutputs)
        {
            int numLayerInputs = 0;
            int numHiddenOutputs = 0;

            numLayerInputs = numInputs;  // First layer's number of inputs is the net's number of inputs.

            // Look up integer division to see exactly what these values are without explicit conversion.
            float numHiddenOutputsStepSizeFloat = (numInputs - numOutputs) / NumFCLayers;
            int numHiddenOutputsStepSize = Convert.ToInt32(Math.Round(numHiddenOutputsStepSizeFloat,
                MidpointRounding.AwayFromZero));

            for (int layer = 0; layer < NumFCLayers; layer++)
            {
                if (layer == (NumFCLayers - 1))
                {
                    // This is the output layer.
                    numLayerOutputs[layer] = numOutputs;
                }
                else
                {
                    // This is a hidden layer.
                    // Set the number of hidden-layer outputs as a proportion of the number of layers.
                    // For example, for a 3-layer net, layer 1 gets 2/3 of the net's number of inputs and layer 2 gets 1/3.
                    //numHiddenOutputs = numInputs - numHiddenOutputsStepSize * layer;

                    float numHiddenOutputsFloat = numInputs - (numHiddenOutputsStepSize * (layer + 1));
                    numHiddenOutputs = Convert.ToInt32(Math.Round(numHiddenOutputsFloat, MidpointRounding.AwayFromZero));

                    // Test different numbers of hidden neurons.
                    //numHiddenOutputs = 60;
                    //numHiddenOutputs = numHiddenOutputs / 2;
                    //numHiddenOutputs = numInputs + 10;

                    if (numHiddenOutputs < 2) numHiddenOutputs = 2;  // Number of hidden neurons can't be less than two.
                    numLayerOutputs[layer] = numHiddenOutputs;

                    File.AppendAllText(AppProperties.ServerLogPath, "numHiddenOutputs layer[" + layer + "] = " +
                        numHiddenOutputs + "\r\n");

                    // Next layer gets this layer's number of outputs as its number of inputs:
                    numLayerInputs = numHiddenOutputs;
                }
            }
        }

        public void WriteNetPropertiesToLog()
        {
            float weightValue;
            string netProperties;

            netProperties =
                "\r\nNet Properties: " +
                "\r\nName: " + Name +
                "\r\nType: " + Type +
                "\r\nActivationFunction: " + ActivationFunction +
                "\r\nIsGrayscale: " + Convert.ToString(IsGrayscale) +
                "\r\nNumInputs: " + Convert.ToString(NumInputs) +
                "\r\nNumOutputs: " + Convert.ToString(NumOutputs) +
                "\r\nNumConvLayers: " + Convert.ToString(NumConvLayers);

            if (Type == NetType.Convolutional)
            {
                for (int convLayer = 0; convLayer < NumConvLayers; convLayer++)
                {
                    netProperties = netProperties +
                        "\r\nNumFilters, layer-" + Convert.ToString(convLayer) + ": " + 
                            Convert.ToString(convLayers[convLayer].NumFilters);
                        //"\r\nNumFilters, layer-1: " + Convert.ToString(convLayers[1].NumFilters);
                }

                netProperties = netProperties +
                    "\r\nfilterSize: " + Convert.ToString(filterSize);
            }

            netProperties = netProperties +
                "\r\nNumFCLayers: " + Convert.ToString(NumFCLayers) +
                "\r\nnumFCInputs: " + Convert.ToString(numFCInputs) +
                "\r\n# First Layer Outputs: " + Convert.ToString(fcLayers[0].Weights.GetLength(0)) + "\r\n\r\n";

            File.AppendAllText(AppProperties.ServerLogPath, netProperties);

            for (int layer = 0; layer < NumFCLayers; layer++)
            {
                File.AppendAllText(AppProperties.ServerLogPath, "Layer[" + layer + "] num inputs = " +
                fcLayers[layer].Weights.GetLength(1) + ", num outputs = " +
                fcLayers[layer].Weights.GetLength(0) + "\r\n");
            }

            if (Knowledgebase.writeWeightsToLog)
            {
                File.AppendAllText(AppProperties.ServerLogPath, "Weight Values For " + Name + ":\r\n");

                if (Type == NetType.Convolutional)
                {
                    // Write convolutional layers.
                    //int numFilterInputs;
                    //int numFilters;

                    for (int layer = 0; layer < NumConvLayers; layer++)
                    {
                        //numFilters = convLayers[layer].NumFilters;
                        //numFilterInputs = convLayers[layer].Weights.GetLength(1);

                        for (int filter = 0; filter < convLayers[layer].NumFilters; filter++)
                        {
                            for (int inputMap = 0; inputMap < convLayers[layer].NumInputMaps; inputMap++)
                            {
                                for (int x = 0; x < filterSize; x++)
                                {
                                    for (int y = 0; y < filterSize; y++)
                                    {
                                        weightValue = convLayers[layer].Weights[filter, inputMap, x, y];
                                        File.AppendAllText(AppProperties.ServerLogPath, "conv layer[" +
                                        layer + "], filter[" + filter + "], input map[" + inputMap + "], x[" + x +
                                        "], y[" + y + "] = " + weightValue + "\r\n");
                                    }
                                }
                            }

                        }
                    }
                }

                // Write fully connected layers.
                int numLayerInputs;
                int numLayerOutputs;
                int maxOutputs;
                int maxInputs;

                //for (int layer = 0; layer < 1; layer++)  // Write just first layer.
                for (int layer = 0; layer < NumFCLayers; layer++)  // Write all layers.
                {
                    numLayerOutputs = fcLayers[layer].Weights.GetLength(0);
                    numLayerInputs = fcLayers[layer].Weights.GetLength(1);

                    if (numLayerOutputs < 10)
                    {
                        maxOutputs = numLayerOutputs;
                    }
                    else
                    {
                        maxOutputs = 10;
                    }

                    if (numLayerInputs < 10)
                    {
                        maxInputs = numLayerInputs;
                    }
                    else
                    {
                        maxInputs = 10;
                    }

                    for (int output = 0; output < maxOutputs; output++)  //iOutput < numLayerOutputs; iOutput++)
                    {
                        for (int input = 0; input < maxInputs; input++) //input < numLayerInputs; input++)
                        {
                            weightValue = fcLayers[layer].Weights[output, input];
                            File.AppendAllText(AppProperties.ServerLogPath, "FC layer[" + layer + "], output[" +
                                output + "], input[" + input + "] = " + weightValue + "\r\n");
                        }
                    }
                }
            }
            File.AppendAllText(AppProperties.ServerLogPath, "\r\n");
        }

        public void GetNetProperties(int netID)
        {
            try
            {
                SqlCommand sql = new SqlCommand(
                "SELECT Name, type, activation_function, num_layers, num_inputs, num_outputs " +
                "FROM net WHERE id = " + netID
                , KBConnection);

                using (SqlDataReader reader = sql.ExecuteReader())
                {
                    reader.Read();
                    Name = Convert.ToString(reader[0]);

                    if (Convert.ToString(reader[1]) == "Convolutional")
                    {
                        Type = NetType.Convolutional;
                    }
                    else
                    {
                        Type = NetType.FullyConnected;
                    }

                    Enum.TryParse(Convert.ToString(reader[2]), out ActivationFunction activationFunctionName);
                    ActivationFunction = activationFunctionName;
                    //ActivationFunction = Convert.ToString(reader[2]);
                    NumConvLayers = Convert.ToInt32(reader[3]);
                    NumInputs = Convert.ToInt32(reader[4]);
                    NumOutputs = Convert.ToInt32(reader[5]);
                }
            }
            catch (SqlException error)
            {
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Error, " + error.Message + "\r\n");
            }
        }

        [Serializable()]
        private class ConvLayer
        {
            public float[,,,] Weights;
            public float[] BiasWeights;
            public int NumInputMaps { get; }
            public int NumFilters { get; }
            //public int FilterSize { get; }  // Add FilterSize later, when ready to handle deserializing nets without it.

            public ConvLayer(int numFilters, int numInputMaps, int filterSize)  // Constructor.
            {
                NumInputMaps = numInputMaps;
                NumFilters = numFilters;
                Weights = new float[numFilters, numInputMaps, filterSize, filterSize];
                BiasWeights = new float[numFilters];
            }
        }

        private class FeatureMapsLayer
        {
            public NetMap[] FeatureMaps;
            public NetMap[] PooledMaps;

            public FeatureMapsLayer(int numFilters, int width, int height, int poolingStepSize)
            {
                FeatureMaps = new NetMap[numFilters];
                PooledMaps = new NetMap[numFilters];

                for (int iFilter = 0; iFilter < numFilters; iFilter++)
                {
                    FeatureMaps[iFilter] = new NetMap(width, height);
                    PooledMaps[iFilter] = new NetMap(width / poolingStepSize, height / poolingStepSize);
                }
            }
        }

        [Serializable()]
        private class FCLayer
        {
            public float[,] Weights;
            public int NumInputs { get; set; }
            public int NumOutputs { get; set; }

            public FCLayer(int numOutputs, int numInputs)
            {
                NumInputs = numInputs;
                NumOutputs = numOutputs;

                Weights = new float[numOutputs, numInputs + 1];
            }
        }

        private class ConvBackpropLayer
        {
            public float[] Errors;
            public float[] Deltas;
            public float[,,,] FilterInputs;
            public int NumNeurons { get; }
            public BackpropMap[] Maps;

            public ConvBackpropLayer(int numFilters, int numInputMaps, int width, int height, int filterSize)
            {
                NumNeurons = numFilters;
                Errors = new float[numFilters];
                Deltas = new float[numFilters];
                FilterInputs = new float[numFilters, numInputMaps, filterSize, filterSize];

                Maps = new BackpropMap[numFilters];
                for (int iMap = 0; iMap < numFilters; iMap++)
                {
                    Maps[iMap] = new BackpropMap(width, height);
                }
            }
        }

        public class BackpropMap
        {
            public int Width { get; }
            public int Height { get; }
            //public float[,] Outputs;
            public float[,] Errors;
            public float[,] Deltas;
            //public int[,] PooledX;
            //public int[,] PooledY;

            public BackpropMap(int width, int height)
            {
                Width = width;
                Height = height;
                //Outputs = new float[Width, Height];
                Errors = new float[Width, Height];
                Deltas = new float[Width, Height];
                //PooledX = new int[Width, Height];
                //PooledY = new int[Width, Height];
            }
        }

        private class FCBackpropLayer
        {
            public float[] Outputs;
            public float[] Errors;
            public float[] Deltas;
            public int NumNeurons { get; }

            public FCBackpropLayer(int numNeurons)
            {
                NumNeurons = numNeurons;
                Outputs = new float[numNeurons];
                Errors = new float[numNeurons];
                Deltas = new float[numNeurons];
            }
        }
    }

    public class NetMap
    {
        public float[,] Points;
        public int Width { get; }
        public int Height { get; }

        public NetMap(int width, int height)
        {
            Width = width;
            Height = height;
            Points = new float[Width, Height];
        }
    }

    public class RGBNetMapSet
    {
        public NetMap[] Maps = new NetMap[3];
        public int Group { get; set; }  // Optional property classifying the image into a group.

        public RGBNetMapSet(int width, int height, int group = -1)
        {
            Maps[0] = new NetMap(width, height);
            Maps[1] = new NetMap(width, height);
            Maps[2] = new NetMap(width, height);

            Group = group;
        }
    }

    enum ActivationFunction
    {
        tanh,
        ReLU,
        sigmoid
    };
}
