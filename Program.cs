using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NytricLanguage
{
    // Token types
    public enum TokenType
    {
        IDENTIFIER, STRING_LITERAL, NUMBER_LITERAL,
        KEYWORD_LET, KEYWORD_PRINT, KEYWORD_SAY,
        KEYWORD_IF, KEYWORD_ELSE, KEYWORD_WHILE,
        KEYWORD_FOR, KEYWORD_FUNCTION, KEYWORD_RETURN,
        OPERATOR_MINUS, OPERATOR_PLUS, OPERATOR_MULTIPLY, OPERATOR_DIVIDE,
        OPERATOR_ASSIGN,
        SYMBOL_COLON, SYMBOL_COMMA, SYMBOL_LPAREN, SYMBOL_RPAREN, SYMBOL_LBRACE, SYMBOL_RBRACE,
        COMMENT, EOF, SQRT, RANDOM, REVERSE, LEN
    }

    // Token
    public class Token { public TokenType Type; public string Value; public Token(TokenType t, string v) { Type = t; Value = v; } }

    // Lexer
    public class Lexer
    {
        private readonly string _source;
        private int _pos;
        private readonly List<Token> _tokens = new();

        public Lexer(string source) { _source = source; _pos = 0; }

        public List<Token> Tokenize()
        {
            // NOTE: keywords and special tokens MUST be placed BEFORE IDENTIFIER
            var patterns = new List<(TokenType, Regex)>
            {
                (TokenType.COMMENT, new Regex(@"^//.*(?:\n|$)")),
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
                        if (type != TokenType.COMMENT) _tokens.Add(new Token(type, v));
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

    // Parser
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
                    // function <name> ( params )
                    if (i + 1 < _tokens.Count && _tokens[i + 1].Type == TokenType.IDENTIFIER)
                    {
                        string name = _tokens[i + 1].Value;
                        i += 2;
                        // expect LPAREN
                        if (i < _tokens.Count && _tokens[i].Type == TokenType.SYMBOL_LPAREN)
                        {
                            i++;
                            int count = 0;
                            // count identifiers until RPAREN
                            while (i < _tokens.Count && _tokens[i].Type != TokenType.SYMBOL_RPAREN)
                            {
                                if (_tokens[i].Type == TokenType.IDENTIFIER) { count++; i++; }
                                else if (_tokens[i].Type == TokenType.SYMBOL_COMMA) i++;
                                else i++; // be tolerant
                            }
                            // now i at RPAREN or end
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

            throw new Exception($"Parsing error: Unexpected token {tok.Type}");
        }
    }

    // Interpreter with scoping and functions
    public class Interpreter
    {
        // Scope stack: last = current
        private readonly List<Dictionary<string, object>> _scopes = new();
        // function name -> function AST node
        private readonly Dictionary<string, Dictionary<string, object>> _functions = new();
        private readonly Random _rand = new();

        public Interpreter()
        {
            // push global scope
            _scopes.Add(new Dictionary<string, object>());
        }

        // Helper: get var value searching from top scope down
        private object GetVar(string name)
        {
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i].TryGetValue(name, out var v)) return v;
            }
            throw new Exception($"Runtime error: Undefined variable '{name}'");
        }

        // Helper: set variable in current scope (let)
        private void SetVarLocal(string name, object value)
        {
            _scopes[_scopes.Count - 1][name] = value;
        }

        // Evaluate full program: first register all function declarations so calls can occur anywhere
        public void Evaluate(Dictionary<string, object> ast)
        {
            var body = (List<Dictionary<string, object>>)ast["body"];
            // pre-register function declarations
            foreach (var stmt in body)
            {
                if (stmt["type"].ToString() == "FunctionDeclaration")
                {
                    var funcName = stmt["name"].ToString();
                    _functions[funcName] = stmt;
                }
            }
            // execute top-level statements
            foreach (var stmt in body)
            {
                Execute(stmt);
            }
        }

        // Execute a statement; returns non-null only for Return propagation when executing inside a function
        private object Execute(Dictionary<string, object> node)
        {
            var type = node["type"].ToString();
            switch (type)
            {
                case "VariableDeclaration":
                    {
                        var name = node["name"].ToString();
                        var value = EvaluateExpression((Dictionary<string, object>)node["value"]);
                        SetVarLocal(name, value);
                        return null;
                    }
                case "PrintStatement":
                    {
                        var value = EvaluateExpression((Dictionary<string, object>)node["value"]);
                        Console.WriteLine(value);
                        return null;
                    }
                case "IfStatement":
                    {
                        var cond = EvaluateExpression((Dictionary<string, object>)node["condition"]);
                        bool condBool = IsTruthy(cond);
                        var body = (List<Dictionary<string, object>>)node["body"];
                        var elseBody = node["elseBody"] as List<Dictionary<string, object>>;
                        if (condBool)
                        {
                            foreach (var s in body)
                            {
                                var r = Execute(s);
                                if (r != null) return r; // propagate return
                            }
                        }
                        else if (elseBody != null)
                        {
                            foreach (var s in elseBody)
                            {
                                var r = Execute(s);
                                if (r != null) return r;
                            }
                        }
                        return null;
                    }
                case "WhileStatement":
                    {
                        var body = (List<Dictionary<string, object>>)node["body"];
                        while (IsTruthy(EvaluateExpression((Dictionary<string, object>)node["condition"])))
                        {
                            foreach (var s in body)
                            {
                                var r = Execute(s);
                                if (r != null) return r;
                            }
                        }
                        return null;
                    }
                case "ForStatement":
                    {
                        string vname = node["varName"].ToString();
                        double start = Convert.ToDouble(EvaluateExpression((Dictionary<string, object>)node["start"]));
                        double end = Convert.ToDouble(EvaluateExpression((Dictionary<string, object>)node["end"]));
                        var body = (List<Dictionary<string, object>>)node["body"];
                        for (double i = start; i <= end; i++)
                        {
                            SetVarLocal(vname, i);
                            foreach (var s in body)
                            {
                                var r = Execute(s);
                                if (r != null) return r;
                            }
                        }
                        return null;
                    }
                case "FunctionDeclaration":
                    {
                        // already registered in Evaluate pre-scan; do nothing at runtime
                        return null;
                    }
                case "ReturnStatement":
                    {
                        var val = EvaluateExpression((Dictionary<string, object>)node["value"]);
                        return new ReturnSignal(val);
                    }
                default:
                    {
                        // Expression used as statement (shouldn't be many), evaluate and ignore result
                        EvaluateExpression(node);
                        return null;
                    }
            }
        }

        // Evaluate expressions; returns objects (string or double)
        private object EvaluateExpression(Dictionary<string, object> expr)
        {
            var type = expr["type"].ToString();
            switch (type)
            {
                case "Literal":
                    return expr["value"];
                case "Variable":
                    return GetVar(expr["name"].ToString());
                case "MathStatement":
                    return EvaluateMath(expr);
                case "UnaryStatement":
                    return EvaluateUnary(expr);
                case "FunctionCall":
                    return EvaluateFunctionCall(expr);
                default:
                    throw new Exception($"Runtime error: Unknown expression type '{type}'");
            }
        }

        private object EvaluateFunctionCall(Dictionary<string, object> node)
        {
            var name = node["name"].ToString();
            if (!_functions.ContainsKey(name)) throw new Exception($"Runtime error: Undefined function '{name}'");
            var funcNode = _functions[name];
            var paramList = (List<string>)funcNode["params"];
            var body = (List<Dictionary<string, object>>)funcNode["body"];
            var args = (List<Dictionary<string, object>>)node["args"];
            if (args.Count != paramList.Count) throw new Exception($"Runtime error: Function '{name}' expects {paramList.Count} args, got {args.Count}");

            // Create new scope for function call
            _scopes.Add(new Dictionary<string, object>());

            // Bind parameters
            for (int i = 0; i < paramList.Count; i++)
            {
                var val = EvaluateExpression(args[i]);
                SetVarLocal(paramList[i], val);
            }

            // Execute function body
            object returnVal = null;
            foreach (var s in body)
            {
                var result = Execute(s);
                if (result is ReturnSignal rs)
                {
                    returnVal = rs.Value;
                    break;
                }
            }

            // Pop function scope
            _scopes.RemoveAt(_scopes.Count - 1);
            return returnVal ?? 0.0; // default return 0.0 if none
        }

        private object EvaluateUnary(Dictionary<string, object> node)
        {
            var op = node["operator"].ToString();
            var val = EvaluateExpression((Dictionary<string, object>)node["value"]);
            switch (op)
            {
                case "SQRT":
                    return Math.Sqrt(Convert.ToDouble(val));
                case "RANDOM":
                    return _rand.NextDouble() * Convert.ToDouble(val);
                case "REVERSE":
                    return ReverseString(val?.ToString() ?? "");
                case "LEN":
                    return val?.ToString().Length ?? 0;
                default:
                    throw new Exception($"Runtime error: Unsupported unary operator '{op}'");
            }
        }

        private object EvaluateMath(Dictionary<string, object> node)
        {
            var leftObj = EvaluateExpression((Dictionary<string, object>)node["left"]);
            var rightObj = EvaluateExpression((Dictionary<string, object>)node["right"]);
            var op = node["operator"].ToString();

            // If either operand is string, only + is valid -> concatenation
            if (leftObj is string || rightObj is string)
            {
                if (op != "+") throw new Exception($"Runtime error: Operator '{op}' not supported for strings");
                return leftObj?.ToString() + rightObj?.ToString();
            }

            double left = Convert.ToDouble(leftObj);
            double right = Convert.ToDouble(rightObj);

            return op switch
            {
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                "/" => right != 0 ? left / right : throw new Exception("Runtime error: Division by zero"),
                _ => throw new Exception($"Runtime error: Unknown operator '{op}'")
            };
        }

        private static bool IsTruthy(object v)
        {
            if (v == null) return false;
            if (v is double d) return Math.Abs(d) > 1e-12; // non-zero is true
            if (v is string s) return s.Length > 0;
            return true;
        }

        private static string ReverseString(string s) { var arr = s.ToCharArray(); Array.Reverse(arr); return new string(arr); }

        // Special wrapper to propagate return out of nested execution
        private class ReturnSignal
        {
            public object Value;
            public ReturnSignal(object v) { Value = v; }
        }
    }

    // Main program
    class Program
    {
        static void Main()
        {
            try
            {
                var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.ncs");
                if (files.Length == 0) { Console.WriteLine("No .ncs file found in the current directory."); return; }
                var source = File.ReadAllText(files[0]);

                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();

                var parser = new Parser(tokens);
                var ast = parser.Parse();

                var interpreter = new Interpreter();
                interpreter.Evaluate(ast);

                Console.WriteLine("--- Execution Finished ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
