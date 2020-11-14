using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AIDevServer
{
    class ServerTCPConnection
    {
        public static bool connected = false;

        static TcpListener ServerTCPListener = null;
        static TcpClient client = null;
        static NetworkStream clientStream = null;

        public static void Start()
        {
            try
            {
                if (ServerTCPListener != null) ServerTCPListener.Stop();
            }
            catch {}
            
            try
            {
                // Start the TcpListener.
                //int port = 8000;
                int port = 13000;
                IPAddress localIP = Dns.GetHostEntry("localhost").AddressList[0];
                ServerTCPListener = new TcpListener(localIP, port);
                ServerTCPListener.Start();  // Start listening for client requests.
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
                System.IO.File.AppendAllText(AppProperties.ServerLogPath, "Error: " + e + Environment.NewLine);
            }
            finally
            {
                // Stop listening for new clients.
                //ServerTCPListener.Stop();
            }
        }

        public static string HandleConnection()
        {
            if (client != null)
            {
                // This doesn't recognize a disconnected client:
                if (!client.Connected) connected = false;
            }
            else
            {
                connected = false;
            }

            string commands;
            //if (IsConnectionGood(clientStream) == false) connected = false;

            if (connected)
            {
                commands = GetCommands(clientStream);
            }
            else
            {
                //Start();
                ServerTCPListener.Start();
                commands = GetConnection();
            }

            return commands;
        }

        public static string GetConnection()
        {
            string commands = null;
            Console.Write("Checking for a connection request...\n");
            Thread.Sleep(500);  // To prevent hogging CPU time.

            if (ServerTCPListener.Pending())
            {
                // Accept pending client connection and return a TcpClient object initialized for communication.
                // Can also use server.AcceptSocket(), which has more functionality.
                client = ServerTCPListener.AcceptTcpClient();
                Console.WriteLine("TCP client accepted.");
                connected = true;
                clientStream = client.GetStream();

                if (connected)
                {
                    commands = GetCommands(clientStream);
                }
            }

            return commands;
        }

        private static string GetCommands(NetworkStream stream)
        {
            // Buffer for reading data
            //Byte[] bytes = new Byte[256];
            byte[] bytes = new byte[256000];

            string commands = null;

            //Console.Write("Getting commands...\n");
            //Thread.Sleep(100);  // To prevent hogging CPU time.

            try
            {
                int i;
                //while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                if (stream.DataAvailable == true)
                {
                    if ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to an ASCII string.
                        commands = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        //Console.WriteLine("Received: {0}", commands);
                    }
                }
            }
            catch (Exception e)
            {
                //Start();  // Try restarting listener.
                ServerTCPListener.Start();
                commands = GetConnection();
                System.IO.File.AppendAllText(AppProperties.ServerLogPath, 
                    "Restarted listener. Error: " + e + Environment.NewLine);
            }

            return commands;
        }

        private static bool IsConnectionGood(NetworkStream stream)
        {
            bool connectionGood = true;
            int result;

            byte[] bytes = new byte[256000];
            //string commands = null;

            try
            {
                result = stream.Read(bytes, 0, bytes.Length);
                //if (result != 0)
                //{
                //    // Translate data bytes to an ASCII string.
                //    commands = System.Text.Encoding.ASCII.GetString(bytes, 0, result);
                //}
            }
            catch
            {
                connectionGood = false;
            }

            return connectionGood;
        }

        public static void ReturnResponse(string response)
        {
            // Returning an empty string as response causes the client to encounter an error reading.
            if (string.IsNullOrEmpty(response)) response = "no response";

            //Byte[] bytes = new Byte[256];
            _ = new byte[256000];

            byte[] msg = System.Text.Encoding.ASCII.GetBytes(response);

            // Send back response.
            clientStream.Write(msg, 0, msg.Length);
            //Console.WriteLine("Sent: {0}", data);
        }
    }
}
