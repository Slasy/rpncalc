using System;
using System.Linq;
using RPNCalc.Tools;

namespace RPNCalc.Extensions
{
    public static class DefaultFunctions
    {
        /// <summary>
        /// If you clear all names from calclulator, you can use this extension to get default functions back.
        /// </summary>
        public static void LoadDefaultFunctions(this RPN calc)
        {
            calc.SetName("+", PLUS);
            calc.SetName("-", stack => stack.Func((x, y) => y.AsNumber() - x));
            calc.SetName("*", stack => stack.Func((x, y) => y.AsNumber() * x));
            calc.SetName("/", stack => stack.Func((x, y) => y.AsNumber() / x));
            calc.SetName("^", stack => stack.Func((x, y) => Math.Pow(y, x)));
            calc.SetName("+-", stack => stack.Func(x => -x.AsNumber()));
            calc.SetName("SQ", stack => stack.Func(x => x.AsNumber() * x));
            calc.SetName("SQRT", stack => stack.Func(x => Math.Sqrt(x)));
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
            calc.SetName("STO", st => STO(calc, st));
            calc.SetName("RCL", st => RCL(calc, st));
            calc.SetName("IFT", st => IFT(calc, st));
            calc.SetName("IFTE", st => IFTE(calc, st));
            calc.SetName("WHILE", st => WHILE(calc, st));
            calc.SetName("FOR", st => FOR(calc, st));
            calc.SetName("LOOP", st => LOOP(calc, st));
            calc.SetName("==", stack => stack.Func((x, y) => y == x));
            calc.SetName("!=", stack => stack.Func((x, y) => y != x));
            calc.SetName("<", stack => stack.Func((x, y) => y.AsNumber() < x));
            calc.SetName("<=", stack => stack.Func((x, y) => y.AsNumber() <= x));
            calc.SetName(">", stack => stack.Func((x, y) => y.AsNumber() > x));
            calc.SetName(">=", stack => stack.Func((x, y) => y.AsNumber() >= x));
            calc.SetName("++", stack => AddToVarOnStack(calc, stack, 1));
            calc.SetName("--", stack => AddToVarOnStack(calc, stack, -1));
            calc.SetName("1/X", stack => stack.Func(x => 1d / x));
            calc.SetCollectionGenerator("[", "]", st => new StackList(st));
            calc.SetCollectionGenerator("{", "}", st => new StackProgram(st));
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
            if (x is StackNumber numX)
            {
                stack.Push(y.AsNumber() + numX);
            }
            else if (x is StackString strX)
            {
                stack.Push(y.AsString() + strX);
            }
            else
            {
                throw new RPNFunctionException("Undefined result");
            }
        }

        private static void STO(RPN calc, Stack<AStackItem> stack)
        {
            string name = stack.Pop();
            var value = stack.Pop();
            calc.SetName(name, value);
        }

        private static void RCL(RPN calc, Stack<AStackItem> stack)
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
        private static void IFT(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            bool predicate = y;
            var branch = x.AsProgramInstructions();
            if (predicate) calc.EvalItems(branch, false);
        }

        /// <summary>
        /// number { nonzero branch } { zerobranch } IFTE
        /// </summary>
        private static void IFTE(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y, z) = stack.Pop3();
            bool condition = z;
            var trueBranch = y.AsProgramInstructions();
            var falseBranch = x.AsProgramInstructions();
            if (condition) calc.EvalItems(trueBranch, false);
            else calc.EvalItems(falseBranch, false);
        }

        /// <summary>
        /// { condition } { program loop } WHILE
        /// </summary>
        private static void WHILE(RPN calc, Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            var program = x.AsProgramInstructions();
            var condition = y.AsProgramInstructions();
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
            var programLoop = x.AsProgramInstructions();
            var stepProgram = y.AsProgramInstructions();
            var conditionProgram = z.AsProgramInstructions();
            string variableName = t;
            calc.GetNameValue(variableName).AsNumber(); // to check type and/or throw exception
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
            var programLoop = x.AsProgramInstructions();
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

        private static void ExpectedDepthEval<T>(RPN calc, AStackItem[] programInstructions, string programName, int expectedDepth = 1) where T : AStackItem
        {
            expectedDepth = calc.StackView.Count + expectedDepth;
            calc.Eval(programInstructions);
            if (expectedDepth != calc.StackView.Count) throw new RPNArgumentException($"Unexpected behavior of {programName} program");
            if (calc.StackView.First() is not T) throw new RPNArgumentException($"Unexpected type of return value of {programName} program");
        }

        private static void AddToVarOnStack(RPN calc, Stack<AStackItem> stack, double valueToAdd)
        {
            string varName = stack.Pop().AsString();
            double number = calc.GetNameValue(varName);
            calc.SetName(varName, number + valueToAdd);
        }
    }
}
