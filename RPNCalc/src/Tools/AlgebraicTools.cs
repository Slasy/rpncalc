using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RPNCalc.Tools
{
    public static class AlgebraicTools
    {
        private static readonly HashSet<string> operators = new(new[] { "(", ")", "*", "/", "+", "-", "^", "==", "!=", "=", ">=", "<=", ">", "<" });
        private static readonly Regex number = new(@"^\d+(?:\.\d+)?(?:e\-?\d+)?");
        private static readonly Regex negNumberVariable = new(@"^\(((?:\-\d+(?:\.\d+)?(?:e\-?\d+)?)|(?:\-\w+))\)");
        private static readonly Regex op = new(@"^(?:\" + string.Join("|\\", operators) + ")");
        private static readonly Regex func = new(@"^((?:\w+)|(?:\(\-\w+))\(");
        private static readonly Regex variable = new(@"^\w+");
        private static readonly Regex text = new(@"^'\w+'");
        private static readonly Regex[] matchers = new[] { negNumberVariable, number, func, op, variable, text };

        /// <summary>
        /// Divide algebraic expression to individual elements.
        /// </summary>
        public static string[] GetTokens(string expression)
        {
            var tokens = new List<string>();
            while (expression.Length > 0)
            {
                Match match = null;
                foreach (var regex in matchers)
                {
                    match = regex.Match(expression);
                    if (match.Success)
                    {
                        if (regex == func)
                        {
                            string name = match.Groups[1].Value;
                            string arguments = MatchFunctionArguments(name, expression.Substring(name.Length), out int matchLen);
                            tokens.Add("(");
                            bool isNegative = name.StartsWith("(-");
                            tokens.AddRange(GetTokens(arguments.Substring(1, arguments.Length - 2)));
                            tokens.Add(isNegative ? name.Substring(1) : name);
                            tokens.Add(")");
                            expression = expression.Substring(name.Length + matchLen + (isNegative ? 1 : 0));
                            break;
                        }
                        else if (match.Groups.Count == 2)
                        {
                            tokens.Add(match.Groups[1].Value);
                        }
                        else
                        {
                            tokens.Add(match.Value);
                        }
                        expression = expression.Substring(match.Length);
                        break;
                    }
                }
                if (!match.Success) expression = expression.Substring(1);
            }
            return tokens.ToArray();
        }

        private static string MatchFunctionArguments(string functionName, string functionExpression, out int matchLength)
        {
            int bracketCounter = 0;
            var sb = new StringBuilder(functionExpression.Length + 2);
            sb.Append('(');
            for (int i = 0; i < functionExpression.Length; i++)
            {
                sb.Append(functionExpression[i]);
                switch (functionExpression[i])
                {
                    case '(': bracketCounter++; break;
                    case ')': bracketCounter--; break;
                    case ',':
                        sb.Append(")(");
                        break;
                }
                if (bracketCounter == 0)
                {
                    matchLength = i + 1;
                    sb.Append(')');
                    return sb.ToString();
                }
            }
            if (functionName.StartsWith("(-")) functionName = functionName.Substring(2);
            throw new ArgumentException($"Function is missing ending bracket: {functionName}");
        }

        // src https://stackoverflow.com/a/1438153
        /// <summary>
        /// Reorder elements in array from infix notation (algebraic) to postfix notation (RPN).
        /// </summary>
        /// <param name="tokenArray">Array of symbols in algebraic (infix) notation</param>
        /// <param name="strictMode">Throw exception on uneven bracket count</param>
        /// <exception cref="ArgumentException"/>
        public static string[] InfixToPostfix(string[] tokenArray, bool strictMode = true)
        {
            var stack = new System.Collections.Generic.Stack<string>();
            var postfix = new System.Collections.Generic.Stack<string>();
            int bracketCounter = 0;

            string st;
            for (int i = 0; i < tokenArray.Length; i++)
            {
                if (!operators.Contains(tokenArray[i]))
                {
                    string token = tokenArray[i];
                    // handle negative numbers/variables/functions
                    if (token.Length > 1 && token.StartsWith("-"))
                    {
                        postfix.Push(token.Substring(1));
                        postfix.Push("+-");
                    }
                    else
                    {
                        postfix.Push(token);
                    }
                }
                else if (tokenArray[i] == "(")
                {
                    bracketCounter++;
                    stack.Push("(");
                }
                else if (tokenArray[i] == ")")
                {
                    bracketCounter--;
                    if (bracketCounter < 0)
                    {
                        if (strictMode) throw new ArgumentException("Too many closing brackets");
                        bracketCounter = 0;
                        continue; // ignore too many closing brackets
                    }
                    st = stack.Pop();
                    while (st != "(")
                    {
                        postfix.Push(st);
                        st = stack.Pop();
                    }
                }
                else
                {
                    while (stack.Count > 0)
                    {
                        st = stack.Pop();
                        if (OpPriority(st) >= OpPriority(tokenArray[i]))
                        {
                            postfix.Push(st);
                        }
                        else
                        {
                            stack.Push(st);
                            break;
                        }
                    }
                    stack.Push(tokenArray[i]);
                }
            }
            if (strictMode && bracketCounter > 0) throw new ArgumentException("Too many opening brackets");
            while (stack.Count > 0)
            {
                string element = stack.Pop();
                if (element == "(") continue; // ignore too many opening brackets
                postfix.Push(element);
            }

            var array = postfix.ToArray();
            Array.Reverse(array);
            return array;
        }

        private static int OpPriority(string c)
        {
            return c switch
            {
                "^" => 4,
                "*" or "/" => 3,
                "+" or "-" => 2,
                "=" or "==" or "!=" or ">" or "<" or ">=" or "<=" => 1,
                _ => 0,
            };
        }
    }
}
