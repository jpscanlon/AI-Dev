﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonConsole
{
    class PythonProgram
    {
        private Process cmdProcess;

        //static async Task Main(string[] args)
        //{
        //    string commands = @"print('Hello world!')";
        //    await RunPythonProcessAsync(commands);

        //    //Console.Write(result);
        //    Console.ReadKey();
        //    //commands = Console.ReadLine();
        //}

        public static void Main(string[] args)
        {
            string command = @"print('Hello world!')";
            //string pythonArgs = @"C:\Users\User2\Dev\AI Dev\Python Scripts";
            //string result = RunPythonProcess(commands, pythonArgs);
            string result = RunPythonProcess(command);

            Console.Write(result);
            Console.ReadKey();
            //commands = Console.ReadLine();
        }

        public static string RunPythonProcess(string command, string args = "")
        {
            //import tensorflow as tf

            ProcessStartInfo start = new ProcessStartInfo
            {
                WorkingDirectory = @"C:\Users\User2\source\venvs\env\Scripts",
                //FileName = "pythonw.exe",
                //FileName = "python.exe",
                FileName = @"cmd.exe",
                //Arguments = string.Format("\"{0}\" \"{1}\"", command, args),
                UseShellExecute = false, // Do not use OS shell
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true, // Any output, generated by application will be redirected back
                RedirectStandardError = true // Any error in standard output will be redirected back (for example exceptions)
            };

            //cmdProcess = new Process;
            Process cmdProcess = Process.Start(start);

            StreamWriter stdInput = cmdProcess.StandardInput;
            StreamReader stdOutput = cmdProcess.StandardOutput;
            StreamReader stdError = cmdProcess.StandardError;

            if (stdInput.BaseStream.CanWrite)
            {
                // Activate virtual environment.
                stdInput.WriteLine("\"C:\\Users\\User2\\source\\venvs\\env\\Scripts\\activate.bat\"");
                // Activate your environment
                //stdInput.WriteLine("activate your-environment");
                //// Any other commands you want to run
                //stdInput.WriteLine("set KERAS_BACKEND=tensorflow");
                //// run your script. You can also pass in arguments
                //stdInput.WriteLine(@"C:\Users\User2\source\venvs\env\Scripts\python.exe");
                stdInput.WriteLine("python.exe");
                stdInput.WriteLine(command);
            }

            //if (stdInput.BaseStream.CanWrite)
            //{
            //    stdInput.WriteLine("dir");
            //}

            stdInput.Flush();
            stdInput.Close();

            string result = stdOutput.ReadToEnd(); // Result of StdOut
            string error = stdError.ReadToEnd(); // Exceptions from our Python script

            //while (process.StandardOutput.Peek() > -1)
            //{
            //    //result.Add(output.ReadLine());
            //    result = result + "\r\n" + output.ReadLine();
            //}

            //while (process.StandardError.Peek() > -1)
            //{
            //    //result.Add(stdError.ReadLine());
            //    error = result + "\r\n" + stdError.ReadLine();
            //}

            ////result = output.ReadLine(); // Here is the result of StdOut(for example: print "test")

            result = result + error;

            return result;
        }

        public async Task RunPythonProcessAsync(string command, string args = "")
        {
            //import tensorflow as tf

            ProcessStartInfo start = new ProcessStartInfo
            {
                WorkingDirectory = @"C:\Users\User2\source\venvs\env\Scripts",
                //FileName = @"C:\Users\User2\source\venvs\env\Scripts\pythonw.exe",
                FileName = @"C:\Users\User2\source\venvs\env\Scripts\python.exe",
                Arguments = string.Format("\"{0}\" \"{1}\"", command, args),
                UseShellExecute = false, // Do not use OS shell
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true, // Any output, generated by application will be redirected back
                RedirectStandardError = true // Any error in standard output will be redirected back (for example exceptions)
            };

            Process process = Process.Start(start);

            StreamWriter stdInput = process.StandardInput;
            stdInput.WriteLine(command);

            StreamReader stdOutput = process.StandardOutput;
            var stderr = await process.StandardError.ReadToEndAsync(); // Here are the exceptions from our Python script
            var result = await stdOutput.ReadToEndAsync(); // Here is the result of StdOut(for example: print "test")

            Console.Write("Result:" + "\r\n");
            Console.Write(result + " | " + stderr);
            //return result + " | " + stderr;
        }
    }
}
