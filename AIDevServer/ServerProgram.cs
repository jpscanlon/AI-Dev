using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AIDevServer
{
    class Server
    {
        public static void Main(string[] args)
        {
            File.WriteAllText(AppProperties.ServerLogPath, "");  // Erase server log file.

            // Set console window position on desktop.
            //try
            //{
            //    //Console.SetWindowPosition(0, 100);
            //    Console.WindowLeft = 0;
            //    Console.WindowTop = 0;
            //}
            //catch (Exception error)
            //{
            //    Console.WriteLine("Error: " + error.Message);
            //    File.AppendAllText(AppProperties.ServerLogPath, "Error: " +
            //        error.Message + Environment.NewLine);
            //}

            ServerTCPConnection.Start();
            //ServerTCPConnection.GetConnection();

            Knowledgebase.Connect();
            Knowledgebase.LoadLang();

            File.AppendAllText(AppProperties.ServerLogPath, "Entering server loop." + 
                Environment.NewLine);
            bool continueRunning = true;
            // For writing to the console only after a number of passes through the server loop.
            int cycleCount = 0;
            while (continueRunning)
            {
                if (cycleCount == 0) Console.Write(DateTime.Now.ToLongTimeString() + 
                    "\r\nServer running...\n");
                if (cycleCount == 0 || cycleCount == 10) Console.Write("Getting commands...\r\n");
                Thread.Sleep(50);
                string commands = ServerTCPConnection.HandleConnection();
                if (!string.IsNullOrEmpty(commands))
                {
                    continueRunning = ProcessCommands(commands);
                }
                cycleCount++;
                if (cycleCount == 20) cycleCount = 0;
            }

            Console.WriteLine("Closing Server...");
            File.AppendAllText(AppProperties.ServerLogPath, "Closing server." + 
                Environment.NewLine);
            Thread.Sleep(300);
            //return 0;
        }

        private static bool ProcessCommands(string commands)
        {
            bool continueRunning = true;
            if (commands != null)
            {
                Console.WriteLine("Commands received [" + commands + "]");
                string response = "";
                //commands = commands.ToLower();

                if (commands == "clearstream")
                {
                    // For clearing the client/network stream.
                    ServerTCPConnection.ReturnResponse("stream cleared");
                }
                else if (commands == "closeserver")
                {
                    ServerTCPConnection.ReturnResponse("closing server");
                    Console.WriteLine("Closing server...");
                    ServerTCPConnection.connected = false;
                    continueRunning = false;
                }
                //else if (commands == "serverconnect")
                //{
                //    ServerTCPConnection.ReturnResponse("server connected");
                //}
                else if (commands == "disconnect")
                {
                    ServerTCPConnection.ReturnResponse("server disconnected");
                    Console.WriteLine("Closing connection...");
                    ServerTCPConnection.connected = false;
                }
                else if (commands == "writetolog")
                {
                    response = Knowledgebase.WriteToLog();
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands == "clearlog")
                {
                    response = Knowledgebase.ClearLog();
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands == "runcode")
                {
                    response = RunCode();
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands == "runsql")
                {
                    response = Knowledgebase.RunSQL();
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands == "getnets")
                {
                    string netsData;
                    netsData = Knowledgebase.GetNetsData();
                    ServerTCPConnection.ReturnResponse(netsData);
                }
                else if (commands == "python")
                {
                    //commands = @"print('Hello world!')";
                    response = Knowledgebase.StartPython();
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("[py]"))
                {
                    if (commands[4..] == "exit()")
                    {
                        response = Knowledgebase.EndPython();
                    }
                    else
                    {
                        response = Knowledgebase.RunPython(commands[4..]);
                    }
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("getpythonoutput"))
                {
                    response = Knowledgebase.GetPythonOutput();
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("addnet"))
                {
                    response = Knowledgebase.CreateNet();
                    ServerTCPConnection.ReturnResponse(response);  // Return new net name.
                }
                else if (commands.StartsWith("deletenet"))
                {
                    response = Knowledgebase.DeleteNet(
                        commands[10..]);
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("opennet"))
                {
                    response = Knowledgebase.OpenNet(commands[8..]);
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("updatenet"))
                {
                    response = Knowledgebase.SaveNet(commands[10..]);
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("updatewholenet"))
                {
                    response = Knowledgebase.ModifyAndSaveNet(
                        commands[15..]);
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("reinitialize"))
                {
                    response = Knowledgebase.ReinitializeNet();
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("runnet"))
                {
                    response = Knowledgebase.RunNet(commands[7..]);
                    ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("trainnet"))
                {
                    ServerTCPConnection.ReturnResponse("training started");
                    //response = Knowledgebase.TrainNet(
                    //    commands.Substring(9, commands.Length - 9));
                    _ = Knowledgebase.TrainNetBackprop(
                        commands[9..]);
                    //ServerTCPConnection.ReturnResponse(response);
                }
                else if (commands.StartsWith("stoptraining"))
                {
                    // Currently handled by command checking in Knowledgebase.Train().
                    ServerTCPConnection.ReturnResponse("training stopped");
                }
                else if (commands.StartsWith("geterrorhistory"))
                {
                    // Command wasn't processed in training methods, so training has stopped.
                    ServerTCPConnection.ReturnResponse("training finished");
                }
                else if (commands.StartsWith("processstatements"))
                {
                    commands = commands.ToLower();
                    Knowledgebase.ProcessStatements(commands[18..]);
                }
                else if (commands.StartsWith("getudwords"))
                {
                    ServerTCPConnection.ReturnResponse(Language.GetUDWordsForClient());
                }
                else
                {
                    Console.WriteLine("Unrecognized command.");
                    ServerTCPConnection.ReturnResponse("unrecognized command: \"" + commands + "\"");
                }
            }

            return continueRunning;
        }

        public static string RunCode()
        {

            //response = Knowledgebase.RunCode();

            //bool userDefined = true;
            //bool temp = true;

            //int rule = LangBNF.LexRules.FindIndex(
            //    lexRule => lexRule.Token == "disc_obj_noun");
            //LangBNF.LexRules[rule].Clauses.Add(new LangBNF.LexClause(userDefined, temp));
            //int clause = LangBNF.LexRules[rule].Clauses.Count - 1;
            //LangBNF.LexRules[rule].Clauses[clause].Items.Add(
            //    new LangBNF.LexItem("leela"));

            //response = Language.GetUDWordsForClient();

            string response = _ = Knowledgebase.RunTensorFlow();

            //response = "Code was executed.";

            return response;
        }
    }

    // Maybe make this class a singleton that can only be instantiated once.
    // Implementing the singleton design pattern:
    // - Hide the constructor of the class.
    // - Define a public static operation(getInstance()) that returns the sole 
    //   instance of the class.
    //
    static class AppProperties
    {
        // "\.." doesn't work with SQL Server connection string.
        //static readonly string solutionPath = System.AppDomain.CurrentDomain.BaseDirectory + 
        //    @"..\..\..";

        public static string AIDevDataFolderPath = Properties.Settings.Default.AIDevFolderPath + 
            @"\AIDev Data";
        public static string IOFolderPath = AIDevDataFolderPath + @"\IO";
        public static string MotorOutPath = IOFolderPath + @"\motor out.txt";  // for motor outputs
        public static string KBFolderPath = AIDevDataFolderPath + @"\Knowledgebase";

        // For normal development locations:
        public static string AIDevNetsFolderPath = KBFolderPath + @"\Nets";
        public static string ServerLogPath = AIDevDataFolderPath + @"\server log.txt";

        // For installed locations:
        // (Server path is set in client MainWindow.xaml.cs.)
        // (Installed app still accesses normal development knowledgebase location.)
        //public static string AIDevInstalledFolderPath = 
        //    @"C:\Users\User2\Dev\AI Dev Installed";
        //public static string AIDevInstalledDataFolderPath = AIDevInstalledFolderPath + 
        //    @"\AIDev Data";
        //public static string AIDevNetsFolderPath = AIDevInstalledDataFolderPath + @"\Nets";
        //public static string ServerLogPath = AIDevInstalledDataFolderPath + @"\server log.txt";
    }
}
