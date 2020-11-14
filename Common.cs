using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
//
// Common shared classes and methods
//
static class Common
{
    // Examples of a nested method, and scope of a variable declared in a block.
    public static bool ContainerMethod()
    {
        bool NestedMethod()
        {
            {
                bool testValue2 = true;
                if (testValue2) return true;
            }

            //  TestValue2 is out of scope.
            bool testValue1 = false;
            //TestValue2 = false;

            return testValue1 || true;
        }

        return NestedMethod();
    }
    
    // “Write a program that prints the numbers from 1 to 100. But for multiples of three print
    // ‘Fizz’ instead of the number and for the multiples of five print ‘Buzz’. For numbers which
    // are multiples of both three and five print ‘FizzBuzz’.”
    public static string FizzBuzz()
    {
        string output = "";

        //for (int number = 1, <= 100, number++)
        {

        }

        return output;
    }

    public static string SerializeToFile(object objectToSerialize, string filePath)
    {
        string error = "";

        try
        {
            Stream stream = File.Open(filePath, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, objectToSerialize);
            stream.Close();
        }
        catch
        {
            error = "error";
        }

        return error;
    }

    public static object DeserializeFromFile(string filePath)
    {
        object deserializedObject;
        _ = new object();

        try
        {
            Stream stream = File.Open(filePath, FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();

            deserializedObject = formatter.Deserialize(stream);
            stream.Close();
        }
        catch
        {
            deserializedObject = null;
        }

        return deserializedObject;
    }

    // Randomizes the order of items in a list.
    public static void ShuffleList<T>(List<T> list, Random random)
    {
        //Random random = new Random();

        int item = list.Count;
        while (item > 1)
        {
            item--;
            int k = random.Next(item + 1);
            T value = list[k];
            list[k] = list[item];
            list[item] = value;
        }
    }

    // Call this in one line instead of creating a char then converting it to a string in two lines.
    public static string CharToString(char charItem)
    {
        return charItem.ToString();
    }

    // Takes a whole number (int) and raises it to the power of a natural number (non-negative integer).
    // Allows using exponents without requiring floating-point operations or conversions.
    public static int NaturalPow(int value, int power)
    {
        int result = 1;

        if (power >= 0)
        {
            if (power == 0)
            {
                result = 1;
            }
            else
            {
                for (int i = 1; i <= power; i++)
                {
                    result *= value;
                }
            }
        }
        else
        {
            // Invalid power parameter. Must be >= 0.
            result = -1;
        }

        return result;
    }

    class Image
    {
        private void ProcessImage()
        {
            //byte red = inputImage.GetPixel(0, 0).R;
            //byte green = inputImage.GetPixel(0, 0).G;
            //byte blue = inputImage.GetPixel(0, 0).B;
            //byte alpha = inputImage.GetPixel(0, 0).A;  // 0 = fully transparent, 255 = fully opaque.

            Bitmap image1;

            // Retrieve the image.
            image1 = new Bitmap(@"C:\Users\User2\Dev\AI Dev Visual Inputs\Datasets\EMNIST\" +
                    @"mnist_png\testing\0\3.png");

            int x, y;

            // Loop through the images pixels to reset color.
            for (x = 0; x < image1.Width; x++)
            {
                for (y = 0; y < image1.Height; y++)
                {
                    Color pixelColor = image1.GetPixel(x, y);
                    Color newColor = Color.FromArgb(pixelColor.R, 0, 0);
                    image1.SetPixel(x, y, newColor);
                }
            }
        }
    }

    // NOT USED

    //public string SaveWeightsToKB_Old()
    //{
    //    string result = "error";
    //    float weightValue = 0.0F;

    //    System.IO.File.AppendAllText(AppProperties.serverLogPath, "\r\nsaving weights\r\n");

    //    try
    //    {
    //        for (int layer = 0; layer < NumLayers; layer++)
    //        {
    //            int numLayerOutputs = Layers[layer].weightArray.GetLength(0);
    //            int numLayerInputs = Layers[layer].weightArray.GetLength(1);

    //            for (int iOutput = 0; iOutput < numLayerOutputs; iOutput++)
    //            {
    //                int iInput = 0;
    //                while (iInput < numLayerInputs)
    //                {
    //                    weightValue = Layers[layer].weightArray[iOutput, iInput];
    //                    //weightValue = 0.7F;

