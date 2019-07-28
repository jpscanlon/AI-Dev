using System;
using System.Collections.Generic;  // For List
using System.Data.SqlClient;

namespace AIDevServer
{
    static class CodeLibrary
    {
        public static void AddWeights(int netID)
        {
            //int maxLayerNum = 0;

            System.IO.File.AppendAllText(AppProperties.serverLogPath, DateTime.Now.ToLongTimeString() +
                "\tStarting AddWeights" + Environment.NewLine);

            //try
            //{
            //    // Used when num_outputs wasn't in net table, just in layer table.
            //    //SqlCommand SQL = new SqlCommand(
            //    //"SELECT DISTINCT MAX(layer_num) AS max_layer_num FROM layer " +
            //    //"GROUP BY net_id " +
            //    //"HAVING net_id = " + netID
            //    //, KBConnection);

            //    //using (SqlDataReader reader = SQL.ExecuteReader())
            //    //{
            //    //    reader.Read();
            //    //    maxLayerNum = Convert.ToInt32(reader[0]);
            //    //}

            //    int numLayers = 0;
            //    int numInputs;
            //    int numOutputs;
            //    SqlCommand SQL = new SqlCommand(
            //    "SELECT num_layers, num_inputs FROM net " +
            //    "WHERE id = " + netID
            //    , KBConnection);

            //    using (SqlDataReader reader = SQL.ExecuteReader())
            //    {
            //        reader.Read();
            //        numLayers = Convert.ToInt32(reader[0]);
            //        //numInputs = Convert.ToInt32(reader[1]);
            //    }

            //    for (int layerNum = 0; layerNum < numLayers; layerNum++)
            //    {
            //        if (layerNum == 0)  // This is the first layer, so it's numInputs comes from net table.
            //        {
            //            SQL = new SqlCommand(
            //            "SELECT num_outputs " +
            //            "FROM layer " +
            //            "WHERE layer.net_id = " + netID + " AND layer.layer_num = 0"
            //            , KBConnection);

            //            using (SqlDataReader reader = SQL.ExecuteReader())
            //            {
            //                reader.Read();
            //                numOutputs = Convert.ToInt32(reader[0]);
            //            }
            //        }
            //        else
            //        {
            //            SQL = new SqlCommand(
            //            "SELECT input_layer.num_outputs AS num_inputs, layer.num_outputs " +
            //            "FROM layer, " +
            //            "   (SELECT num_outputs " +
            //            "   FROM layer " +
            //            "       WHERE layer.net_id = " + netID + " AND layer.layer_num = " + Convert.ToString(layerNum - 1) + ") " +
            //            "   AS input_layer " +
            //            "WHERE layer.net_id = " + netID + " AND layer.layer_num = " + layerNum
            //            , KBConnection);

            //            using (SqlDataReader reader = SQL.ExecuteReader())
            //            {
            //                reader.Read();
            //                numInputs = Convert.ToInt32(reader[0]);
            //                numOutputs = Convert.ToInt32(reader[1]);
            //            }
            //        }
            //        System.IO.File.AppendAllText(AppProperties.serverLogPath, "Layer num: " + layerNum +
            //            ", Num inputs: " + numInputs + ", Num outputs: " + numOutputs + Environment.NewLine);

            //        // Last inputNum is bias/threshold input to match weights array in net class.
            //        for (int inputNum = 0; inputNum <= numInputs; inputNum++)
            //        {
            //            for (int outputNum = 0; outputNum < numOutputs; outputNum++)
            //            {
            //                SQL = new SqlCommand(
            //                "INSERT INTO weight (net_id, layer_num, input_num, output_num, value) " +
            //                "VALUES(" + netID + ", " + layerNum + ", " + inputNum + ", " + outputNum + ", 0.0)"
            //                , KBConnection);

            //                SQL.ExecuteNonQuery();
            //            }
            //        }
            //    }
            //}
            //catch (SqlException error)
            //{
            //    Console.WriteLine("There was an error reported by SQL Server, " + error.Message);
            //    System.IO.File.AppendAllText(AppProperties.serverLogPath, "SQL Error, " + error.Message + "\"\r\n");
            //}
            System.IO.File.AppendAllText(AppProperties.serverLogPath, DateTime.Now.ToLongTimeString() +
                "\tFinished AddWeights" + Environment.NewLine);
        }
    }
}