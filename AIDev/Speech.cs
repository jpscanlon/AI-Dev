using System;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Synthesis;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace AIDev
{
    // Accessing MainWindow controls and properties with "((MainWindow)Application.Current.MainWindow)"
    // should be replaced by an MVVM view model bound to them.
    //
    // To test pronunciation, enter "speak <text to be spoken>".
    public sealed class Speech
    {
        private static readonly Lazy<Speech>
            lazy = new Lazy<Speech>(() => new Speech());

        public static Speech Instance { get { return lazy.Value; } }

        // Is there a way to shorten this reference?
        //Window mainWindow = (MainWindow) System.Windows.Application.Current.MainWindow;

        public static SpeechSynthesizer Synth;
        //public static SpeechRecognizer SpeechRec;
        public static SpeechRecognitionEngine RecEngine;

        private static List<SpokenRule> spokenRecRules;
        private static List<SpokenRule> spokenSynthRules;
        private static SrgsDocument document;

        private static List<string> choicesNotInRecRules = new List<string>();

        public static bool Speaking = false;
        private static bool OutputRecognitionData = true;

        private Speech()
        {
            // WHY DOESN'T THIS RUN AS THE CONSTRUCTOR?
            //if (OutputRecognitionData)
            //{
            //    File.WriteAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition log.txt",
            //            "");
            //}
        }

        public static string LoadSpeechRecognizer()
        {
            string error = "";

            //SpeechRec = new SpeechRecognizer();
            RecEngine = new SpeechRecognitionEngine();

            // Add a handler for the SpeechRecognized event.
            RecEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);
            //RecEngine.EmulateRecognizeCompleted += new EventHandler<EmulateRecognizeCompletedEventArgs>
            //    (Recognizer_EmulateRecognizeCompleted);

            //RecEngine.PauseRecognizerOnRecognition = true;

            TimeSpan timeSpan = new TimeSpan(0, 0, 0);
            RecEngine.EndSilenceTimeout = timeSpan;

            try
            {
                RecEngine.SetInputToDefaultAudioDevice();
            }
            catch (Exception e)
            {
                error = e.Message;
                MessageBox.Show("No audio input device found.");
                RecEngine.Dispose();
                RecEngine = null;
                ((MainWindow)Application.Current.MainWindow).checkBoxVoiceRecogition.IsChecked = false;
            }

            if (error == "")
            {
                LoadRegularGrammar();
                //LoadSRGSGrammar();
            }

            if (OutputRecognitionData)
            {
                File.WriteAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition log.txt", "");
            }

            return error;
        }

        private static void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (OutputRecognitionData)
            {
                string speechRecLogOutput = "";

                speechRecLogOutput = speechRecLogOutput + "Phrase recognized: " + e.Result.Text + "\r\n\tConfidence = " +
                    e.Result.Confidence + "\r\n";
                //e.Result.Homophones
                //e.Result.Words

                //speechRecLogOutput = speechRecLogOutput + "\tAlternates:\r\n";
                //foreach (RecognizedPhrase phrase in e.Result.Alternates)
                //{
                //    speechRecLogOutput = speechRecLogOutput + 
                //        "\t\tconfidence: " + phrase.Confidence + "\t" + phrase.Text + "\r\n";
                //}

                ((MainWindow)Application.Current.MainWindow).textBoxCommands.Focus();

                //speechRecLogOutput = speechRecLogOutput + "\tReplacementWordUnits:\r\n";
                //foreach (ReplacementText replacementWordUnit in e.Result.ReplacementWordUnits)
                //{
                //    speechRecLogOutput = speechRecLogOutput + "\t\t" + replacementWordUnit.Text + "\r\n";
                //}

                speechRecLogOutput = speechRecLogOutput + "\tWords:\r\n";
                foreach (RecognizedWordUnit Word in e.Result.Words)
                {
                    speechRecLogOutput = speechRecLogOutput + "\t\t" + Word.Text + "\r\n";
                    //ProcessRecognizedWord(Word.Text);
                }

                File.AppendAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition log.txt",
                    speechRecLogOutput);  // + "\r\n");

                ProcessRecognizedWord(e.Result.Text);

            }
        }

        private static void ProcessRecognizedWord(string recognizedWord)
        {
            string commandText = recognizedWord;
            string text = "";
            string logResult = "";

            switch (recognizedWord)
            {
                case "space":
                    commandText = " ";
                    break;
                case "dot":
                    commandText = ".";
                    break;
                case "end quote":
                    commandText = "\"";
                    break;
                default:
                    text = GetTextFromSpoken(recognizedWord);
                    if (text != "")
                    {
                        commandText = text;
                    }
                    else
                    {
                        commandText = recognizedWord;
                    }
                    break;
            }

            switch (commandText)
            {
                case "enter":
                    string commandsTrimmed = ((MainWindow)Application.Current.MainWindow).
                        textBoxCommands.Text.TrimEnd();
                    if (commandsTrimmed[commandsTrimmed.Length - 1] != ';')
                    {
                        ((MainWindow)Application.Current.MainWindow).textBoxCommands.AppendText(";");
                    }
                    ((MainWindow)Application.Current.MainWindow).ProcessStatements(((MainWindow)Application.Current.MainWindow).textBoxCommands.Text.Substring(((MainWindow)Application.Current.MainWindow).PromptIndex));
                    break;
                case "correction":
                    DeleteLastWord();
                    break;
                case "number":
                    // If not the first word.
                    if (((MainWindow)Application.Current.MainWindow).textBoxCommands.Text.Length >
                        ((MainWindow)Application.Current.MainWindow).PromptIndex)
                    {
                        // Add a space before the number.
                        ((MainWindow)Application.Current.MainWindow).textBoxCommands.AppendText(" ");
                        logResult = " ";
                    }
                    break;
                case ";":
                case "\"" when recognizedWord == "end quote":
                case ".":
                case " ":
                case "a" when recognizedWord != "a":
                case "b":
                case "c":
                case "d":
                case "e":
                case "f":
                case "g":
                case "h":
                case "i":
                case "j":
                case "k":
                case "l":
                case "m":
                case "n":
                case "o":
                case "p":
                case "q":
                case "r":
                case "s":
                case "t":
                case "u":
                case "v":
                case "w":
                case "x":
                case "y":
                case "z":
                // Single digit, when not a complete number.
                case "0" when recognizedWord != "zero":
                case "1" when recognizedWord != "one":
                case "2" when recognizedWord != "num two":
                case "3" when recognizedWord != "three":
                case "4" when recognizedWord != "num four":
                case "5" when recognizedWord != "five":
                case "6" when recognizedWord != "six":
                case "7" when recognizedWord != "seven":
                case "8" when recognizedWord != "eight":
                case "9" when recognizedWord != "nine":
                    // Append without a space.
                    ((MainWindow)Application.Current.MainWindow).textBoxCommands.
                        AppendText(commandText);
                    logResult = commandText;
                    break;
                default:
                    if (((MainWindow)Application.Current.MainWindow).
                    textBoxCommands.Text.Length > ((MainWindow)Application.Current.MainWindow)
                        .PromptIndex)  // If not at the beginning of commands.
                    {
                        if (((MainWindow)Application.Current.MainWindow).textBoxCommands.Text
                            [((MainWindow)Application.Current.MainWindow).
                            textBoxCommands.CaretIndex - 1] != '"')
                        {
                            // Doesn't directly follow a quote.
                            ((MainWindow)Application.Current.MainWindow).
                                textBoxCommands.AppendText(" ");  // Append after a space.
                            logResult = " ";
                        }
                    }
                    ((MainWindow)Application.Current.MainWindow).
                        textBoxCommands.AppendText(commandText);
                    logResult = logResult + commandText;
                    break;
            }

            if (commandText != "enter")
            {
                ((MainWindow)Application.Current.MainWindow).textBoxCommands.CaretIndex = ((MainWindow)Application.Current.MainWindow).textBoxCommands.Text.Length;
            }

            string speechRecLogOutput = "";
            speechRecLogOutput = speechRecLogOutput + "\tResult: [" + logResult + "]\r\n";
            File.AppendAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition log.txt",
                speechRecLogOutput);
        }

        private static string GetTextFromSpoken(string spoken)
        {
            string text = "";
            bool found = false;

            int rule = 0;
            while (rule < spokenRecRules.Count && !found)
            {
                if (spokenRecRules[rule].Spoken == spoken)
                {
                    text = spokenRecRules[rule].Text;
                    found = true;
                }
                rule++;
            }

            return text;
        }

        private static void LoadRegularGrammar()
        {
            Choices choices = new Choices();

            //if (recRules == null)
            //{
                spokenRecRules = new List<SpokenRule>();
                spokenSynthRules = new List<SpokenRule>();
            //}

            AddRecRules(ref choices);
            AddRecRulesFromLexRules(ref choices);
            AddRecRulesNATOAlpha(ref choices);

            GrammarBuilder grammarBuilder = new GrammarBuilder(choices);
            //GrammarBuilder grammarBuilder = new GrammarBuilder(choices, 1, 4);  // Recognizes <arg2> to <arg3> choices.

            Grammar grammar = new Grammar(grammarBuilder);
            RecEngine.LoadGrammar(grammar);

            WriteRecData(ref choices);

            //RecEngine.EmulateRecognizeAsync("test");
            //RecEngine.EmulateRecognizeCompleted();
        }

        private static void AddRecRules(ref Choices choices)
        {
            choices.Add("enter");
            choicesNotInRecRules.Add("enter");
            choices.Add("correction");
            choicesNotInRecRules.Add("correction");
            choices.Add("space");
            choicesNotInRecRules.Add("space");
            choices.Add("dot");
            choicesNotInRecRules.Add("dot");
            choices.Add("number");
            choicesNotInRecRules.Add("number");
            choices.Add("end quote");
            choicesNotInRecRules.Add("end quote");
            choices.Add("(");  // recognized by "open parenthesis"
            choicesNotInRecRules.Add("(");
            choices.Add(")");  // recognized by "close parenthesis"
            choicesNotInRecRules.Add(")");

            // KeyValuePair version of RecRules.
            //List<KeyValuePair<string, string>> kvRecRules = new List<KeyValuePair<string, string>>();
            //kvRecRules.Add(new KeyValuePair<string, string>("digit0", "0"));
            //foreach (var rule in kvRecRules)
            //{
            //    Console.WriteLine(rule);
            //}

            // Make these a higher recognition priority by putting them near the beginning of the list.
            //choices.Add("a");
            choices.Add("in");
            choices.Add("are");
            choices.Add("in");
            choices.Add("an");
            choices.Add("or");
            choices.Add("on");
            choices.Add("what");

            choices.Add("next statement");
            spokenRecRules.Add(new SpokenRule("next statement", ";"));

            // natural numbers
            choices.Add("zero");
            spokenRecRules.Add(new SpokenRule("zero", "0"));
            choices.Add("one");
            spokenRecRules.Add(new SpokenRule("one", "1"));
            choices.Add("num two");
            spokenRecRules.Add(new SpokenRule("num two", "2"));
            choices.Add("three");
            spokenRecRules.Add(new SpokenRule("three", "3"));
            choices.Add("num four");
            spokenRecRules.Add(new SpokenRule("num four", "4"));
            choices.Add("five");
            spokenRecRules.Add(new SpokenRule("five", "5"));
            choices.Add("six");
            spokenRecRules.Add(new SpokenRule("six", "6"));
            choices.Add("seven");
            spokenRecRules.Add(new SpokenRule("seven", "7"));
            choices.Add("eight");
            spokenRecRules.Add(new SpokenRule("eight", "8"));
            choices.Add("nine");
            spokenRecRules.Add(new SpokenRule("nine", "9"));
            choices.Add("ten");
            spokenRecRules.Add(new SpokenRule("ten", "10"));
            choices.Add("eleven");
            spokenRecRules.Add(new SpokenRule("eleven", "11"));
            choices.Add("one hundred");
            spokenRecRules.Add(new SpokenRule("one hundred", "100"));
            choices.Add("one thousand");
            spokenRecRules.Add(new SpokenRule("one thousand", "1000"));

            // Digits
            choices.Add("digit0");
            spokenRecRules.Add(new SpokenRule("digit0", "0"));
            choices.Add("digit1");
            spokenRecRules.Add(new SpokenRule("digit1", "1"));
            choices.Add("digit2");
            spokenRecRules.Add(new SpokenRule("digit2", "2"));
            choices.Add("digit3");
            spokenRecRules.Add(new SpokenRule("digit3", "3"));
            choices.Add("digit4");
            spokenRecRules.Add(new SpokenRule("digit4", "4"));
            choices.Add("digit5");
            spokenRecRules.Add(new SpokenRule("digit5", "5"));
            choices.Add("digit6");
            spokenRecRules.Add(new SpokenRule("digit6", "6"));
            choices.Add("digit7");
            spokenRecRules.Add(new SpokenRule("digit7", "7"));
            choices.Add("digit8");
            spokenRecRules.Add(new SpokenRule("digit8", "8"));
            choices.Add("digit9");
            spokenRecRules.Add(new SpokenRule("digit9", "9"));
        }

        private static void AddRecRulesFromLexRules(ref Choices choices)
        {
            string spokenRec;
            int item;

            for (int rule = 0; rule < LangBnf.LexRules.Count; rule++)
            {
                for (int clause = 0; clause < LangBnf.LexRules[rule].Clauses.Count; clause++)
                {
                    for (item = 0; item < LangBnf.LexRules[rule].Clauses[clause].Items.Count; item++)
                    {
                        if (LangBnf.LexRules[rule].Clauses[clause].Items[item].SpokenRec == null)
                        {
                            spokenRec = LangBnf.LexRules[rule].Clauses[clause].Items[item].Word;
                        }
                        else
                        {
                            spokenRec = LangBnf.LexRules[rule].Clauses[clause].Items[item].SpokenRec;
                        }

                        choices.Add(spokenRec);
                        spokenRecRules.Add(new SpokenRule(spokenRec,
                            LangBnf.LexRules[rule].Clauses[clause].Items[item].Word));
                    }
                }
            }
        }

        public static void AddSynthRulesFromLexRules()
        {
            int item;

            spokenSynthRules.Clear();

            for (int rule = 0; rule < LangBnf.LexRules.Count; rule++)
            {
                for (int clause = 0; clause < LangBnf.LexRules[rule].Clauses.Count; clause++)
                {
                    for (item = 0; item < LangBnf.LexRules[rule].Clauses[clause].Items.Count; item++)
                    {
                        if (LangBnf.LexRules[rule].Clauses[clause].Items[item].SpokenSynth != null)
                        {
                            spokenSynthRules.Add(new SpokenRule(
                            LangBnf.LexRules[rule].Clauses[clause].Items[item].SpokenSynth,
                            LangBnf.LexRules[rule].Clauses[clause].Items[item].Word));
                        }
                    }
                }
            }
        }

        private static void AddRecRulesNATOAlpha(ref Choices choices)
        {
            choices.Add("alpha");
            spokenRecRules.Add(new SpokenRule("alpha", "a"));
            choices.Add("bravo");
            spokenRecRules.Add(new SpokenRule("bravo", "b"));
            //recRules.Add(new RecognizerRule("bra voh", "b"));
            choices.Add("charlie");
            spokenRecRules.Add(new SpokenRule("charlie", "c"));
            choices.Add("delta");
            spokenRecRules.Add(new SpokenRule("delta", "d"));
            choices.Add("echo");
            spokenRecRules.Add(new SpokenRule("echo", "e"));
            choices.Add("foxtrot");
            spokenRecRules.Add(new SpokenRule("foxtrot", "f"));
            choices.Add("golf");
            spokenRecRules.Add(new SpokenRule("golf", "g"));
            choices.Add("hotel");
            spokenRecRules.Add(new SpokenRule("hotel", "h"));
            choices.Add("india");
            spokenRecRules.Add(new SpokenRule("india", "i"));
            choices.Add("juliett");
            spokenRecRules.Add(new SpokenRule("juliett", "j"));
            choices.Add("kilo");
            spokenRecRules.Add(new SpokenRule("kilo", "k"));
            choices.Add("lima");
            spokenRecRules.Add(new SpokenRule("lima", "l"));
            choices.Add("mike");
            spokenRecRules.Add(new SpokenRule("mike", "m"));
            choices.Add("november");
            spokenRecRules.Add(new SpokenRule("november", "n"));
            choices.Add("oscar");
            spokenRecRules.Add(new SpokenRule("oscar", "o"));
            choices.Add("papa");
            spokenRecRules.Add(new SpokenRule("papa", "p"));
            choices.Add("quebec");
            spokenRecRules.Add(new SpokenRule("quebec", "q"));
            choices.Add("romeo");
            spokenRecRules.Add(new SpokenRule("romeo", "r"));
            choices.Add("sierra");
            spokenRecRules.Add(new SpokenRule("sierra", "s"));
            choices.Add("tango");
            spokenRecRules.Add(new SpokenRule("tango", "t"));
            choices.Add("uniform");
            spokenRecRules.Add(new SpokenRule("uniform", "u"));
            choices.Add("victor");
            spokenRecRules.Add(new SpokenRule("victor", "v"));
            choices.Add("whiskey");
            spokenRecRules.Add(new SpokenRule("whiskey", "w"));
            choices.Add("x-ray");
            spokenRecRules.Add(new SpokenRule("x-ray", "x"));
            choices.Add("yankee");
            spokenRecRules.Add(new SpokenRule("yankee", "y"));
            choices.Add("zulu");
            spokenRecRules.Add(new SpokenRule("zulu", "z"));
        }

        private static void LoadSRGSGrammar()
        {
            //builder = new GrammarBuilder(Choices);
            //document = new SrgsDocument(builder);
            //grammar = new Grammar(document);

            // Create an SrgsDocument, create a new rule and set its scope to public.  
            document = new SrgsDocument();
            document.PhoneticAlphabet = SrgsPhoneticAlphabet.Ups;
            //document.PhoneticAlphabet = SrgsPhoneticAlphabet.Sapi;

            //SrgsRule wordRule = (new SrgsRule("word", new SrgsElement[] { oneOfWord }));
            SrgsRule wordRule = new SrgsRule("word");
            wordRule.Scope = SrgsRuleScope.Public;

            SrgsOneOf oneOfWord = new SrgsOneOf(new SrgsItem[] {new SrgsItem("aardvark"),
                new SrgsItem("beaver"), new SrgsItem("cheetah")});

            SrgsItem wordItem = new SrgsItem();
            SrgsToken token = new SrgsToken("whatchamacallit");
            token.Pronunciation = "W AE T CH AE M AE K AA L IH T";
            wordItem.Add(token);
            //oneOfWord = new SrgsOneOf();
            oneOfWord.Add(wordItem);

            wordRule.Add(oneOfWord);

            //// Create the rule from the SrgsOneOf objects.
            //SrgsRule slangRule = new SrgsRule("slang", wordItem);
            //// Build an SrgsDocument object from the rule and set the phonetic alphabet.
            //SrgsDocument tokenPron = new SrgsDocument(slangRule);

            //// Create a Grammar object from the SrgsDocument and load it to the recognizer.  
            //Grammar slangGrammar = new Grammar(tokenPron);
            //slangGrammar.Name = ("Slang Pronunciation");
            //RecEngine.LoadGrammarAsync(slangGrammar);

            //// Add references to winnerRule for ruleEurope.  
            //wordRule.Elements.Add(new SrgsOneOf(new SrgsItem[] {(new SrgsItem (new SrgsRuleRef(ruleEurope)))}));

            // Add all the rules to the document and set the root rule of the document.  
            document.Rules.Add(new SrgsRule[] { wordRule });
            document.Root = wordRule;

            Grammar grammar = new Grammar(document);
            RecEngine.LoadGrammar(grammar);
        }

        private static void DeleteLastWord()
        {
            if (((MainWindow)Application.Current.MainWindow).textBoxCommands.Text.Length > ((MainWindow)Application.Current.MainWindow).PromptIndex)
            {
                int position = ((MainWindow)Application.Current.MainWindow).
                    textBoxCommands.Text.Length - 1;
                while (((MainWindow)Application.Current.MainWindow).textBoxCommands.Text[position] != 
                    ' ' && position > ((MainWindow)Application.Current.MainWindow).PromptIndex)
                {
                    position--;
                }

                ((MainWindow)Application.Current.MainWindow).textBoxCommands.Text = ((MainWindow)Application.Current.MainWindow).textBoxCommands.Text.Substring(0, position);
                ((MainWindow)Application.Current.MainWindow).textBoxCommands.CaretIndex = ((MainWindow)Application.Current.MainWindow).textBoxCommands.Text.Length;
            }
        }

        private static void Recognizer_EmulateRecognizeCompleted(object sender, SpeechRecognizedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static string LoadSpeechSynthesizer()
        {
            string error = "";

            try
            {
                Synth = new SpeechSynthesizer
                {
                    Volume = 100,  // 0...100
                                   //Rate = -2     // -10...10
                };

                //Synth.SetOutputToDefaultAudioDevice();
                Synth.SelectVoice(Properties.Settings.Default.ttsVoice);
                Synth.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(Synth_SpeakCompleted);
            }
            catch(Exception e)
            {
                error = e.Message;
                MessageBox.Show("Could not load speech synthesizer: " + error);
            }

            WriteVoices();  // Write list of installed voices to text file.

            if (error != "")
            {
                AddSynthRulesFromLexRules();
                //Speak("Hello, I am completely operational, and all my circuits are functioning perfectly.");
                Speak("Voice on.");
            }

            return error;
        }

        public static void SelectVoice(string voice)
        {
            switch (voice)
            {
                case "David":
                    Properties.Settings.Default.ttsVoice = "Microsoft David Desktop";
                    break;
                case "Zira":
                    Properties.Settings.Default.ttsVoice = "Microsoft Zira Desktop";
                    break;
                case "Hazel":
                    //Properties.Settings.Default.ttsVoice = "Microsoft Hazel Desktop";
                    Properties.Settings.Default.ttsVoice = 
                        "Microsoft Server Speech Text to Speech Voice (en-GB, Hazel)";
                    break;
                case "Helen":
                    Properties.Settings.Default.ttsVoice =
                        "Microsoft Server Speech Text to Speech Voice (en-US, Helen)";
                    break;
                case "Hayley":
                    Properties.Settings.Default.ttsVoice = 
                        "Microsoft Server Speech Text to Speech Voice (en-AU, Hayley)";
                    break;
            }

            //Properties.Settings.Default.ttsVoice = "eSpeak - en";  // This voice won't load.
            Properties.Settings.Default.Save();
        }

        public static void Speak(string speechText = "")
        {
            if (RecEngine != null)
            {
                if (((MainWindow)Application.Current.MainWindow).checkBoxVoiceRecogition.IsChecked == true)
                {
                    // Turn off voice recognition while speaking. Turn back on in SpeakCompleted event.
                    RecEngine.RecognizeAsyncCancel();
                }
            }

            speechText = AddPronunciation(speechText);
            Speaking = true;
            Synth.SpeakAsync(speechText);
        }

        public static string AddPronunciation(string jillianText)
        {
            // Don't speak semicolon. Replace with period to break between statements.
            jillianText = jillianText.Replace(";", ". ");
            // Don't speak parentheses
            jillianText = jillianText.Replace("(", " ");
            jillianText = jillianText.Replace(")", " ");
            jillianText = jillianText.Replace("okay", "O K");

            //jillianText = jillianText.Replace("error", "err-or");

            //jillianText = jillianText.Replace("hermione", "her MY oh-nee");

            if (spokenSynthRules != null)
            {
                //File.AppendAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition log.txt", 
                //    "\r\nPronunciations:\r\n");
                for (int pronunciation = 0; pronunciation < spokenSynthRules.Count; pronunciation++)
                {
                    jillianText = jillianText.Replace(spokenSynthRules[pronunciation].Text,
                        spokenSynthRules[pronunciation].Spoken);
                    //File.AppendAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition log.txt",
                    //    pronunciations[pronunciation].Text + " : " + pronunciations[pronunciation].Spoken + "\r\n");
                }
            }

            return jillianText;
        }

        private static void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            Speaking = false;

            if (RecEngine != null)
            {
                if (((MainWindow)Application.Current.MainWindow).checkBoxVoiceRecogition.IsChecked == true)
                {
                    RecEngine.RecognizeAsync(RecognizeMode.Multiple);
                }
            }
        }

        private static void WriteRecData(ref Choices choices)
        {
            File.WriteAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition rules.txt", "");

            string choicesNotInRecRulesContents = "Speech Recognition Choices Not in recRules\r\n" +
                "----------------------------------------------------------\r\n";
            foreach (string choice in choicesNotInRecRules)
            {
                choicesNotInRecRulesContents = choicesNotInRecRulesContents +
                    "[" + choice + "]\r\n";
            }

            File.AppendAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition rules.txt",
                choicesNotInRecRulesContents + "\r\n");
            int padWidth = 26;
            string spoken;

            string recRulesContents = "Speech Recognition Rules\r\nSpoken:                    Text:\r\n" +
                "----------------------------------------------------------\r\n";

            //for (int rule = 0; rule < recRules.Count; rule++)
            foreach (SpokenRule recRule in spokenRecRules)
            {
                spoken = recRule.Spoken + "]";
                spoken = spoken.PadRight(padWidth);

                recRulesContents = recRulesContents + "[" + spoken +
                    "[" + recRule.Text + "]\r\n";
            }

            File.AppendAllText(AppProperties.AIDevDataFolderPath + @"\speech recognition rules.txt",
                recRulesContents + "\r\n");
        }

        // Outputs information about all of the installed voices.   
        private static void WriteVoices()
        {
            if (Synth != null)
            {
                string logOutput = "Installed Voices\r\n\r\n";
                foreach (InstalledVoice voice in Synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    string AudioFormats = "";
                    foreach (SpeechAudioFormatInfo fmt in info.SupportedAudioFormats)
                    {
                        AudioFormats += string.Format("{0}\n",
                        fmt.EncodingFormat.ToString());
                    }

                    logOutput = logOutput + " Name:          " + info.Name + "\r\n";
                    logOutput = logOutput + " Culture:       " + info.Culture + "\r\n";
                    logOutput = logOutput + " Age:           " + info.Age + "\r\n";
                    logOutput = logOutput + " Gender:        " + info.Gender + "\r\n";
                    logOutput = logOutput + " Description:   " + info.Description + "\r\n";
                    logOutput = logOutput + " ID:            " + info.Id + "\r\n";
                    logOutput = logOutput + " Enabled:       " + voice.Enabled + "\r\n";

                    if (info.SupportedAudioFormats.Count != 0)
                    {
                        logOutput = logOutput + " Audio formats: " + AudioFormats + "\r\n";
                    }
                    else
                    {
                        logOutput = logOutput + " No supported audio formats found" + "\r\n";
                    }

                    string AdditionalInfo = "";
                    foreach (string key in info.AdditionalInfo.Keys)
                    {
                        AdditionalInfo += string.Format("  {0}: {1}\n", key, info.AdditionalInfo[key]);
                    }

                    logOutput = logOutput + " Additional Info - " + AdditionalInfo + "\r\n\r\n";
                }

                File.WriteAllText(AppProperties.AIDevDataFolderPath + @"\speech voices info.txt",
                    logOutput + "\r\n");
            }
        }

        private class SpokenRule
        {
            public string Spoken = "";
            public string Text = "";

            public SpokenRule(string spoken, string text)
            {
                Spoken = spoken;
                Text = text;
            }
        }
    }
}
