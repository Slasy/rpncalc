using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RPNCalc.Tools
{
    public static class RPNTools
    {
        private static readonly Regex number = new Regex(@"^\d+(?:\.\d+)?");
        private static readonly Regex negNumber = new Regex(@"^\((-\d+(?:\.\d+)?)\)");
        private static readonly Regex op = new Regex(@"^[\^\+\-\*\/\(\)]");
        private static readonly Regex func = new Regex(@"^(\w[\w\d]+?)\((.*?)\)");
        private static readonly Regex variable = new Regex(@"^\w(?:[\w\d]+)?");
        private static readonly Regex[] matchers = new[] { negNumber, number, op, func, variable };

        /// <summary>
        /// Divide algebraic expression to individual elements.
        /// </summary>
        public static string[] Tokenize(string expression)
        {
            List<string> tokens = new List<string>();
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
                            tokens.AddRange(Tokenize(match.Groups[2].Value)); // recursive evaluation of function's arguments
                            tokens.Add(match.Groups[1].Value);
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

        /// <summary>
        /// Reorder elements in array from infix notation (algebraic) to postfix notation (RPN).
        /// </summary>
        /// <param name="tokenArray">Array of symbols in algebraic (infix) notation</param>
        /// <param name="strictMode">Throw exception on uneven bracket count</param>
        /// <exception cref="ArgumentException"/>
        public static string[] InfixToPostfix(string[] tokenArray, bool strictMode = true)
        {
            const string operators = "()*/+-^";
            var stack = new Stack<string>();
            var postfix = new Stack<string>();
            int bracketCounter = 0;

            string st;
            for (int i = 0; i < tokenArray.Length; i++)
            {
                if (!operators.Contains(tokenArray[i]))
                {
                    postfix.Push(tokenArray[i]);
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
            switch (c)
            {
                case "^": return 3;
                case "*": return 2;
                case "/": return 2;
                case "+": return 1;
                case "-": return 1;
                default: return 0;
            }
        }
    }
}
