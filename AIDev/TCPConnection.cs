using System;
using System.Net;
using System.Net.Sockets;

namespace AIDev
{
    public class TcpConnection
    {
        // These values should eventually be changed from static to dynamic runtime.
        //private static readonly string serverIP = "127.0.0.1";
        private static readonly IPAddress localIP = Dns.GetHostEntry("localhost").AddressList[0];
        private static readonly string serverIP = localIP.ToString();
        //private static readonly int port = 8000;
        private static readonly int port = 13000;
        static TcpClient client = null;
        static NetworkStream clientStream = null;

        //public static string TestConnection()
        //{
        //    string message = "serverconnect";
        //    string response = "";

        //    try
        //    {
        //        // Create a TcpClient.
        //        client = new TcpClient(serverIP, port);

        //        // Translate the message into ASCII and store it as a Byte array.
        //        Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

        //        // Get a client stream for reading and writing.
        //        clientStream = client.GetStream();

        //        // Send the message to the connected TcpServer. 
        //        clientStream.Write(data, 0, data.Length);

        //        //response = response + "Sent: " + message + "\n";

        //        // Receive the TcpServer.response.

        //        // Buffer to store the response bytes.
        //        //data = new Byte[256];
        //        data = new Byte[256000];
        //        // String to store the ASCII response.
        //        string responseData = String.Empty;

        //        // Add code to handle no response from server.
        //        Thread.Sleep(500);  // Give the server time to respond.
        //        // Read the first batch of the TcpServer response bytes.
        //        if (clientStream.DataAvailable)
        //        {
        //            clientStream.ReadTimeout = 1000;
        //            Int32 bytes = clientStream.Read(data, 0, data.Length);
        //            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        //            response = responseData;
        //            //if (message != "closeserver") MessageBox.Show("Server response: " + response);

        //            //ClearClientStream();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        response = "Exception: " + e;
        //    }

        //    return response;
        //}

        public static string Connect()
        {
            //string message = "serverconnect"
            string result;
            //Byte[] data;

            try
            {
                // Create a TcpClient.
                client = new TcpClient(serverIP, port);

                // Translate the message into ASCII and store it as a Byte array.
                //data = System.Text.Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                clientStream = client.GetStream();

                result = "Connected to server.";
                
                // Send message to the connected TcpServer. 
                //clientStream.Write(data, 0, data.Length);

                //response = "Sent: " + message + "\n";

                //// Receive the TcpServer response.
                //// Buffer to store the response bytes.
                ////data = new Byte[256];
                //data = new Byte[256000];
                //// String to store the ASCII response.
                //string responseData = String.Empty;

                //if (clientStream.DataAvailable)
                //{
                //    // Read the first batch of the TcpServer response bytes.
                //    clientStream.ReadTimeout = 5000;
                //    Int32 bytes = clientStream.Read(data, 0, data.Length);
                //    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //    result = responseData;
                //}
            }
            catch (Exception e)
            {
                result = "Exception: " + e;
            }

            return result;
        }

        public static string Disconnect()
        {
            string response = "";

            try
            {
                response = SendMessage("disconnect");
                clientStream.Close();
                client.Dispose();
                client.Close();
                client = null;
            }
            catch
            {
            }

            return response;
        }

        public static string SendMessage(string message)
        {
            string response;

            //ClearClientStream();

            //if (client != null || client.Connected == true)
            if (client != null)
            {
                // Translate the message into ASCII and store it as a Byte array.
                byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                try
                {
                    // Send the message to the connected TcpServer. 
                    clientStream.Write(data, 0, data.Length);

                    // Buffer to store the response bytes.
                    //data = new Byte[256];
                    data = new byte[256000];

                    // String to store the ASCII response.
                    string responseData = string.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    // It takes time for DataAvailable to become true, so just use ReadTimeout.
                    //if (clientStream.DataAvailable)
                    //{
                    clientStream.ReadTimeout = 20000;
                    int bytes = clientStream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    response = responseData;
                    //}
                }
                //catch (Exception e)
                catch (Exception)
                {
                    //MessageBox.Show("Exception: " + e);
                    response = Connect();  // Attempt to reconnect.
                }
            }
            else
            {
                response = "not connected";
            }

            //MessageBox.Show("Message sent: \r\n" + message + "\r\n" +
            //    "Server response: \r\n" + response);

            return response;
        }

        public static string ClearClientStream()
        {
            string response = "";

            if (client != null)
            {
                try
                {
                    // Buffer to store the response bytes.
                    //data = new Byte[256];
                    byte[] data = new byte[256000];

                    // String to store the ASCII response.
                    string responseData = string.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    // It takes time for DataAvailable to become true, so just use ReadTimeout.
                    //if (clientStream.DataAvailable)
                    //{
                        clientStream.ReadTimeout = 100;
                    int bytes = clientStream.Read(data, 0, data.Length);
                        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                        response = responseData;
                    //}
                }
                //catch (Exception e)
                catch (Exception)
                {
                    //MessageBox.Show("Exception: " + e);
                    //response = Connect();  // Attempt to reconnect.
                }
            }
            else
            {
                response = "not connected";
            }

            return response;
        }
    }
}
