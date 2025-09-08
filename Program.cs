using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Token;
public class Token
{
    public string Type { get; }

    public string Value { get; }
    public List<Token> GetTokens() => tokens;
    public List<Token> tokens;
    public Token(string type, string value)
    {
        Type = type;
        Value = value;
    }
    public class AstNode
    {
        public string Type { get; set; }
        public List<AstNode> Body { get; set; } = new List<AstNode>();
    }

    class Program
    {
        public enum TokenType
        {
            Identifier,
            StringLiteral,
            NumberLiteral,
            KeywordLet,
            KeywordPrint,
            KeywordSay,
            OperatorMinus,
            OperatorPlus,
            OperatorMultiply,
            OperatorDivide,
            OperatorAssign,
            SymbolColon,
            Comment,
            EOF,
            Sqrt
        }

        public class Token
        {
            public TokenType Type { get; }
            public string Value { get; }

            public Token(TokenType type, string value)
            {
                Type = type;
                Value = value;
            }

            public override string ToString() => $"{Type}: {Value}";
        }

        static void Main()
        {
            string source = "let x = 42\nprint x";
            var tokens = Tokenize(source);
            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }
        }

        public static List<Token> Tokenize(string source)
        {
            var tokens = new List<Token>();
            int position = 0;

            var regexPatterns = new List<(TokenType Type, Regex Pattern)>
        {
            (TokenType.Comment, new Regex(@"^//.*(?:\n|$)")),
            (TokenType.KeywordLet, new Regex(@"^(let)")),
            (TokenType.KeywordPrint, new Regex(@"^(print)")),
            (TokenType.KeywordSay, new Regex(@"^(say)")),
            (TokenType.OperatorMinus, new Regex(@"^(-)")),
            (TokenType.OperatorPlus, new Regex(@"^(\+)")),
            (TokenType.OperatorMultiply, new Regex(@"^(\*)")),
            (TokenType.OperatorDivide, new Regex(@"^(\/)")),
            (TokenType.OperatorAssign, new Regex(@"^(=)")),
            (TokenType.SymbolColon, new Regex(@"^(:)")),
            (TokenType.NumberLiteral, new Regex(@"^(\d+(\.\d+)?)")),
            (TokenType.StringLiteral, new Regex(@"^((?:""(?:[^""\\]|\\.)*"")|(?:'(?:[^'\\]|\\.)*'))")),
            (TokenType.Identifier, new Regex(@"^([a-zA-Z_]\w*)")),
            (TokenType.Sqrt, new Regex(@"^(SQRT)"))
        };

            while (position < source.Length)
            {
                var remainingSource = source.Substring(position);
                bool tokenFound = false;

                foreach (var (Type, Pattern) in regexPatterns)
                {
                    var match = Pattern.Match(remainingSource);
                    if (match.Success)
                    {
                        string value = match.Value;

                        if (Type != TokenType.Comment)
                        {
                            tokens.Add(new Token(Type, value));
                        }

                        position += value.Length;
                        tokenFound = true;
                        break;
                    }
                }

                if (!tokenFound)
                {

                    var whitespaceMatch = Regex.Match(remainingSource, @"^\s+");
                    if (whitespaceMatch.Success)
                    {
                        position += whitespaceMatch.Value.Length;
                    }
                    else
                    {
                        throw new Exception($"Lexing error: Unexpected character {source[position]} at position {position}");
                    }
                }
            }
            tokens.Add(new Token(TokenType.EOF, ""));
            return tokens;
        }
    }
}

class Parser(string tokens)
{
    void parse()
    {
        var ast = new AstNode
        {
            Type = "Program",
            Body = new List<AstNode>()
        };

        while (peek().type != TokenTypes.eof) {
        const statement = this.parseStatement();
        if (statement)
        {
            ast.body.push(statement);
        }
    }
      return ast;
    }
}

