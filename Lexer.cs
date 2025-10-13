/*
 
 This file contains the lexing logic for the Nytric interpreter.
 
 */

using System.Text.RegularExpressions;

namespace Nytric
{
    public class Lexer
    {
        private string _source;
        private int _pos;
        private readonly List<Token> _tokens = new();
        private readonly HashSet<string> _included;
        private readonly string _baseDir;

        public Lexer(string source, string baseDir = null, HashSet<string> included = null)
        {
            _source = source;
            _pos = 0;
            _baseDir = baseDir ?? Directory.GetCurrentDirectory();
            _included = included ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public List<Token> Tokenize()
        {
            var patterns = new List<(TokenType, Regex)>
            {
                (TokenType.COMMENT, new Regex(@"^//.*(?:\n|$)")),
                (TokenType.OPERATOR_EQUAL, new Regex(@"^(==)")),
                (TokenType.OPERATOR_NOT_EQUAL, new Regex(@"^(!=)")),
                (TokenType.KEYWORD_IMP, new Regex(@"^(IMP\s+""([^""]+)"")")),
                (TokenType.KEYWORD_LET, new Regex(@"^(let)")),
                (TokenType.KEYWORD_PRINT, new Regex(@"^(print)")),
                (TokenType.KEYWORD_SAY, new Regex(@"^(say)")),
                (TokenType.KEYWORD_IF, new Regex(@"^(if)")),
                (TokenType.KEYWORD_ELSE, new Regex(@"^(else)")),
                (TokenType.KEYWORD_WHILE, new Regex(@"^(while)")),
                (TokenType.KEYWORD_FOR, new Regex(@"^(for)")),
                (TokenType.KEYWORD_FUNCTION, new Regex(@"^(function)")),
                (TokenType.KEYWORD_RETURN, new Regex(@"^(return)")),
                (TokenType.OPERATOR_MINUS, new Regex(@"^(-)")),
                (TokenType.OPERATOR_PLUS, new Regex(@"^(\+)")),
                (TokenType.OPERATOR_MULTIPLY, new Regex(@"^(\*)")),
                (TokenType.OPERATOR_DIVIDE, new Regex(@"^(\/)")),
                (TokenType.OPERATOR_ASSIGN, new Regex(@"^(=)")),
                (TokenType.SYMBOL_LBRACE, new Regex(@"^(\{)")),
                (TokenType.SYMBOL_RBRACE, new Regex(@"^(\})")),
                (TokenType.SYMBOL_LPAREN, new Regex(@"^(\()")),
                (TokenType.SYMBOL_RPAREN, new Regex(@"^(\))")),
                (TokenType.SYMBOL_COLON, new Regex(@"^(:)")),
                (TokenType.SYMBOL_COMMA, new Regex(@"^(,)")),
                (TokenType.NUMBER_LITERAL, new Regex(@"^(\d+(\.\d+)?)")),
                (TokenType.STRING_LITERAL, new Regex(@"^(?:""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*')")),
                (TokenType.SQRT, new Regex(@"^(SQRT)")),
                (TokenType.RANDOM, new Regex(@"^(RANDOM)")),
                (TokenType.REVERSE, new Regex(@"^(REVERSE)")),
                (TokenType.LEN, new Regex(@"^(LEN)")),
                (TokenType.KEYWORD_READ, new Regex(@"^(read)")),
                (TokenType.KEYWORD_PAUSE, new Regex(@"^(pause)")),
                (TokenType.KEYWORD_WIPE, new Regex(@"^(wipe)")),
                (TokenType.IDENTIFIER, new Regex(@"^([a-zA-Z_]\w*)"))
            };

            while (_pos < _source.Length)
            {
                string rem = _source.Substring(_pos);
                bool matched = false;

                foreach (var (type, regex) in patterns)
                {
                    var m = regex.Match(rem);
                    if (m.Success)
                    {
                        string v = m.Value;

                        if (type == TokenType.KEYWORD_IMP)
                        {
                            string fileName = m.Groups[2].Value;
                            string fullPath = Path.GetFullPath(Path.Combine(_baseDir, fileName));

                            if (_included.Contains(fullPath))
                            {
                                Console.WriteLine($"[Nytric] Skipping importing already imported file: \"{fileName}\".");
                            }
                            else if (File.Exists(fullPath))
                            {
                                _included.Add(fullPath);
                                string injected = File.ReadAllText(fullPath) + "\n";
                                string newBase = Path.GetDirectoryName(fullPath);
                                var innerLexer = new Lexer(injected, newBase, _included);
                                var innerTokens = innerLexer.Tokenize();
                                innerTokens.RemoveAll(t => t.Type == TokenType.EOF);
                                _tokens.AddRange(innerTokens);
                            }
                            else
                            {
                                throw new Exception($"IMP error: File '{fileName}' not found.");
                            }
                            _pos += v.Length;
                            matched = true;
                            break;
                        }

                        if (type != TokenType.COMMENT)
                            _tokens.Add(new Token(type, v));

                        _pos += v.Length;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    var ws = Regex.Match(rem, @"^\s+");
                    if (ws.Success) { _pos += ws.Length; continue; }
                    throw new Exception($"Lexing error: Unexpected character '{_source[_pos]}' at position {_pos}");
                }
            }

            _tokens.Add(new Token(TokenType.EOF, null));
            return _tokens;
        }
    }
}