    //                    sql = new SqlCommand(
    //                    "UPDATE weight " +
    //                    "SET value = " + weightValue + " " +
    //                    "WHERE net_id = " + NetID + " AND layer_num = " + layer + " AND output_num = " +
    //                    iOutput + " AND input_num = " + iInput
    //                    , KBConnection);

    //                    sql.ExecuteNonQuery();

    //                    System.IO.File.AppendAllText(AppProperties.serverLogPath, "layer[" + layer + "], output[" +
    //                        iOutput + "], input[" + iInput + "] = " + weightValue + "\r\n");
    //                    iInput++;
    //                }
    //            }
    //        }
    //        System.IO.File.AppendAllText(AppProperties.serverLogPath, "weights saved successfully\r\n\r\n");
    //        result = "weights saved successfully";
    //    }
    //    catch (SqlException error)
    //    {
    //        Console.WriteLine("There was an error reported by SQL Server, " + error.Message);
    //        System.IO.File.AppendAllText(AppProperties.serverLogPath, "\r\nSQL Error, " + error.Message + "\r\n");
    //        result = "SQL Error, " + error.Message + "\r\n";
    //    }

    //    return result;
    //}

    //// Gets the lowest-value unused NetID found. Used if net id isn't an identity column in KB.
    //public static int GetNewNetID()
    //{
    //    int newNetID = 0;
    //    bool newNetIDInList = true;
    //    while (newNetIDInList)
    //    {
    //        newNetIDInList = false;
    //        for (int netIndex = 0; netIndex < (NetsList.Count); netIndex++)
    //        {
    //            if (newNetID == NetsList[netIndex].NetID)
    //                newNetIDInList = true;
    //        }
    //        if (newNetIDInList)
    //            newNetID++;
    //    }
    //    return newNetID;
    //}

    //public static void AddWeights_Old(int netID)
    //{
    //    //int maxLayerNum = 0;
    //    int numLayers = 0;
    //    int numInputs;
    //    int numOutputs;

    //    System.IO.File.AppendAllText(AppProperties.serverLogPath, DateTime.Now.ToLongTimeString() +
    //        "\tStarting AddWeights" + Environment.NewLine);

    //    try
    //    {
    //        sql = new SqlCommand(
    //        "SELECT num_layers FROM net " +
    //        "WHERE id = " + netID
    //        , KBConnection);

    //        using (SqlDataReader reader = sql.ExecuteReader())
    //        {
    //            reader.Read();
    //            numLayers = Convert.ToInt32(reader[0]);
    //        }

    //        for (int layerNum = 0; layerNum < numLayers; layerNum++)
    //        {
    //            sql = new SqlCommand(
    //            "SELECT num_inputs, num_outputs FROM layer " +
    //            "WHERE net_id = " + netID + " AND layer_num = " + layerNum
    //            , KBConnection);

    //            using (SqlDataReader reader = sql.ExecuteReader())
    //            {
    //                reader.Read();
    //                numInputs = Convert.ToInt32(reader[0]);
    //                numOutputs = Convert.ToInt32(reader[1]);
    //            }

    //            System.IO.File.AppendAllText(AppProperties.serverLogPath, "Layer num: " + layerNum +
    //                ", Num inputs: " + numInputs + ", Num outputs: " + numOutputs + Environment.NewLine);

    //            // Last inputNum is bias/threshold input.
    //            for (int inputNum = 0; inputNum <= numInputs; inputNum++)
    //            {
    //                for (int outputNum = 0; outputNum < numOutputs; outputNum++)
    //                {
    //                    sql = new SqlCommand(
    //                    "INSERT INTO weight (net_id, layer_num, input_num, output_num, value) " +
    //                    "VALUES(" + netID + ", " + layerNum + ", " + inputNum + ", " + outputNum + ", 0.0)"
    //                    , KBConnection);

    //                    sql.ExecuteNonQuery();
    //                }
    //            }
    //        }

    //        InitializeWeights(netID);
    //    }
    //    catch (SqlException error)
    //    {
    //        Console.WriteLine("There was an error reported by SQL Server, " + error.Message);
    //        System.IO.File.AppendAllText(AppProperties.serverLogPath, "SQL Error, " + error.Message + "\r\n");
    //    }
    //    System.IO.File.AppendAllText(AppProperties.serverLogPath, DateTime.Now.ToLongTimeString() +
    //        "\tFinished AddWeights" + Environment.NewLine);
    //}
}
