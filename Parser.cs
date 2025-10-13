/*
 
 This file contains the parsing logic for the Nytric interpreter.
 
 */


namespace Nytric
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;
        // map function name -> parameter count (discovered by pre-scan)
        private readonly Dictionary<string, int> _functionParamCounts = new();

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _pos = 0;
            PreScanFunctions();
        }

        // Pre-scan tokens to find function declarations and their parameter counts
        private void PreScanFunctions()
        {
            int i = 0;
            while (i < _tokens.Count)
            {
                if (_tokens[i].Type == TokenType.KEYWORD_FUNCTION)
                {
                    if (i + 1 < _tokens.Count && _tokens[i + 1].Type == TokenType.IDENTIFIER)
                    {
                        string name = _tokens[i + 1].Value;
                        i += 2;
                        if (i < _tokens.Count && _tokens[i].Type == TokenType.SYMBOL_LPAREN)
                        {
                            i++;
                            int count = 0;
                            while (i < _tokens.Count && _tokens[i].Type != TokenType.SYMBOL_RPAREN)
                            {
                                if (_tokens[i].Type == TokenType.IDENTIFIER) { count++; i++; }
                                else if (_tokens[i].Type == TokenType.SYMBOL_COMMA) i++;
                                else i++;
                            }
                            _functionParamCounts[name] = count;
                        }
                    }
                }
                i++;
            }
        }

        private Token Peek() => _tokens[_pos];
        private Token Consume(TokenType expected)
        {
            var t = Peek();
            if (t.Type != expected) throw new Exception($"Parsing error: Expected {expected} but got {t.Type}");
            _pos++;
            return t;
        }

        public Dictionary<string, object> Parse()
        {
            var ast = new Dictionary<string, object> { { "type", "Program" }, { "body", new List<Dictionary<string, object>>() } };
            while (Peek().Type != TokenType.EOF)
            {
                ((List<Dictionary<string, object>>)ast["body"]).Add(ParseStatement());
            }
            return ast;
        }

        // Parse statement: let, print/say, if/while/for/function/return, or expression (auto-printed)
        private Dictionary<string, object> ParseStatement()
        {
            var t = Peek().Type;
            if (t == TokenType.KEYWORD_LET) return ParseVariableDeclaration();
            if (t == TokenType.KEYWORD_PRINT || t == TokenType.KEYWORD_SAY) return ParsePrintStatement();
            if (t == TokenType.KEYWORD_IF) return ParseIf();
            if (t == TokenType.KEYWORD_WHILE) return ParseWhile();
            if (t == TokenType.KEYWORD_FOR) return ParseFor();
            if (t == TokenType.KEYWORD_FUNCTION) return ParseFunction();
            if (t == TokenType.KEYWORD_RETURN) return ParseReturn();
            if (t == TokenType.KEYWORD_PAUSE) return ParsePause();
            if (t == TokenType.KEYWORD_WIPE) return ParseExpression();


            // Otherwise, parse expression and wrap into PrintStatement for top-level
            var expr = ParseExpression();
            return new Dictionary<string, object> { { "type", "PrintStatement" }, { "value", expr } };
        }

        private Dictionary<string, object> ParseVariableDeclaration()
        {
            Consume(TokenType.KEYWORD_LET);
            var name = Consume(TokenType.IDENTIFIER).Value;
            Consume(TokenType.OPERATOR_ASSIGN);
            var value = ParseExpression();
            return new Dictionary<string, object> { { "type", "VariableDeclaration" }, { "name", name }, { "value", value } };
        }

        private Dictionary<string, object> ParsePause()
        {
            Consume(TokenType.KEYWORD_PAUSE);
            var time = ParseExpression();
            return new Dictionary<string, object> { { "type", "Pause" }, { "time", time } };
        }

        private Dictionary<string, object> ParsePrintStatement()
        {
            if (Peek().Type == TokenType.KEYWORD_PRINT) Consume(TokenType.KEYWORD_PRINT);
            if (Peek().Type == TokenType.KEYWORD_SAY) Consume(TokenType.KEYWORD_SAY);
            var expr = ParseExpression();
            return new Dictionary<string, object> { { "type", "PrintStatement" }, { "value", expr } };
        }

        // Parse if with optional else; both then/else can be single statement or { block }
        private Dictionary<string, object> ParseIf()
        {
            Consume(TokenType.KEYWORD_IF);
            var condition = ParseExpression();

            List<Dictionary<string, object>> body = Peek().Type == TokenType.SYMBOL_LBRACE ? ParseBlock() : new List<Dictionary<string, object>> { ParseStatement() };

            List<Dictionary<string, object>> elseBody = null;
            if (Peek().Type == TokenType.KEYWORD_ELSE)
            {
                Consume(TokenType.KEYWORD_ELSE);
                elseBody = Peek().Type == TokenType.SYMBOL_LBRACE ? ParseBlock() : new List<Dictionary<string, object>> { ParseStatement() };
            }

            return new Dictionary<string, object> { { "type", "IfStatement" }, { "condition", condition }, { "body", body }, { "elseBody", elseBody } };
        }

        private Dictionary<string, object> ParseWhile()
        {
            Consume(TokenType.KEYWORD_WHILE);
            var condition = ParseExpression();
            var body = Peek().Type == TokenType.SYMBOL_LBRACE ? ParseBlock() : new List<Dictionary<string, object>> { ParseStatement() };
            return new Dictionary<string, object> { { "type", "WhileStatement" }, { "condition", condition }, { "body", body } };
        }

        private Dictionary<string, object> ParseFor()
        {
            Consume(TokenType.KEYWORD_FOR);
            var varName = Consume(TokenType.IDENTIFIER).Value;
            Consume(TokenType.OPERATOR_ASSIGN);
            var start = ParseExpression();
            var end = ParseExpression();
            var body = Peek().Type == TokenType.SYMBOL_LBRACE ? ParseBlock() : new List<Dictionary<string, object>> { ParseStatement() };
            return new Dictionary<string, object> { { "type", "ForStatement" }, { "varName", varName }, { "start", start }, { "end", end }, { "body", body } };
        }

        private Dictionary<string, object> ParseFunction()
        {
            Consume(TokenType.KEYWORD_FUNCTION);
            var name = Consume(TokenType.IDENTIFIER).Value;
            Consume(TokenType.SYMBOL_LPAREN);
            var parameters = new List<string>();
            while (Peek().Type != TokenType.SYMBOL_RPAREN)
            {
                parameters.Add(Consume(TokenType.IDENTIFIER).Value);
                if (Peek().Type == TokenType.SYMBOL_COMMA) Consume(TokenType.SYMBOL_COMMA);
            }
            Consume(TokenType.SYMBOL_RPAREN);
            var body = ParseBlock();
            return new Dictionary<string, object> { { "type", "FunctionDeclaration" }, { "name", name }, { "params", parameters }, { "body", body } };
        }

        private Dictionary<string, object> ParseReturn()
        {
            Consume(TokenType.KEYWORD_RETURN);
            var val = ParseExpression();
            return new Dictionary<string, object> { { "type", "ReturnStatement" }, { "value", val } };
        }

        private List<Dictionary<string, object>> ParseBlock()
        {
            Consume(TokenType.SYMBOL_LBRACE);
            var stmts = new List<Dictionary<string, object>>();
            while (Peek().Type != TokenType.SYMBOL_RBRACE) stmts.Add(ParseStatement());
            Consume(TokenType.SYMBOL_RBRACE);
            return stmts;
        }

        // Expression parsing: handles unary ops, math, function calls, literals, identifiers (variables)
        private Dictionary<string, object> ParseExpression()
        {
            var tok = Peek();

            // Unary operators
            if (tok.Type == TokenType.SQRT || tok.Type == TokenType.RANDOM || tok.Type == TokenType.REVERSE || tok.Type == TokenType.LEN)
            {
                var op = tok.Value;
                Consume(tok.Type);
                var val = ParseExpression();
                return new Dictionary<string, object> { { "type", "UnaryStatement" }, { "operator", op }, { "value", val } };
            }


            if (tok.Type == TokenType.OPERATOR_EQUAL || tok.Type == TokenType.OPERATOR_NOT_EQUAL)
            {
                var op = Consume(tok.Type).Value;

                // Force exactly two arguments for comparison
                var left = ParseExpression();
                var right = ParseExpression();

                return new Dictionary<string, object>
            {
                { "type", "ComparisonStatement" },
                { "operator", op },
                { "left", left },
                { "right", right }
            };
            }

            // Math operators (binary)
            if (tok.Type == TokenType.OPERATOR_PLUS || tok.Type == TokenType.OPERATOR_MINUS || tok.Type == TokenType.OPERATOR_MULTIPLY || tok.Type == TokenType.OPERATOR_DIVIDE)
            {
                var op = Consume(tok.Type).Value;
                var left = ParseExpression();
                var right = ParseExpression();
                return new Dictionary<string, object> { { "type", "MathStatement" }, { "operator", op }, { "left", left }, { "right", right } };
            }

            // Identifier: could be variable or function call (if we pre-saw function and know arity)
            if (tok.Type == TokenType.IDENTIFIER)
            {
                var name = Consume(TokenType.IDENTIFIER).Value;
                if (_functionParamCounts.TryGetValue(name, out int arity))
                {
                    var args = new List<Dictionary<string, object>>();
                    for (int i = 0; i < arity; i++)
                        args.Add(ParseExpression());
                    return new Dictionary<string, object> { { "type", "FunctionCall" }, { "name", name }, { "args", args } };
                }
                // not a function (or unknown arity) -> treat as variable
                return new Dictionary<string, object> { { "type", "Variable" }, { "name", name } };
            }

            // Number literal
            if (tok.Type == TokenType.NUMBER_LITERAL)
            {
                var val = Consume(TokenType.NUMBER_LITERAL).Value;
                return new Dictionary<string, object> { { "type", "Literal" }, { "value", double.Parse(val) } };
            }

            // String literal
            if (tok.Type == TokenType.STRING_LITERAL)
            {
                var raw = Consume(TokenType.STRING_LITERAL).Value;
                var content = raw.Substring(1, raw.Length - 2).Replace("\\\"", "\"").Replace("\\'", "'").Replace("\\\\", "\\");
                return new Dictionary<string, object> { { "type", "Literal" }, { "value", content } };
            }

            // Reading a line
            if (tok.Type == TokenType.KEYWORD_READ)
            {
                var val = Consume(TokenType.KEYWORD_READ).Value;
                return new Dictionary<string, object> { { "type", "Read" }, { "value", val } };
            }

            // Pausing (delay)
            if (tok.Type == TokenType.KEYWORD_PAUSE)
            {
                var val = Consume(TokenType.KEYWORD_PAUSE).Value;
                return new Dictionary<string, object> { { "type", "Pause" }, { "value", val }, { "delay", ParseExpression() } };
            }

            // Clearing the screen
            if (tok.Type == TokenType.KEYWORD_WIPE)
            {
                var val = Consume(TokenType.KEYWORD_WIPE).Value;
                return new Dictionary<string, object> { { "type", "Wipe" }, { "value", val } };
            }

            throw new Exception($"Parsing error: Unexpected token {tok.Type}");
        }
    }
}
