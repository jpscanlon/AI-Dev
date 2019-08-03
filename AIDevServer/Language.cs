using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AIDevServer
{
    static class Language
    {
        private static string debugOutput;

        private static bool returnParseResults = false;
        private static bool debugOn = true;  // indicates whether to output debugging info

        private static List<Token> parseTreeTokens;
        private static int numParseTreeTokens;
        private static int parseTreeRoot;
        private static List<Token> astTokens;
        private static int astRoot;
        private static bool startNewAstPass;

        // topTokens holds indexes of all top-level tokens being parsed
        private static List<int> topTokens;
        private static int topTokensLen;

        private static string stmtsStripped;
        private static List<Stmt> astStmts;

        private static LangKnowledge langKnowledge;

        private static readonly SqlConnection KBConnection = Knowledgebase.KBConnection;

        public static void LoadLang()
        {
            LangBnf.LoadLangRules();
            LangBnf.LoadLexRules();
            LoadUDWords();
            langKnowledge = LoadLangKnowledge();
        }

        //"I am completely operational, and all my circuits are functioning perfectly."
        //"I don't want to insist on it, Dave, but I am incapable of making an error."
        //"I'm sorry, Dave. I'm afraid I can't do that."
        //"Just what do you think you're doing, Dave? Dave, " +
        //  "I really think I'm entitled to an answer to that question."
        //"I know everything hasn't been quite right with me, but I can assure you now, very confidently, " +
        //  "that it's going to be all right again. I feel much better now. I really do."
        public static string Execute(string statements)
        {
            string errorMsg = "";
            string response = "";
            string strippedStmts;

            if (LangBnf.LangRules == null || LangBnf.LexRules == null || langKnowledge == null)
            {
                LoadLang();
            }

            strippedStmts = StripWhiteSpace(statements.ToLower());
            errorMsg = ParseStmts(strippedStmts);

            if (errorMsg == "")
            {
                // Traverse parse tree and execute statements.
                GenerateAst();

                string semanticResponse = ProcessSemantics();

                if (debugOn)
                {
                    debugOutput = debugOutput + GetParseTreeDescription() + "\r\n\r\n";
                    debugOutput = debugOutput + GetParseTreeDescription(true) + "\r\n";
                    File.AppendAllText(AppProperties.ServerLogPath, "\r\n" + debugOutput);
                }

                if (returnParseResults)
                {
                    response = "Parse succeeded.\r\n\r\n" + GetParseTreeDescription() + "\r\n\r\n" +
                        GetParseTreeDescription(true);
                }

                if (semanticResponse == "")
                {
                    response = response + "okay;\r\n";
                }
                else
                {
                    response = response + semanticResponse;
                }
            }
            else
            {
                //response = "I'm sorry, Dave. I'm afraid I can't do that.\r\n";
                response = errorMsg + "\r\n";
                if (debugOn)
                {
                    File.AppendAllText(AppProperties.ServerLogPath, debugOutput + 
                        errorMsg + "\r\n");
                }
            }
            // Save non-temporary linguistic knowledge to file.
            SaveLangKnowledge();

            return response;
        }

        private static string ProcessSemantics()
        {
            astStmts = new List<Stmt>();
            string response = "";

            // Add statements in AST to astStatements.
            for (int child = 0; child < astTokens[astRoot].Children.Count; child++)
            {
                Stmt stmt = new Stmt();
                GetStmt(ref stmt, astTokens[astRoot].Children[child]);
                astStmts.Add(stmt);
            }

            // Process statements in order.
            for (int stmtNum = 0; stmtNum < astStmts.Count; stmtNum++)
            {
                switch (astStmts[stmtNum].Type)
                {
                    case "decl_stmt":
                        AddLangKnowledgeStmt(astStmts[stmtNum].ASTNode);
                        break;
                    case "query_stmt":
                        response = GetQueryResponse(astStmts[stmtNum].Query) + "\r\n";
                        break;
                    case "imperative_stmt":
                        response = GetImperativeResponse(astStmts[stmtNum].ImperStmt) + "\r\n";
                        break;
                    case "langdef_stmt":
                        response = ProcessStmtLangDef(astTokens, astStmts[stmtNum].ASTNode);
                        break;
                    case "newword_stmt":
                        response = ProcessStmtNewWord(astTokens, astStmts[stmtNum].ASTNode);
                        if (response == "") response = "RefreshUDWords";
                        break;
                    case "pronounce_stmt":
                        response = ProcessStmtPronounce(astTokens, astStmts[stmtNum].ASTNode);
                        if (response == "") response = "RefreshUDWords";
                        break;
                    case "deleteword_stmt":
                        response = ProcessStmtDeleteWord(astTokens, astStmts[stmtNum].ASTNode);
                        if (response == "") response = "RefreshUDWords";
                        break;
                    default:
                        break;
                }
            }

            return response.Trim();
        }

        private static string ProcessStmtLangDef(List<Token> astTokens, int stmtRoot)
        {
            int node = stmtRoot;
            int stmtChild;
            string response = "";

            if (astTokens[node].Children.Count > 1)
            {
                // Get the definition for the given word.
                stmtChild = astTokens[node].Children[1];
                string word = astTokens[stmtChild].Literal;

                int rule = -1;
                int clause = -1;
                int item = -1;
                if (LangBnf.FindWordInBNF(word, ref rule, ref clause, ref item) == true)
                {
                    //string type = LangBNF.LexRules[rule].Token;
                    response = response +
                        "Type: " + LangBnf.LexRules[rule].Token +
                       ", UD: " + LangBnf.LexRules[rule].Clauses[clause].UserDefined +
                        ", Temp: " + LangBnf.LexRules[rule].Clauses[clause].Temp + "\r\n" +
                        LangBnf.LexRules[rule].Clauses[clause].Items[0].Word + 
                        " SpokenRec: \"" +
                        LangBnf.LexRules[rule].Clauses[clause].Items[0].SpokenRec + 
                        "\", SpokenSynth: \"" +
                        LangBnf.LexRules[rule].Clauses[clause].Items[0].SpokenSynth + "\"\r\n";
                    for (int itemNum = 1; 
                        itemNum < LangBnf.LexRules[rule].Clauses[clause].Items.Count; itemNum++)
                    {
                        response = response +
                            LangBnf.LexRules[rule].Clauses[clause].Items[itemNum].Word +
                        " SpokenRec: \"" +
                        LangBnf.LexRules[rule].Clauses[clause].Items[itemNum].SpokenRec +
                        "\", SpokenSynth: \"" +
                        LangBnf.LexRules[rule].Clauses[clause].Items[itemNum].SpokenSynth + "\"\r\n";

                    }
                }
                else
                {
                    response = "Word not found.";
                }
            }
            else
            {
                // Get the whole language definition.
                response = GetLanguageDefinition(true);
                File.WriteAllText(AppProperties.AIDevDataFolderPath + @"\Language Definition.txt",
                    response + "\r\n");
            }

            return response;
        }

        private static string ProcessStmtNewWord(List<Token> astTokens, int stmtRoot)
        {
            string response = "";
            int node = stmtRoot;
            int stmtChild;
            bool wordExists = false;

            stmtChild = astTokens[node].Children[1];
            int paramsStart = 1;
            bool temp = false;
            if (astTokens[stmtChild].Name == "temp")
            {
                temp = true;
                paramsStart = 2;
            }

            string type = astTokens[astTokens[node].Children[paramsStart]].Literal;
            string word = astTokens[astTokens[node].Children[paramsStart + 1]].Literal;

            string noun_plural = null;
            string verb_singular = null;
            string verb_past = null;

            string duplicateWord = "";

            stmtChild = astTokens[node].Children[paramsStart];
            switch (astTokens[stmtChild].Literal)
            {
                case "classnoun":
                    noun_plural =
                        astTokens[astTokens[node].Children[paramsStart + 2]].Literal;
                    if (LangBnf.WordExistsInBnf(noun_plural))
                    {
                        wordExists = true;
                        duplicateWord = noun_plural;
                    }
                    break;
                case "intransverb":
                case "transverb":
                    verb_singular =
                        astTokens[astTokens[node].Children[paramsStart + 2]].Literal;
                    if (LangBnf.WordExistsInBnf(verb_singular))
                    {
                        wordExists = true;
                        duplicateWord = verb_singular;
                    }
                    verb_past =
                        astTokens[astTokens[node].Children[paramsStart + 3]].Literal;
                    if (LangBnf.WordExistsInBnf(verb_past))
                    {
                        wordExists = true;
                        duplicateWord = verb_past;
                    }
                    break;
                default:
                    break;
            }

            if (LangBnf.WordExistsInBnf(word))
            {
                wordExists = true;
                duplicateWord = word;
            }

            if (!wordExists)
            {
                AddWordBNF(temp, type, word, null, null,
                    noun_plural, null, null,
                    verb_singular, null, null,
                    verb_past, null, null);
                // If this is not a temporary word, add it to the knowledgebase.
                if (!temp)
                {
                    AddWordKB(type, word, null, null,
                        noun_plural, null, null,
                        verb_singular, null, null,
                        verb_past, null, null);
                }
            }
            else
            {
                response = "The word \"" + duplicateWord + "\" already exists.";
            }

            return response;
        }

        private static string ProcessStmtDeleteWord(List<Token> astTokens, int stmtRoot)
        {
            string response = "";
            int node = stmtRoot;

            string word = astTokens[astTokens[node].Children[1]].Literal;

            LangBnf.WordLocationInBnf wordLocation = LangBnf.FindWordInBNF(word);

            if (wordLocation.Rule != -1)
            {
                //string type = LangBNF.LexRules[wordLocation.Rule].Token;
                //if (type == "adj" || type == "disc_obj_noun" || type == "non_disc_obj_noun" ||
                //    type == "class_noun" || type == "intrans_verb" || type == "trans_verb")
                if (LangBnf.LexRules[wordLocation.Rule].Clauses[wordLocation.Clause].UserDefined)
                {
                    LangBnf.WordForm wordForm = GetWordForm(word);
                    LangBnf.LexRules[wordLocation.Rule].Clauses.RemoveAt(wordLocation.Clause);
                    DeleteWordKB(word, wordForm);
                }
                else
                {
                    response = "\"" + word + "\" is not a user-defined word.";
                }
            }
            else
            {
                response = "The word \"" + word + "\" doesn't exist.";
            }

            return response;
        }

        private static void DeleteWordKB(string word, LangBnf.WordForm wordForm)
        {
            string wordColumn = "";

            switch (wordForm)
            {
                case LangBnf.WordForm.Base:
                    wordColumn = "base";
                    break;
                case LangBnf.WordForm.NounPlural:
                    wordColumn = "noun_plural";
                    break;
                case LangBnf.WordForm.VerbSingular:
                    wordColumn = "verb_singular";
                    break;
                case LangBnf.WordForm.VerbPast:
                    wordColumn = "verb_past";
                    break;
            }

            try
            {
                string query = "DELETE FROM ud_word " + "WHERE " + wordColumn + " = @word";
                SqlCommand sql = new SqlCommand(query, KBConnection);
                sql.Parameters.AddWithValue("@word", word);
                sql.ExecuteNonQuery();
            }
            catch (SqlException error)
            {
                Console.WriteLine("SQL Server Error, " + error.Message);

                // FIX LOG WRITING SO THIS IS WRITTEN IN ORDER, AFTER PARSING.
                File.AppendAllText(AppProperties.ServerLogPath, "*******************************************\r\n" +
                    "SQL Error, " + error.Message + "\r\n" + "*******************************************\r\n");
            }
        }

        private static string ProcessStmtPronounce(List<Token> astTokens, int stmtRoot)
        {
            string response = "";
            int node = stmtRoot;
            int stmtChild;

            //   <pronounce_stmt>
            //      <pronounce>
            //         "pronounce"
            //      <string>
            //         "controls"
            //      <spokenrec>
            //         "spokenrec"
            //      <string>
            //         "kon-trolls"
            //      <spokensynth>
            //         "spokensynth"
            //      <string>
            //         "kon-trolls"

            string word = astTokens[astTokens[node].Children[1]].Literal;
            if (LangBnf.WordExistsInBnf(word))
            {
                string spokenRec = null;
                string spokenSynth = null;

                stmtChild = astTokens[node].Children[2];

                // First argument will be either spokenrec or spokensynth.
                if (astTokens[astTokens[node].Children[2]].Name == "spokenrec")
                {
                    spokenRec = astTokens[astTokens[node].Children[3]].Literal;
                }
                else
                {
                    spokenSynth = astTokens[astTokens[node].Children[3]].Literal;
                }

                // If there is a second argument, it will be spokensynth.
                if (astTokens[node].Children.Count > 4)
                {
                    spokenSynth = astTokens[astTokens[node].Children[5]].Literal;
                }

                UpdateSpokenBNF(word, spokenRec, spokenSynth);
                UpdateSpokenKB(word, spokenRec, spokenSynth);
            }
            else
            {
                response = "Word not found.";
            }

            return response;
        }

        private static void UpdateSpokenBNF(string word, string spokenRec, string spokenSynth)
        {
            LangBnf.WordLocationInBnf wordLocation = LangBnf.FindWordInBNF(word);

            if (wordLocation.Rule != -1)
            {
                // Don't update if parameter is null.
                if (spokenRec != null)
                {
                    // If parameter is an empty string, update to null.
                    if (spokenRec == "") spokenRec = null;
                    LangBnf.LexRules[wordLocation.Rule].Clauses[wordLocation.Clause].
                        Items[wordLocation.Item].SpokenRec = spokenRec;
                }

                if (spokenSynth != null)
                {
                    if (spokenSynth == "") spokenSynth = null;
                    LangBnf.LexRules[wordLocation.Rule].Clauses[wordLocation.Clause].
                        Items[wordLocation.Item].SpokenSynth = spokenSynth;
                }
            }
        }

        private static void UpdateSpokenKB(string word, string spokenRec, string spokenSynth)
        {
            //wordForm = LangBNF.WordForm.Base;
            //wordForm = LangBNF.WordForm.NounPlural;
            //wordForm = LangBNF.WordForm.VerbSingular;
            //wordForm = LangBNF.WordForm.VerbPast;

            if (!(spokenRec == null && spokenSynth == null))
            {
                string wordColumn = "";
                string spokenRecColumn = "";
                string spokenSynthColumn = "";

                //UPDATE ud_word
                //SET base_spoken_rec = @spoken_rec,
                //    base_spoken_synth = @spoken_synth
                //WHERE base = @base

                LangBnf.WordForm wordForm = GetWordForm(word);

                switch (wordForm)
                {
                    case LangBnf.WordForm.Base:
                        wordColumn = "base";
                        spokenRecColumn = "base_spoken_rec";
                        spokenSynthColumn = "base_spoken_synth";
                        break;
                    case LangBnf.WordForm.NounPlural:
                        wordColumn = "noun_plural";
                        spokenRecColumn = "noun_plural_spoken_rec";
                        spokenSynthColumn = "noun_plural_spoken_synth";
                        break;
                    case LangBnf.WordForm.VerbSingular:
                        wordColumn = "verb_singular";
                        spokenRecColumn = "verb_singular_spoken_rec";
                        spokenSynthColumn = "verb_singular_spoken_synth";
                        break;
                    case LangBnf.WordForm.VerbPast:
                        wordColumn = "verb_past";
                        spokenRecColumn = "verb_past_spoken_rec";
                        spokenSynthColumn = "verb_past_spoken_synth";
                        break;
                }

                try
                {
                    string query = "UPDATE ud_word " +
                        "SET ";

                    if (spokenRec == null)
                    {
                        query = query + spokenSynthColumn + " = @spoken_synth ";
                    }
                    else if (spokenSynth == null)
                    {
                        query = query + spokenRecColumn + " = @spoken_rec ";
                    }
                    else
                    {
                        query = query +
                        spokenRecColumn + " = @spoken_rec, " +
                        spokenSynthColumn + " = @spoken_synth ";
                    }

                    query = query + "WHERE " + wordColumn + " = @word";

                    SqlCommand sql = new SqlCommand(query, KBConnection);

                    // Pass values to Parameters, setting empty strings to database null values.
                    sql.Parameters.AddWithValue("@word", word);
                    sql.Parameters.AddWithValue("@spoken_rec", (spokenRec == "") ?
                        (object)DBNull.Value : spokenRec);
                    //sql.Parameters.AddWithValue("@spoken_synth", (spokenSynth == "") ?
                    //    (object)DBNull.Value : spokenSynth);

                    // Version that explicitly declares parameter types.
                    //sql.Parameters.Add("@spoken_rec", SqlDbType.NVarChar);
                    //sql.Parameters["@spoken_rec"].Value = (spokenRec == "") ?
                    //    (object)DBNull.Value : spokenRec;
                    //sql.Parameters.Add("@spoken_synth", SqlDbType.NVarChar);
                    //sql.Parameters["@spoken_synth"].Value = (spokenSynth == "") ?
                    //    (object)DBNull.Value : spokenSynth;

                    sql.ExecuteNonQuery();
                }
                catch (SqlException error)
                {
                    Console.WriteLine("SQL Server Error, " + error.Message);

                    // FIX LOG WRITING SO THIS IS WRITTEN IN ORDER, AFTER THE PARSING.
                    File.AppendAllText(AppProperties.ServerLogPath, "*******************************************\r\n" + 
                        "SQL Error, " + error.Message + "\r\n" + "*******************************************\r\n");
                }
            }
        }

        private static void AddWordBNF(bool temp, string type, 
            string word, string wordSpokenRec, string wordSpokenSynth,
            string nounPlural, string nounPluralSpokenRec, string nounPluralSpokenSynth,
            string verbSingular, string verbSingularSpokenRec, string verbSingularSpokenSynth,
            string verbPast, string verbPastSpokenRec, string verbPastSpokenSynth)
        {
            type = LangBnf.GetBnfType(type);

            int rule = LangBnf.LexRules.FindIndex(
                    lexRule => lexRule.Token == type);
            LangBnf.LexRules[rule].Clauses.Add(new LangBnf.LexClause(true, temp));
            int clause = LangBnf.LexRules[rule].Clauses.Count - 1;
            LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                new LangBnf.LexItem(word, wordSpokenRec,
                wordSpokenSynth));

            switch (type)
            {
                case "class_noun":
                    LangBnf.LexRules[rule].Clauses[clause].Items.Add(new LangBnf.LexItem(
                        nounPlural, nounPluralSpokenRec, nounPluralSpokenSynth));
                    break;
                case "intrans_verb":
                case "trans_verb":
                    LangBnf.LexRules[rule].Clauses[clause].Items.Add(new LangBnf.LexItem(
                        verbSingular, verbSingularSpokenRec, verbSingularSpokenSynth));
                    LangBnf.LexRules[rule].Clauses[clause].Items.Add(new LangBnf.LexItem(
                        verbPast, verbPastSpokenRec, verbPastSpokenSynth));
                    break;
                default:
                    break;
            }
        }

        private static void AddWordKB(string type,
            string baseForm, string baseSpokenRec, string baseSpokenSynth,
            string nounPlural, string nounPluralSpokenRec, string nounPluralSpokenSynth,
            string verbSingular, string verbSingularSpokenRec, string verbSingularSpokenSynth,
            string verbPast, string verbPastSpokenRec, string verbPastSpokenSynth)
        {
            try
            {
                // Replace Parameters with Values
                string query = "INSERT INTO ud_word (type, base, base_spoken_rec, base_spoken_synth, " +
                    "noun_plural, noun_plural_spoken_rec, noun_plural_spoken_synth, " +
                    "verb_singular, verb_singular_spoken_rec, verb_singular_spoken_synth, " +
                    "verb_past, verb_past_spoken_rec, verb_past_spoken_synth) VALUES(" +
                    "@type, @base, @base_spoken_rec, @base_spoken_synth, " +
                    "@noun_plural, @noun_plural_spoken_rec, @noun_plural_spoken_synth, " +
                    "@verb_singular, @verb_singular_spoken_rec, @verb_singular_spoken_synth, " +
                    "@verb_past, @verb_past_spoken_rec, @verb_past_spoken_synth)";

                SqlCommand sql = new SqlCommand(query, KBConnection);

                // Pass values to Parameters
                // Setting any empty and null strings to database null values.
                sql.Parameters.AddWithValue("@type", type);
                sql.Parameters.AddWithValue("@base", baseForm);
                sql.Parameters.AddWithValue("@base_spoken_rec", !string.IsNullOrEmpty(
                    baseSpokenRec) ? baseSpokenRec : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@base_spoken_synth", !string.IsNullOrEmpty(
                    baseSpokenSynth) ? baseSpokenSynth : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@noun_plural", !string.IsNullOrEmpty
                    (nounPlural) ? nounPlural : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@noun_plural_spoken_rec", !string.IsNullOrEmpty(
                    nounPluralSpokenRec) ? nounPluralSpokenRec : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@noun_plural_spoken_synth", !string.IsNullOrEmpty(
                    nounPluralSpokenSynth) ? nounPluralSpokenSynth : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@verb_singular", !string.IsNullOrEmpty(
                    verbSingular) ? verbSingular : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@verb_singular_spoken_rec", !string.IsNullOrEmpty(
                    verbSingularSpokenRec) ? verbSingularSpokenRec : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@verb_singular_spoken_synth", !string.IsNullOrEmpty(
                    verbSingularSpokenSynth) ? verbSingularSpokenSynth : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@verb_past", !string.IsNullOrEmpty(
                    verbPast) ? verbPast : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@verb_past_spoken_rec", !string.IsNullOrEmpty(
                    verbPastSpokenRec) ? verbPastSpokenRec : (object)DBNull.Value);
                sql.Parameters.AddWithValue("@verb_past_spoken_synth", !string.IsNullOrEmpty(
                    verbPastSpokenSynth) ? verbPastSpokenSynth : (object)DBNull.Value);

                sql.ExecuteNonQuery();
            }
            catch (SqlException error)
            {
                Console.WriteLine("SQL Server Error, " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Error, " + error.Message + "\r\n");
            }
        }

        private static void AddLangKnowledgeStmt(int node)
        {
            List<Token> stmtTreeTokens = GetTokenSubTreeCopy(astTokens, node);
            const int subTreeRoot = 0;

            if (stmtTreeTokens[stmtTreeTokens[subTreeRoot].Children[0]].Name == "remember")
            {
                // Remove "remember" from the knowledge statement tree.
                RemoveNode(stmtTreeTokens, stmtTreeTokens[subTreeRoot].Children[0], 
                    0);
                stmtTreeTokens = GetTokenSubTreeCopy(stmtTreeTokens, 0);
                langKnowledge.AddStmt(stmtTreeTokens, false);
            }
            else
            {
                langKnowledge.AddStmt(stmtTreeTokens, true);
            }
        }

        private static void SaveLangKnowledge()
        {
            // Make a copy of langknowledge, remove all temporary items, then save it.
            LangKnowledge langKnowledgeClone = (LangKnowledge)CloneObject(langKnowledge);
            langKnowledgeClone.RemoveTempItems();
            Common.SerializeToFile(langKnowledgeClone, AppProperties.KBFolderPath +
                    @"\langknowledge.bin");
        }

        private static LangKnowledge LoadLangKnowledge()
        {
            LangKnowledge langKnowledge;

            langKnowledge = (LangKnowledge)Common.DeserializeFromFile(
                AppProperties.KBFolderPath + @"\langknowledge.bin");

            if (langKnowledge == null)
            {
                // File couldn't be loaded. Create a new langKnowledge.
                langKnowledge = new LangKnowledge();
            }

            return langKnowledge;
        }

        private static string GetQueryResponse(QueryStmt queryStmt)
        {
            string response = "unknown;";
            
            switch (queryStmt.Type)
            {
                case "what":
                    response = GetAnswerToWhat(queryStmt.SubjectNode);
                    break;
                default:
                    response = "query type = " + queryStmt.Type;
                    response = response + ", subject = " + GetTokenTreeText(astTokens, 
                        queryStmt.SubjectNode).Trim();
                    break;
            }

            return response;
        }

        private static string GetImperativeResponse(ImperativeStmt imperStmt)
        {
            string response = "unknown;";

            switch (imperStmt.Type)
            {
                //case "find":
                //    response = GetResultOfFind(imperStmt.SubjectNode, imperStmt.SubjectWord);
                //    break;
                default:
                    response = "imperative stmt type = " + imperStmt.Type;
                    response = response + ", subject = " + GetTokenTreeText(astTokens,
                        imperStmt.SubjectNode).Trim();
                    break;
            }

            return response;
        }

        //private static string GetResultOfFind(int subjectNode, string subjectWord)
        //{
        //    string result = "word nodes found:";

        //    List<int> foundWordNodes = FindWord(astTokens, subjectNode, subjectWord);

        //    for (int item = 0; item < foundWordNodes.Count; item++)
        //    {
        //        result = result + " " + foundWordNodes[item];
        //    }

        //    return result;
        //}

        private static string GetAnswerToWhat(int subjectNode)
        {
            string answer = "";
            string word = "";
            string stmt;
            List<Token> stmtTokens;

            List<string> baseNouns = GetBaseNouns(subjectNode);
            word = baseNouns[0];

            List<int> stmtsContainingWord = langKnowledge.FindStmtsContainingWord(word);

            for (int stmtNum = 0; stmtNum < stmtsContainingWord.Count; stmtNum++)
            {
                stmtTokens = langKnowledge.Stmts[stmtsContainingWord[stmtNum]].StmtTokens;
                stmt = GetTokenTreeText(stmtTokens, 0);
                answer = answer + stmt + "; ";
            }
            
            return answer;
        }

        private static void GetStmt(ref Stmt stmt, int node)
        {
            stmt.ASTNode = node;
            stmt.Type = astTokens[node].Name;

            switch (stmt.Type)
            {
                case "decl_stmt":
                    break;
                case "query_stmt":
                    stmt.Query = GetQueryStmt(node);
                    break;
                case "imperative_stmt":
                    stmt.ImperStmt = GetImperativeStmt(node);
                    break;
                default:
                    break;
            }
        }

        private static QueryStmt GetQueryStmt(int node)
        {
            QueryStmt queryStmt = new QueryStmt();

            if (astTokens[astTokens[node].Children[0]].Name == "truthof")
            {
                queryStmt.Type = "truthof";
                // Subject directly follows "truthof" and is second child.
                queryStmt.SubjectNode = astTokens[node].Children[1];
            }
            else
            {
                // Type is "what", "where", "when", "why", or "how".
                queryStmt.Type = GetQueryType(node);
                // Subject is first child.
                queryStmt.SubjectNode = astTokens[node].Children[0];
            }

            return queryStmt;
        }

        private static string GetQueryType(int node)
        {
            string type = "";
            List<Token> queryTreeTokens = new List<Token>();

            CopyTokenSubTree(astTokens, queryTreeTokens, node);

            for (int token = 0; token < queryTreeTokens.Count; token++)
            {
                if (queryTreeTokens[token].Name == "what" ||
                    queryTreeTokens[token].Name == "where" ||
                    queryTreeTokens[token].Name == "when_why_how" ||
                    queryTreeTokens[token].Name == "truthof")
                {
                    type = queryTreeTokens[token].Literal;
                }
            }

            return type;
        }

        private static ImperativeStmt GetImperativeStmt(int node)
        {
            ImperativeStmt imperStmt = new ImperativeStmt();
            int childNode;

            if (astTokens[astTokens[node].Children[0]].Name == "find")
            {
                imperStmt.Type = "find";
                childNode = astTokens[node].Children[1];
                imperStmt.SubjectWord = astTokens[childNode].Literal;
                imperStmt.SubjectNode = astTokens[node].Children[2];
            }
            else
            {
                // Type is not "find".
                imperStmt.Type = "undetermined";
                //childNode = astTokens[node].Children[astTokens[node].Children.Count - 1];
                //imperStmt.Command = astTokens[childNode].Literal;
                // Subject is first child.
                //imperStmt.SubjectNode = astTokens[node].Children[0];
                imperStmt.SubjectNode = -1;
            }

            //private class ImperativeStmt
            //{
            //    public string Command;  // "find", etc.
            //    public int SubjectNode;
            //}

            return imperStmt;
        }

        private static List<string> GetBaseNouns(int nounPhraNode)
        {
            List<string> baseNouns = new List<string>();

            List<Token> subjectTokens = GetTokenSubTreeCopy(astTokens, nounPhraNode);

            for (int token = 0; token < subjectTokens.Count; token++)
            {
                if (subjectTokens[token].Name == "class_noun" ||
                    subjectTokens[token].Name == "disc_obj_noun" ||
                    subjectTokens[token].Name == "non_disc_obj_noun")
                {
                    if (!baseNouns.Contains(subjectTokens[token].Literal))
                    {
                        baseNouns.Add(subjectTokens[token].Literal);
                    }
                }
            }

            return baseNouns;
        }

        private static LangBnf.WordForm GetWordForm(string word)
        {
            LangBnf.WordForm wordForm = LangBnf.WordForm.Base;

            LangBnf.WordLocationInBnf wordLocation = LangBnf.FindWordInBNF(word);

            if (wordLocation.Rule != -1)  // If word found.
            {
                if (wordLocation.Item != 0)
                {
                    switch (LangBnf.LexRules[wordLocation.Rule].Token)
                    {
                        case "class_noun":
                            wordForm = LangBnf.WordForm.NounPlural;
                            break;
                        case "trans_verb":
                        case "intrans_verb":
                            if (wordLocation.Item == 1)
                            {
                                wordForm = LangBnf.WordForm.VerbSingular;
                            }
                            else
                            {
                                wordForm = LangBnf.WordForm.VerbPast;
                            }
                            break;
                    }
                }
            }

            return wordForm;
        }

        // Generates a string representation of the AST subtree of a given node.
        public static string GetTokenTreeText(List<Token> subTreeTokens, int node)
        {
            string text = "";
            int child = 0;

            if (subTreeTokens[node].Terminal == false)
            {
                child = 0;
                while (child < subTreeTokens[node].Children.Count)
                {
                    text = text + GetTokenTreeText(subTreeTokens, subTreeTokens[node].Children[child]);
                    child = child + 1;
                }
            }
            else
            {
                text = text + " " + subTreeTokens[node].Literal;
            }

            return text;
        }

        // THIS METHOD IS JUST AN EXAMPLE CALL OF FINDWORDINTREE()
        private static List<int> FindWord(List<Token> treeTokens, int node, string word)
        {
            List<int> wordNodes = new List<int>();
            wordNodes = FindWordInTree(treeTokens, node, word, wordNodes);

            return wordNodes;
        }

        private static List<int> FindWordInTree(List<Token> treeTokens, int node, string word, 
            List<int> wordNodes)
        {
            int child = 0;

            if (treeTokens[node].Terminal == false)
            {
                child = 0;
                while (child < treeTokens[node].Children.Count)
                {
                    wordNodes = FindWordInTree(
                        treeTokens, treeTokens[node].Children[child], word, wordNodes);
                    child = child + 1;
                }
            }
            else
            {
                if (treeTokens[node].Literal == word)
                {
                    wordNodes.Add(node);
                }
            }

            return wordNodes;
        }

        private static List<Token> GetTokenSubTreeCopy(List<Token> treeTokens, int startNode)
        {
            List<Token> subTreeTokens = new List<Token>();

            CopyTokenSubTree(treeTokens, subTreeTokens, startNode);

            return subTreeTokens;
        }

        private static string ParseStmts(string stmtList)
        {
            string errorMsg = "";

            // Initialize parsing variables
            topTokensLen = 0;
            parseTreeRoot = 0;
            debugOutput = "";

            parseTreeTokens = new List<Token>();
            topTokens = new List<int>();

            // Parse statement list.
            errorMsg = Lex(stmtList);

            if (errorMsg == "")
            {
                // What was the purpose for subParseName, such as "stmt_list"?
                errorMsg = ParseTokens();
            }

            return errorMsg;
        }

        private static string Lex(string stmtList)
        {
            string errorMsg = "";
            int pos = 0;  // position in string

            numParseTreeTokens = 0;

            stmtList = stmtList.ToLower();  // Make non-case-sensitive.
            stmtsStripped = StripWhiteSpace(stmtList);
            if (debugOn)
            {
                debugOutput = debugOutput + "\"" + stmtsStripped + "\"\r\n";
            }

            while ((pos < stmtsStripped.Length) && errorMsg == "")
            {
                if (stmtsStripped[pos] == ' ')
                {
                    pos = pos + 1;  // Skip over space character
                }

                pos = GetToken(pos, ref errorMsg);
                if (pos == -1)
                {
                    errorMsg = "Lexical error parsing statements.";
                }
            }

            if (errorMsg == "")
            {
                numParseTreeTokens = parseTreeTokens.Count;
                topTokensLen = numParseTreeTokens;
            }

            if (returnParseResults)
            {
                debugOutput = debugOutput + GetTopTokensOutput(0) + "\r\n";
                debugOutput = debugOutput + GetLexResults();
            }

            return errorMsg;
        }

        //	Read chars until space or symbol. If string matches a token, add.
        static int GetToken(int pos, ref string errorMsg)
        {
            string newToken;
            bool gotToken;
            bool identifiedToken;
            int rule;
            int clause;
            errorMsg = "";
            newToken = "";
            identifiedToken = false;

            if (stmtsStripped[pos] == '\"')  // String literal. Get the token.
            {
                // Add open quote token
                parseTreeTokens.Add(new Token());
                parseTreeTokens[numParseTreeTokens].Name = "quote";
                parseTreeTokens[numParseTreeTokens].Terminal = true;
                parseTreeTokens[numParseTreeTokens].Literal = "\"";
                parseTreeTokens[numParseTreeTokens].Parent = -1;

                topTokens.Add(new int());
                topTokens[numParseTreeTokens] = numParseTreeTokens;
                numParseTreeTokens = numParseTreeTokens + 1;

                // Get chars until next quote.
                pos = pos + 1;
                while (stmtsStripped[pos] != '\"')
                {
                    newToken = newToken + stmtsStripped[pos];
                    pos = pos + 1;
                }

                // Add string literal token
                parseTreeTokens.Add(new Token());
                parseTreeTokens[numParseTreeTokens].Name = "string";
                parseTreeTokens[numParseTreeTokens].Terminal = true;
                parseTreeTokens[numParseTreeTokens].Literal = newToken;
                parseTreeTokens[numParseTreeTokens].Parent = -1;

                topTokens.Add(new int());
                topTokens[numParseTreeTokens] = numParseTreeTokens;
                numParseTreeTokens = numParseTreeTokens + 1;
                identifiedToken = true;

                // Add close quote token
                parseTreeTokens.Add(new Token());
                parseTreeTokens[numParseTreeTokens].Name = "quote";
                parseTreeTokens[numParseTreeTokens].Terminal = true;
                parseTreeTokens[numParseTreeTokens].Literal = "\"";
                parseTreeTokens[numParseTreeTokens].Parent = -1;

                topTokens.Add(new int());
                topTokens[numParseTreeTokens] = numParseTreeTokens;
                numParseTreeTokens = numParseTreeTokens + 1;

                pos = pos + 1;
            }
            else    // Not a string literal. Get the token.
            {
                gotToken = false;
                if (stmtsStripped[pos] == ';' ||
                    stmtsStripped[pos] == '(' || stmtsStripped[pos] == ')' ||
                    stmtsStripped[pos] == '{' || stmtsStripped[pos] == '}' ||
                    stmtsStripped[pos] == ',')
                {
                    // Token is a single-char symbol
                    newToken = stmtsStripped[pos].ToString();
                    gotToken = true;
                    pos = pos + 1;
                }

                while (gotToken == false && pos < stmtsStripped.Length &&
                    stmtsStripped[pos] != ' ' && stmtsStripped[pos] != ';' &&
                    stmtsStripped[pos] != '(' && stmtsStripped[pos] != ')' &&
                    stmtsStripped[pos] != '{' && stmtsStripped[pos] != '}' &&
                    stmtsStripped[pos] != ',')
                {
                    newToken = newToken + stmtsStripped[pos];
                    pos = pos + 1;
                }

                // Identify and add new token.
                rule = 0;

                if (!IsIdentifierByContext(newToken))
                {
                    while ((rule < LangBnf.LexRules.Count) && (identifiedToken == false))
                    {
                        clause = 0;
                        while ((clause < LangBnf.LexRules[rule].Clauses.Count) &&
                            (identifiedToken == false))
                        {
                            if (LangBnf.LexRules[rule].Clauses[clause].Items.Count == 1)
                            {
                                if (newToken ==
                                    LangBnf.LexRules[rule].Clauses[clause].Items[0].Word)
                                {
                                    parseTreeTokens.Add(new Token());
                                    parseTreeTokens[numParseTreeTokens].Name =
                                        LangBnf.LexRules[rule].Token;
                                    parseTreeTokens[numParseTreeTokens].Terminal = true;
                                    parseTreeTokens[numParseTreeTokens].Literal = newToken;
                                    parseTreeTokens[numParseTreeTokens].Parent = -1;

                                    topTokens.Add(new int());
                                    topTokens[numParseTreeTokens] = numParseTreeTokens;
                                    numParseTreeTokens = numParseTreeTokens + 1;
                                    identifiedToken = true;
                                }
                            }
                            else
                            // More than one lexical item: has plural & past tense options.
                            {
                                bool matchedItem = false;
                                int item = 0;
                                while (item < LangBnf.LexRules[rule].Clauses[clause].Items.Count && matchedItem == false)
                                {
                                    if (newToken == LangBnf.LexRules[rule].Clauses[clause].Items[item].Word)
                                    {
                                        matchedItem = true;
                                    }
                                    item++;
                                }

                                if (matchedItem)
                                {
                                    parseTreeTokens.Add(new Token());
                                    parseTreeTokens[numParseTreeTokens].Name = LangBnf.LexRules[rule].Token;
                                    parseTreeTokens[numParseTreeTokens].Terminal = true;
                                    parseTreeTokens[numParseTreeTokens].Literal = newToken;
                                    parseTreeTokens[numParseTreeTokens].Parent = -1;

                                    topTokens.Add(new int());
                                    topTokens[numParseTreeTokens] = numParseTreeTokens;

                                    if (LangBnf.LexRules[rule].Clauses[clause].Items[0].Word !=
                                        LangBnf.LexRules[rule].Clauses[clause].Items[1].Word)
                                    {
                                        // This word has a plural option.
                                        parseTreeTokens[numParseTreeTokens].HasPlural = true;
                                        if (item == 1)
                                        {
                                            parseTreeTokens[numParseTreeTokens].IsPlural = true;
                                        }
                                    }

                                    if (LangBnf.LexRules[rule].Clauses[clause].Items.Count == 3)
                                    {
                                        // This is a verb and has a past tense item.
                                        if (LangBnf.LexRules[rule].Clauses[clause].Items[0].Word !=
                                            LangBnf.LexRules[rule].Clauses[clause].Items[2].Word)
                                        {
                                            // This word has a past-tense option.
                                            parseTreeTokens[numParseTreeTokens].HasPastTense = true;
                                            if (item == 2)
                                            {
                                                parseTreeTokens[numParseTreeTokens].IsPastTense = true;
                                            }
                                        }
                                    }
                                    numParseTreeTokens = numParseTreeTokens + 1;
                                    identifiedToken = true;
                                }
                            }
                            clause = clause + 1;
                        }
                        rule = rule + 1;
                    }
                }

                if (identifiedToken == false)
                {
                    // Not a known token, check for an identifier or number.
                    if (VerifyIdentifier(newToken) == true)
                    {
                        //if (IDENTIFIER IS AN ALIAS)
                        //{
                        // EXPAND THE ALIASED PHRASE & GET TOKENS
                        // Alias info is stored in a data structure, but not in KB.
                        //}
                        //else
                        //{
                        // Identifier(alpha/digit/underscore - starting with alpha)
                        parseTreeTokens.Add(new Token());
                        parseTreeTokens[numParseTreeTokens].Name = "identifier";
                        parseTreeTokens[numParseTreeTokens].Terminal = true;
                        parseTreeTokens[numParseTreeTokens].Literal = newToken;
                        parseTreeTokens[numParseTreeTokens].Parent = -1;

                        topTokens.Add(new int());
                        topTokens[numParseTreeTokens] = numParseTreeTokens;
                        numParseTreeTokens = numParseTreeTokens + 1;
                        identifiedToken = true;
                        //}
                    }
                    else if (VerifyNumber(newToken) != "")
                    {
                        parseTreeTokens.Add(new Token());
                        parseTreeTokens[numParseTreeTokens].Name = VerifyNumber(newToken);
                        parseTreeTokens[numParseTreeTokens].Terminal = true;
                        parseTreeTokens[numParseTreeTokens].Literal = newToken;
                        parseTreeTokens[numParseTreeTokens].Parent = -1;

                        topTokens.Add(new int());
                        topTokens[numParseTreeTokens] = numParseTreeTokens;
                        numParseTreeTokens = numParseTreeTokens + 1;
                        identifiedToken = true;
                    }
                    else
                    {
                        // GET THIS ERROR MESSAGE TO DISPLAY
                        errorMsg = "[" + newToken + "] is not recognized and is not " +
                                    "a valid identifier.\r\n";
                        debugOutput = debugOutput + errorMsg;
                        pos = -1;
                    }
                }
            }
            return pos;
        }

        private static bool IsIdentifierByContext(string newToken)
        {
            bool isIdentifier = false;

            //topTokens[numParseTreeTokens];
            if (parseTreeTokens.Count > 0 && parseTreeTokens[parseTreeTokens.Count - 1].Literal == "langdef" &&
                newToken != ";")
            {
                isIdentifier = true;
            }
            //else if (parseTreeTokens.Count > 1 && parseTreeTokens[parseTreeTokens.Count - 2].Literal == "newword")
            //{
            //    isIdentifier = true;
            //}
            //else if (parseTreeTokens.Count > 2 && parseTreeTokens[parseTreeTokens.Count - 3].Literal == "newword")
            //{
            //    if (parseTreeTokens[parseTreeTokens.Count - 1].Name == "class_noun_word_type" ||
            //        parseTreeTokens[parseTreeTokens.Count - 1].Name == "verb_word_type")
            //    {
            //        isIdentifier = true;
            //    }
            //}
            //else if (parseTreeTokens.Count > 2 && parseTreeTokens[parseTreeTokens.Count - 4].Literal == "newword")
            //{
            //    if (parseTreeTokens[parseTreeTokens.Count - 1].Name == "verb_word_type")
            //    {
            //        isIdentifier = true;
            //    }
            //}

            return isIdentifier;
        }

        private static string GetLexResults()
        {
            string results = "";
            int tokenNum;

            tokenNum = 0;
            while (tokenNum < parseTreeTokens.Count)
            {
                results = results + "[" + parseTreeTokens[tokenNum].Name.PadLeft(16) +
                    "] [" + parseTreeTokens[tokenNum].Literal + "]\r\n";

                tokenNum = tokenNum + 1;
            }

            return results;
        }

        private static string ParseTokens()
        {
            string errorMsg = "";
            int maxPhraseSize;
            bool parseDone = false;
            bool reduceOccurred;
            int numLoopsNoReduce = 0;  // number of top level loops carried out - for limit

            if (debugOn)
            {
                debugOutput = debugOutput + "\r\nentering ParseTokens\r\n";
            }

            maxPhraseSize = LangBnf.LongestPhraseSize;  // currently 8

            // Repeat parsing of top-level tokens until done.
            while (errorMsg == "" && parseDone == false)
            {
                if (debugOn)
                {
                    debugOutput = debugOutput + "beginning of ParseTokens loop\r\n";
                    debugOutput = debugOutput + GetTopTokensOutput(0) + "\r\n";
                }

                reduceOccurred = false;
                errorMsg = ParseOnePass(0, maxPhraseSize, "", ref reduceOccurred);

                if (errorMsg != "")
                {
                }
                else
                {
                    if (reduceOccurred)
                    {
                        numLoopsNoReduce = 0;
                    }
                    else
                    {
                        numLoopsNoReduce = numLoopsNoReduce + 1;
                    }

                    if (topTokensLen == 1)
                    {
                        if (parseTreeTokens[topTokens[0]].Name == "stmt_list")
                        {
                            parseDone = true;
                            parseTreeRoot = topTokens[0];
                        }
                        else
                        {
                            if (numLoopsNoReduce > 1)
                            {
                                // Stmts did not reduce to "stmt_list".
                                errorMsg = "Syntax error: could not resolve statements.";
                                if (returnParseResults)
                                {
                                    debugOutput = debugOutput + GetTopTokensOutput(0) + "\r\n";
                                }
                            }
                        }
                    }

                    if (debugOn)
                    {
                        debugOutput = debugOutput + "numLoopsNoReduce = " + numLoopsNoReduce + "\r\n";
                    }

                    if (numLoopsNoReduce > 1)
                    {
                        errorMsg = "Syntax error: could not resolve statements.";
                    }
                }
            }

            if (debugOn)
            {
                debugOutput = debugOutput + "parseDone = " + parseDone + "\r\n";
                debugOutput = debugOutput + "\r\nexiting ParseTokens\r\n\r\n";
            }

            return errorMsg;
        }

        private static string ParseOnePass(int startPos, int maxPhraseSize, string subParseName, 
            ref bool reduceOccurred)
        {
            string errorMsg = "";
            bool subParseDone = false;
            int tokenStringStart = 0;
            int tokenStringLength = 1;
            int ruleSearchResult = -1;
            bool phraseMatched = false;
            bool allowReduce = true;

            if (debugOn)
            {
                debugOutput = debugOutput + "\r\nentering SubParseTokens, subParseName: \"" +
                    subParseName + "\"\r\n";
            }

            // Loop through tokens, increasing start pos. after each loop.
            if (debugOn)
            {
                debugOutput = debugOutput + "entering all-tokens loop\r\n";
            }
            phraseMatched = false;
            reduceOccurred = false;
            tokenStringStart = startPos;
            tokenStringLength = 1;
            while (errorMsg == "" && subParseDone == false &&
                phraseMatched == false && tokenStringStart < topTokensLen)
            {
                // From phrase start, len = 1, increase phrase len by 1 each loop.
                if (debugOn)
                {
                    debugOutput = debugOutput + "entering phrase increase-by-one loop\r\n";
                }

                errorMsg = CheckSyntax(tokenStringStart);

                tokenStringLength = 1;
                allowReduce = true;
                while (errorMsg == "" && subParseDone == false &&
                    reduceOccurred == false && tokenStringLength <= maxPhraseSize &&
                    tokenStringStart + (tokenStringLength - 1) <= topTokensLen - 1)
                {
                    if (debugOn)
                    {
                        debugOutput = debugOutput + "entering phrase-match loop\r\n";
                    }
                    phraseMatched = false;
                    allowReduce = true;
                    if (debugOn)
                    {
                        debugOutput = debugOutput + "\r\n";
                        debugOutput = debugOutput + GetTopTokensOutput(0);
                        debugOutput = debugOutput + GetPhraseInfoOutput(subParseName, tokenStringStart, tokenStringLength);
                    }

                    //// Perform a sub-parse starting with last token in phrase if needed.
                    //string newSubParseName = "";
                    //newSubParseName = CheckIfSubParseNeeded(tokenStringStart, tokenStringLength,
                    //    startPos, subParseName);
                    //if (newSubParseName != "")
                    //{
                    //    if (debugOn)
                    //    {
                    //        debugOutput = debugOutput + "entering sub-parse\r\n";
                    //    }
                    //    prevTopTokensLen = topTokensLen;
                    //    errorMsg = SubParseTokens(tokenStringStart + (tokenStringLength - 1),  // sub parse
                    //        newSubParseName);
                    //    if (topTokensLen < prevTopTokensLen)
                    //    {
                    //        reduceOccurred = true;
                    //    }
                    //}

                    List<int> rulesFound = new List<int>();
                    ruleSearchResult = FindRule(tokenStringStart, tokenStringLength, rulesFound);
                    if (ruleSearchResult != -1)
                    {
                        phraseMatched = true;

                        if (rulesFound.Count > 1)
                        {
                            // Two possible rules found.
                            // Decide reduce on higher precedence rule first. If no reduce, try next rule.
                            int firstRule;
                            int secondRule;
                            if (LangBnf.LangRules[rulesFound[0]].Precedence <
                                LangBnf.LangRules[rulesFound[1]].Precedence)
                            {
                                firstRule = rulesFound[0];
                                secondRule = rulesFound[1];
                            }
                            else
                            {
                                firstRule = rulesFound[1];
                                secondRule = rulesFound[0];
                            }

                            allowReduce = DecideReduce(tokenStringStart, tokenStringLength,
                                            firstRule, ref errorMsg);
                            if (allowReduce == true)
                            {
                                Reduce(firstRule, tokenStringStart, tokenStringLength);
                                reduceOccurred = true;
                            }
                            else
                            {
                                allowReduce = DecideReduce(tokenStringStart, tokenStringLength,
                                                secondRule, ref errorMsg);
                                if (allowReduce == true)
                                {
                                    Reduce(secondRule, tokenStringStart, tokenStringLength);
                                    reduceOccurred = true;
                                }
                            }
                        }
                        else
                        {
                            allowReduce = DecideReduce(tokenStringStart, tokenStringLength,
                                ruleSearchResult, ref errorMsg);
                            if (allowReduce == true)
                            {
                                Reduce(ruleSearchResult, tokenStringStart, tokenStringLength);
                                reduceOccurred = true;
                            }
                        }

                        if (allowReduce == false)
                        {
                            // Match found, but allowReduce was false.
                            tokenStringLength = tokenStringLength + 1;  // add next token to phrase
                        }

                        //// If sub-parse reduction was completed, sub-parse is done.
                        //if (reduceOccurred == true && subParseName != "")
                        //{
                        //    if (CheckSubParseCompletion(subParseName,
                        //        ruleSearchResult, tokenStringStart) == true)
                        //    {
                        //        subParseName = "";
                        //        if (debugOn)
                        //        {
                        //            debugOutput = debugOutput + "sub-parse done\r\n";
                        //        }
                        //        subParseDone = true;
                        //    }
                        //}
                    }
                    else  // match not found
                    {
                        tokenStringLength = tokenStringLength + 1;  // add next token to phrase
                    }
                }
                tokenStringStart = tokenStringStart + 1;  // shift beginning of phrase forward
            }

            if (debugOn)
            {
                debugOutput = debugOutput + "subParseDone = " + subParseDone + "\r\n";
                debugOutput = debugOutput + "\r\nexiting SubParseTokens\r\n\r\n";
            }

            return errorMsg;
        }

        // Returns name of sub parse needed, or "" for none.
        private static string CheckIfSubParseNeeded(int tokenStringStart, int tokenStringLength,
                                            int startPos, string subParseName)
        {
            string subParseNeeded = "";
            string name = "";

            name = parseTreeTokens[topTokens[tokenStringStart + (tokenStringLength - 1)]].Name;

            if ((name == "of" || name == "which") && 
                tokenStringStart + (tokenStringLength - 1) > startPos)
            {
                subParseNeeded = "specifying_phra";
            }
            else if ((name == "prep" || name == "with" || name == "inimage") && 
                tokenStringStart + (tokenStringLength - 1) > startPos)
            {
                subParseNeeded = "prep_phra_item";
            }

            return subParseNeeded;
        }

        static bool CheckSubParseCompletion(string subParseName,
                                            string ruleSearchResult, int tokenStringStart)
        {
            bool subParseCompleted = false;

            if (subParseName == "adj_phra")
            {
                if (ruleSearchResult == "adj_phra" &&
                    parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "adj" &&
                    parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "adj_mod_adv")
                {
                    subParseCompleted = true;
                }
            }
            if (subParseName == "specifying_phra")
            {
                if (ruleSearchResult == "specifying_phra")
                {
                    subParseCompleted = true;
                }
            }
            if (subParseName == "prep_phra")
            {
                if (ruleSearchResult == "prep_phra" &&
                    parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "prep_phra_item" &&
                    parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "prep" &&
                    parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "with")
                {
                    subParseCompleted = true;
                }
            }

            return subParseCompleted;
        }

        private static string CheckSyntax(int tokenStringStart)
        {
            string errorMsg = "";

            switch (parseTreeTokens[topTokens[tokenStringStart]].Name)
            {
                case "newword":
                    //int shift = 0;  // Shift to adjust for "temp" option.
                    //bool error = false;
                    //if (parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "temp")
                    //{
                    //    shift = 1;
                    //}

                    //int topTokensLengthToEnd = topTokens.Count - tokenStringStart;
                    //switch (parseTreeTokens[topTokens[tokenStringStart + shift + 1]].Name)
                    //{
                    //    case "simple_word_type":
                    //        if (topTokensLengthToEnd >= 4 + shift)
                    //        {
                    //            if (parseTreeTokens[topTokens[tokenStringStart + shift + 2]].Name != "identifier")
                    //            {
                    //                error = true;
                    //            }
                    //        }
                    //        break;
                    //    case "class_noun_type":
                    //        if (topTokensLengthToEnd >= 5 + shift)
                    //        {
                    //            if (parseTreeTokens[topTokens[tokenStringStart + shift + 2]].Name != "identifier" ||
                    //            parseTreeTokens[topTokens[tokenStringStart + shift + 3]].Name != "identifier")
                    //            {
                    //                error = true;
                    //            }
                    //        }
                    //        break;
                    //    case "verb_word_type":
                    //        if (topTokensLengthToEnd >= 6 + shift)
                    //            if (parseTreeTokens[topTokens[tokenStringStart + shift + 2]].Name != "identifier" ||
                    //            parseTreeTokens[topTokens[tokenStringStart + shift + 3]].Name != "identifier" ||
                    //            parseTreeTokens[topTokens[tokenStringStart + shift + 4]].Name != "identifier")
                    //        {
                    //            error = true;
                    //        }
                    //        break;
                    //}

                    //if (error)
                    //{
                    //    errorMsg = "Word already exists.";
                    //}
                    break;
            }

            return errorMsg;
        }

        // Decides if a phrase should be allowed to reduce now or not.
        //
        // ADD NOUN_PHRA -
        // REDUCE UNSPECIFIED NOUN "the <noun>" ONLY IF IT IS IN A SPECIFYING PHRASE AND
        // MATCHES THE NOUN BEING SPECIFIED.
        //
        // ADD SPECIFYING PHRASE.
        // IF SPECIFYING PHRASE REDUCES BUT IS MISSING A REFERENCE TO THE NOUN BEING
        // SPECIFIED, THEN PARSE FAILS.
        //
        // Parsing of specifying phrase is done when we have:
        //	 <which> <decl_phra> followed by anything but:
        //		and/or/if, <temporal_conj>, because
        //
        static bool DecideReduce(int tokenStringStart, int tokenStringLength, 
            int ruleSearchResult, ref string errorMsg)
        {
            bool allowReduce = true;
            string ruleName = LangBnf.LangRules[ruleSearchResult].Token;

            switch (ruleName)
            {
                case "stmt":
                    if (parseTreeTokens[topTokens[tokenStringStart]].Name == "if_stmt")
                    {
                        // Don't reduce if this is part of a larger <if_stmt>.
                        if ((tokenStringStart > 0 &&
                            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "else") || (
                            tokenStringStart < topTokensLen - 1 &&
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "else"))
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "decl_stmt":
                    // Don't reduce if this is part of a larger statement.
                    if (tokenStringStart > 0)
                    {
                        if (topTokensLen > 2 && parseTreeTokens[topTokens[
                            tokenStringStart - 1]].Name != ";" &&
                            parseTreeTokens[topTokens[
                            tokenStringStart - 1]].Name != "{" &&
                            parseTreeTokens[topTokens[
                            tokenStringStart - 1]].Name != "}" &&
                            parseTreeTokens[topTokens[
                            tokenStringStart - 1]].Name != "stmt" &&
                            parseTreeTokens[topTokens[
                            tokenStringStart - 1]].Name != "stmt_list")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "logic_conj":
                    if (parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "decl_phra")
                    {
                            allowReduce = false;
                    }
                    //else if (tokenStringLength == 1 && tokenStringStart >= 1 &&
                    //    (parseTreeTokens[topTokens[tokenStringStart]].Name == "and" ||
                    //    parseTreeTokens[topTokens[tokenStringStart]].Name == "or"))
                    //{
                    //    // Don't reduce if <and> connects two noun phrases and not decl phrases
                    //    // enclosed in parentheses.
                    //    if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name != ")" ||
                    //        parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "(")
                    //    {
                    //        allowReduce = false;
                    //    }
                    //}
                    break;
                case "decl_phra":
                    if (tokenStringStart > 0 && 
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "noun_phra")
                    {
                        // Don't reduce if noun_phra is part of a preceding prepositional phrase
                        if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "prep" ||
                            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "prep_phra_item" ||
                            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "prep_phra")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "predicate":
                    if ((tokenStringStart > 0 && 
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name != "decl_phra" &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name != ";" &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name != ")" &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name !=
                            "where_when_why_how" &&
                        parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "which" &&
                        !IsConjunction(parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name)) ||
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name == "prep_phra_item")
                    {
                        allowReduce = false;
                    }
                    else if (tokenStringLength == 1 && (
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "intrans_verb_phra"))
                    {
                        // Don't reduce if a prepositional phrase follows.
                        if (parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "prep" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "with" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "inimage" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "prep_phra_item" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "prep_phra")
                        {
                            allowReduce = false;
                        }
                    }
                    else if (tokenStringStart > 0 && 
                        (parseTreeTokens[topTokens[tokenStringStart]].Name == "intrans_verb_phra" ||
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "trans_verb_phra" ||
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "is_phra" ||
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "has_phra"))
                    {
                        // Don't reduce verb phrase if a frequency adverb precedes it.
                        if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "frequency_adv")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "intrans_verb_phra":
                    if (tokenStringStart > 0 && tokenStringLength == 1 &&
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "intrans_verb")
                    {
                        // Don't reduce before tense is resolved.
                        if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "did" ||
                            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "will")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "trans_verb_phra":
                    if (tokenStringStart > 0 && tokenStringLength == 1 &&
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "trans_verb")
                    {
                        // Don't reduce before tense is resolved.
                        if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "did" ||
                            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "will")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "noun_phra":
                    if (tokenStringStart > 0 && tokenStringLength > 2 &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name != ";" &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name != "decl_phra" &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name != "logic_conj" &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name != ")" &&
                        // And is not a noun phrase between quotes.
                        (parseTreeTokens[topTokens[tokenStringStart]].Name != "(" ||
                        parseTreeTokens[topTokens[tokenStringStart + 2]].Name != ")"))
                    {
                        if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "prep" ||
                            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "and" ||
                            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "or")
                        {
                            allowReduce = false;
                        }
                    }
                    else if (tokenStringLength == 1 && (
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "specified_noun" ||
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "unspecified_noun"))
                    {
                        // Don't reduce if specifying phrase follows.
                        if (parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "which" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "of" ||

                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "prep" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "with" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "inimage" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "prep_phra_item" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "prep_phra" ||

                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "specifying_phra")
                        {
                            allowReduce = false;
                        }
                    }
                    // THIS ISN'T NEEDED IF PRONOUNS ARE CHANGED FROM BEING IDENTIFIERS.
                    else if (tokenStringStart > 0 && tokenStringLength == 1)
                    {
                        // Don't reduce if identifier is part of a pronounce_stmt.
                        if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "pronounce")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "prep_phra":
                    if (tokenStringStart > 1)
                    {
                        if (parseTreeTokens[topTokens[tokenStringStart - 2]].Name == "prep")
                        {
                            allowReduce = false;
                        }
                    }
                    else if (tokenStringLength >= 1 && (
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "prep_phra_item"))
                    {
                        // Don't reduce if an unreduced prepositional phrase item follows.
                        if (parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name == "prep" ||
                            parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name == "with" ||
                            parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name == "inimage" ||
                            parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name == "prep_phra_item")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                case "prep_phra_item":
                    if (parseTreeTokens[topTokens[tokenStringStart + 2]].Name != "decl_phra" &&
                        parseTreeTokens[topTokens[tokenStringStart + 2]].Name != ";" &&
                        parseTreeTokens[topTokens[tokenStringStart + 2]].Name != ")" &&
                        parseTreeTokens[topTokens[tokenStringStart + tokenStringLength]].Name !=
                            "where_when_why_how" &&
                        !IsConjunction(parseTreeTokens[topTokens[tokenStringStart + 2]].Name) &&
                        parseTreeTokens[topTokens[tokenStringStart + 2]].Name != "prep_phra_item" &&

                        parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "specified_noun" &&        
                        parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "unspecified_noun")
                    {
                            allowReduce = false;
                    }
                    break;
                case "imperative_and_or_phra":
                    if (tokenStringLength == 1 &&
                    parseTreeTokens[topTokens[tokenStringStart]].Name == "predicate")
                    {
                        // Don't reduce if this is not the
                        // beginning of a statement and is not enclosed in parentheses.
                        if (IsBeginningOfStmt(tokenStringStart) == false)
                        {
                            if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "(" &&
                                parseTreeTokens[topTokens[tokenStringStart + 1]].Name != ")")
                            {
                                allowReduce = false;
                            }
                        }
                    }
                    break;
                case "adj_phra":
                    allowReduce = false;
                    // Reduce only if:
                    if (((
                        // this is a single adjective unit
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "adj_unit" &&
                        parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "adj_list" &&
                        parseTreeTokens[topTokens[tokenStringStart + 1]].Name != "comma_delim_adj_list" &&
                        !IsAdjUnit(parseTreeTokens[topTokens[tokenStringStart + 1]].Name)) ||
                        // or, this is an adjective unit followed by a completed adjective list
                        (tokenStringLength > 1 &&
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "adj_unit" &&
                        (parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "adj_list" ||
                        parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "comma_delim_adj_list") &&
                        !IsAdjUnit(parseTreeTokens[topTokens[tokenStringStart + 2]].Name))) &&
                        // And, it begins an adjective phrase
                        (parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "adj_list" &&
                        parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "comma_delim_adj_list" &&
                        !IsAdjUnit(parseTreeTokens[topTokens[tokenStringStart - 1]].Name)))
                    {
                        allowReduce = true;
                    }
                    break;
                case "adj_list":
                    // Don't reduce if this is the first adjective unit.
                    if (!IsAdjUnit(parseTreeTokens[topTokens[tokenStringStart - 1]].Name) &&
                        parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "adj_list")
                    {
                        allowReduce = false;
                    }
                    break;
                case "comma_delim_adj_list":
                    // Don't reduce if this is the first adjective unit.
                    if (!IsAdjUnit(parseTreeTokens[topTokens[tokenStringStart - 1]].Name) &&
                        parseTreeTokens[topTokens[tokenStringStart - 1]].Name != "comma_delim_adj_list")
                    {
                        allowReduce = false;
                    }
                    break;
                case "adj_unit":
                    // Don't reduce if preceded by adj_mod_adv.
                    if (parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "adj_mod_adv")
                    {
                        allowReduce = false;
                    }
                    break;
                case "specified_noun":
                    if (tokenStringLength == 1 &&
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "non_disc_obj_noun")
                    {
                        if (
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "of" ||
                            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "which")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
                //case "is_phra":
                //    if (tokenStringLength == 1 &&
                //        parseTreeTokens[topTokens[tokenStringStart]].Name == "is")
                //    {
                //        if (
                //            parseTreeTokens[topTokens[tokenStringStart - 1]].Name == "what" ||
                //            parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "what")
                //        {
                //            allowReduce = false;
                //        }
                //    }
                //    break;
                case "specifying_phra":
                    if (tokenStringLength == 2 &&
                        parseTreeTokens[topTokens[tokenStringStart]].Name == "which" &&
                        parseTreeTokens[topTokens[tokenStringStart + 1]].Name == "decl_phra")
                    {
                        // Don't reduce if this is part of a compound decl_phra.
                        if (
                            parseTreeTokens[topTokens[tokenStringStart + 2]].Name == ")" ||
                            parseTreeTokens[topTokens[tokenStringStart + 2]].Name == "and" ||
                            parseTreeTokens[topTokens[tokenStringStart + 2]].Name == "or" ||
                            parseTreeTokens[topTokens[tokenStringStart + 2]].Name == "if" ||
                            parseTreeTokens[topTokens[tokenStringStart + 2]].Name == "logic_conj")
                        {
                            allowReduce = false;
                        }
                    }
                    break;
            }
            
            if (debugOn)
            {
                debugOutput = debugOutput + "deciding reduce to [" + ruleName + "]\r\n";

                if (allowReduce == true)
                {
                    debugOutput = debugOutput + "allow reduce = true.\r\n";
                }
                else
                {
                    debugOutput = debugOutput + "allow reduce = false.\r\n";
                }
            }

            return allowReduce;
        }

        static bool IsAdjUnit(string token)
        {
            bool isAdjUnit = false;

            if (
            token == "adj_unit" ||
            token == "comma_delim_adj_unit" ||
            token == "," ||
            token == "adj" ||
            token == "adj_mod_adv")
            {
                isAdjUnit = true;
            }

            return isAdjUnit;
        }

        static bool IsConjunction(string token)
        {
            bool isConjunction = false;

            if (
            token == "logic_conj" ||
            token == "temporal_conj" ||
            token == "because")
            {
                isConjunction = true;
            }

            return isConjunction;
        }

        static bool BeginsPredicate(string token)
        {
            bool beginsPredicate = false;

            if (
            token == "predicate" ||
            token == "intrans_verb_phra" ||
            token == "trans_verb_phra" ||
            token == "is_phra" ||
            token == "has_phra")
            {
                beginsPredicate = true;
            }

            return beginsPredicate;
        }

        // Reduce and shift
        static void Reduce(int ruleNum, int startPos, int tokenStringLength)
        {
            int pos;
            int parent;
            bool reduceToExistingToken = false;

            string ruleName = LangBnf.LangRules[ruleNum].Token;

            if (debugOn)
            {
                debugOutput = debugOutput + "Reducing to [" + ruleName + "], Phrase: " + 
                    GetTokenString(startPos, tokenStringLength) + "\r\n";
            }

            if (LangBnf.LangRules[ruleNum].Type == "list" && startPos > 0)
            {
                if (parseTreeTokens[topTokens[startPos - 1]].Type == "list")
                {
                    // Parent is existing "list" token.
                    parent = topTokens[startPos - 1];
                    reduceToExistingToken = true;
                }
                else
                {
                    // Add new token to parse tree
                    parseTreeTokens.Add(new Token());
                    numParseTreeTokens = numParseTreeTokens + 1;
                    parent = numParseTreeTokens - 1;
                    parseTreeTokens[parent].Name = ruleName;
                    parseTreeTokens[parent].Terminal = false;
                    parseTreeTokens[parent].Type = LangBnf.LangRules[ruleNum].Type;
                    parseTreeTokens[parent].Parent = -1;
                }
            }
            else
            {
                // Add new token to parse tree
                parseTreeTokens.Add(new Token());
                numParseTreeTokens = numParseTreeTokens + 1;
                parent = numParseTreeTokens - 1;
                parseTreeTokens[parent].Name = ruleName;
                parseTreeTokens[parent].Terminal = false;
                parseTreeTokens[parent].Type = LangBnf.LangRules[ruleNum].Type;
                parseTreeTokens[parent].Parent = -1;
            }

            pos = startPos;
            while (pos < (startPos + tokenStringLength))
            {
                // Link new token to token below
                parseTreeTokens[parent].Children.Add(topTokens[pos]);

                // Link token below to new token
                parseTreeTokens[topTokens[pos]].Parent = parent;

                pos = pos + 1;
            }
            topTokens[startPos] = parent;

            if (reduceToExistingToken)
            {
                ShiftTopTokens(startPos + tokenStringLength, tokenStringLength);
            }
            else
            {
                ShiftTopTokens(startPos + tokenStringLength, tokenStringLength - 1);
            }

            return;
        }

        // Shifts top tokens left to fill gap after reduce.
        static void ShiftTopTokens(int shiftStartPos, int numTokensToRemove)
        {
            int tokensRemoved = 0;

            while (tokensRemoved < numTokensToRemove)
            {
                topTokens.RemoveAt(shiftStartPos - numTokensToRemove);
                tokensRemoved = tokensRemoved + 1;
            }

            topTokensLen = topTokensLen - tokensRemoved;
            //topTokensLen = topTokens.Count;
        }

        static int FindRule(int startPos, int tokenStringLength, List<int>rulesFound)
        {
            int clauseNum;
            int ruleNumFound = -1;
            bool ruleFound = false;

            int ruleNum = 0;
            //while ((ruleFound == false) && (ruleNum < LangBNF.LangRules.Count))

            //if (startPos == 1 && tokenStringLength == 3 && parseTreeTokens[topTokens[1]].Name == "decl_phra")
            //{
            //    bool test = true;
            //}

            while (ruleNum < LangBnf.LangRules.Count)
            {
                clauseNum = 0;
                //while ((ruleFound == false) && (clauseNum < LangBNF.LangRules[ruleNum].Clauses.Count))
                while (clauseNum < LangBnf.LangRules[ruleNum].Clauses.Count)
                {
                    ruleFound = MatchTokenClause(ruleNum, clauseNum, startPos, tokenStringLength);
                    if (ruleFound == true)
                    {
                        ruleNumFound = ruleNum;
                        rulesFound.Add(ruleNum);
                    }
                    clauseNum = clauseNum + 1;
                }
                ruleNum = ruleNum + 1;
            }

            //if (ruleToken == "predicate")
            //{
            //    bool test = true;
            //}

            if (debugOn)
            {
                //debugOutput = debugOutput + "rule found: [" + ruleToken + "]\r\n";
            }

            return ruleNumFound;
        }

        static bool MatchTokenClause(int ruleNum, int clauseNum, int startPos,
                                     int tokenStringLength)
        {
            int tokenNum = 0;
            bool matched = true;

            if (LangBnf.LangRules[ruleNum].Type == "list")
            {
                while ((matched == true) && (tokenNum < tokenStringLength))
                {
                    if (LangBnf.LangRules[ruleNum].Clauses[clauseNum].Items[0]
                        != parseTreeTokens[topTokens[startPos + tokenNum]].Name)
                    {
                        matched = false;
                    }
                    tokenNum = tokenNum + 1;
                }
            }
            else
            {
                if (tokenStringLength == LangBnf.LangRules[ruleNum].Clauses[clauseNum].Items.Count)
                {
                    while ((matched == true) && (tokenNum < tokenStringLength) &&
                        (tokenNum < LangBnf.LangRules[ruleNum].Clauses[clauseNum].Items.Count))
                    {
                        if (LangBnf.LangRules[ruleNum].Clauses[clauseNum].Items[tokenNum]
                            != parseTreeTokens[topTokens[startPos + tokenNum]].Name)
                        {
                            matched = false;
                        }
                        tokenNum = tokenNum + 1;
                    }
                }
                else
                {
                    matched = false;
                }
            }

            return matched;
        }

        static bool IsBeginningOfStmt(int pos)
        {
            bool beginningOfStmt = false;

            if (pos > 0)  // not first position in token list
            {
                if (parseTreeTokens[topTokens[pos - 1]].Name == "stmt_list" ||
                    parseTreeTokens[topTokens[pos - 1]].Name == "stmt" ||
                    parseTreeTokens[topTokens[pos - 1]].Name == ";" ||
                    parseTreeTokens[topTokens[pos - 1]].Name == "{" ||
                    parseTreeTokens[topTokens[pos - 1]].Name == "}")
                {
                    beginningOfStmt = true;
                }
            }
            else
            {
                beginningOfStmt = true;
            }

            return beginningOfStmt;
        }

        private static string GetTokenString(int startPos, int length)
        {
            string tokenStringText = "";

            for (int posInPhrase = 0; posInPhrase < length; posInPhrase++)
            {
                tokenStringText = tokenStringText + "[" + parseTreeTokens[topTokens[startPos + posInPhrase]].Name + "]";
            }

            return tokenStringText;
        }

        // Generates an Abstract Syntax Tree (AST) from the parse tree.
        static void GenerateAst()
        {
            startNewAstPass = true;
            astTokens = CloneTokenList(parseTreeTokens);

            while (startNewAstPass)
            {
                startNewAstPass = false;
                ConvertNodeToAst(parseTreeRoot, 0);
            }

            // Copy the tree node-by-node to remove all unused tokens.
            astTokens = GetTokenSubTreeCopy(astTokens, parseTreeRoot);
            astRoot = 0;

            return;
        }

        //static bool ConvertNodeToAST(int node)
        static void ConvertNodeToAst(int node, int childPosition)
        {
            if (!startNewAstPass)
            {
                int currentNode;
                int childNode;
                int child;
                //bool convertChildren = true;
                // backstep could be the number of levels still to return before restarting children.
                // The recursive method would return one less each return until reaching zero.
                //bool backstep = false;
                //bool restartChildren = false;

                //if (debugOn)
                //{
                //    debugOutput = debugOutput + GetParseTreeOutput(parseTreeRoot) + "\r\n";
                //    File.AppendAllText(AppProperties.ServerLogPath, "\r\n" + debugOutput);
                //}

                switch (astTokens[node].Name)
                {
                    case "stmt":
                        // Replace "stmt" node with "decl_stmt", "query_stmt", etc. directly under it.
                        childNode = astTokens[node].Children[0];
                        ReplaceNode(astTokens, node, childNode);
                        break;
                    case "decl_phra":
                        if (astTokens[node].Children.Count == 1)
                        {
                            //bool stop = true;
                        }

                        childNode = astTokens[node].Children[0];
                        if (astTokens[childNode].Name == "decl_phra" && astTokens[node].Children.Count == 1)
                        {
                            ReplaceNode(astTokens, node, childNode);
                        }
                        break;
                    case "adj_phra":
                        if (astTokens[node].Children.Count == 2)
                        {
                            currentNode = astTokens[node].Children[1];
                            for (child = 0; child < astTokens[currentNode].Children.Count; child++)
                            {
                                childNode = astTokens[currentNode].Children[child];
                                astTokens[node].Children.Add(childNode);
                                astTokens[childNode].Parent = node;
                            }
                            astTokens[node].Children.RemoveAt(1);
                            // IS THERE A FAST WAY TO REMOVE TOKENS FROM LIST WHILE PRESERVING THE TREE?
                            //astTokens.RemoveAt(currentNode);
                            //astTokens[currentNode] = null;
                        }
                        break;
                    case "comma_delim_adj_unit":
                        ReplaceNode(astTokens, node, astTokens[node].Children[1]);
                        break;
                    case "string_lit":
                        // Replace string_lit with the string.
                        ReplaceNode(astTokens, node, astTokens[node].Children[1]);
                        break;
                    case "(":
                    case "quote":
                        RemoveOpenCloseSymbolPair(node, childPosition);
                        //backstep = true;
                        //convertChildren = false;
                        startNewAstPass = true;
                        break;
                    // Remove semicolon.
                    case ";":
                        astTokens[astTokens[node].Parent].Children.RemoveAt(childPosition);
                        break;
                }

                child = 0;
                while (child < astTokens[node].Children.Count && startNewAstPass == false)
                {
                    ConvertNodeToAst(astTokens[node].Children[child], child);
                    child = child + 1;
                }
            }
            //if (convertChildren)
            //{
            //    child = 0;
                //while (child < astTokens[node].Children.Count && restartChildren == false)
                //{
                //    restartChildren = ConvertNodeToAst(astTokens[node].Children[child]);
                //    if (restartChildren)
                //    {
                //        child = 0;
                //        restartChildren = false;
                //    }
                //    else
                //    {
                //        child = child + 1;
                //    }
                //}
            //}
            //return backstep;
        }

        // Replaces a node with a copy of a source node, preserving its parent link.
        private static void ReplaceNode(List<Token> astTokens, int destinationNode, int sourceNode)
        {
            int savedParent;

            savedParent = astTokens[destinationNode].Parent;
            astTokens[destinationNode] = astTokens[sourceNode];
            astTokens[destinationNode].Parent = savedParent;
        }

        // Removes a node (and its sub-nodes).
        private static void RemoveNode(List<Token> astTokens, int node, int childPosition)
        {
            int parent = astTokens[node].Parent;

            int phraseLen = astTokens[parent].Children.Count;

            for (int ChildInPhrase = childPosition + 1; ChildInPhrase < phraseLen; ChildInPhrase++)
            {
                // Shift each node after open symbol left one position.
                ReplaceNode(astTokens, astTokens[parent].Children[ChildInPhrase - 1], astTokens[parent].Children[ChildInPhrase]);
            }

            // Remove extra child slot left over.
            astTokens[parent].Children.RemoveAt(phraseLen - 1);
        }

        private static void RemoveOpenCloseSymbolPair(int node, int childPosition)
        {
            int parent = astTokens[node].Parent;

            string closeSymbol = GetMatchingCloseSymbol(astTokens[node].Name);

            int phraseLen = astTokens[parent].Children.Count;
            bool closeSymbolPassed = false;
            string childName;

            for (int ChildInPhrase = childPosition + 1; ChildInPhrase < phraseLen; ChildInPhrase++)
            {
                childName = astTokens[astTokens[parent].Children[ChildInPhrase]].Name;

                if (closeSymbolPassed == false)
                {
                    // Shift each node after open symbol left one position.
                    ReplaceNode(astTokens, astTokens[parent].Children[ChildInPhrase - 1], 
                        astTokens[parent].Children[ChildInPhrase]);
                }
                else
                {
                    // Shift each node after open symbol left two positions.
                    ReplaceNode(astTokens, astTokens[parent].Children[ChildInPhrase - 2], 
                        astTokens[parent].Children[ChildInPhrase]);
                }

                if (childName == closeSymbol)
                {
                    closeSymbolPassed = true;
                }
            }

            // Remove extra slots that are left after removing symbols.
            astTokens[parent].Children.RemoveAt(phraseLen - 1);
            astTokens[parent].Children.RemoveAt(phraseLen - 2);

            // Remove symbols that are on their own tree level.
            //int phraseLen = astTokens[node].Children.Count;
            //astTokens[parent].Children.Clear();
            //for (int ChildInPhrase = 0; ChildInPhrase < phraseLen; ChildInPhrase++)
            //{
            //    //ReplaceNode(astTokens[parent].Children[ChildInPhrase - 1], astTokens[parent].Children[ChildInPhrase]);
            //    astTokens[parent].Children.Add(astTokens[node].Children[ChildInPhrase]);
            //    astTokens[astTokens[node].Children[ChildInPhrase]].Parent = parent;
            //}
        }

        private static string GetMatchingCloseSymbol(string openSymbol)
        {
            string closeSymbol;

            switch (openSymbol)
            {
                case "\"":
                    closeSymbol = "\"";
                    break;
                case "(":
                    closeSymbol = ")";
                    break;
                default:
                    closeSymbol = "";
                    break;
            }

            return closeSymbol;
        }

        // Creates a deep clone of an object by serializing it, and returns the clone.
        private static object CloneObject(object originalObject)
        {
            object clonedObject = null;
            //List<Token> tokensListClone = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, originalObject);
                memoryStream.Position = 0;
                clonedObject = binaryFormatter.Deserialize(memoryStream);
                //tokensListClone = (List<Token>)objectClone;
            }

            return clonedObject;
        }

        // Creates a deep clone of a list of tokens by serializing it, and returns the copy.
        private static List<Token> CloneTokenList(List<Token> tokens)
        {
            object objectClone = null;
            List<Token> tokensListClone = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, tokens);
                memoryStream.Position = 0;
                objectClone = binaryFormatter.Deserialize(memoryStream);
                tokensListClone = (List<Token>)objectClone;
            }

            return tokensListClone;
        }

        // Copies a subtree from a given tree to the new tree (recursive),
        // eliminating all the unused token nodes in the new tree.
        private static int CopyTokenSubTree(List<Token> treeTokens, List<Token> newTreeTokens, 
            int node, int newParent = -1)
        {
            int child = 0;
            Token newToken = new Token();
            newTreeTokens.Add(newToken);

            int newNode = newTreeTokens.Count - 1;
            int newChild;

            newTreeTokens[newNode].Name = treeTokens[node].Name;
            newTreeTokens[newNode].Terminal = treeTokens[node].Terminal;
            newTreeTokens[newNode].Literal = treeTokens[node].Literal;
            newTreeTokens[newNode].Type = treeTokens[node].Type;
            newTreeTokens[newNode].HasPlural = treeTokens[node].HasPlural;
            newTreeTokens[newNode].IsPlural = treeTokens[node].IsPlural;
            newTreeTokens[newNode].HasPastTense = treeTokens[node].HasPastTense;
            newTreeTokens[newNode].IsPastTense = treeTokens[node].IsPastTense;
            newTreeTokens[newNode].Parent = newParent;

            //File.AppendAllText(AppProperties.ServerLogPath, "Token Name = " + astTokens[node].Name.PadRight(20) +
            //    "# of Children = " + astTokens[node].Children.Count + "\r\n");

            if (treeTokens[node].Terminal == false)
            {
                child = 0;
                while (child < treeTokens[node].Children.Count)
                {
                    newChild = CopyTokenSubTree(treeTokens, newTreeTokens, treeTokens[node].Children[child], newNode);
                    newTreeTokens[newNode].Children.Add(newChild);
                    child = child + 1;
                }
            }

            return newNode;
        }

        private static string GetParseTreeDescription(bool ast = false)
        {
            string parseTreeOutput = "";
            int indentLevel = 0;

            if (ast)
            {
                parseTreeOutput = "Abstract Syntax Tree:" + "\r\n" + "-----------\r\n";
                parseTreeOutput = parseTreeOutput + GetSubTreeDescription(astTokens, astRoot, indentLevel);
            }
            else
            {
                parseTreeOutput = "Parse Tree:" + "\r\n" + "-----------\r\n";
                parseTreeOutput = parseTreeOutput + GetSubTreeDescription(parseTreeTokens, parseTreeRoot, indentLevel);
            }

            return parseTreeOutput;
        }

        private static string GetSubTreeDescription(List<Token> subTreeTokens, int node, int indentLevel)
        {
            string subTreeDescription = "";
            int child = 0;
            int tabCount = 0;

            while (tabCount < indentLevel)
            {
                subTreeDescription = subTreeDescription + "   ";
                tabCount = tabCount + 1;
            }
            subTreeDescription = subTreeDescription + "<" + subTreeTokens[node].Name + ">\r\n";

            if (subTreeTokens[node].Terminal == false)
            {
                indentLevel = indentLevel + 1;
                child = 0;
                while (child < subTreeTokens[node].Children.Count)
                {
                    subTreeDescription = subTreeDescription + GetSubTreeDescription(
                        subTreeTokens, subTreeTokens[node].Children[child], indentLevel);
                    child = child + 1;
                }
            }
            else
            {
                tabCount = 0;
                while (tabCount <= indentLevel)
                {
                    subTreeDescription = subTreeDescription + "   ";
                    tabCount = tabCount + 1;
                }
                subTreeDescription = subTreeDescription + "\"" + subTreeTokens[node].Literal + "\"\r\n";
            }

            return subTreeDescription;
        }

        // Outputs topTokens as text.
        private static string GetTopTokensOutput(int startPos)
        {
            int pos = startPos;
            string topTokensString = "Top tokens: ";

            while (pos < topTokensLen)
            {
                topTokensString = topTokensString + "[" + parseTreeTokens[topTokens[pos]].Name + "]";
                pos = pos + 1;
            }
            topTokensString = topTokensString + "\r\n";

            return topTokensString;
        }

        // Gets current phrase info as text for log output.
        private static string GetPhraseInfoOutput(string subParseName, int tokenStringStart,
            int tokenStringLength)
        {
            string phraseInfo = "";
            phraseInfo = phraseInfo + "subParseName: \"" + subParseName + "\"\r\n";
            phraseInfo = phraseInfo + "tokenString start: " + tokenStringStart +
                ", length: " + tokenStringLength + "\r\n";
            phraseInfo = phraseInfo + "tokenString: ";
            phraseInfo = phraseInfo + GetPhraseOutput(tokenStringStart, tokenStringLength);
            phraseInfo = phraseInfo + "\r\n";

            return phraseInfo;
        }

        // Outputs current phrase (without a carriage return).
        private static string GetPhraseOutput(int startPos, int tokenStringLength)
        {
            string phrase = "";
            int pos;
            pos = startPos;
            while (pos < (startPos + tokenStringLength) && (topTokens.Count > pos))
            {
                phrase = phrase + "[" + parseTreeTokens[topTokens[pos]].Name + "]";
                pos = pos + 1;
            }

            return phrase;
        }

        // ASCII underscore: 95
        static bool VerifyIdentifier(string newToken)
        {
            int pos;  // position in token string
            bool isIdentifier;

            isIdentifier = false;

            if (newToken.Length <= 64)
            {
                // Token is within size limit for identifiers of 64 chars.
                if (newToken != "" && VerifyAlpha(newToken[0]) == true)
                {
                    isIdentifier = true;
                    pos = 1;
                    while (isIdentifier == true && pos < newToken.Length)
                    {
                        if ((VerifyAlpha(newToken[pos]) == true
                          || VerifyNumeric(newToken[pos]) == true
                          || newToken[pos] == '_') == false)
                        {
                            isIdentifier = false;
                        }
                        pos = pos + 1;
                    }
                }
            }
            return isIdentifier;
        }

        static string VerifyNumber(string newToken)
        {
            int pos;  // position in token string
            bool isNumber = false;
            string numberType = "";

            if (newToken.Length <= 11)  // maximum digits is 11 (-9999999999)
            {
                // Token is within size limit for numbers
                if (newToken != "" && VerifyNumeric(newToken[0]) == true)
                {
                    isNumber = true;
                    pos = 1;
                    while (isNumber == true && pos < newToken.Length)
                    {
                        if (VerifyNumeric(newToken[pos]) == false)
                        {
                            isNumber = false;
                        }
                        pos = pos + 1;
                    }
                }

                int value;
                if (int.TryParse(newToken.Trim(), out value))
                {
                    if (value >= 0)
                    {
                        numberType = "natural_number";
                    }
                    else
                    {
                        numberType = "integer";
                    }
                }
            }

            return numberType;
        }

        // ASCII uppercase letters: 65 - 90
        // ASCII lowercase letters:  97 - 122
        static bool VerifyAlpha(char ch)
        {
            if ((ch >= 65 && ch <= 90) || (ch >= 97 && ch <= 122))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // ASCII numerals: 48-57
        static bool VerifyNumeric(char ch)
        {
            if ((ch >= 48) && (ch <= 57))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Removes all unnecessary whitespace characters from a string, leaving only 
        // spaces necessary for separating words.
        // (Need to strip comments eventually.)
        private static string StripWhiteSpace(string str)
        {
            int pos = 0;  // position in string
            bool whiteSpaceAdded = false;
            bool inStrLiteral = false;
            string strStripped = "";

            str = str.Trim();

            // Remove whitespace within the string
            while (pos < str.Length)
            {
                if (str[pos] == '"')
                {
                    if (inStrLiteral == false)
                    {
                        inStrLiteral = true;
                    }
                    else
                    {
                        inStrLiteral = false;
                    }
                }

                if (inStrLiteral == false)
                {
                    if (str[pos] == ' ' || str[pos] == '\t' ||
                        str[pos] == '\n')
                    {
                        if (whiteSpaceAdded == false)
                        {
                            strStripped = strStripped + " ";
                            whiteSpaceAdded = true;
                        }
                    }
                    // All chars which should have a preceding space and a following space.
                    else if (str[pos] == ';' || str[pos] == '(' || str[pos] == ')' || str[pos] == '{' ||
                        str[pos] == '}' || str[pos] == ',' || str[pos] == '=' || str[pos] == '+' ||
                        str[pos] == '-')
                    {
                        if (whiteSpaceAdded == false)
                        {
                            // Add a preceding space then the char then a space.
                            strStripped = strStripped + " " + str[pos] + " ";
                        }
                        else
                        {
                            // Leave preceding space then add the char then a space.
                            strStripped = strStripped + str[pos] + " ";
                        }
                        whiteSpaceAdded = true;
                    }
                    else
                    {
                        strStripped = strStripped + str[pos];
                        whiteSpaceAdded = false;
                    }
                }
                else
                {
                    strStripped = strStripped + str[pos];
                }
                pos = pos + 1;
            }

            return strStripped.Trim();
        }

        public static string GetUDWordsForClient()
        {
            string udWords = "";

            for (int rule = 0; rule < LangBnf.LexRules.Count; rule++)
            {
                if (LangBnf.LexRules[rule].Token == "adj" ||
                    LangBnf.LexRules[rule].Token == "disc_obj_noun" ||
                    LangBnf.LexRules[rule].Token == "non_disc_obj_noun" ||
                    LangBnf.LexRules[rule].Token == "class_noun" ||
                    LangBnf.LexRules[rule].Token == "trans_verb" ||
                    LangBnf.LexRules[rule].Token == "intrans_verb")
                {
                    for (int clause = 0; clause < LangBnf.LexRules[rule].Clauses.Count; clause++)
                    {
                        if (LangBnf.LexRules[rule].Clauses[clause].UserDefined)
                        {
                            //type
                            //<temp>
                            //word
                            //word_spoken_rec
                            //word_spoken_synth
                            //noun_plural
                            //noun_plural_spoken_rec
                            //noun_plural_spoken_synth
                            //verb_singular
                            //verb_singular_spoken_rec
                            //verb_singular_spoken_synth
                            //verb_past
                            //verb_past_spoken_rec
                            //verb_past_spoken_synth

                            // word clause delimiter: NewLine, item delimiter: "|"

                            udWords = udWords + LangBnf.LexRules[rule].Token + "|" +
                                LangBnf.LexRules[rule].Clauses[clause].Temp + "|" +
                                LangBnf.LexRules[rule].Clauses[clause].Items[0].Word + "|" +
                                LangBnf.LexRules[rule].Clauses[clause].Items[0].SpokenRec + "|" +
                                LangBnf.LexRules[rule].Clauses[clause].Items[0].SpokenSynth + "|";
                            switch (LangBnf.LexRules[rule].Token)
                            {
                                case "disc_obj_noun":
                                case "non_disc_obj_noun":
                                case "adj":
                                    udWords = udWords + "||||||||";
                                    break;
                                case "class_noun":
                                    udWords = udWords +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[1].Word + "|" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[1].SpokenRec + "|" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[1].SpokenSynth +
                                        "||||||";
                                    break;
                                case "intrans_verb":
                                case "trans_verb":
                                    udWords = udWords + "|||" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[1].Word + "|" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[1].SpokenRec + "|" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[1].SpokenSynth + "|" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[2].Word + "|" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[2].SpokenRec + "|" +
                                        LangBnf.LexRules[rule].Clauses[clause].Items[2].SpokenSynth;
                                    break;
                                default:
                                    break;
                            }
                            udWords = udWords + Environment.NewLine;
                        }
                    }
                }
            }

            File.AppendAllText(AppProperties.ServerLogPath,
                "UD Words data formatted for client:\r\n" + udWords + "\r\n");

            return udWords;
        }

        public static void LoadUDWords()
        {
            int rule;
            int clause;
            string bnfType;

            try
            {
                SqlCommand sql = new SqlCommand(
                "SELECT type, base, base_spoken_rec, base_spoken_synth, " +
                "noun_plural, noun_plural_spoken_rec, noun_plural_spoken_synth, " +
                    "verb_singular, verb_singular_spoken_rec, verb_singular_spoken_synth, " +
                    "verb_past, verb_past_spoken_rec, verb_past_spoken_synth " +
                "FROM ud_word ORDER BY type, base", KBConnection);

                using (SqlDataReader reader = sql.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bnfType = LangBnf.GetBnfType(reader[0].ToString());
                        rule = LangBnf.LexRules.FindIndex(
                            lexRule => lexRule.Token == bnfType);
                        LangBnf.LexRules[rule].Clauses.Add(new LangBnf.LexClause(true, false));
                        clause = LangBnf.LexRules[rule].Clauses.Count - 1;
                        LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                            new LangBnf.LexItem(reader[1].ToString(), reader[2].ToString(),
                            reader[3].ToString()));

                        switch (bnfType)
                        {
                            case "class_noun":
                                LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                                    new LangBnf.LexItem(reader[4].ToString(), reader[5].ToString(),
                                    reader[6].ToString()));
                                break;
                            case "intrans_verb":
                            case "trans_verb":
                                LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                                    new LangBnf.LexItem(reader[7].ToString(), reader[8].ToString(),
                                    reader[9].ToString()));
                                LangBnf.LexRules[rule].Clauses[clause].Items.Add(
                                    new LangBnf.LexItem(reader[10].ToString(), reader[11].ToString(),
                                    reader[12].ToString()));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (SqlException error)
            {
                Console.WriteLine("SQL Server Error, " + error.Message);
                File.AppendAllText(AppProperties.ServerLogPath, "SQL Error, " + error.Message + "\r\n");
            }
        }

        private static string GetLanguageDefinition(bool simpleVersion)
        {
            string languageDefinition;

            languageDefinition = "Language Rules:\r\n\r\n";
            
            for (int rule = 0; rule < LangBnf.LangRules.Count; rule++)
            {
                if (simpleVersion)
                {
                    languageDefinition = languageDefinition + LangBnf.LangRules[rule].Token + "\r\n";
                }
                else
                {
                    languageDefinition = languageDefinition + "Rule [" + rule.ToString().PadLeft(3) + "] " +
                        LangBnf.LangRules[rule].Token + "\r\n";
                }

                for (int clause = 0; clause < LangBnf.LangRules[rule].Clauses.Count; clause++)
                {
                    if (simpleVersion)
                    {
                        languageDefinition = languageDefinition + "\t[Clause]\r\n";
                    }
                    else
                    {
                        languageDefinition = languageDefinition + "\tClause [" + clause.ToString().PadLeft(2) + "] " +
                        "\r\n";
                    }

                    for (int item = 0; item < LangBnf.LangRules[rule].Clauses[clause].Items.Count; item++)
                    {
                        if (simpleVersion)
                        {
                            languageDefinition = languageDefinition + "\t\t" + 
                                LangBnf.LangRules[rule].Clauses[clause].Items[item] + "\r\n";
                        }
                        else
                        {
                            languageDefinition = languageDefinition + "\t\tItem [" + item.ToString().PadLeft(2) + "] " +
                                LangBnf.LangRules[rule].Clauses[clause].Items[item] + "\r\n";
                        }
                    }
                }
            }

            languageDefinition = languageDefinition + "\r\nLexical Rules:\r\n\r\n";
            for (int rule = 0; rule < LangBnf.LexRules.Count; rule++)
            {
                if (simpleVersion)
                {
                    languageDefinition = languageDefinition + LangBnf.LexRules[rule].Token + "\r\n";
                }
                else
                {
                    languageDefinition = languageDefinition + "Rule [" + 
                        rule.ToString().PadLeft(3) + "] " +
                        LangBnf.LexRules[rule].Token + "\r\n";
                }

                for (int clause = 0; clause < LangBnf.LexRules[rule].Clauses.Count; clause++)
                {
                    if (simpleVersion)
                    {
                        languageDefinition = languageDefinition + "\t";
                    }
                    else
                    {
                        languageDefinition = languageDefinition +
                            "\tClause [" + clause.ToString().PadLeft(2) + "] \r\n";
                    }

                    languageDefinition = languageDefinition + 
                        LangBnf.LexRules[rule].Clauses[clause].Items[0].Word;

                    for (int item = 1; item < LangBnf.LexRules[rule].Clauses[clause].Items.Count; 
                        item++)
                    {
                        languageDefinition = languageDefinition + ", " +
                            LangBnf.LexRules[rule].Clauses[clause].Items[item].Word;
                    }

                    languageDefinition = languageDefinition + "\r\n\t(UD: " +
                        LangBnf.LexRules[rule].Clauses[clause].UserDefined +
                        ", Temp: " + LangBnf.LexRules[rule].Clauses[clause].Temp + ")\r\n";
                }
            }

            return languageDefinition;
        }

        private class Stmt
        {
            public string Type;
            public int ASTNode;
            public QueryStmt Query;
            public ImperativeStmt ImperStmt;
        }

        private class QueryStmt
        {
            public string Type;  // "truthof", "what", "where", etc.
            public int SubjectNode;
        }

        private class ImperativeStmt
        {
            public string Type;  // "find", etc.
            public string SubjectWord;
            public int SubjectNode;
        }
    }

    // Token-node structure for linked-list parse tree of statements
    [Serializable()]
    public class Token
    {
        public string Name = "";
        public bool Terminal = false;  // indicates if token is a terminal or not
        public string Literal = "";    // literal string if token is terminal
        public string Type = "";       // Language rule type (such as "list")
        public bool HasPlural = false;    // True if noun or verb has a separate plural form
        public bool IsPlural = false;    // True if this is the plural of a noun or verb
        public bool HasPastTense = false;    // True if this verb has a separate past tense
        public bool IsPastTense = false;    // True if this is the past tense of a verb
        public int Parent = -1;        // no link indicated by -1
        public List<int> Children = new List<int>();
    }

    [Serializable()]
    public class WordInStatement
    {
        public string Name;
        public List<int> Locations = new List<int>();
    }

    [Serializable()]
    public class KnowledgeStmt
    {
        public bool Temporary = false;
        public List<Token> StmtTokens = new List<Token>();

        public List<WordInStatement> Keywords;
    }

    // WILL HAVE CONNECTIONS TO ELEMENTS OF MEANING: SENSORY PATTERNS, MOTOR PATTERNS,
    // AND RELATIONSHIPS TO OTHER WORDS.
    [Serializable()]
    public class KnowledgeWord
    {
        //"discobjnoun"
        //"nondiscobjnoun"
        //"classnoun"
        //"intransverb"
        //"transverb"
        //"adj"
        public bool Temporary = false;
        public string Type = "";
        public string Name = "";  // Base form is same as singular class nouns, plural verbs.
        // Plural verb is same as base form ("they fly", "to fly", "will fly")
        public string NounPlural = "";
        public string VerbSingular = "";
        public string VerbPast = "";
        public List<int> InStmts = new List<int>();  // Statements word is found in.
    }

    // Holds all linguistic-knowledge items.
    [Serializable()]
    public class LangKnowledge
    {
        public List<KnowledgeStmt> Stmts = new List<KnowledgeStmt>();
        public List<KnowledgeWord> Words = new List<KnowledgeWord>();

        public void AddStmt(List<Token> stmtTokens, bool temporary)
        {
            int stmt;

            stmt = FindStmt(stmtTokens);

            if (stmt == -1)
            {
                // New statement is not in list, so add it.
                Stmts.Add(new KnowledgeStmt());
                stmt = Stmts.Count - 1;
                Stmts[stmt].StmtTokens = stmtTokens;
                Stmts[stmt].Temporary = temporary;
                LoadKeywords(Stmts[stmt], 0);
            }
            else
            {
                // If new statement is not temporary but matching existing statement is,
                // update existing statement to not temporary.
                if (Stmts[stmt].Temporary == true && temporary == false)
                {
                    Stmts[stmt].Temporary = false;
                }
            }
        }

        private void LoadKeywords(KnowledgeStmt stmt, int node)
        {
            stmt.Keywords = new List<WordInStatement>();
            FindKeywordLocations(stmt, node);
        }

        // Recursive.
        private void FindKeywordLocations(KnowledgeStmt stmt, int node)
        {
            int child = 0;

            if (stmt.StmtTokens[node].Terminal == false)
            {
                child = 0;
                while (child < stmt.StmtTokens[node].Children.Count)
                {
                    FindKeywordLocations(stmt, stmt.StmtTokens[node].Children[child]);
                    child = child + 1;
                }
            }
            else
            {
                if (stmt.StmtTokens[node].Name == "disc_obj_noun" ||
                    stmt.StmtTokens[node].Name == "non_disc_obj_noun" ||
                    stmt.StmtTokens[node].Name == "class_noun" ||
                    stmt.StmtTokens[node].Name == "intrans_verb" ||
                    stmt.StmtTokens[node].Name == "trans_verb" ||
                    stmt.StmtTokens[node].Name == "adj")
                {
                    int index = stmt.Keywords.FindIndex(word => word.Name == stmt.StmtTokens[node].Name);

                    if (index == -1)
                    {
                        // Word isn't in the list, so add it.
                        stmt.Keywords.Add(new WordInStatement());
                        index = stmt.Keywords.Count - 1;
                        stmt.Keywords[index].Name = stmt.StmtTokens[node].Literal;
                    }

                    stmt.Keywords[index].Locations.Add(node);
                }
            }

        }

        public void AddWord(string word, bool temporary)
        {
            KnowledgeWord newWord = GetWordFromBnf(word);
            newWord.Temporary = temporary;

            if (!Words.Exists(item => item.Name == newWord.Name))
            {
                    Words.Add(newWord);
            }
        }

        private string GetWordBase(string word)
        {
            string wordBase = "";

            KnowledgeWord knowledgeWord = GetWordFromBnf(word);

            if (knowledgeWord.Name != "")
            {
                wordBase = knowledgeWord.Name;
            }

            return wordBase;
        }

        // CONVERT TO LAMBDA EXPRESSIONS TO SIMPLIFY.
        private KnowledgeWord GetWordFromBnf(string word)
        {
            KnowledgeWord newWord = new KnowledgeWord();

            int rule = 0;
            while (rule < LangBnf.LexRules.Count && newWord.Name == "")
            {
                int clause = 0;
                while (clause < LangBnf.LexRules[rule].Clauses.Count && newWord.Name == "")
                {
                    int item = 0;
                    while (item < LangBnf.LexRules[rule].Clauses[clause].Items.Count && newWord.Name == "")
                    {
                        if (LangBnf.LexRules[rule].Clauses[clause].Items[item].Word == word)
                        {
                            newWord.Type = LangBnf.LexRules[rule].Token;
                            newWord.Name = LangBnf.LexRules[rule].Clauses[clause].Items[0].Word;

                            switch (LangBnf.LexRules[rule].Token)
                            {
                                case "classnoun":
                                    newWord.NounPlural = LangBnf.LexRules[rule].Clauses[clause].Items[1].Word;
                                    break;
                                case "intransverb":
                                case "transverb":
                                    newWord.VerbSingular = LangBnf.LexRules[rule].Clauses[clause].Items[1].Word;
                                    newWord.VerbPast = LangBnf.LexRules[rule].Clauses[clause].Items[2].Word;
                                    break;
                            }
                        }
                        item++;
                    }
                    clause++;
                }
                rule++;
            }

            return newWord;
        }

        private int FindStmt(List<Token> stmtTokens)
        {
            int index = -1;

            for (int stmtNum = 0; stmtNum < Stmts.Count; stmtNum++)
            {
                if (Language.GetTokenTreeText(Stmts[stmtNum].StmtTokens, 0) ==
                    Language.GetTokenTreeText(stmtTokens, 0))
                {
                    index = stmtNum;
                    stmtNum = Stmts.Count;
                }
            }

            return index;
        }

        // Returns a list of all statements that match the keyword.
        public List<int> FindStmtsContainingWord(string word)
        {
            List<int> StmtsContainingWord = new List<int>();
            string wordBase = GetWordBase(word);

            for (int stmtNum = 0; stmtNum < Stmts.Count; stmtNum++)
            {
                if (Stmts[stmtNum].Keywords.Exists(keyWord => keyWord.Name == wordBase))
                {
                    StmtsContainingWord.Add(stmtNum);
                }
            }

            return StmtsContainingWord;
        }

        public void RemoveTempItems()
        {
            Stmts.RemoveAll(stmt => stmt.Temporary == true);
            Words.RemoveAll(word => word.Temporary == true);
        }
    }
}