using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AIDev
{
    partial class MainWindow
    {
        const string commandsHeader = "AI Dev Jillian Interface\r\n\r\n";
        private string commandPrompt = "Commands> ";
        public int PromptIndex;
        private int lastCaretIndex;
        private string commandsText;
        private static bool StartWithRecAndSynthOn = false;

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

            //commandsText = GetDemoText();
            commandsText = commandsHeader + commandPrompt;

            textBoxCommands.AppendText(commandsText);

            commandsText = textBoxCommands.Text;

            textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
            PromptIndex = textBoxCommands.CaretIndex;
            lastCaretIndex = textBoxCommands.CaretIndex;

            // Image Control
            BitmapImage imageSource = new BitmapImage(new Uri(@"C:\Users\Admin\OneDrive\Data\Images\Interesting\readingcouch660x357.jpg",
                UriKind.RelativeOrAbsolute));
            imageInputImage1.Source = imageSource;
            //C:\Users\Admin\OneDrive\Data\Images\Interesting\readingcouch660x357.jpg
            //C:\Users\Admin\OneDrive\Data\Images\Interesting\Psychedelic Explosion.jpg

            // Place in first tab position.
            tabWorkspace.Items.Remove(tabItemCommands);
            tabWorkspace.Items.Insert(0, tabItemCommands);
            
            LangBNF.LoadLexRules();
            LoadUDWords();

            string loadSynthResult = "";
            string loadRecResult = "";

            if (StartWithRecAndSynthOn)
            {
                loadSynthResult = Speech.LoadSpeechSynthesizer();
                if (loadSynthResult == "")
                {
                    checkBoxVoiceSynth.IsChecked = true;
                }

                loadRecResult = Speech.LoadSpeechRecognizer();
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
        }

        public void ProcessStatements(string statements)
        {
            if (statements.StartsWith("speak"))
            {
                statements = statements.Substring(6, statements.Length - 6);
                Speech.Speak(Speech.AddPronunciation(statements));
            }
            else
            {
                string response = TCPConnection.SendMessage("processstatements " + statements);

                if (response == "RefreshUDWords")
                {
                    // Maybe split pronunciations out of AddRecRulesFromLexRules, since they are
                    // for spokenSynth and can be added separately just for voice synth.
                    LangBNF.LoadLexRules();
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

                lastCaretIndex = textBoxCommands.CaretIndex;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                textBoxCommands.AppendText("\r\n\r\n" + response + "\r\n\r\n" + commandPrompt);
                commandsText = textBoxCommands.Text;
                PromptIndex = textBoxCommands.Text.Length;
                textBoxCommands.CaretIndex = textBoxCommands.Text.Length;
                lastCaretIndex = textBoxCommands.Text.Length;

                if (Speech.Synth != null && checkBoxVoiceSynth.IsChecked == true && 
                    response.Length < 200)
                {
                    Speech.Speak(response);
                }
            }
        }

        private void LoadUDWords()
        {
            string udWords = TCPConnection.SendMessage("getudwords");
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
                rule = LangBNF.LexRules.FindIndex(
                    lexRule => lexRule.Token == wordFields[0]);
                LangBNF.LexRules[rule].Clauses.Add(new LangBNF.LexClause(true, wordFields[1] == "true"));
                clause = LangBNF.LexRules[rule].Clauses.Count - 1;
                LangBNF.LexRules[rule].Clauses[clause].Items.Add(
                    new LangBNF.LexItem(wordFields[2], wordFields[3],
                    wordFields[4]));

                switch (wordFields[0])
                {
                    case "class_noun":
                        LangBNF.LexRules[rule].Clauses[clause].Items.Add(
                            new LangBNF.LexItem(wordFields[5], wordFields[6],
                            wordFields[7]));
                        break;
                    case "intrans_verb":
                    case "trans_verb":
                        LangBNF.LexRules[rule].Clauses[clause].Items.Add(
                            new LangBNF.LexItem(wordFields[8], wordFields[9],
                            wordFields[10]));
                        LangBNF.LexRules[rule].Clauses[clause].Items.Add(
                            new LangBNF.LexItem(wordFields[11], wordFields[12],
                            wordFields[13]));
                        break;
                    default:
                        break;
                }
                i++;
            }
        }

        private void SelectVoice(string voice)
        {
            Speech.SelectVoice(voice);

            if (checkBoxVoiceSynth.IsChecked == true)
            {
                string loadSynthResult = "";
                loadSynthResult = Speech.LoadSpeechSynthesizer();

                if (loadSynthResult != "")
                {
                    checkBoxVoiceSynth.IsChecked = false;
                }
            }
        }

        private void TestRecognize(string word)
        {
            checkBoxVoiceRecogition.IsChecked = false;
            Speech.RecEngine.EmulateRecognizeAsync(word);
        }

        private void TextBoxCommands_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ProcessStatements(textBoxCommands.Text.Substring(PromptIndex));
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
                if (lastCaretIndex < PromptIndex)
                {
                    if (textBoxCommands.Text != commandsText)
                    {
                        textBoxCommands.Text = commandsText;
                        textBoxCommands.CaretIndex = PromptIndex;
                    }
                }
            }
            else
            {
                int changeLength = textBoxCommands.Text.Length - commandsText.Length;

                if ((textBoxCommands.CaretIndex - changeLength) < PromptIndex ||
                    textBoxCommands.CaretIndex < PromptIndex)
                {
                    // Change is to text before the prompt index - don't allow.
                    textBoxCommands.Text = commandsText;
                    textBoxCommands.CaretIndex = PromptIndex;
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
                string loadSynthResult = "";
                loadSynthResult = Speech.LoadSpeechSynthesizer();

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
        private string GetDemoText()
        {
            string demoText;

            demoText = "[...1....|....2....|....3....|....4....|....5....|....6....|....7....|....8....]\n" +
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

            demoText = demoText +
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

            return demoText;
        }

        private class RecognizerRule
        {
            public string Spoken = "";
            public string Text = "";
        }
    }
}
