﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AIDevServer
{
    static class Knowledgebase
    {
        public static SqlConnection KBConnection = null;
        private static SqlCommand sql;

        private static PythonSession PythonSession;

        private static readonly List<Net> Nets = new List<Net>();
        private static int currentNet = -1;

        // Net training
        public static bool fastExecution = true;  // If true, log and console writing are minimized.
        private static float currentError;
        private static readonly float targetError = 0.1F;  // Training is complete when it reaches this error value.
        //private static bool useGrayScale = true;
        private static readonly bool useTemperature = false;  // If false, only changes that decrease error will be accepted.
        const float startingTemperature = 0.25F;
        private static float temperature = startingTemperature;  // Multiply by alpha after each numEpochsPerTempChange.
        const float alpha = 0.98F;   // "typical choices are between 0.8 and 0.99"
        private static readonly int numEpochsPerTempChange = 50;  // Usually 100-1,000 error compares per temp change.
        // Mu is the size of a weight change. (pronounced "myoo")
        const float startingMu = 0.1F;  // "Ranges from as low as 10^-6 to as high as 0.1." (maybe for 0.0-1.0 weight range)
        const float maxMu = 0.2F;
        private static float mu = startingMu;
        const float muVariance = 0.3F;  // mu varies randomly up or down by this factor changing a weight.
        const int noChangeEpochsBeforeMuChange = 300;
        private static int lastMuChange;
        const float bumpFactor = 1.5F;  // mu is multiplied by this in a bump.
        const int epochsBeforeBump = -1;  // Set to -1 for no automatic bump.
        const float percentFCWeightsToChange = 0.02F;
        const float percentConvWeightsToChange = 0.02F;
        private static readonly bool useSignificantOutputFactor = false;
        //const bool useSignificantOutputFactor = false;
        const float significantOutputFactor = 10;  // Extra weight given to significant output in error. 2x, 4x, etc.
        const int maxEpochs = 200000;
        private static int iterationSize = 10;  // 10 is default.
        public static bool writeWeightsToLog = false;
        private static readonly bool writeOutputsToLog = false;
        const int outputsPerLogWrite = 100;
        private static int outputWriteCounter = 1;
        private static TrainingDataset trainingDataset = TrainingDataset.dataset00_2Percent;
        private static int checkForCommandsIntervalSeconds = 4;

        // Backpropagation
        const float learningRate = 0.015F;

        //static string NewLine = Environment.NewLine;  // newline in current environment is "\r\n".

        // "Initial Catalogue" is an alternative to "AttachDbFilename":
        //    @"Initial Catalog = " + AppProperties.KBFolderPath + @"\Knowledgebase.mdf;" +

        // Location for knowledgebases.

        static readonly string sqlConnectionString = "Data Source = FUTURETEKDEVLAB\\SQLEXPRESS;Initial Catalog = " +
            "\"AI Dev Knowledgebase\"; Integrated Security = True; Connect Timeout = 30; Encrypt=False; " +
            "TrustServerCertificate=False; ApplicationIntent=ReadWrite; MultiSubnetFailover=False";

        //static readonly string sqlConnectionString =
        //@"Data Source = (LocalDB)\MSSQLLocalDB;" +
        //@"AttachDbFilename = " + AppProperties.KBFolderPath + @"\Knowledgebase.mdf;" +
        //"Integrated Security = True; Connect Timeout = 30";

        public static void Connect()
        {
            File.WriteAllText(AppProperties.ServerLogPath, "");  // Clear server log file.

            try
            {
                Console.WriteLine("Connecting to knowledgebase...");
                KBConnection = new SqlConnection(sqlConnectionString);
                KBConnection.Open();
                Console.WriteLine("Connected to knowledgebase.");
                File.AppendAllText(AppProperties.ServerLogPath, "Connected to knowledgebase." + Environment.NewLine);
            }
            catch (Exception error)
            {
                Console.WriteLine("Error: " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "Error: " + error.Message + Environment.NewLine + Environment.NewLine);
            }
        }
        public static void ProcessStatements(string statement)
        {
            string response;

            response = Language.Execute(statement);

            ServerTCPConnection.ReturnResponse(response);
        }

        public static string StartPython()
        {
            string response;

            if (PythonSession == null)
            {
                PythonSession = new PythonSession();
            }

            response = PythonSession.StartPythonAsync();

            return response;
        }

        public static string RunPython(string commands)
        {
            //commands = @"print('Hello world!')";
            string response;

            if (PythonSession == null)
            {
                PythonSession = new PythonSession();
            }

            response = PythonSession.RunPythonAsync(commands);
            
            return response;
        }

        public static string GetPythonOutput()
        {
            string response;

            if (PythonSession != null)
            {
                response = PythonSession.GetPythonOutput();
            }
            else
            {
                response = "[python closed]";
            }

            return response;
        }
        
        public static string EndPython()
        {
            if (PythonSession != null)
            {
                PythonSession.RunPythonAsync("exit()");
                PythonSession = null;
            }

            return "[python closed]";
        }
        public static void LoadLang()
        {
            Language.LoadLang();
        }

        // Retrieve data from nets table.
        public static string GetNetsData()
        {
            string netsData = "";

            try
            {
                //sql = new SqlCommand("SELECT * FROM net WHERE FirstColumn = @firstColumnValue", 
                //                 DataConnection);
                // Add the parameters.
                //sql.Parameters.Add(new SqlParameter("firstColumnValue", 1));

                sql = new SqlCommand(
                "SELECT id, name, type, activation_function, num_inputs, num_outputs, num_fc_layers, num_conv_layers " +
                "FROM net " +
                "ORDER BY name"
                , KBConnection);

                // Save, as good example for using subqueries/sub-select statements.
                // This statement gets numbers of inputs and outputs from layer table (used if columns are not in net
                // table):
                //"SELECT id, name, activation_function, num_layers, layer.num_outputs AS num_inputs, " +
                //"output_layer.num_outputs " +
                //"FROM net, layer, " +
                //"	(SELECT layer.net_id, num_outputs " +
                //"	FROM layer,  " +
                //"	    (SELECT net_id, MAX(layer_num) AS max_layer_num FROM layer " +
                //"	    GROUP BY net_id) AS max_layer " +
                //"	WHERE layer.net_id = max_layer.net_id AND layer.layer_num = max_layer.max_layer_num) "
                //"AS output_layer " +
                //"WHERE net.id = layer.net_id AND layer.layer_num = 0 AND net.id = output_layer.net_id " +
                //"ORDER BY id"

                // Create new SqlDataReader object and read data from the command.
                using SqlDataReader reader = sql.ExecuteReader();
                // Add line to output string while there is another record present.
                while (reader.Read())
                {
                    // column delimiter: "|", net delimiter: "$"
                    netsData += string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                    reader[0], reader[1], reader[2], reader[3], reader[4], reader[5], reader[6], reader[7] + "$");
                }
                //File.AppendAllText(AppProperties.ServerLogPath,
                //    "Nets data string formatted for client:\r\n" + netsData + "\r\n");
            }
            catch (SqlException error)
            {
                Console.WriteLine("There was an error reported by SQL Server, " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Error, " + error.Message + "\r\n");
                netsData = "commanderror: There was an error reported by SQL Server, " +
                    error.Message + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(netsData)) netsData = "nodata";

            return netsData;
        }

        public static string CreateNet()
        {

            // Give the new net default values.
            string name = GetNewNetName();

            File.AppendAllText(AppProperties.ServerLogPath, "Generated new net name: " +
                                    name + Environment.NewLine);

            // Default properties.
            //NetType type = NetType.Convolutional;
            NetType type = NetType.FullyConnected;
            string activationFunction = "tanh";

            // Temporary default properties.
            int numInputs = 784;
            int numOutputs = 10;
            int numFCLayers = 2;
            int numConvLayers = 0;
            bool isGrayscale = true;

            // Add net to net table.
            try
            {
                sql = new SqlCommand(
                "INSERT INTO net (name, type, activation_function, " +
                "num_inputs, num_outputs, num_fc_layers, num_conv_layers) " +
                "VALUES('" + name + "', '" + type + "', '" + activationFunction + "', " +
                numInputs + ", " + numOutputs + ", " + numFCLayers + ", " + numConvLayers + ")"
                , KBConnection);

                sql.ExecuteNonQuery();

                int netID = GetNetID(name);

                File.AppendAllText(AppProperties.ServerLogPath, "Retrieved net ID: " +
                                    netID + Environment.NewLine);
                if (netID != -1)
                {
                    // Create and initialize new net.
                    Net net = new Net();
                    net.Create(netID, name, type, activationFunction, numInputs, numOutputs,
                        numFCLayers, numConvLayers, isGrayscale);
                    Nets.Add(net);
                    currentNet = Nets.Count - 1;
                }
            }
            catch (SqlException error)
            {
                //Console.WriteLine("There was an error reported by SQL Server, " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Error, " + error.Message + "\r\n");
            }

            string response;
            if (string.IsNullOrEmpty(name))
            {
                response = "commanderror";
            }
            else
            {
                response = name;
            }

            if (response != "commanderror")
            {
                try
                {
                    //Stream stream = File.Open(AppProperties.AIDevNetsFolderPath + @"\" + Nets[currentNet].Name +
                    //".bin", FileMode.Create);
                    //BinaryFormatter formatter = new BinaryFormatter();
                    //formatter.Serialize(stream, Nets[currentNet]);
                    //stream.Close();

                    SerializeNet(Nets[currentNet]);
                    Nets[currentNet].WriteNetPropertiesToLog();
                }
                catch { }
            }

            return response;
        }

        public static string ReinitializeNet()
        {
            Nets[currentNet].InitializeWeights();
            SerializeNet(Nets[currentNet]);

            string response = "Reinitialized weights.";
            return response;
        }

        // Saves the current properties and weights of a net to a file.
        public static string SaveNet(string args)
        {
            int i = 0;
            args = args.Trim();
            string oldName = ReadNextArg(args, ref i);
            string newName = ReadNextArg(args, ref i);
            string type = ReadNextArg(args, ref i);
            string activationFunction = ReadNextArg(args, ref i);
            string isGrayscale = ReadNextArg(args, ref i);
            string response = "Arg string:" + args + "\r\n" +
"Parsed args: [" + oldName + "] [" + newName + "] [" + activationFunction + "] [" + isGrayscale + "]";

            try
            {
                sql = new SqlCommand(
                "UPDATE net " +
                "SET name = '" + newName + "', activation_function = '" + activationFunction + "' " +
                "WHERE name = '" + oldName + "'"
                , KBConnection);

                sql.ExecuteNonQuery();
            }
            catch (SqlException error)
            {
                //Console.WriteLine("There was an error reported by SQL Server, " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Error, " + error.Message + "\r\n");
                response = "commanderror";
            }

            if (response != "commanderror")
            {
                Nets[currentNet].Name = newName;

                if (type == "Convolutional")
                {
                    Nets[currentNet].Type = NetType.Convolutional;
                }
                else
                {
                    Nets[currentNet].Type = NetType.FullyConnected;
                }

                Enum.TryParse(activationFunction, out ActivationFunction activationFunctionName);
                Nets[currentNet].ActivationFunction = activationFunctionName;
                Nets[currentNet].IsGrayscale = Convert.ToBoolean(isGrayscale);

                // If net name has been changed, delete the old net file.
                if (newName != oldName)
                {
                    try
                    {
                        File.Delete(AppProperties.AIDevNetsFolderPath + @"\" + oldName + ".bin");
                        //// Rename the file.
                        //File.Move(AppProperties.AIDevNetsFolderPath + @"\" + oldName + ".bin",
                        //    AppProperties.AIDevNetsFolderPath + @"\" + newName + ".bin");
                    }
                    catch { }
                }

                SerializeNet(Nets[currentNet]);

                //Nets[currentNet].WriteDataToLog();
                response = "success - Net saved";
            }

            return response;
        }

        // Modifies the structure of a net and saves it to a file.
        public static string ModifyAndSaveNet(string args)
        {
            int i = 0;
            args = args.Trim();
            string oldName = ReadNextArg(args, ref i);
            string newName = ReadNextArg(args, ref i);
            string type = ReadNextArg(args, ref i);
            string activationFunction = ReadNextArg(args, ref i);
            string isGrayscale = ReadNextArg(args, ref i);
            string numInputs = ReadNextArg(args, ref i);
            string numOutputs = ReadNextArg(args, ref i);
            string numFCLayers = ReadNextArg(args, ref i);
            string numConvLayers = ReadNextArg(args, ref i);

            if (type == "Convolutional")
            {
                // Net must have at least one convolutional layer.
                if (Convert.ToInt32(numConvLayers) < 1) numConvLayers = "1";
            }
            else
            {
                // Net must have at least one fully connected layer.
                if (Convert.ToInt32(numFCLayers) < 1) numFCLayers = "1";
            }

            string response;
            try
            {
                sql = new SqlCommand(
                "UPDATE net " +
                "SET name = '" + newName + "', type = '" + type + "', activation_function = '" + activationFunction +
                "', num_inputs = " + numInputs + ", num_outputs = " + numOutputs +
                ", num_fc_layers = " + numFCLayers + ", num_conv_layers = " + numConvLayers + " " +
                "WHERE name = '" + oldName + "'"
                , KBConnection);

                sql.ExecuteNonQuery();

                response = "Arg string:" + args + "\r\n" +
                "Parsed args: [" + oldName + "] [" + newName + "] [" + activationFunction + "] [" +
                 numInputs + "] [" + numOutputs + "] [" + numFCLayers + "] [" + numConvLayers + "]";
            }
            catch (SqlException error)
            {
                //Console.WriteLine("SQL Server Error, " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Server Error, " + error.Message + "\r\n");
                response = "commanderror";
            }

            if (response != "commanderror")
            {
                NetType netType;

                if (type == "Convolutional")
                {
                    netType = NetType.Convolutional;
                }
                else
                {
                    netType = NetType.FullyConnected;
                }

                Nets[currentNet].Create(GetNetID(newName), newName, netType, activationFunction,
                    Convert.ToInt32(numInputs), Convert.ToInt32(numOutputs),
                    Convert.ToInt32(numFCLayers), Convert.ToInt32(numConvLayers), Convert.ToBoolean(isGrayscale));

                try
                {
                    //Stream stream = File.Open(AppProperties.AIDevNetsFolderPath + @"\" + Nets[currentNet].Name +
                    //    ".bin", FileMode.Create);
                    //BinaryFormatter formatter = new BinaryFormatter();
                    //formatter.Serialize(stream, Nets[currentNet]);
                    //stream.Close();

                    // If net name has been changed, delete the old net file.
                    if (newName != oldName)
                    {
                        File.Delete(AppProperties.AIDevNetsFolderPath + @"\" + oldName + ".bin");
                    }
                }
                catch (Exception error)
                {
                    File.AppendAllText(AppProperties.ServerLogPath, "Error, " + error.Message + "\r\n");
                    response = "commanderror";
                }

                if (response != "commanderror")
                {
                    SerializeNet(Nets[currentNet]);
                    response = "success - Net modified and saved";
                }
            }

            return response;
        }

        private static void SerializeNet(Net net)
        {
            try
            {
                Stream stream = File.Open(AppProperties.AIDevNetsFolderPath + @"\" + net.Name +
                    ".bin", FileMode.Create);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, net);
                stream.Close();
            }
            catch
            {
            }
        }

        public static string RunNet(string args)
        {
            int i = 0;
            NetMap[] inputMaps;
            float error;
            float[] netTargetOutputs = new float[Nets[currentNet].NumOutputs];
            float[] netOutputs;

            args = args.Trim();
            string imageFilePath = ReadNextArg(args, ref i);
            Bitmap inputImage = new Bitmap(imageFilePath);
            //Bitmap inputImage = new Bitmap(@"C:\Users\User2\Dev\AI Dev Visual Inputs\Datasets\EMNIST\" +
            //    @"mnist_png\testing\0\3.png");

            if (Nets[currentNet].IsGrayscale)
            {
                inputMaps = new NetMap[1];
                inputMaps[0] = GetNetImageMapFromBitmap(inputImage, RGB.Red);
            }
            else
            {
                inputMaps = new NetMap[3];
                inputMaps[0] = GetNetImageMapFromBitmap(inputImage, RGB.Red);
                inputMaps[1] = GetNetImageMapFromBitmap(inputImage, RGB.Green);
                inputMaps[2] = GetNetImageMapFromBitmap(inputImage, RGB.Blue);
            }

            netOutputs = Nets[currentNet].Run(inputMaps, false, Nets[currentNet].IsGrayscale);

            //int targetGroup = 8;
            int targetGroup = GetTargetFromFilePath(imageFilePath);
            GetTargetOutputs(netTargetOutputs, targetGroup);

            if (useSignificantOutputFactor)
            {
                error = GetError(netTargetOutputs, netOutputs, targetGroup);
            }
            else
            {
                error = GetError(netTargetOutputs, netOutputs);
            }

            string outputText = "Net outputs:\r\n";
            for (int output = 0; output < netOutputs.Length; output++)
            {
                outputText = outputText +
                    "Target[" + output + "] = " + netTargetOutputs[output].ToString("+0.0000000;-0.0000000") +
                    ", Output[" + output + "] = " + netOutputs[output].ToString("+0.0000000;-0.0000000") +
                    "\r\n";
            }

            outputText = outputText + "Error = " + error.ToString("+0.0000000;-0.0000000") + "\r\n\r\n";

            File.AppendAllText(AppProperties.ServerLogPath, outputText);

            //response = "success - Net was run successfully.";
            string response = outputText;

            return response;
        }

        //private static float[] RunOnnxMinst()
        //{
        //    float[] results = new float[100];

        //    // To use the automatic generated wrapper classes, you simply need the following three lines of code:
        //    // Create the model – This will create the model with the ONNX model file.
        //    // Initialize the input – Initialize the input object with application data to be bound to the model for evaluation.
        //    // Evaluate the model – Evaluate the model with the input data to obtain the resulting output data.

        //    // Create the model
        //    PcbModel model = await PcbModel.CreatePcbModel("mninst.onnx");

        //    // Create the input
        //    PcbModelInput inputs = new PcbModelInput() { data = videoFrame };

        //    // Evaluate the model
        //    PcbModelOutput output = await model.EvaluateAsunc(inputs);

        //    return results;
        //}

        private static int GetTargetFromFilePath(string filePath)
        {
            int target = -1;

            if (filePath.Contains(@"\0\")) { target = 0; }
            else if (filePath.Contains(@"\1\")) { target = 1; }
            else if (filePath.Contains(@"\2\")) { target = 2; }
            else if (filePath.Contains(@"\3\")) { target = 3; }
            else if (filePath.Contains(@"\4\")) { target = 4; }
            else if (filePath.Contains(@"\5\")) { target = 5; }
            else if (filePath.Contains(@"\6\")) { target = 6; }
            else if (filePath.Contains(@"\7\")) { target = 7; }
            else if (filePath.Contains(@"\8\")) { target = 8; }
            else if (filePath.Contains(@"\9\")) { target = 9; }

            //File.AppendAllText(AppProperties.ServerLogPath, "Target from path = " + target + "\r\n");

            return target;
        }

        // Loads all training images from files into memory.
        private static void LoadTrainingImages(List<RGBNetMapSet> trainingImages)
        {
            string trainingDataPath = @"C:\Users\User2\Dev\AI Dev Visual Inputs\Datasets\EMNIST - Reduced Sets";

            switch (trainingDataset)
            {
                case TrainingDataset.dataset1Item:
                    trainingDataPath += @"\mnist_png - (1 & 1)\training";
                    //iterationSize = 10;
                    break;
                case TrainingDataset.dataset2Item:
                    trainingDataPath += @"\mnist_png - (1 & 2)\training";
                    //iterationSize = 20;
                    break;
                case TrainingDataset.dataset00_2Percent:
                    trainingDataPath += @"\mnist_png - 0.1 & 0.2 Percent\training";
                    //iterationSize = 60;
                    break;
                case TrainingDataset.dataset02_0Percent:
                    trainingDataPath += @"\mnist_png - 1 & 2 Percent\training";
                    //iterationSize = 600;
                    //iterationSize = 100;
                    break;
                case TrainingDataset.dataset10_0Percent:
                    trainingDataPath += @"\mnist_png - 10 Percent\training";
                    //iterationSize = 600;
                    break;
            }

            string inputImageFilePath;
            RGBNetMapSet rgbNetMapSet;

            // Read all training image files into memory.
            for (int folderNameNum = 0; folderNameNum <= 9; folderNameNum++)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(trainingDataPath + "\\" +
                    Convert.ToString(folderNameNum));
                foreach (var file in directoryInfo.GetFiles("*.png"))
                {
                    // Load image file into list.
                    inputImageFilePath = trainingDataPath + "\\" + Convert.ToString(folderNameNum) + "\\" + file.Name;
                    Bitmap inputImage = new Bitmap(inputImageFilePath);

                    rgbNetMapSet = new RGBNetMapSet(28, 28);
                    rgbNetMapSet.Maps[0] = GetNetImageMapFromBitmap(inputImage, RGB.Red);
                    rgbNetMapSet.Maps[1] = GetNetImageMapFromBitmap(inputImage, RGB.Green);
                    rgbNetMapSet.Maps[2] = GetNetImageMapFromBitmap(inputImage, RGB.Blue);
                    rgbNetMapSet.Group = folderNameNum;

                    trainingImages.Add(rgbNetMapSet);
                }
            }
        }

        private static void SetTrainingParameters(string args)
        {
            int i = 0;
            args = args.Trim();
            string dataset = ReadNextArg(args, ref i);
            string iterationSize = ReadNextArg(args, ref i);
            switch (dataset)
            {
                case "1 Item":
                    trainingDataset = TrainingDataset.dataset1Item;
                    checkForCommandsIntervalSeconds = 3;
                    break;
                case "2 Items":
                    trainingDataset = TrainingDataset.dataset2Item;
                    checkForCommandsIntervalSeconds = 3;
                    break;
                case "0.2 Percent":
                    trainingDataset = TrainingDataset.dataset00_2Percent;
                    checkForCommandsIntervalSeconds = 3;
                    break;
                case "2.0 Percent":
                    trainingDataset = TrainingDataset.dataset02_0Percent;
                    checkForCommandsIntervalSeconds = 5;
                    break;
                case "10.0 Percent":
                    trainingDataset = TrainingDataset.dataset10_0Percent;
                    checkForCommandsIntervalSeconds = 8;
                    break;
            }

            Knowledgebase.iterationSize = Convert.ToInt32(iterationSize);
        }

        public static string TrainNet(string args)
        {
            float newIterationError;
            //float iterationError;
            List<float> trainingErrorHistory = new List<float>();
            Random random = new Random();
            //Random random = new Random(1);  // Use seed for testing identical training.
            double acceptanceProbability = 0;
            bool changesKept;
            string logOutput;
            int writeEpochCounter = 0;

            mu = startingMu;
            lastMuChange = 0;

            SetTrainingParameters(args);

            List<RGBNetMapSet> trainingImages = new List<RGBNetMapSet>();
            LoadTrainingImages(trainingImages);
            Common.ShuffleList(trainingImages, random);

            DateTime lastCommandCheck = DateTime.Now;
            TimeSpan checkForCommandsInterval = new TimeSpan(0, 0, checkForCommandsIntervalSeconds);

            // Get the initial current error.
            currentError = RunIteration(trainingImages, 1);

            Nets[currentNet].SaveWeights();
            if (writeWeightsToLog && !fastExecution) Nets[currentNet].OutputWeights();

            int epoch = 1;
            int epochsSinceLastTempChange = 0;
            int epochsSinceWeightChange = 0;
            bool stopTraining = false;
            while ((epoch <= maxEpochs) && (currentError > targetError) && (stopTraining == false))
            {
                writeEpochCounter++;
                if (!fastExecution || writeEpochCounter == 10)
                {
                    writeEpochCounter = 0;
                    logOutput = DateTime.Now.ToLongTimeString() + ": Epoch[" + epoch + "] " + "current error = " +
                        currentError.ToString("+0.0000000;-0.0000000");
                    if (useTemperature)
                    {
                        logOutput = logOutput + ", temperature = " + temperature;
                    }
                    logOutput += "\r\n";
                    Console.Write(logOutput);
                    File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                }

                // No need to turn shuffle off when 1 iteration = 1 epoch for slightly faster execution.
                // Execution speed is only reduced by %0.39.
                Common.ShuffleList(trainingImages, random);  // Shuffle training instances before each epoch.

                bool changeMadeDuringEpoch = false;

                for (int iteration = 1; ((iteration - 1) * iterationSize) < trainingImages.Count && 
                    stopTraining == false; iteration++)
                {
                    //iterationError = RunIteration(trainingImages, iteration);

                    Nets[currentNet].ModifyWeights(random, mu, muVariance, 
                        percentFCWeightsToChange, percentConvWeightsToChange);
                    //Nets[currentNet].TestModifyWeights(mu);
                    //if (writeWeightsToLog) Nets[currentNet].OutputWeights();

                    newIterationError = RunIteration(trainingImages, iteration);

                    // Decide whether to accept weight changes.
                    //changesKept = Accept(random, currentError, iterationError, newIterationError, ref acceptanceProbability);
                    changesKept = AcceptChange(random, currentError, 0.0F, newIterationError, ref acceptanceProbability);

                    if (changesKept)
                    {
                        currentError = newIterationError;
                        changeMadeDuringEpoch = true;

                        //File.AppendAllText(AppProperties.ServerLogPath, "*** Changes kept = true ***\r\n");
                    }
                    //else
                    //{
                    //    currentError = iterationError;
                    //}

                    if (!fastExecution)
                    {
                        if (writeWeightsToLog) Nets[currentNet].OutputWeights();

                        logOutput = "Iteration[" + iteration.ToString("D2") + "] " +
                            //"iterationError = " + iterationError.ToString("+0.0000000;-0.0000000") +
                            "currentError = " + currentError.ToString("+0.0000000;-0.0000000") +
                            ", newIterationError = " + newIterationError.ToString("+0.0000000;-0.0000000") +
                            ", AP = " + acceptanceProbability.ToString("+0.0000000;-0.0000000") + ", changes kept = " +
                            changesKept + "\r\n";
                        Console.Write(logOutput);
                        File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                    }

                    if (DateTime.Now >= (lastCommandCheck + checkForCommandsInterval))
                    {
                        string clientCommand;
                        clientCommand = ServerTCPConnection.HandleConnection();
                        if (clientCommand == "stoptraining")
                        {
                            stopTraining = true;
                            File.AppendAllText(AppProperties.ServerLogPath, "Training stopped.\r\n");
                            ServerTCPConnection.ReturnResponse("training stopped");
                        }
                        else if (clientCommand == "geterrorhistory")
                        {
                            ReturnErrorHistory(trainingErrorHistory);
                        }
                        else if (clientCommand == "bump")
                        {
                            ServerTCPConnection.ReturnResponse("bumped");
                            Nets[currentNet].ModifyWeights(random, mu * bumpFactor, muVariance,
                                percentFCWeightsToChange, percentConvWeightsToChange);
                            currentError = RunIteration(trainingImages, iteration);
                            changeMadeDuringEpoch = true;
                        }
                        lastCommandCheck = DateTime.Now;
                    }
                }

                trainingErrorHistory.Add(currentError);

                if (useTemperature)
                {
                    epochsSinceLastTempChange++;
                    if (epochsSinceLastTempChange == numEpochsPerTempChange)
                    {
                        temperature *= alpha;  // Decrease the temperature.
                        epochsSinceLastTempChange = 0;
                    }
                }

                if (changeMadeDuringEpoch)
                {
                    epochsSinceWeightChange = 0;
                    mu = startingMu;
                }
                else
                {
                    epochsSinceWeightChange++;
                }

                if (epochsSinceWeightChange == epochsBeforeBump)
                {
                    Nets[currentNet].ModifyWeights(random, mu * bumpFactor, muVariance, 
                        percentFCWeightsToChange, percentConvWeightsToChange);
                    currentError = RunIteration(trainingImages, 1);
                    epochsSinceWeightChange = 0;
                }

                if (epochsSinceWeightChange >= noChangeEpochsBeforeMuChange)
                {
                    if ((epoch - lastMuChange) >= noChangeEpochsBeforeMuChange)
                    {
                        if (mu < maxMu)
                        {
                            mu += 0.025F;
                            lastMuChange = epoch;

                            logOutput = "Mu changed to mu = " + mu + "\r\n";
                            Console.Write(logOutput);
                            File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                        }
                    }
                }

                //logOutput = "epochsSinceWeightChange = " + epochsSinceWeightChange +
                //    ", (epochs - lastMuChange) = " + (epoch - lastMuChange) + "\r\n";
                //Console.Write(logOutput);
                //File.AppendAllText(AppProperties.ServerLogPath, logOutput);

                //mu = GetNewMu(mu, epoch);
                epoch++;
            }

            temperature = startingTemperature;

            logOutput = DateTime.Now.ToLongTimeString() + ": Epoch[" + epoch + "] " + "current error = " +
                currentError.ToString("+0.0000000;-0.0000000");
            logOutput += "\r\n";
            Console.Write(logOutput);
            File.AppendAllText(AppProperties.ServerLogPath, logOutput + "\r\n");

            string response;
            if (stopTraining)
            {
                response = "training stopped";
            }
            else
            {
                File.AppendAllText(AppProperties.ServerLogPath, "Training finished.\r\n\r\n");
                response = "training finished";
            }

            return response;
        }

        public static string TrainNetBackprop(string args)
        {
            float newIterationError;
            //float iterationError;
            List<float> trainingErrorHistory = new List<float>();
            Random random = new Random();
            //Random random = new Random(2);  // Use seed for testing identical training.
            string logOutput;
            int writeEpochCounter = 0;

            SetTrainingParameters(args);
            iterationSize = 1;

            float[] netTargetOutputs = new float[Nets[currentNet].NumOutputs];
            List<RGBNetMapSet> trainingImages = new List<RGBNetMapSet>();
            LoadTrainingImages(trainingImages);
            Common.ShuffleList(trainingImages, random);

            DateTime lastCommandCheck = DateTime.Now;
            TimeSpan checkForCommandsInterval = new TimeSpan(0, 0, checkForCommandsIntervalSeconds);

            // Get the initial current error.
            //currentError = RunIteration(trainingImages, 1);
            currentError = 2;

            if (writeWeightsToLog && !fastExecution) Nets[currentNet].OutputWeights();

            float errorSum;
            int iterations;
            int epoch = 1;
            bool stopTraining = false;
            while ((epoch <= maxEpochs) && (currentError > targetError) && (stopTraining == false))
            {
                writeEpochCounter++;
                //if (!fastExecution || writeEpochCounter == 10)
                if (!fastExecution || writeEpochCounter == 1)
                {
                    writeEpochCounter = 0;
                    logOutput = DateTime.Now.ToLongTimeString() + ": Epoch[" + epoch + "] " + "current error = " +
                        currentError.ToString("+0.0000000;-0.0000000");
                    logOutput += "\r\n";
                    Console.Write(logOutput);
                    File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                }
                
                // No need to turn shuffle off when 1 iteration = 1 epoch for slightly faster execution.
                // Execution speed is only reduced by %0.39.
                Common.ShuffleList(trainingImages, random);  // Shuffle training instances before each epoch.

                errorSum = 0;
                iterations = 0;
                for (int iteration = 1; ((iteration - 1) * iterationSize) < trainingImages.Count &&
                    stopTraining == false; iteration++)
                {
                    newIterationError = RunIteration(trainingImages, iteration);
                    errorSum += newIterationError;
                    iterations++;
                    //currentError = newIterationError;
                    currentError = errorSum / iterations;

                    GetTargetOutputs(netTargetOutputs, trainingImages[iteration - 1].Group);
                    Nets[currentNet].BackpropagateError(netTargetOutputs);
                    Nets[currentNet].UpdateWeightsBackprop(learningRate);

                    if (!fastExecution)
                    {
                        if (writeWeightsToLog) Nets[currentNet].OutputWeights();

                        logOutput = "Iteration[" + iteration.ToString("D2") + "] " +
                            //"iterationError = " + iterationError.ToString("+0.0000000;-0.0000000") +
                            "currentError = " + currentError.ToString("+0.0000000;-0.0000000") +
                            ", newIterationError = " + newIterationError.ToString("+0.0000000;-0.0000000") +
                            "\r\n";
                        Console.Write(logOutput);
                        File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                    }

                    if (DateTime.Now >= (lastCommandCheck + checkForCommandsInterval))
                    {
                        string clientCommand;
                        clientCommand = ServerTCPConnection.HandleConnection();
                        if (clientCommand == "stoptraining")
                        {
                            stopTraining = true;
                            File.AppendAllText(AppProperties.ServerLogPath, "Training stopped.\r\n");
                            ServerTCPConnection.ReturnResponse("training stopped");
                        }
                        else if (clientCommand == "geterrorhistory")
                        {
                            ReturnErrorHistory(trainingErrorHistory);
                        }
                        else if (clientCommand == "bump")
                        {
                            ServerTCPConnection.ReturnResponse("bumped");
                            Nets[currentNet].ModifyWeights(random, mu * bumpFactor, muVariance,
                                percentFCWeightsToChange, percentConvWeightsToChange);
                            //currentError = RunIteration(trainingImages, iteration);
                            errorSum += RunIteration(trainingImages, iteration);
                            iterations++;
                            currentError = errorSum / iterations;
                        }
                        lastCommandCheck = DateTime.Now;
                    }
                }

                trainingErrorHistory.Add(currentError);

                epoch++;
            }

            logOutput = DateTime.Now.ToLongTimeString() + ": Epoch[" + epoch + "] " + "current error = " +
                currentError.ToString("+0.0000000;-0.0000000");
            logOutput += "\r\n";
            Console.Write(logOutput);
            File.AppendAllText(AppProperties.ServerLogPath, logOutput + "\r\n");

            string response;
            if (stopTraining)
            {
                response = "training stopped";
            }
            else
            {
                File.AppendAllText(AppProperties.ServerLogPath, "Training finished.\r\n\r\n");
                response = "training finished";
            }

            return response;
        }

        // Makes mu progressively smaller through training.
        private static float GetNewMu(float currentMu, int epochs)
        {
            int muChangeIntervals = epochs / 500;

            // Lower mu at these points in training.
            if (muChangeIntervals == 1 || muChangeIntervals == 2 || muChangeIntervals == 3)
            {
                currentMu -= 0.5F;
            }

            return currentMu;
        }

        private static bool AcceptChange(Random random, float currentError, float iterationError, float newIterationError, 
            ref double acceptanceProbability)
        {
            if (newIterationError < currentError)
            //if (newIterationError < iterationError)
            //if (newIterationError < currentError && newIterationError < iterationError)
            {
                acceptanceProbability = 1.0;
            }
            else
            {
                if (useTemperature)
                {
                    //acceptanceProbability = Math.Exp(-((newIterationError - iterationError) / temperature));
                    acceptanceProbability = Math.Exp(-((newIterationError - currentError) / temperature));
                }
                else
                {
                    // Only accept changes that decrease the error.
                    acceptanceProbability = 0.0;
                }
            }

            bool accepted;
            if (random.NextDouble() < acceptanceProbability)
            {
                Nets[currentNet].SaveWeights();
                if (writeWeightsToLog && !fastExecution) Nets[currentNet].OutputWeights();

                accepted = true;  // Keep changes.
            }
            else
            {
                Nets[currentNet].RevertWeights();  // Discard changes.
                accepted = false;
            }

            return accepted;
        }

        //Training Error Data Points
        // String memory size = 17 + (2 * length)
        // Length = # epochs * 10
        // Size of errorHistory string = 17 + (# epochs * 20)
        //17 bytes +
        //100 Epochs - 2000
        //800 Epochs - 16,000
        //6,400 Epochs - 128,000
        //12,800 Epochs - 256,000
        private static void ReturnErrorHistory(List<float> errorHistory)
        {
            string errorHistoryMessage = "empty";

            if (errorHistory.Count < 5000)
            {
                for (int epoch = 1; epoch <= errorHistory.Count; epoch++)
                {
                    if (epoch == 1)
                    {
                        errorHistoryMessage = Convert.ToString(errorHistory[epoch - 1]);
                    }
                    else
                    {
                        errorHistoryMessage = errorHistoryMessage + "|" + Convert.ToString(errorHistory[epoch - 1]);
                    }
                }
                errorHistoryMessage = "01|" + errorHistoryMessage;
            }
            else
            {
                // Return error history as every 10th epoch.
                for (int epoch = 1; epoch <= errorHistory.Count; epoch += 10)
                //for (int epoch = 1; epoch <= errorHistory.Count; epoch = epoch + 1)
                {
                    if (epoch == 1)
                    {
                        errorHistoryMessage = Convert.ToString(errorHistory[epoch - 1]);
                    }
                    else
                    {
                        errorHistoryMessage = errorHistoryMessage + "|" + Convert.ToString(errorHistory[epoch - 1]);
                    }
                }
                errorHistoryMessage = "10|" + errorHistoryMessage;
            }

            ServerTCPConnection.ReturnResponse(errorHistoryMessage);
        }

        // Run net through 1 iteration, a subset of the epoch.
        private static float RunIteration(List<RGBNetMapSet> trainingImages, int iteration)
        {
            float[] netTargetOutputs = new float[Nets[currentNet].NumOutputs];
            _ = new float[Nets[currentNet].NumOutputs];
            NetMap[] inputMaps = new NetMap[1];
            float error;
            float sumOfIterationErrors = 0;
            float meanIterationError = 0;
            int firstIterationImage;
            int imagesTested = 0;
            //string logOutput = "";

            firstIterationImage = (iteration - 1) * iterationSize;

            for (int image = firstIterationImage; image < (firstIterationImage + iterationSize) &&
                image < trainingImages.Count; image++)
            {
                //logOutput = "iImage[" + iImage + "]\r\n";
                //Console.Write(logOutput);
                //File.AppendAllText(AppProperties.ServerLogPath, logOutput);

                // Run net with current image as input.
                GetTargetOutputs(netTargetOutputs, trainingImages[image].Group);
                float[] netOutputs;
                if (Nets[currentNet].IsGrayscale)
                {
                    inputMaps[0] = trainingImages[image].Maps[0];
                    netOutputs = Nets[currentNet].Run(inputMaps, true, true);
                }
                else
                {
                    netOutputs = Nets[currentNet].Run(trainingImages[image].Maps, true, false);
                }

                if (useSignificantOutputFactor)
                {
                    error = GetError(netTargetOutputs, netOutputs, trainingImages[image].Group);
                }
                else
                {
                    error = GetError(netTargetOutputs, netOutputs);
                }

                // Add the error for this output to the total for the iteration.
                sumOfIterationErrors += error;
                imagesTested++;

                //File.AppendAllText(AppProperties.ServerLogPath, "iImage[" + iImage + "] " +
                //"Error = " + error + ", Last error = " + lastError + "\r\n" + 
                //"current error = " + currentError + 
                //", sumOfIterationErrors = " + sumOfIterationErrors + \r\n");
            }

            if (imagesTested > 0)
            {
                meanIterationError = sumOfIterationErrors / (imagesTested);
            }

            return meanIterationError;
        }

        private static float GetError(float[] netTargetOutputs, float[] netOutputs, int significantOutput = -1)
        {
            double error = 0;
            string logOutput;

            // For debugging:
            if (writeOutputsToLog && !fastExecution)
            {
                if (outputWriteCounter == outputsPerLogWrite)
                {
                    logOutput = "Target outputs: ";

                    for (int output = 0; output < netTargetOutputs.Length; output++)
                    {
                        logOutput = logOutput + "[" + netTargetOutputs[output].ToString("+0.0000000;-0.0000000") + "]";
                    }

                    logOutput += "\r\nOutputs:        ";
                    Console.Write(logOutput);
                    File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                }
            }

            // Sum the squares of the errors for the outputs.
            for (int output = 0; output < netTargetOutputs.Length; output++)
            {
                // Add the square of the error for this output to the total.
                error += Math.Pow(netTargetOutputs[output] - netOutputs[output], 2);

                if (writeOutputsToLog && !fastExecution)
                {
                    if (outputWriteCounter == outputsPerLogWrite)
                    {
                        logOutput = "[" + netOutputs[output].ToString("+0.0000000;-0.0000000") + "]";
                        Console.Write(logOutput);
                        File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                    }
                }
            }

            // For experimenting. Only use the error for the significant output. (This currently causes the
            // target error to be reached immediately.)
            //error = error + Math.Pow(netTargetOutputs[significantOutput] - netOutputs[significantOutput], 2);
            //significantOutput = -1;

            if (significantOutput != -1)
            {
                // Give the significant output additional weight determined by significantOutputFactor.
                error += Math.Pow(netTargetOutputs[significantOutput] - netOutputs[significantOutput], 2) * 
                    (significantOutputFactor - 1);
                //error = Math.Sqrt(error / (netTargetOutputs.Length + (significantOutputFactor - 1)));
                // Leave error as sum of squares.
            }
            else
            {
                //error = Math.Sqrt(error / netTargetOutputs.Length);
                // Leave error as sum of squares.
            }

            // Convert error range from (0.0 - 2.0) to (0.0 to 1.0).
            //error = error / 2;
            // Leave error as sum of squares.

            if (writeOutputsToLog && !fastExecution)
            {
                if (outputWriteCounter == outputsPerLogWrite)
                {
                    logOutput = "\r\nError = " + error.ToString("+0.0000000;-0.0000000") + "\r\n";
                    Console.Write(logOutput);
                    File.AppendAllText(AppProperties.ServerLogPath, logOutput);
                    outputWriteCounter = 0;
                }
                outputWriteCounter++;
            }

            return Convert.ToSingle(error);
        }

        private static void GetTargetOutputs(float[] netTargetOutputs, int targetOutput)
        {
            for (int output = 0; output < netTargetOutputs.Length; output++)
            {
                //netTargetOutputs[output] = 0.0F;  // Set all targets to 0.0.
                if (output == targetOutput)
                {
                    netTargetOutputs[output] = 0.9F;
                }
                else
                {
                    netTargetOutputs[output] = -0.9F;
                }
            }

            // Target outputs only for 8
            //for (int output = 0; output < netTargetOutputs.Length; output++)
            //{
            //    if (output == targetOutput)
            //    {
            //        if (output == 8)
            //        {
            //            netTargetOutputs[output] = 0.5F;
            //        }
            //        else
            //        {
            //            netTargetOutputs[output] = -0.5F;
            //        }
            //    }
            //    else
            //    {
            //        netTargetOutputs[output] = -0.5F;
            //    }
            //}

            // All target outputs = -0.5 for 1 - 4 and 0.5 for 5 - 8.
            //for (int output = 0; output < netTargetOutputs.Length; output++)
            //{
            //    if (targetOutput > 4)
            //    {
            //        netTargetOutputs[output] = 0.5F;
            //    }
            //    else
            //    {
            //        netTargetOutputs[output] = -0.5F;
            //    }
            //}

            //netTargetOutputs[0] = 0.0F;
            //netTargetOutputs[1] = -0.9F;
            //netTargetOutputs[2] = 0.9F;
            //netTargetOutputs[3] = 0.0F;
            //netTargetOutputs[4] = -0.9F;
            //netTargetOutputs[5] = 0.9F;
            //netTargetOutputs[6] = 0.0F;
            //netTargetOutputs[7] = -0.9F;
            //netTargetOutputs[8] = 0.9F;
            //netTargetOutputs[9] = 0.0F;
        }

        private static void TestErrorFunction()
        {
            float[] netTargetOutputs = new float[10];
            float[] netOutputs = new float[10];
            float error;
            const int targetOutput = -1;

            for (int output = 0; output < netTargetOutputs.Length; output++)
            {
                if (output == targetOutput)
                {
                    netTargetOutputs[output] = 0.9F;
                }
                else
                {
                    netTargetOutputs[output] = -0.9F;
                }
            }

            //for (int output = 0; output < netOutputs.Length; output++)
            //{
            //    netOutputs[output] = -0.9F;
            //}

            //netTargetOutputs[0] = 0.0F;
            //netTargetOutputs[1] = -0.9F;
            //netTargetOutputs[2] = 0.9F;
            //netTargetOutputs[3] = 0.0F;
            //netTargetOutputs[4] = -0.9F;
            //netTargetOutputs[5] = 0.9F;
            //netTargetOutputs[6] = 0.0F;
            //netTargetOutputs[7] = -0.9F;
            //netTargetOutputs[8] = 0.9F;
            //netTargetOutputs[9] = 0.0F;

            netOutputs[0] = 0.9F;
            netOutputs[1] = 0.9F;
            netOutputs[2] = 0.9F;
            netOutputs[3] = 0.9F;
            netOutputs[4] = 0.9F;
            netOutputs[5] = -0.9F;
            netOutputs[6] = -0.9F;
            netOutputs[7] = -0.9F;
            netOutputs[8] = -0.9F;
            netOutputs[9] = -0.9F;

            if (useSignificantOutputFactor)
            {
                error = GetError(netTargetOutputs, netOutputs, targetOutput);
            }
            else
            {
                error = GetError(netTargetOutputs, netOutputs);
            }
            string outputText = "Net outputs:\r\n";
            for (int output = 0; output < netOutputs.Length; output++)
            {
                outputText = outputText +
                    "Target[" + output + "] = " + netTargetOutputs[output].ToString("+0.0000000;-0.0000000") +
                    ", Output[" + output + "] = " + netOutputs[output].ToString("+0.0000000;-0.0000000") +
                    "\r\n";
            }

            outputText = outputText + "Error = " + error.ToString("+0.0000000;-0.0000000") + "\r\n\r\n";

            File.AppendAllText(AppProperties.ServerLogPath, outputText);
        }

        public static NetMap GetNetImageMapFromBitmap(Bitmap imageBitmap, RGB rgb)
        {
            NetMap imageMap = new NetMap(imageBitmap.Width, imageBitmap.Height);
            byte pixelColor = 0;
            const float colorLevelUnit = 1.0F / 255.0F;  // For converting range 0-255 to 0.0-1.0.

            //File.AppendAllText(AppProperties.ServerLogPath, "\r\nInput Image values for " + rgb + ":\r\n");

            // Loop through the images pixels and place values in input image map.
            for (int x = 0; x < imageBitmap.Width; x++)
            {
                for (int y = 0; y < imageBitmap.Height; y++)
                {
                    switch (rgb)
                    {
                        case RGB.Red:
                            pixelColor = imageBitmap.GetPixel(x, y).R;
                            break;
                        case RGB.Green:
                            pixelColor = imageBitmap.GetPixel(x, y).G;
                            break;
                        case RGB.Blue:
                            pixelColor = imageBitmap.GetPixel(x, y).B;
                            break;
                    }
                    // Convert byte pixel values to float range -1.0 to +1.0.
                    // (0 - 255 value) x colorLevelUnit = 0.0 to 1.0 float
                    // ((1.0 - 1.0 float) x 2) - 1 = -1.0 to +1.0 float.
                    imageMap.Points[x, y] = ((pixelColor * colorLevelUnit) * 2) - 1;

                    //File.AppendAllText(AppProperties.ServerLogPath, "x[" + x + "], y[" + y + "] = " +
                    //    imageMap.Points[x, y] + "\r\n");
                }
            }

            return imageMap;
        }

        public static string DeleteNet(string args)
        {
            int i = 0;
            args = args.Trim();
            string name = ReadNextArg(args, ref i);
            string response;
            // Delete net from database.
            try
            {
                sql = new SqlCommand(
                "DELETE FROM net " + "WHERE name = '" + name + "'"
                , KBConnection);

                sql.ExecuteNonQuery();

                response = "Success - Net deleted.";
            }
            catch (SqlException error)
            {
                //Console.WriteLine("There was an error reported by SQL Server, " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Error, " + error.Message + "\r\n");
                response = "commanderror - Net was not deleted.";
            }

            // Delete net data file.
            if (!response.StartsWith("commanderror"))
            {
                try
                {
                    File.Delete(AppProperties.AIDevNetsFolderPath + @"\" + name + ".bin");
                }
                catch { };
            }

            return response;
        }

        private static string ReadNextArg(string args, ref int i)
        {
            string arg = "";
            char quote = '"';

            try
            {
                if (i < args.Length)
                {
                    while (i < args.Length && char.IsWhiteSpace(args[i]))  // First clause evaluated first.
                    {
                        i++;
                    }

                    if (args[i] == quote)
                    {
                        // This is an argument in quotes.
                        i++;
                        while (i < args.Length && args[i] != quote)
                        {
                            arg += args[i];
                            i++;
                        }
                        i++;
                    }
                    else
                    {
                        // This is an argument without quotes.
                        while (i < args.Length && !char.IsWhiteSpace(args[i]))
                        {
                            arg += args[i];
                            i++;
                        }
                        i++;
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("Error: " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "Error: " +
                    error.Message + Environment.NewLine);
            }

            return arg;
        }

        public static string OpenNet(string args)
        {
            int i = 0;
            args = args.Trim();
            string name = ReadNextArg(args, ref i);

            // Only allow one net open at a time for now.
            if (Nets.Count > 0)
            {
                Nets.Clear();
                currentNet = -1;
            }
            string response = LoadNet(name);

            return response;
        }

        public static string LoadNet(string name)
        {
            _ = new Net();

            string result;
            try
            {
                Stream stream = File.Open(AppProperties.AIDevNetsFolderPath + @"\" + name + ".bin", FileMode.Open);
                BinaryFormatter formatter = new BinaryFormatter();

                Net net = (Net)formatter.Deserialize(stream);
                stream.Close();

                Nets.Add(net);
                currentNet = Nets.Count - 1;
                //result = "success - Net loaded";

                Nets[currentNet].WriteNetPropertiesToLog();

                result = Convert.ToString(Nets[currentNet].IsGrayscale);
            }
            catch
            {
                result = "error";
            }

            return result;
        }

        // Create new net name. If "Net1" exists, make it "Net2," etc., going up to "Net_n_."
        private static string GetNewNetName()
        {
            int newNetNameNum = 1;
            string newNetName = "";

            bool nameFree = false;

            while (!nameFree)
            {
                newNetName = "Net" + newNetNameNum.ToString();

                sql = new SqlCommand(
                "SELECT DISTINCT name FROM net " +
                "WHERE name = '" + newNetName + "'"
                , KBConnection);

                using (SqlDataReader reader = sql.ExecuteReader())
                {
                    reader.Read();
                    if (!reader.HasRows) nameFree = true;
                }

                newNetNameNum++;
            }

            return newNetName;
        }

        private static int GetNetID(string netName)
        {
            int netID = -1;  // Return -1 if no net with this name.

            try
            {
                sql = new SqlCommand(
                "SELECT DISTINCT id FROM net " +
                "WHERE name = '" + netName + "'"
                , KBConnection);

                using SqlDataReader reader = sql.ExecuteReader();
                reader.Read();
                if (reader.HasRows) netID = Convert.ToInt32(reader[0]);
            }
            catch (Exception error)
            {
                File.AppendAllText(AppProperties.ServerLogPath, "Error, " + error.Message + "\r\n");
            }

            return netID;
        }

        public static string WriteToLog()
        {
            File.WriteAllText(AppProperties.ServerLogPath, "");  // Erase server log file.
            Nets[currentNet].WriteNetPropertiesToLog();
            string response = "Wrote to log.";
            return response;
        }

        public static string ClearLog()
        {
            File.WriteAllText(AppProperties.ServerLogPath, "");  // Erase server log file.
            string response = "Cleared log.";
            return response;
        }

        public static string RunCode()
        {

            //TestErrorFunction();

            string response = "Done running code.";

            return response;
        }

        public static string RunSQL()
        {
            string response = "Added net.";

            //AddNet(1, "new net", "tanh", 3, 1024, 26);

            return response;
        }

        public static string RunTensorFlow()
        {
            ///* Create a Constant op
            //   The op is added as a node to the default graph.

            //   The value returned by the constructor represents the output
            //   of the Constant op. */
            //var hello = tf.constant("Hello, TensorFlow!");

            //// Start tf session
            //using (var sess = tf.Session())
            //{
            //    // Run the op
            //    var result = sess.run(hello);
            //    Console.WriteLine(result);
            //}

            string response = "Finished running TensorFlow.";

            return response;
        }
    }

    enum TrainingDataset
    {
        dataset1Item,
        dataset2Item,
        dataset00_2Percent,  // 0.2 percent
        dataset02_0Percent,   // 2.0 percent
        dataset10_0Percent  // 10.0 percent
    };

    enum RGB
    {
        Red,
        Green,
        Blue
    };

    enum NetType
    {
        FullyConnected,
        Convolutional
    };
}
