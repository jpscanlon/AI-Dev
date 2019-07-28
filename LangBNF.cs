// Loads interface language definition into data structures.
using System.Collections.Generic;

//namespace AIDev
//{
class LangBNF
{
    public static List<LangRule> LangRules;
    public static List<LexRule> LexRules;

    public static int LongestPhraseSize { get; protected set; }

    public static void LoadLangRules()
    {
        int rule;
        int clause;

        LangRules = new List<LangRule>();

        // ***** LOAD LANGUAGE DEFINITION RULES *******************************************************************

        // TOP-LEVEL RULES

        // Statement list
        rule = 0;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "stmt_list";
        LangRules[rule].Type = "list";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("stmt");

        // Alternative to "list" type.
        //LangRules.Add(new LangRule());
        //LangRules[rule].Token = "stmt_list";
        //LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        //LangRules[rule].Clauses[clause].Items.Add("stmt");
        //LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("stmt_list");  // ultimately, just this
        //LangRules[rule].Clauses[clause].Items.Add("stmt");       // should be used

        // Alternative coding (longer lines, less readability).
        //LangRules[rule].Clauses.Add(new LangClause());
        //LangRules[rule].Clauses[LangRules[rule].Clauses.Count - 1].Items.Add("stmt");
        //LangRules[rule].Clauses.Add(new LangClause());
        //LangRules[rule].Clauses[LangRules[rule].Clauses.Count - 1].Items.Add("stmt_list");
        //LangRules[rule].Clauses[LangRules[rule].Clauses.Count - 1].Items.Add("stmt");
        //LangRules[rule].Clauses.Add(new LangClause());

        // Statement type
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("decl_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("query_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("imperative_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("cancel_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("createkb_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("openkb_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("closekb_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("newword_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("pronounce_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("deleteword_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("learn_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("if_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("while_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("langdef_stmt");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("test_stmt");

        // CONTROL FLOW STATEMENTS
        //	A phrase with:
        //	"if" as conjunction resolves to a truth value:
        //		"(a circle is red) if (a square is blue);"
        //	"if" as control flow doesn't:
        //		"if (a square is blue) {a circle is red;}

        // "if" statement (control-flow conditional)
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "if_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("ifcase");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("{");
        LangRules[rule].Clauses[clause].Items.Add("stmt_list");
        LangRules[rule].Clauses[clause].Items.Add("}");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("ifcase");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("bool_expr");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("{");
        LangRules[rule].Clauses[clause].Items.Add("stmt_list");
        LangRules[rule].Clauses[clause].Items.Add("}");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("if_stmt");
        LangRules[rule].Clauses[clause].Items.Add("else");
        LangRules[rule].Clauses[clause].Items.Add("{");
        LangRules[rule].Clauses[clause].Items.Add("stmt_list");
        LangRules[rule].Clauses[clause].Items.Add("}");
        // Allows "ifcase ( ) { } else ifcase ( ) { } else { }"
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("if_stmt");
        LangRules[rule].Clauses[clause].Items.Add("else");
        LangRules[rule].Clauses[clause].Items.Add("if_stmt");

        // "while" statement (control-flow iteration)
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "while_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("while");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("{");
        LangRules[rule].Clauses[clause].Items.Add("stmt_list");
        LangRules[rule].Clauses[clause].Items.Add("}");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("while");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("bool_expr");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("{");
        LangRules[rule].Clauses[clause].Items.Add("stmt_list");
        LangRules[rule].Clauses[clause].Items.Add("}");

        // SEMICOLON-TERMINATED STATEMENTS

        // Declarative statement
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "decl_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("remember");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // Query statement
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "query_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("truthof");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("is");
        LangRules[rule].Clauses[clause].Items.Add("what");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("truthof");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("bool_expr");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("is");
        LangRules[rule].Clauses[clause].Items.Add("what");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("what");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("what");
        LangRules[rule].Clauses[clause].Items.Add("query_qual");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("where");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("is_phra");  // OR "is_phra"
        LangRules[rule].Clauses[clause].Items.Add("where");
        LangRules[rule].Clauses[clause].Items.Add("query_qual");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add("when_why_how");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // Query qualifier
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "query_qual";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("temporal_conj");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("if");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");

        // Imperative statements:
        //		(close the_door and lock the_door) if the_door is open;
        //		close the_door and (lock the_door after you open the_door);
        // "if" and temporal conjunctions are always followed by decl. phrases
        // Imperative statement
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "imperative_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("imperative_and_or_phra");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("imperative_and_or_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("ifcase");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("imperative_and_or_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("temporal_conj");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add(";");
        //LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("find");
        //LangRules[rule].Clauses[clause].Items.Add("string_lit");
        //LangRules[rule].Clauses[clause].Items.Add("in");
        //LangRules[rule].Clauses[clause].Items.Add("(");
        //LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");
        //LangRules[rule].Clauses[clause].Items.Add(";");

        // Imperative-and/or phrase
        // (contains only predicates and "and/or" conjunctions)
        // Needs to be re-worked - the relationship between logical "if" and control-flow "ifcase" and "and"/"or".
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "imperative_and_or_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("predicate");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("imperative_and_or_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("logic_conj");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("imperative_and_or_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");

        // "cancel" statement - cancels the current statement list being executed
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "cancel_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("cancel");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // "createkb" statement - creates a new knowledge base
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "createkb_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("createkb");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // "openkb" statement - opens a knowledge base
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "openkb_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("openkb");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // "closekb" statement - closes an open knowledge base
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "closekb_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("closekb");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // "langdef" statement - returns language definition or a part of it.
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "langdef_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("langdef");
        LangRules[rule].Clauses[clause].Items.Add(";");
        // Returns the definition a word, including pronunciation.
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("langdef");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        //LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // "newword" statement - creates a new user-defined word
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "newword_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("newword");
        LangRules[rule].Clauses[clause].Items.Add("simple_word_type");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("newword");  // new temporary word
        LangRules[rule].Clauses[clause].Items.Add("temp");
        LangRules[rule].Clauses[clause].Items.Add("simple_word_type");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("newword");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_word_type");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Base, singular
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Plural
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("newword");  // new temporary word
        LangRules[rule].Clauses[clause].Items.Add("temp");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_word_type");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Base, singular
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Plural
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("newword");
        LangRules[rule].Clauses[clause].Items.Add("verb_word_type");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Base, plural
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Singular
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Past
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("newword");  // new temporary word
        LangRules[rule].Clauses[clause].Items.Add("temp");
        LangRules[rule].Clauses[clause].Items.Add("verb_word_type");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Base, plural
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Singular
        LangRules[rule].Clauses[clause].Items.Add("string_lit");  // Past
        LangRules[rule].Clauses[clause].Items.Add(";");

        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "pronounce_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("pronounce");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add("spokenrec");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add("spokensynth");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("pronounce");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add("spokenrec");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("pronounce");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add("spokensynth");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // "deleteword" statement - deletes a user-defined word
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "deleteword_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("deleteword");
        LangRules[rule].Clauses[clause].Items.Add("string_lit");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // "learn" statement - describes a learning task/procedure/process
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "learn_stmt";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("learn");
        LangRules[rule].Clauses[clause].Items.Add("quote");
        LangRules[rule].Clauses[clause].Items.Add("string"); // handle in semantics
        LangRules[rule].Clauses[clause].Items.Add("quote");
        LangRules[rule].Clauses[clause].Items.Add(";");
        // Uses all images in I/O folder as examples of visual class noun to learn
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("learn");
        LangRules[rule].Clauses[clause].Items.Add("image");
        LangRules[rule].Clauses[clause].Items.Add("class_noun");
        LangRules[rule].Clauses[clause].Items.Add(";");

        // EXPRESSIONS

        // Boolean expression
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "bool_expr";
        // An example expression
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("integer");
        LangRules[rule].Clauses[clause].Items.Add("+");
        LangRules[rule].Clauses[clause].Items.Add("integer");
        LangRules[rule].Clauses[clause].Items.Add("=");
        LangRules[rule].Clauses[clause].Items.Add("integer");

        // PHRASES

        // "inimage" phrase
        //	Two types: used here applies to everything between parentheses,
        //	and used as a prepositional phrase (defined in prepositional phrases).
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "inimage_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("inimage");
        LangRules[rule].Clauses[clause].Items.Add("natural_number");
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");

        // Declarative phrase
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "decl_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("inimage_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("predicate");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("is");
        LangRules[rule].Clauses[clause].Items.Add("false");
        // With logical conjunctions
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("logic_conj");
        //LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");
        // With temporal conjunctions & because
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("temporal_conj"); // tense must match
                                                                    //LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses[clause].Items.Add("because");  // tense needn't match
                                                                //LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        //LangRules[rule].Clauses[clause].Items.Add(")");

        // Noun phrase
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "noun_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("(");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add(")");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("and");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("or");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("disc_obj_noun");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("identifier");  // pronoun
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("specified_noun");
        // Reduce noun and specifying_phra to noun_phra only if it is in a specifying
        // phrase and matches the noun being specified.
        //LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("unspecified_noun");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("specified_noun");
        LangRules[rule].Clauses[clause].Items.Add("specifying_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("unspecified_noun");
        LangRules[rule].Clauses[clause].Items.Add("specifying_phra");

        // Comparitive noun phrase - with transitive verbs
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "compar_noun_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("as");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("as");
        LangRules[rule].Clauses[clause].Items.Add("object");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");

        // Specified noun
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "specified_noun";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("def_quant");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("log_quant");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("log_quant");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("disc_quant");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("disc_quant");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("natural_number");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("natural_number");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("non_disc_obj_noun");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("non_disc_obj_noun");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("def_quant");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("non_disc_obj_noun");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("log_quant");
        LangRules[rule].Clauses[clause].Items.Add("non_disc_obj_noun");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("log_quant");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("non_disc_obj_noun");

        // Unspecified noun (definite quantifier with no adjective list)
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "unspecified_noun";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("def_quant");
        LangRules[rule].Clauses[clause].Items.Add("class_noun_item");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("def_quant");
        LangRules[rule].Clauses[clause].Items.Add("non_disc_obj_noun");

        // Class noun with or without declaration of pronoun
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "class_noun_item";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("class_noun");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("class_noun");
        LangRules[rule].Clauses[clause].Items.Add(":");
        LangRules[rule].Clauses[clause].Items.Add("identifier");

        // Specifying phrase
        //  "the ball which is red" (predicate allowed only immediately after "which")
        //	"the ball which (tim has the ball) and (tim throw the ball) exist"
        //	"the ball which (tim has the ball) if (the_game is over)"
        //	"the red ball of tim and terry"
        //	"the ball of a game which resemble baseball"
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "specifying_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("which");
        LangRules[rule].Clauses[clause].Items.Add("decl_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("which");
        LangRules[rule].Clauses[clause].Items.Add("predicate");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("of");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");


        // Adjective phrase
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "adj_phra";
        LangRules[rule].Precedence = 2;  // adj_list must be reduced first.
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("adj_unit");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("adj_unit");
        //LangRules[rule].Clauses[clause].Items.Add("[list] adj_unit");
        LangRules[rule].Clauses[clause].Items.Add("adj_list");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("adj_unit");
        //LangRules[rule].Clauses[clause].Items.Add("[list] comma_delim_adj_unit");
        LangRules[rule].Clauses[clause].Items.Add("comma_delim_adj_list");

        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "adj_list";
        LangRules[rule].Type = "list";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        //LangRules[rule].Clauses[clause].Items.Add("[list] adj_unit");
        LangRules[rule].Clauses[clause].Items.Add("adj_unit");

        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "comma_delim_adj_list";
        LangRules[rule].Type = "list";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        //LangRules[rule].Clauses[clause].Items.Add("[list] comma_delim_adj_unit");
        LangRules[rule].Clauses[clause].Items.Add("comma_delim_adj_unit");

        // Comma-delimited adjective list item for text clarity
        // (commas left out for making spoken statements simpler).
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "comma_delim_adj_unit";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add(",");
        LangRules[rule].Clauses[clause].Items.Add("adj_unit");

        // Adjective unit (adjective with optional modifying adverb)
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "adj_unit";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("adj");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("adj_mod_adv");
        LangRules[rule].Clauses[clause].Items.Add("adj");

        // Predicate
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "predicate";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("intrans_verb_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("intrans_verb_phra");
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("trans_verb_phra");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("trans_verb_phra");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");
        //LangRules[rule].Clauses.Add(new LangClause()); clause++;
        //LangRules[rule].Clauses[clause].Items.Add("is_phra");  // when followed by where
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("has_phra");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("has_phra");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");
        //  property + adj_list could be used as a non_disc_obj_noun: 
        //	"something has under the_sky property tall"
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("has_phra");
        LangRules[rule].Clauses[clause].Items.Add("property_list");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("has_phra");
        LangRules[rule].Clauses[clause].Items.Add("property_list");
        LangRules[rule].Clauses[clause].Items.Add("prep_phra");

        // Intransitive verb phrase
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "intrans_verb_phra";
        // ***** FREQUENCY ADVERB SHOULD BE MOVED TO PREDICATE RULE SO IT CAN
        //       BE APPLIED TO HAS_PHRASE + PROPERTY_LIST *****
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("frequency_adv");
        LangRules[rule].Clauses[clause].Items.Add("intrans_verb_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("intrans_verb");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("tense");
        LangRules[rule].Clauses[clause].Items.Add("intrans_verb");
        // With negation
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("intrans_verb");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("tense");
        LangRules[rule].Clauses[clause].Items.Add("intrans_verb");
        LangRules[rule].Clauses[clause].Items.Add("not");

        // Transitive verb phrase
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "trans_verb_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("frequency_adv");
        LangRules[rule].Clauses[clause].Items.Add("trans_verb_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("trans_verb");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("tense");
        LangRules[rule].Clauses[clause].Items.Add("trans_verb");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        // With negation
        LangRules[rule].Clauses[clause].Items.Add("trans_verb");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("tense");
        LangRules[rule].Clauses[clause].Items.Add("trans_verb");
        LangRules[rule].Clauses[clause].Items.Add("not");

        // Tense
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "tense";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("did");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("will");

        // Is phrase
        // (always transitive, except when meaning "exist" followed by a prep_phra.)
        // AND MAYBE ALSO INSTRANSITIVE IF FOLLOWED BY "WHERE_WHY_WHEN_HOW"
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "is_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("frequency_adv");
        LangRules[rule].Clauses[clause].Items.Add("is_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("is");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("are");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("was");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("were");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("will");
        LangRules[rule].Clauses[clause].Items.Add("be");
        // With negation
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("is");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("was");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("will");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses[clause].Items.Add("be");

        // Has phrase
        // (transitive)
        // With negation
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "has_phra";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("frequency_adv");
        LangRules[rule].Clauses[clause].Items.Add("has_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("has");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("did");
        LangRules[rule].Clauses[clause].Items.Add("have");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("will");
        LangRules[rule].Clauses[clause].Items.Add("have");
        // With negation
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("does");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses[clause].Items.Add("have");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("did");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses[clause].Items.Add("have");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("will");
        LangRules[rule].Clauses[clause].Items.Add("not");
        LangRules[rule].Clauses[clause].Items.Add("have");

        // Prepositional phrase list
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "prep_phra";
        LangRules[rule].Type = "list";  // Contains one or more of the items.
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("prep_phra_item");

        // Prepositional phrase
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "prep_phra_item";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("prep");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("with");
        LangRules[rule].Clauses[clause].Items.Add("noun_phra");
        // "something drive with property fast under the_sky a car"
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("with");  // can function as adverb list
        LangRules[rule].Clauses[clause].Items.Add("property_list");
        // "which is inimage 1 inimage 2" means "in images 1 and 2"
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("inimage");
        LangRules[rule].Clauses[clause].Items.Add("natural_number");

        // Quantifier
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "disc_quant";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("num_quant");
        // REDUCTION OF NATURAL_NUMBER TO DISC_QUANT SHOULD HAPPEN ONLY WHEN USED AS A QUANTIFIER
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("natural_number");

        // Logical conjunction.
        // NEED TO PREVENT REDUCTION BEFORE "AND" AND "OR" JOINING NOUN PHRASES.
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "logic_conj";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("and");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("or");
        LangRules[rule].Clauses.Add(new LangClause()); clause++;
        LangRules[rule].Clauses[clause].Items.Add("if");

        // Property list
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "property_list";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("property");
        LangRules[rule].Clauses[clause].Items.Add("adj_phra");

        // String literal
        rule++;
        LangRules.Add(new LangRule());
        LangRules[rule].Token = "string_lit";
        LangRules[rule].Clauses.Add(new LangClause()); clause = 0;
        LangRules[rule].Clauses[clause].Items.Add("quote");
        LangRules[rule].Clauses[clause].Items.Add("string");
        LangRules[rule].Clauses[clause].Items.Add("quote");

        LongestPhraseSize = GetLongestPhraseSize();

        //LoadEBNF();
    }

    public static void LoadLexRules()
    {
        int rule;
        int clause;

        LexRules = new List<LexRule>();

        // ***** LOAD LEXICAL RULES *******************************************************************************

        // TOP-LEVEL KEYWORDS

        rule = 0;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "cancel";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("cancel"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "createkb";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("createkb", "create k b", "create k b"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "openkb";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("openkb", "open k b", "open k b"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "closekb";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("closekb", "close k b", "close k b"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "else";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("else"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "while";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("while"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "return";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("return"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "inimage";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("inimage"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "remember";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("remember"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "newword";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("newword", "new word", "new word"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "deleteword";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("deleteword", "delete word", "delete word"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "temp";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("temp"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "pronounce";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("pronounce"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "spokenrec";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("spokenrec"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "spokensynth";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("spokensynth"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "learn";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("learn"));

        // Used to learn to recognize a visual pattern assigned to a noun
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "image";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("image"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "load";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("load"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "images";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("images"));

        //rule++;
        //LexRules.Add(new LexRule());
        //LexRules[rule].Token = "find";
        //LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        //LexRules[rule].Clauses[clause].ItemsObjs.Add(new LexItem("find"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "langdef";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("langdef", "lang def", "lang def"));

        // Logical operator
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "false";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("false"));

        // Temporal conjunction (A <conjunction> B)
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "temporal_conj";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("before"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("when"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("after"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("while"));

        // Causal conjunction (A <conjunction> B)
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "because";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("because"));

        // QUANTIFIERS

        // Definite quantifier
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "def_quant";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("the"));

        // Logical quantifier
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "log_quant";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("no"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("some"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("all"));

        // Numeric quantifier
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "num_quant";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("a"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("an"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("multiple"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("few"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("many"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("most"));
        // Other
        //LexRules[rule].Clauses.Add(new LexClause()); clause++;
        //LexRules[rule].Clauses[clause].ItemsObjs.Add(new LexItem("one_million", "one million", "one million"));
        //LexRules[rule].Clauses.Add(new LexClause()); clause++;
        //LexRules[rule].Clauses[clause].ItemsObjs.Add(new LexItem("a gazillion"));

        // Logical conjunctions
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "and";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("and"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "or";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("or", "ore", null));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "if";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("if"));

        // SPECIFYING-PHRASE KEYWORDS

        // Which
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "which";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("which"));

        // Of (denotes possession)
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "of";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("of"));

        // OTHER KEYWORDS

        // "has/with property <adj_list>
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "property";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("property"));

        // "truthof (<decl_phra>) is what"
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "truthof";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("truthof", "truth of", "truth of"));

        // Query place holders
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "what";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("what"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "where";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("where"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "when_why_how";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("when"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("why"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("how"));

        // Is components
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "is";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("is"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "are";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("are"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "was";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("was"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "were";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("were"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "be";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("be"));

        // Has components
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "has";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("has"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "have";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("have"));

        // Verb tense
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "did";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("did"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "will";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("will"));

        // Adverb (ordinary adverb, goes after the verb it modifies)
        // "not" negates the verb, not the truth of the statement.
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "not";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("not"));

        // Frequency adverb
        // (goes in front of the verb it modifies)
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "frequency_adv";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("never"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("sometimes"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("frequently"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("usually"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("always"));

        // Adjective-modifier adverb
        // (goes in front of the adjective it modifies)
        // SEEMS THAT THIS FORM OF ADVERB SHOULDN'T BE USABLE WITH COMPARISON:
        //		"A is very red more than B is very red"
        // Degree
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "adj_mod_adv";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("slightly"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("moderately"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("very"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("partly"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("half"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("mostly"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("entirely"));

        // Comparison operators
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "comparison";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("more"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("less"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("equally"));

        // Other comparison keywords
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "as";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("as"));

        // "object" indicates that noun phrase is an object rather than subject.
        // "jane drinks equally often milk as object juice"
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "object";  // precedes the noun phrase it modifies
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("object"));

        // Preposition
        // Position
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "prep";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("above"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("below"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("rightof", "right of", "right of"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("leftof", "left of", "left of"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("infrontof", "in front of", "in front of")); // 2-D or 3-D
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("behind")); // 2-D or 3-D
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("nextto", "next to", "next to"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("in"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("outside"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("off"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("on"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        // opposite: "elvis sing at a place which is not near here"
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("near"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        // opposite: "elvis sing at a place which is not at here" or
        // "elvis sing at a place which is not here" (same meaning without the preposition)
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("at"));
        // Motion
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("to"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("from"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("toward"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("awayfrom", "away from", "away from"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("into"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("outof"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("up"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("down"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("over"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("under"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("through"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("around"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("across"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("along"));
        // Association
        // "with" also functions as a preposition for association.
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("without"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("like"));

        // Adverbial preposition
        // creates an adverb from an adjective ("with property <adj>")
        // "property <adj>" gets reduced to a noun phrase.
        // with and without can also be used as ordinary prepositions
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "with";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("with"));

        // User-Defined Word Types
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "simple_word_type";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "discobjnoun", "discrete object noun", "discrete object noun"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "nondiscobjnoun", "non discrete object noun", "non discrete object noun"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("adj"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "class_noun_word_type";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "classnoun", "class noun", "class noun"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "verb_word_type";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "intransverb", "intrans verb", "intrans verb"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "transverb", "trans verb", "trans verb"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("adj"));

        // Discrete Object noun
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "disc_obj_noun";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("something"));

        // Non-Discrete object noun:
        //	Can be modified by adjectives (red hair is attractive, 
        //		the red hair of jill is attractive)
        //	Can be modified by specifying phrases (water which is very cold will freeze)
        //	Can be modified by logical quantifiers and "the" (some communism is radical, 
        //		the water which is in the small jar is blue)
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "non_disc_obj_noun";

        // Class noun
        // Items:
        //      0: Singular and base form
        //      1: Plural
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "class_noun";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("portion"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("portions"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("thing"));  // unspecified object
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("things"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("dot"));  // SD (standard dictionary)
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("dots"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("line"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("lines"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("shape"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("shapes"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("circle"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("circles"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("triangle"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("triangles"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("square"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("squares"));

        // Intransitive verb
        // Items:
        //      0: Plural and base form
        //      1: Singular
        //      2: Past
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "intrans_verb";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("exist"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("exists"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("existed"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("begin"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("begins"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("began"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("end"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("ends"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("ended"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("move"));  // SD (standard dict.)
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("moves"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("moved"));

        // Transitive verb
        // Items:
        //      0: Plural and base form
        //      1: Singular
        //      2: Past
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "trans_verb";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("start"));  // SD (standard dict.)
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("starts"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("started"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("stop"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("stops"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("stopped"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("contain"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("contains"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("contained"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("enclose"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("encloses"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("enclosed"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("touch"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("touches"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("touched"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("open"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("opens"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("opened"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("close"));  // SD
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("closes"));
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("closed"));

        // Truth-value adjective
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "truth_val";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("true"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("false"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("unknown"));

        // Adjective
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "adj";
        // Logical colors
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lwhite", "logical white", "logical white"));  // RGB: 255, 255, 255
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lblack", "logical black", "logical black"));  // RGB: 0, 0, 0
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lred", "logical red", "logical red"));  // RGB: 255, 0, 0
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lgreen", "logical green", "logical green"));  // RGB: 0, 255, 0
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lblue", "logical blue", "logical blue"));  // RGB: 0, 0, 255
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lmagenta", "logical magenta", "logical magenta"));  // RGB: 255, 0, 255
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lyellow", "logical yellow", "logical yellow"));  // RGB: 255, 255, 0
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lcyan", "logical cyan", "logical cyan"));  // RGB: 0, 255, 255
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(
            "lorange", "logical orange", "logical orange"));  // RGB: 255, 128, 0
        // Non-logical color words
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("red"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("orange"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("yellow"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("green"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("blue"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("purple"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("white"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("black"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("brown"));
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("gray"));
        // Standard dictionary adjectives for testing and development.
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("small"));  // SD (standard dict.)
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("large"));  // SD
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("low"));  // SD
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("high"));  // SD
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("left"));  // SD
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("right"));  // SD
        // Adjectives that can be used as adverbs
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("slow"));  // SD
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("fast"));  // SD
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("open"));  // SD
        LexRules[rule].Clauses.Add(new LexClause()); clause++;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("closed"));  // SD

        // SYMBOLS

        // Assignment and equality
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "=";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("="));

        // Addition operator
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "+";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("+"));

        // Subtraction operator
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "-";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("-"));

        // Statement terminator
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = ";";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(";", "semicolon", "semicolon"));

        // Parentheses
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "(";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("(", "open paren", "open paren"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = ")";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(")", "close paren", "close paren"));

        // Braces for statement blocks
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "{";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        // Or pronounce "brace" like "open paren".
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("{", "block", "block"));

        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "}";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("}", "end block", "end block"));

        // Commas between list items (adjective list items)
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = ",";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem(","));

        // Quotes for string literals
        // quote = ["]  single quote = [']
        rule++;
        LexRules.Add(new LexRule());
        LexRules[rule].Token = "quote";
        LexRules[rule].Clauses.Add(new LexClause()); clause = 0;
        LexRules[rule].Clauses[clause].Items.Add(new LexItem("\"", "quote", "quote"));
    }

    // Returns the matching BNF token name for word type from the word-type as expressed in
    // statements. Returns an empty string if there isn't a match.
    public static string GetBNFType(string type)
    {
        string bnfType;

        switch (type)
        {
            case "adj":
                bnfType = "adj";
                break;
            case "discobjnoun":
                bnfType = "disc_obj_noun";
                break;
            case "nondiscobjnoun":
                bnfType = "non_disc_obj_noun";
                break;
            case "classnoun":
                bnfType = "class_noun";
                break;
            case "transverb":
                bnfType = "trans_verb";
                break;
            case "intransverb":
                bnfType = "intrans_verb";
                break;
            default:
                bnfType = "";
                break;
        }

        return bnfType;
    }

    // Loads item types from item strings into corresponding ItemTypes arrays. For example: takes
    // "[list] adj_unit" and sets the item type to enum ItemType.List, then removes "[list] " from the string.
    // Ordinary items get set to enum ItemType.Simple.
    private static void LoadEBNF()
    {
        int numClauseItems;

        for (int rule = 0; rule < LangRules.Count; rule++)
        {
            for (int clause = 0; clause < LangRules[rule].Clauses.Count; clause++)
            {
                numClauseItems = LangRules[rule].Clauses[clause].Items.Count;
                LangRules[rule].Clauses[clause].ItemTypes = new ItemType[numClauseItems];
                for (int item = 0; item < numClauseItems; item++)
                {
                    if (LangRules[rule].Clauses[clause].Items[item].StartsWith("[list]"))
                    {
                        LangRules[rule].Clauses[clause].ItemTypes[item] = ItemType.List;
                        LangRules[rule].Clauses[clause].Items[item] =
                            LangRules[rule].Clauses[clause].Items[item].Substring(7);
                    }
                    else
                    {
                        LangRules[rule].Clauses[clause].ItemTypes[item] = ItemType.Simple;
                    }
                }
            }
        }
    }

    private static int GetLongestPhraseSize()
    {
        int longestPhraseSize = 0;

        for (int rule = 0; rule < LangRules.Count; rule++)
        {
            for (int clause = 0; clause < LangRules[rule].Clauses.Count; clause++)
            {
                if (LangRules[rule].Clauses[clause].Items.Count > longestPhraseSize)
                {
                    longestPhraseSize = LangRules[rule].Clauses[clause].Items.Count;
                }
            }
        }

        return longestPhraseSize;
    }

    public class LangRule
    {
        public string Token = "";
        public string Type = "";
        public int Precedence = 1;  // Lower value has higher precedence.
        public List<LangClause> Clauses = new List<LangClause>();
    }

    public class LangClause
    {
        public List<string> Items = new List<string>();
        public ItemType[] ItemTypes;  // "list" for list of one or more.
    }

    public class LexRule
    {
        public string Token = "";
        public List<LexClause> Clauses = new List<LexClause>();
    }

    public class LexClause
    {
        public bool UserDefined = false;
        private bool temp = false;
        public bool Temp
        {
            get { return temp; }
            set
            {
                // Only UD words can be temporary.
                if (UserDefined == false)
                {
                    temp = false;
                }
                else
                {
                    temp = value;
                }
            }
        }
        public List<LexItem> Items = new List<LexItem>();

        public LexClause(bool userDefined = false, bool temp = false)
        {
            //// Only UD words can be temporary.
            //if (userDefined == false) temp = false;

            UserDefined = userDefined;
            Temp = temp;
        }
    }

    public class LexItem
    {
        public string Word;
        public string SpokenRec;  // For speech recognition
        public string SpokenSynth;  // For speech synthesis

        public LexItem(string word, string spokenRec = null, string spokenSynth = null)
        {
            if (spokenRec == "") spokenRec = null;
            if (spokenSynth == "") spokenSynth = null;

            Word = word;
            SpokenRec = spokenRec;
            SpokenSynth = spokenSynth;
        }
    }

    public class WordLocation
    {
        public int Rule;
        public int Clause;
        public int Item;
    }

    public enum ItemType
    {
        Simple,     // Just a simple item token.
        List        // Sequence of one or more of the item tokens.
    };

    public enum WordForm
    {
        Base,
        NounPlural,
        VerbSingular,
        VerbPast
    };
}
//}