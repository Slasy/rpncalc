using System;
using System.Numerics;
using RPNCalc.StackItems;
using RPNCalc.Tools;

namespace RPNCalc.Extensions
{
    public static class DefaultFunctions
    {
        private static RPNFunctionException UndefinedResult => new RPNFunctionException("Undefined result");

        /// <summary>
        /// If you clear all names from calclulator, you can use this extension to get default functions back.
        /// </summary>
        public static void LoadDefaultFunctions(this RPN calc)
        {
            calc.SetName("+", PLUS);
            calc.SetName("-", MINUS);
            calc.SetName("*", MUL);
            calc.SetName("/", DIV);
            calc.SetName("^", POW);
            calc.SetName("+-", NEG);
            calc.SetName("++", stack => AddToVarOnStack(calc, stack, 1));
            calc.SetName("--", stack => AddToVarOnStack(calc, stack, -1));
            calc.SetName("1/X", ONE_OVER_X);

            calc.SetName("SQ", SQUARE);
            calc.SetName("SQRT", SQUARE_ROOT);
            calc.SetName("DROP", StackExtensions.Drop);
            calc.SetName("DUP", StackExtensions.Dup);
            calc.SetName("SWAP", stack => stack.Swap());
            calc.SetName("DEPTH", stack => stack.Push(stack.Count));
            calc.SetName("ROT", stack => stack.Rotate(3));
            calc.SetName("ROLL", stack => stack.Roll(1));
            calc.SetName("OVER", StackExtensions.Over);
            calc.SetName("CLST", CLEAR_STACK);
            calc.SetName("CLV", st => CLEAR_VAR(calc, st));
            calc.SetName("EVAL", st => EVAL(calc, st));
            calc.SetName("STO", st => STORE(calc, st));
            calc.SetName("RCL", st => RECALL(calc, st));
            calc.SetName("RND", ROUND);

            calc.SetName("IFT", st => IF_THEN(calc, st));
            calc.SetName("IFTE", st => IF_THEN_ELSE(calc, st));
            calc.SetName("WHILE", st => WHILE(calc, st));
            calc.SetName("FOR", st => FOR(calc, st));
            calc.SetName("LOOP", st => LOOP(calc, st));

            calc.SetName("==", stack => stack.Func((x, y) => y == x));
            calc.SetName("!=", stack => stack.Func((x, y) => y != x));
            calc.SetName("<", stack => stack.Func((x, y) => y.GetRealNumber() < x));
            calc.SetName("<=", stack => stack.Func((x, y) => y.GetRealNumber() <= x));
            calc.SetName(">", stack => stack.Func((x, y) => y.GetRealNumber() > x));
            calc.SetName(">=", stack => stack.Func((x, y) => y.GetRealNumber() >= x));

            calc.SetName("HEAD", HEAD);
            calc.SetName("TAIL", TAIL);
            calc.SetName("CONTAIN", CONTAIN);
            calc.SetName(">LIST", TO_LIST);
            calc.SetName("LIST>", EXPLODE_LIST);

            calc.SetCollectionGenerator("[", "]", st => new StackList(st));
            calc.SetCollectionGenerator("{", "}", st => new StackProgram(st));
            calc.SetCollectionGenerator("(", ")", CreateComplexNumber);
        }

        private static void EVAL(RPN calc, Stack<AStackItem> stack)
        {
            AStackItem item = stack.Pop();
            if (item is StackString expression)
            {
                var tokens = AlgebraicTools.GetTokens(expression.value);
                tokens = AlgebraicTools.InfixToPostfix(tokens);
                var items = RPNTools.TokensToItems(tokens);
                foreach (var _item in items)
                {
                    if (_item is StackName name)
                    {
                        // to try if name exists in memory or throw exception early before running evaluating
                        calc.GetNameValue(name.value);
                    }
                }
                calc.EvalItems(items, false);
            }
            else
            {
                calc.EvalItem(item, true);
            }
        }

        private static void PLUS(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is StackNumber && y is StackNumber) stack.Push(y.GetRealNumber() + x.GetRealNumber());
            else if (x is StackString || y is StackString) stack.Push(y.AsString() + x.AsString());
            else if (x is StackComplex || y is StackComplex) stack.Push(y.AsComplex() + x.AsComplex());
            else throw UndefinedResult;
        }

