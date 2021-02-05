using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

public class PythonSession
{
    private Process pythonProcess;
    private StreamWriter stdInput;
    //private StreamReader stdOutput;
    //private StreamReader stdError;
    //private string initResult;

    private Queue outputQueue;

    private bool endOfOutput = false;

    //public PythonSession(ref string response)
    public PythonSession()
    {
        // IS PYTHON RUNNING IN THE ACTIVATED VIRTUAL ENVIRONMENT?
        const string pythonWorkingDir = @"C:\Users\User2\source\venvs\env\Scripts";
        //Process.Start(pythonWorkingDir + @"\activate.bat");
        Process.Start(@"C:\Users\User2\source\venvs\env\Scripts\activate.bat");

        // Set working directory and create process
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            WorkingDirectory = pythonWorkingDir,
            FileName = @"C:\Users\User2\source\venvs\env\Scripts\python.exe",
            Arguments = "-i -u",  // Interactive mode, unbuffered
            UseShellExecute = false, // Do not use OS shell
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        pythonProcess = Process.Start(startInfo);
        pythonProcess.EnableRaisingEvents = true;

        // Set event handler to asynchronously read the output.
        pythonProcess.OutputDataReceived += new DataReceivedEventHandler(Process_OutputDataReceived);
        //cmdProcess.ErrorDataReceived += Process_OutputDataReceived;
        pythonProcess.ErrorDataReceived += new DataReceivedEventHandler(Process_ErrorDataReceived);

        outputQueue = Queue.Synchronized(new Queue(10));

        // Start asynchronous read.
        pythonProcess.BeginOutputReadLine();
        pythonProcess.BeginErrorReadLine();

        //pythonProcess.CancelOutputRead();  // Stop asyncronous reading.
        //pythonProcess.CancelOutputRead();  // Restart ascyncronous reading.

        //cmdProcess.WaitForExit();

        stdInput = pythonProcess.StandardInput;
    }

    public string StartPythonAsync()
    {
        string result = "";

        endOfOutput = false;

        string outputLine;
        //outputLine = (string)outputQueue.Dequeue();

        var dateTimeStart = DateTime.Now;
        //while ((DateTime.Now - dateTimeStart).TotalSeconds < 5)
        while (!endOfOutput && (DateTime.Now - dateTimeStart).TotalSeconds < 0.5)
        {
            if (outputQueue.Count > 0)
            {
                outputLine = (string)outputQueue.Dequeue();
                result += outputLine + "\r\n";
            }
        }

        endOfOutput = false;

        return result;
    }

    public string RunPythonAsync(string commands)
    {
        string result = "";

        endOfOutput = false;

        commands = commands.Replace("\r\n", "\n") + "\n";

        if (stdInput.BaseStream.CanWrite)
        {
            stdInput.WriteLine(commands);
            //stdInput.FlushAsync();

            //    string outputLine;
            //    //outputLine = (string)outputQueue.Dequeue();

            //    var dateTimeStart = DateTime.Now;
            //    while (!endOfOutput && (DateTime.Now - dateTimeStart).TotalSeconds < 0.5)
            //    {
            //        if (outputQueue.Count > 0)
            //        {
            //            outputLine = (string)outputQueue.Dequeue();
            //            result += outputLine + "\r\n";
            //        }
            //    }

            //    endOfOutput = false;

            result = "[command Sent]";
        }

        return result;
    }

    public string GetPythonOutput()
    {
        string result = "";
        string outputLine;
        bool outputReceived = false;

        while (outputQueue.Count > 0)
        {
            outputLine = (string)outputQueue.Dequeue();
            result += outputLine + "\r\n";
            outputReceived = true;
        }

        if (outputReceived)
        {
            return result;
        }
        else
        {
            return "[no output]";
        }
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            outputQueue.Enqueue(e.Data);
        }
        else
        {
            endOfOutput = true;
        }
    }

    //Process_ErrorDataReceived
    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        //if (e.Data != null)
        //{
        outputQueue.Enqueue(e.Data);
        //}
        //else
        //{
        //    endOfOutput = true;
        //}
    }

    //public string RunPython(string commands, string args = "")
    //{
    //    //import tensorflow as tf

    //    stdInput = cmdProcess.StandardInput;
    //    stdOutput = cmdProcess.StandardOutput;
    //    //stdError = process.StandardError;
    //    string outputLine;
    //    string result = "";

    //    if (stdInput.BaseStream.CanWrite)
    //    {
    //        stdOutput.DiscardBufferedData();

    //        stdInput.WriteLine(commands);
    //        stdInput.WriteLine(@"print('# End Standard Output #')");
    //        stdInput.WriteLine(@"print('# End Standard Output #')");

    //        //stdInput.Flush();
    //        //stdInput.Close();

    //        outputLine = stdOutput.ReadLine();

    //        //result = commands + "\r\n";
    //        while (outputLine != "# End Standard Output #") // && !stdOutput.EndOfStream)
    //        {
    //            result = result + outputLine + "\r\n";
    //            outputLine = stdOutput.ReadLine();
    //            //result += stdOutput.ReadToEnd();
    //        }

    //        if (outputLine == "# End Standard Output #")
    //        {
    //            result += "# End Standard Output #" + "\r\n";
    //        }

    //        //result.Substring(0);

    //            //process.BeginOutputReadLine();
    //            //process.WaitForExit(); //you need this in order to flush the output buffer        
    //    }

    //    return result;
    //}

    //public static string RunPythonProcess(string cmd, string args = "")
    //{
    //    //import tensorflow as tf

    //    ProcessStartInfo start = new ProcessStartInfo
    //    {
    //        FileName = @"C:\Users\User2\source\venvs\env\Scripts\pythonw.exe",
    //        //FileName = @"C:\Users\User2\source\venvs\env\Scripts\python.exe",
    //        Arguments = string.Format("\"{0}\" \"{1}\"", cmd, args),
    //        UseShellExecute = false,// Do not use OS shell
    //        CreateNoWindow = true, // We don't need new window
    //        RedirectStandardOutput = true,// Any output, generated by application will be redirected back
    //        RedirectStandardError = true // Any error in standard output will be redirected back (exceptions)
    //    };

    //    using Process process = Process.Start(start);
        
    //    //input.WriteLine("python YourScript.py");

    //    using StreamReader output = process.StandardOutput;
    //    string stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from our Python script
    //    string result = output.ReadToEnd(); // Here is the result of StdOut(for example: print "test")

    //    return result + " | " + stderr;
    //}
}
