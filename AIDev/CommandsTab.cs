using System;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AIDev
{
    partial class MainWindow
    {
        const string commandsHeader = "AI Dev Jillian Interface\r\n\r\n";
        // LINUX terminal doesn't use a space after prompt. Windows Powershell does, 
        // Command Prompt doesn't, Python console does.
        private string commandPrompt = ">";
        private const string pythonCommandPrompt = ">>>";
        public int promptIndex;
        private int lastCaretIndex;
        private string commandsText;
        private static readonly bool startWithRecAndSynthOn = false;

        DispatcherTimer timerPythonOutput;
        DateTime lastPythonOutputTime;
        //int getPythonOutputInterval = 0.5;

        private void ViewCommands()
        {
            textBoxCommands.IsEnabled = false;  // Prevent changes before textBox is loaded.
            tabItemCommands.IsEnabled = true;
            tabItemCommands.Visibility = Visibility.Visible;
            gridCommands.Visibility = Visibility.Visible;
            gridCommands.IsEnabled = true;
            tabItemCommands.Focus();
            textBoxCommands.Focus();
            
            textBoxCommands.HorizontalScrollBarVisibility = 0;
            textBoxCommands.Text = "";

            //textBoxCommands.AcceptsReturn = true;

            //commandsText = GetExampleText();
            commandsText = commandsHeader + commandPrompt;

            textBoxCommands.AppendText(commandsText);

            commandsText = textBoxCommands.Text;

            textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
            promptIndex = textBoxCommands.CaretIndex;
            lastCaretIndex = textBoxCommands.CaretIndex;

            // Image Control
            BitmapImage imageSource = new BitmapImage(new Uri(
                @"C:\Users\User2\Dev\AI Dev\AIDev Resources\Images\readingcouch660x357.jpg",
                UriKind.RelativeOrAbsolute));
            imageInputImage1.Source = imageSource;
            //C:\Users\User2\Dev\AI Dev\AIDev Resources\Images\readingcouch660x357.jpg
            //C:\Users\User2\Dev\AI Dev\AIDev Resources\Images\Psychedelic Explosion.jpg

            // Place in first tab position.
            tabWorkspace.Items.Remove(tabItemCommands);
            tabWorkspace.Items.Insert(0, tabItemCommands);
            
            LangBnf.LoadLexRules();
            LoadUDWords();

            string loadSynthResult = "";
            if (startWithRecAndSynthOn)
            {
                loadSynthResult = Speech.LoadSpeechSynthesizer();
                if (loadSynthResult == "")
                {
                    checkBoxVoiceSynth.IsChecked = true;
                }

                string loadRecResult = Speech.LoadSpeechRecognizer();
                if (loadRecResult == "")
                {
                    checkBoxVoiceRecogition.IsChecked = true;
                }
            }
            else
            {
                if (checkBoxVoiceSynth.IsChecked == true)
                {
                    loadSynthResult = Speech.LoadSpeechSynthesizer();
                }

                if (checkBoxVoiceRecogition.IsChecked == true)
                {
                    Speech.LoadSpeechRecognizer();
                }
            }

            if (loadSynthResult != "")
            {
                checkBoxVoiceSynth.IsChecked = false;
            }

            textBoxCommands.IsEnabled = true;

            tabItemCommands.Focus();
        }

        public void ProcessStatements(string statements)
        {
            statements = statements.Trim();
            if (statements.StartsWith("speak"))
            {
                statements = statements.Substring(6, statements.Length - 6);
                Speech.Speak(Speech.AddPronunciation(statements));

                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                textBoxCommands.AppendText("\r\n\r\n" + commandPrompt);
                commandsText = textBoxCommands.Text;
                promptIndex = textBoxCommands.Text.Length;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                lastCaretIndex = textBoxCommands.Text.Length;
            }
            else if (statements.StartsWith("getvoices"))
            {
                string installedVoices = Speech.GetInstalledVoices();

                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                textBoxCommands.AppendText("\r\n\r\n" + installedVoices + "\r\n" + commandPrompt);
                commandsText = textBoxCommands.Text;
                promptIndex = textBoxCommands.Text.Length;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                lastCaretIndex = textBoxCommands.Text.Length;
            }
            else if (statements.ToLower() == "python")
            {
                string response = TcpConnection.SendMessage("python");

                commandPrompt = pythonCommandPrompt;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                textBoxCommands.AppendText("\r\n" + response.TrimEnd() + "\r\n\r\n" + 
                    commandPrompt);
                commandsText = textBoxCommands.Text;
                promptIndex = textBoxCommands.Text.Length;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                lastCaretIndex = textBoxCommands.Text.Length;
            }
            else if (commandPrompt == pythonCommandPrompt)
            {
                string response = TcpConnection.SendMessage("[py]" + statements);

                if (response == "[python closed]")
                {
                    commandPrompt = ">";  // Switch back to normal command prompt.
                    StopOutput();
                }
                else
                {
                    lastPythonOutputTime = DateTime.UtcNow;
                    timerPythonOutput = new DispatcherTimer();
                    timerPythonOutput.Tick += new EventHandler(TimerPythonOutput_Tick);
                    timerPythonOutput.Interval = new TimeSpan(0, 0, 0, 0, 200);
                    timerPythonOutput.Start();
                }
            }
            else
            {
                string response = TcpConnection.SendMessage("processstatements " + statements);

                if (response == "RefreshUDWords")
                {
                    // Maybe split pronunciations out of AddRecRulesFromLexRules, since they are
                    // for spokenSynth and can be added separately just for voice synth.
                    LangBnf.LoadLexRules();
                    LoadUDWords();

                    if (checkBoxVoiceRecogition.IsChecked == true)
                    {
                        Speech.LoadSpeechRecognizer();
                    }

                    if (checkBoxVoiceSynth.IsChecked == true)
                    {
                        Speech.AddSynthRulesFromLexRules();
                    }
                    response = "okay;";
                }

                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                textBoxCommands.AppendText("\r\n" + response.TrimEnd() + "\r\n\r\n" + commandPrompt);
                commandsText = textBoxCommands.Text;
                promptIndex = textBoxCommands.Text.Length;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                lastCaretIndex = textBoxCommands.Text.Length;

                if (Speech.Synth != null && checkBoxVoiceSynth.IsChecked == true &&
                    response.Length < 200)
                {
                    Speech.Speak(response);
                }
            }
        }

        private void TimerPythonOutput_Tick(object sender, EventArgs e)
        {
            string response = TcpConnection.SendMessage("getpythonoutput");

            if (response == "[python closed]")
            {
                StopOutput();
            }
            else if (response == "[no output]")
            {
                // If max time passed since last output, stop checking for outputs.
                TimeSpan timeSinceLastPythonOutput = DateTime.UtcNow - lastPythonOutputTime;
                if (timeSinceLastPythonOutput.TotalMilliseconds > 1000)
                {
                    StopOutput();
                }
            }
            else
            {
                // Display output in Commands textbox.
                lastPythonOutputTime = DateTime.UtcNow;
                textBoxCommands.AppendText("\r\n" + response.TrimEnd() + "\r\n");
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                commandsText = textBoxCommands.Text;
            }
        }

        private void GetOutput()
        {
            if (commandPrompt == pythonCommandPrompt)
            {
                string response = TcpConnection.SendMessage("getpythonoutput");

                //textBoxCommands.Text = textBoxCommands.Text.Substring(0, promptIndex);
                //textBoxCommands.CaretIndex = promptIndex;

                if (response != "[no output]")
                {
                    textBoxCommands.AppendText("\r\n" + response.TrimEnd() + "\r\n");
                    textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                    commandsText = textBoxCommands.Text;
                }
            }
        }

        private void StopOutput()
        {
            if (timerPythonOutput != null)
            {
                timerPythonOutput.Stop();
                timerPythonOutput = null;
            }

            textBoxCommands.AppendText("\r\n" + commandPrompt);
            commandsText = textBoxCommands.Text;
            promptIndex = textBoxCommands.Text.Length;
            textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
            lastCaretIndex = textBoxCommands.Text.Length;
        }

        private void LoadUDWords()
        {
            string udWords = TcpConnection.SendMessage("getudwords");

            if (udWords != "not connected")
            {
                int rule;
                int clause;

                string[] words = udWords.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries
                );

                string[] wordFields = new string[14];
                int i = 0;

                while ((i < words.Length) && (!string.IsNullOrEmpty(words[i])))
                {
                    // There is a new line of data. Process it.
                    wordFields = words[i].Split('|');
                    rule = LangBnf.LexRules.FindIndex(
                        lexRule => lexRule.Token == wordFields[0]);
                    LangBnf.LexRules[rule].Clauses.Add(new LangBnf.LexClause(true, wordFields[1] == "true"));
                    clause = LangBnf.LexRules[rule].Clauses.Count - 1;
                    LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                        new LangBnf.LexItem(wordFields[2], wordFields[3],
                        wordFields[4]));

                    switch (wordFields[0])
                    {
                        case "class_noun":
                            LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                                new LangBnf.LexItem(wordFields[5], wordFields[6],
                                wordFields[7]));
                            break;
                        case "intrans_verb":
                        case "trans_verb":
                            LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                                new LangBnf.LexItem(wordFields[8], wordFields[9],
                                wordFields[10]));
                            LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                                new LangBnf.LexItem(wordFields[11], wordFields[12],
                                wordFields[13]));
                            break;
                        default:
                            break;
                    }
                    i++;
                }
            }
        }

        private void GetInstalledVoices()
        {
            if (tabItemCommands.IsEnabled == false)
            {
                ViewCommands();
            }

            textBoxCommands.Text = textBoxCommands.Text.Remove(promptIndex - 1);
            textBoxCommands.AppendText(">getvoices");
            textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
            ProcessStatements(textBoxCommands.Text.Substring(promptIndex));
        }

        private void SelectVoice(string voice)
        {
            Speech.SelectVoice(voice);

            if (checkBoxVoiceSynth.IsChecked == true)
            {
                string loadSynthResult = Speech.LoadSpeechSynthesizer();

                if (loadSynthResult != "")
                {
                    checkBoxVoiceSynth.IsChecked = false;
                }
            }
        }

        //private void TestRecognize(string word)
        //{
        //    checkBoxVoiceRecogition.IsChecked = false;
        //    Speech.RecEngine.EmulateRecognizeAsync(word);
        //}

        private void TextBoxCommands_CommandExecuted(object sender, RoutedEventArgs e)
        {
            // If this is a paste event, paste all lines from the clipboard (not just the first).
            if ((e as ExecutedRoutedEventArgs).Command == ApplicationCommands.Paste)
            {
                string clipboardText = Clipboard.GetText().TrimEnd();

                textBoxCommands.Text = commandsText.Substring(0, promptIndex) + clipboardText;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                commandsText = textBoxCommands.Text;
                lastCaretIndex = textBoxCommands.Text.Length;

                //if (e.Handled) { }
            }
        }

        private void TextBoxCommands_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            if (e.Key == Key.Return)
            {
                //textBoxCommands.AcceptsReturn = false;
                ProcessStatements(textBoxCommands.Text.Substring(promptIndex));
            }
        }

        private void TextBoxCommands_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(textBoxCommands);
            textBoxCommands.ScrollToEnd();
        }

        private void TextBoxCommands_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //if (textBoxCommands.CaretIndex < promptIndex)
            //{
            //    e.Handled = true;
            //}
        }

        private void TextBoxCommands_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Prevent caret from changing before prompt (also prevents copying and pasting).
            //if (textBoxCommands.CaretIndex < promptIndex)
            //{
            //    textBoxCommands.CaretIndex = promptIndex;
            //}
        }

        private void TextBoxCommands_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxCommands.CaretIndex == 0)
            {
                // Handle changes while textBox is being initialized.
                if (lastCaretIndex < promptIndex)
                {
                    if (textBoxCommands.Text != commandsText)
                    {
                        textBoxCommands.Text = commandsText;
                        textBoxCommands.CaretIndex = promptIndex;
                    }
                }
            }
            else
            {
                int changeLength = textBoxCommands.Text.Length - commandsText.Length;

                if ((textBoxCommands.CaretIndex - changeLength) < promptIndex ||
                    textBoxCommands.CaretIndex < promptIndex)
                {
                    // Change is to text before the prompt index - don't allow.
                    textBoxCommands.Text = commandsText;
                    textBoxCommands.CaretIndex = promptIndex;
                }
                else
                {
                    commandsText = textBoxCommands.Text;
                }
            }

            textBoxCommands.ScrollToEnd();
        }

        private void CheckBoxVoiceSynth_Checked(object sender, RoutedEventArgs e)
        {
            if (Speech.Synth == null)
            {
                string loadSynthResult = Speech.LoadSpeechSynthesizer();

                if (loadSynthResult != "")
                {
                    checkBoxVoiceSynth.IsChecked = false;
                }
            }
        }

        private void CheckBoxVoiceSynth_Unchecked(object sender, RoutedEventArgs e)
        {
            Speech.Synth.Dispose();
            Speech.Synth = null;
        }

        private void CheckBoxVoiceRecognition_Checked(object sender, RoutedEventArgs e)
        {
            string loadRecResult = "";

            if (Speech.RecEngine == null)
            {
                loadRecResult = Speech.LoadSpeechRecognizer();
            }

            if (loadRecResult == "")
            {
                if (!Speech.Speaking)
                {
                    //Speech.RecEngine.EmulateRecognizeAsync("bravo");
                    Speech.RecEngine.RecognizeAsync(RecognizeMode.Multiple);
                }
            }
        }

        private void CheckBoxVoiceRecognition_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Speech.RecEngine != null)
            {
                Speech.RecEngine.RecognizeAsyncCancel();
            }
        }

        // RE-WRITE THIS WITH REALISTIC STATEMENTS AND RESPONSES.
        private string GetExampleText()
        {
            string exampleText;

            // REDO DEMO TEXT TO SHOW ENVISIONED FUTURE INTERACTION WITH A KNOWLEDGEBASE.
            exampleText = "[...1....|....2....|....3....|....4....|....5....|....6....|....7....|....8....]\n" +
                //"---------1---------2---------3---------4---------5---------6---------7---------8---------9--------10\n" +
                //"[...1....|....2....|....3....|....4....|....5....|....6....|....7....|....8....]" +
                //"....1....|....2....|....3....|....4....|....5....|....6....|....7....|....8....]\n\n" +
                "PS C:\\Users\\Admin > dir\n" +
                "\n" +
                "Directory: C: \\Users\\Admin\n" +
                "\n" +
                "Mode" + "\t\t" + "LastWriteTime" + "\t\t\t" + "Name\n" +
                "----------" + "\t" + "---------------------" + "\t\t" + "--------------\n" +
                "d-----" + "\t\t" + "7 / 22 / 2018   5:13 AM" + "\t" + ".dotnet\n" +
                "d-----" + "\t\t" + "2 / 22 / 2017   7:20 PM" + "\t" + ".MCTranscodingSDK\n" +
                "d-----" + "\t\t" + "4 / 14 / 2018  11:21 AM" + "\t" + ".templateengine\n" +
                "d-----" + "\t\t" + "6 / 28 / 2018   2:56 AM" + "\t" + ".vscode\n" +
                "d - r-- - " + "\t" + "7 / 11 / 2018   1:51 PM" + "\t" + "3D Objects\n" +
                "d - r-- - " + "\t" + "7 / 11 / 2018   1:51 PM" + "\t" + "Images\n" +
                "d - r-- - " + "\t" + "7 / 11 / 2018   1:52 PM" + "\t" + "Virtual Domains\n" +
                "d - r-- - " + "\t" + "7 / 11 / 2018   1:51 PM" + "\t" + "Searches\n" +
                "d---- - " + "\t" + "7 / 04 / 2018  10:15 PM" + "\t" + "source\n" +
                "d - r-- - " + "\t" + "7 / 11 / 2018   1:51 PM" + "\t" + "Video\n" +
                "\n";

            exampleText = exampleText +
                "PS C:\\Users\\Admin > \n" +
                "\n" +
                "Commands:\n" +
                "  h - Help: display command menu\n" +
                "  q - Exit AI Dev.\n" +
                "  c - Process commands in commands file\n" +
                "  load - Load a bitmap file and copy contents into a text file.\n" +
                "  add mem  -Add an image to SA1 memory.\n" +
                "  write - Write a loaded bitmap to a text file.\n" +
                "\n" +
                "PS C:\\Users\\Admin > \n\n" +
                "Commands:\n" +
                "  h - Help: display command menu\n" +
                "  q - Exit AI Dev.\n" +
                "  c - Process commands in commands file\n" +
                "  load - Load a bitmap file and copy contents into a text file.\n" +
                "  add mem  -Add an image to SA1 memory.\n" +
                "  write - Write a loaded bitmap to a text file.\n" +
                "\n" +
                commandPrompt;

            return exampleText;
        }

        private class RecognizerRule
        {
            public string Spoken = "";
            public string Text = "";
        }
    }
}
