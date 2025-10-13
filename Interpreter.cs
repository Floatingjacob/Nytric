/*
 
 This file contains the code-interpreting logic for the Nytric interpreter.
 
 */

namespace Nytric
{

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
                        if (value != null) Console.WriteLine(value);
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
                case "Pause":
                    {
                        var val = EvaluateExpression((Dictionary<string, object>)node["time"]);
                        int time = Convert.ToInt32(val);
                        Thread.Sleep(time);
                        return null;
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
            // Inside EvaluateExpression method

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
                case "Read":
                    return Console.ReadLine();
                case "ComparisonStatement":
                    return EvaluateComparison(expr);
                case "Wipe":
                    Console.Clear();
                    return null;
                default:
                    throw new Exception($"Runtime error: Unknown expression type '{type}'");
            }
        }

        private object EvaluateComparison(Dictionary<string, object> node)
        {
            var leftObj = EvaluateExpression((Dictionary<string, object>)node["left"]);
            var rightObj = EvaluateExpression((Dictionary<string, object>)node["right"]);
            var op = node["operator"].ToString();
            if (leftObj is double leftNum && rightObj is double rightNum)
            {
                return op switch
                {
                    "==" => leftNum == rightNum,
                    "!=" => leftNum != rightNum,
                    _ => throw new Exception($"Runtime error: Unknown comparison operator '{op}'")
                };
            }
            else
            {
                string leftStr = leftObj?.ToString() ?? "";
                string rightStr = rightObj?.ToString() ?? "";

                return op switch
                {
                    "==" => leftStr == rightStr,
                    "!=" => leftStr != rightStr,
                    _ => throw new Exception($"Runtime error: Unknown comparison operator '{op}'")
                };
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
            return returnVal; // default return 0.0 if none
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
                "/" => right != 0 ? left / right : throw new Exception("Division by zero causes problems. Don't do that again."),
                _ => throw new Exception($"Runtime error: Unknown operator '{op}'")
            };
        }

        private static bool IsTruthy(object v)
        {
            if (v == null) return false;
            if (v is bool b) return b;                 // <-- add this
            if (v is double d) return Math.Abs(d) > 1e-12;
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
}