        private static void MINUS(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is StackNumber && y is StackNumber) stack.Push(y.GetRealNumber() - x.GetRealNumber());
            else if (x is StackComplex || y is StackComplex) stack.Push(y.AsComplex() - x.AsComplex());
            else throw UndefinedResult;
        }

        private static void MUL(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is StackNumber && y is StackNumber) stack.Push(y.GetRealNumber() * x.GetRealNumber());
            else if (x is StackComplex || y is StackComplex) stack.Push(y.AsComplex() * x.AsComplex());
            else throw UndefinedResult;
        }

        private static void DIV(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is StackNumber && y is StackNumber) stack.Push(y.GetRealNumber() / x.GetRealNumber());
            else if (x is StackComplex || y is StackComplex) stack.Push(y.AsComplex() / x.AsComplex());
            else throw UndefinedResult;
        }

        private static void POW(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is StackNumber && y is StackNumber) stack.Push(Math.Pow(y.GetRealNumber(), x.GetRealNumber()));
            else if (x is StackComplex || y is StackComplex) stack.Push(Complex.Pow(y.AsComplex(), x.AsComplex()));
            else throw UndefinedResult;
        }

        private static void NEG(Stack<AStackItem> stack)
        {
            AStackItem x = stack.Pop();
            if (x is StackNumber) stack.Push(-x.GetRealNumber());
            else if (x is StackComplex) stack.Push(-x.AsComplex());
            else throw UndefinedResult;
        }

        private static void SQUARE(Stack<AStackItem> stack)
        {
            AStackItem x = stack.Pop();
            if (x is StackNumber) stack.Push(x.GetRealNumber() * x.GetRealNumber());
            else if (x is StackComplex) stack.Push(x.AsComplex() * x.AsComplex());
            else throw UndefinedResult;
        }

        private static void SQUARE_ROOT(Stack<AStackItem> stack)
        {
            AStackItem x = stack.Pop();
            if (x is StackNumber) stack.Push(Math.Sqrt(x.GetRealNumber()));
            else if (x is StackComplex) stack.Push(Complex.Sqrt(x.AsComplex()));
            else throw UndefinedResult;
        }

        private static void ONE_OVER_X(Stack<AStackItem> stack)
        {
            AStackItem x = stack.Pop();
            if (x is StackNumber) stack.Push(1d / x.GetRealNumber());
            else if (x is StackComplex) stack.Push(new Complex(1, 0) / x.GetComplex());
            else throw UndefinedResult;
        }

        private static void STORE(RPN calc, Stack<AStackItem> stack)
        {
            string name = stack.Pop();
            var value = stack.Pop();
            calc.SetName(name, value);
        }

        private static void RECALL(RPN calc, Stack<AStackItem> stack)
        {
            string name = stack.Pop();
            AStackItem item = calc.GetNameValue(name);
            stack.Push(item);
        }

        private static void CLEAR_STACK(Stack<AStackItem> stack) => stack.Clear();

        private static void CLEAR_VAR(RPN calc, Stack<AStackItem> stack)
        {
            string name = stack.Pop();
            //EnsureValidName(name);
            calc.RemoveName(name);
        }

        /// <summary>
        /// number { nonzero branch } IFT
        /// </summary>
        private static void IF_THEN(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            bool predicate = y;
            var branch = x.GetProgramInstructions();
            if (predicate) calc.EvalItems(branch, false);
        }

        /// <summary>
        /// number { nonzero branch } { zerobranch } IFTE
        /// </summary>
        private static void IF_THEN_ELSE(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y, z) = stack.Pop3();
            bool condition = z;
            var trueBranch = y.GetProgramInstructions();
            var falseBranch = x.GetProgramInstructions();
            if (condition) calc.EvalItems(trueBranch, false);
            else calc.EvalItems(falseBranch, false);
        }

        /// <summary>
        /// { condition } { program loop } WHILE
        /// </summary>
        private static void WHILE(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            var program = x.GetProgramInstructions();
            var condition = y.GetProgramInstructions();
            while (evalCondition())
            {
                calc.EvalItems(program, false);
            }

            bool evalCondition()
            {
                calc.EvalItems(condition, false);
                return stack.Pop();
            }
        }

        /// <summary>
        /// <para>more complex 'for' loop</para>
        /// <para>'variable' { condition } { step } { program loop } FOR</para>
        /// </summary>
        private static void FOR(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y, z, t) = stack.Pop4();
            var programLoop = x.GetProgramInstructions();
            var stepProgram = y.GetProgramInstructions();
            var conditionProgram = z.GetProgramInstructions();
            string variableName = t;
            calc.GetNameValue(variableName).GetRealNumber(); // to check type and/or throw exception
            for (; condition(); step())
            {
                calc.EvalItems(programLoop, false);
            }

            bool condition()
            {
                ExpectedDepthEval<StackNumber>(calc, conditionProgram, "condition");
                return stack.Pop();
            }

            void step()
            {
                ExpectedDepthEval<StackNumber>(calc, stepProgram, "step");
                double varValue = calc.GetNameValue(variableName);
                calc.SetName(variableName, varValue + stack.Pop());
            }
        }

        /// <summary>
        /// <para>simple 'for' loop</para>
        /// <para>'variable' end_num step_num { program loop } LOOP</para>
        /// </summary>
        private static void LOOP(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y, z, t) = stack.Pop4();
            var programLoop = x.GetProgramInstructions();
            double stepValue = y;
            double endValue = z;
            if (!z) throw new RPNArgumentException("Step value is zero");
            string variableName = t;
            for (; condition(); step())
            {
                calc.EvalItems(programLoop, false);
            }

            bool condition()
            {
                double varValue = calc.GetNameValue(variableName);
                if (stepValue > 0) return varValue <= endValue;
                else return varValue >= endValue;
            }

            void step()
            {
                double varValue = calc.GetNameValue(variableName);
                calc.SetName(variableName, varValue + stepValue);
            }
        }

        /// <summary>
        /// <para>[ a b c d ] -> a</para>
        /// <para>'string' -> 's'</para>
        /// </summary>
        private static void HEAD(Stack<AStackItem> stack)
        {
            AStackItem item = stack.Pop();
            if (item is StackString str)
            {
                if (str.value.Length == 0) throw new RPNArgumentException("Empty string");
                stack.Push(str.value[0].ToString());
                return;
            }
            AStackItem[] array = item.GetArray();
            if (array.Length == 0) throw new RPNArgumentException("Empty list");
            stack.Push(array[0]);
        }

        /// <summary>
        /// <para>[ a b c d ] -> [ b c d]</para>
        /// <para>'string' -> 'tring'</para>
        /// </summary>
        private static void TAIL(Stack<AStackItem> stack)
        {
            AStackItem item = stack.Pop();
            if (item is StackString str)
            {
                if (str.value.Length == 0)
                {
                    stack.Push(string.Empty);
                    return;
                }
                stack.Push(str.value.Substring(1));
                return;
            }
            AStackItem[] array = item.GetArray();
            if (array.Length == 0)
            {
                stack.Push(new StackList());
                return;
            }
            AStackItem[] tail = new AStackItem[array.Length - 1];
            Array.Copy(array, 1, tail, 0, tail.Length);
            stack.Push(tail);
        }

        /// <summary>
        /// <para>[ list ] item CONTAIN</para>
        /// <para>'string' 'string' CONTAIN</para>
        /// </summary>
        private static void CONTAIN(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (y is StackString str)
            {
                string subStr = x.GetString();
                stack.Push(str.value.IndexOf(subStr) >= 0);
                return;
            }
            AStackItem[] array = y.GetArray();
            bool contain = Array.IndexOf(array, x) >= 0;
            stack.Push(contain);
        }

        private static void TO_LIST(Stack<AStackItem> stack)
        {
            int count = (int)Math.Round(stack.Pop().GetRealNumber(), MidpointRounding.AwayFromZero);
            AStackItem[] array = new AStackItem[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = stack.Pop();
            }
            stack.Push(array);
        }

        private static void EXPLODE_LIST(Stack<AStackItem> stack)
        {
            AStackItem[] array = stack.Pop().GetArray();
            foreach (AStackItem item in array)
            {
                stack.Push(item);
            }
            stack.Push(array.Length);
        }

        private static void ROUND(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            int digits = (int)Math.Round(x.GetRealNumber(), MidpointRounding.AwayFromZero);
            double rounded = Math.Round(y.GetRealNumber(), digits, MidpointRounding.AwayFromZero);
            stack.Push(rounded);
        }

        private static AStackItem CreateComplexNumber(Stack<AStackItem> stack)
        {
            if (stack.Count != 2) throw new RPNArgumentException("Invalid syntax of complex number");
            var (i, r) = stack.Pop2();
            return new StackComplex(r, i);
        }

        private static void ExpectedDepthEval<T>(RPN calc, AStackItem[] programInstructions, string programName, int expectedDepth = 1) where T : AStackItem
        {
            expectedDepth = calc.StackView.Count + expectedDepth;
            calc.Eval(programInstructions);
            if (expectedDepth != calc.StackView.Count) throw new RPNArgumentException($"Unexpected behavior of {programName} program");
            if (expectedDepth > 0 && calc.StackView[0] is not T) throw new RPNArgumentException($"Unexpected type of return value of {programName} program");
        }

        private static void AddToVarOnStack(RPN calc, Stack<AStackItem> stack, double valueToAdd)
        {
            string varName = stack.Pop().GetString();
            double number = calc.GetNameValue(varName);
            calc.SetName(varName, number + valueToAdd);
        }
    }
}
