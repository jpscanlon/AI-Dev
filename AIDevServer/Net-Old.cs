using System;
using System.Collections.Generic;  // For List

namespace AIDevServer
{
    class Net
    {
        public int NetID { get; set; }
        public string name = "";
        public string activationFunction = "";
        public static int numLayers = 2;
        public static int numInputs = 2;
        public static int numOutputs = 2;

        //List<Net> NetsList = new List<Net>();

        public List<Layer> layerList = new List<Layer>();
        public Layer[] layerArray = new Layer[numLayers];

        public class Layer
        {
            //public List<float> testWeightList;  // If weights collection were a single list.
            
            public List<List<float>> outputRowList = new List<List<float>>();
            public float[,] weightArray = new float[numOutputs, numInputs];
        }

        public void LoadListApproach()
        {
            float weightValue = 0.0F;
            
            private List<float> inputWeightList;
            

            List<float> newInputWeightList = new List<float>();
            newInputWeightList



            layerArray[1].outputRowList.Add(newInputWeightList);

        }

        public void LoadArrayApproach()
        {
            //int numIn = 8;
            //int numOut = 6;
            float weightValue = 0.0F;



            //// Load weights.
            //for (int iIn = 0; iIn <= numIn; iIn++)
            //{
            //    List<float> Inputs = new List<float>();

            //    for (int iOut = 0; iOut < numOut; iOut++)
            //    {
            //        Inputs.Add(weightValue);
            //    }
            //    //Layer.Add(Inputs);
            //}

            //// Write weight values to server log.
            //for (int iIn = 0; iIn <= numIn; iIn++)
            //{
            //    System.IO.File.AppendAllText(AppProperties.serverLogPath, "Input neuron " + iIn + ": ");
            //    for (int iOut = 0; iOut < numOut; iOut++)
            //    {
            //        //System.IO.File.AppendAllText(AppProperties.serverLogPath, Layer[iIn][iOut].ToString() + " ");
            //    }
            //    System.IO.File.AppendAllText(AppProperties.serverLogPath, Environment.NewLine);
            //}
            //Console.WriteLine("Done writing net.");
            //System.IO.File.AppendAllText(AppProperties.serverLogPath, "Finished writing net." + Environment.NewLine);
        }
    }
}
