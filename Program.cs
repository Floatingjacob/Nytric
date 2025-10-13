/*
 
 This is the main file for the Nytric interpreter.
 The 'key to the code' if you will.


 */
namespace Nytric
{
    class Program
    {
        static void Main(string[] args)
        {/*
            foreach (var argument in args)
            {
                Console.WriteLine(argument);
            }*/

            string source = "";
            bool executed = false;
            bool multiline = false;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(welcome);

            while (true)
            {
                try
                {
                    if (args.Length > 0 && !executed)  // If there is a command line argument, assume it is a file and interpret it as .ncs
                    {
                        source = File.ReadAllText(args[0]);
                        executed = true; // Makes sure Nytric doesnt spam execute the file over and over.
                    }

                    else
                    {

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("Nytric> "); // Pretty much an interactive shell that ONLY knows Nytric

                        Console.ForegroundColor = ConsoleColor.Yellow; // Makes the input yellow. (ooh, fancy...)
                        source = Console.ReadLine();
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        switch (source.ToUpper())
                        {

                            case "CLEAR" or "CLS":
                                Console.Clear();
                                source = "";
                                break;
                            case "MULTILINE" or "ML":
                                File.WriteAllText(".multiline", ""); // Makes sure the file is empty
                                multiline = true;
                                while (multiline)
                                {
                                    string input;
                                    input = Console.ReadLine();
                                    if (input.ToUpper() == "ENDMULTILINE" || input.ToUpper() == "EML") break;
                                    else File.WriteAllText(".multiline", $"{File.ReadAllText(".multiline")}\n{input}"); // Writes a new line of code to the temporary file
                                }
                                source = File.ReadAllText(".multiline"); // Runs the temporary file aftet multiline is done.
                                break;
                            case "EXIT" or "QUIT":
                                Environment.Exit(0); // Exits the program (duh)
                                break;
                            case "HELP" or "?":
                                Console.WriteLine(help);
                                source = "";
                                break;
                        }
                        // If you type 'ex', 'execute' or 'run', assume the second argument is a file path and interpret it as ncs
                        if (source.ToUpper().Split(' ', 2)[0] == "EX" || source.ToUpper().Split(' ', 2)[0] == "EXECUTE" || source.ToUpper().Split(' ', 2)[0] == "RUN")
                        {
                            source = File.ReadAllText(source.Split(' ', 2)[1]);

                        }
                    }
                    var lexer = new Lexer(source);
                    var tokens = lexer.Tokenize();

                    var parser = new Parser(tokens);
                    var ast = parser.Parse();

                    var interpreter = new Interpreter();
                    interpreter.Evaluate(ast);

                    //Console.WriteLine("--- Execution Finished ---");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        const string welcome = @"Welcome to the Nytric shell!
Type 'Exit' or 'Quit' to leave.
Type 'Help' or '?' to get help.
";

        const string help = @"
HELP - Spits out this menu.
CLEAR, CLS - Clears the screen.
MULTILINE, ML - Enters MultiLine mode, allowing you to type multiple lines of code before executing.
ENDMULTILINE, EML - Exits MultiLine mode, executing whatever you entered while in MultiLine mode. 
    NOTE: ENDMULTILINE/ EML can only be executed while in MULTILINE/ML mode.
EXIT, QUIT - Closes the shell and exits.
";
    }
    public class Token { public TokenType Type; public string Value; public Token(TokenType t, string v) { Type = t; Value = v; } }
    // Token types
    public enum TokenType
    {
        IDENTIFIER, STRING_LITERAL, NUMBER_LITERAL,
        KEYWORD_LET, KEYWORD_PRINT, KEYWORD_SAY,
        KEYWORD_IF, KEYWORD_ELSE, KEYWORD_WHILE,
        KEYWORD_FOR, KEYWORD_FUNCTION, KEYWORD_RETURN,
        OPERATOR_MINUS, OPERATOR_PLUS, OPERATOR_MULTIPLY, OPERATOR_DIVIDE,
        OPERATOR_ASSIGN, OPERATOR_EQUAL, OPERATOR_NOT_EQUAL,
        SYMBOL_COLON, SYMBOL_COMMA, SYMBOL_LPAREN, SYMBOL_RPAREN, SYMBOL_LBRACE, SYMBOL_RBRACE,
        COMMENT, EOF, SQRT, RANDOM, REVERSE, LEN, KEYWORD_IMP, KEYWORD_READ, KEYWORD_PAUSE, KEYWORD_WIPE
    }
}
