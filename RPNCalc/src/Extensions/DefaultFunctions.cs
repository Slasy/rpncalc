using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using RPNCalc.Flags;
using RPNCalc.Items;
using RPNCalc.Tools;

[assembly: InternalsVisibleTo("RPNCalc.Tests")]

namespace RPNCalc.Extensions
{
    public static class DefaultFunctions
    {
        private static RPNFunctionException UndefinedResult => new("Undefined result");
        private static RPNArgumentException IndexOutOfRange => new("Index out of range");
        private static RPNFunctionException UndefinedRoot => new("Undefined root of negative number");

        public const string FLAG_STOP_LOOP = "STOP_LOOP";
        public const string FLAG_COMPLEX_ROOT = "CPX_ROOT";

        public static void LoadMinimalFunctions(this RPN calc)
        {
            calc.SetNameValue("EVAL", st => EVAL(calc, st));

            calc.SetNameValue("PI", Math.PI);
            calc.SetNameValue("E", Math.E);

            calc.SetNameValue("+", PLUS);
            calc.SetNameValue("-", MINUS);
            calc.SetNameValue("*", MUL);
            calc.SetNameValue("/", DIV);
            calc.SetNameValue("^", stack => POW(calc.Flags, stack));
            calc.SetNameValue("+-", NEG);
            calc.SetNameValue("++", stack => AddToVarOnStack(calc, stack, 1));
            calc.SetNameValue("--", stack => AddToVarOnStack(calc, stack, -1));
            calc.SetNameValue("1/X", ONE_OVER_X);

            calc.SetNameValue("SQ", SQUARE);
            calc.SetNameValue("SQRT", stack => SQUARE_ROOT(calc, stack));

            calc.SetNameValue("SIN", SIN);
            calc.SetNameValue("COS", COS);
            calc.SetNameValue("TAN", TAN);

            calc.SetNameValue("LOG", LOG);
            calc.SetNameValue("LN", LN);
            calc.SetNameValue("EXP", EXP);

            calc.SetNameValue("RND", ROUND);
            calc.SetNameValue("RND0", RPNTools.CreateMacroInstructions("0 RND"));
        }

        /// <summary>
        /// If you clear all names from calculator, you can use this extension to get default functions back.
        /// </summary>
        public static void LoadDefaultFunctions(this RPN calc)
        {
            LoadMinimalFunctions(calc);

            calc.SetNameValue("DROP", StackExtensions.Drop);
            calc.SetNameValue("DUP", StackExtensions.Dup);
            calc.SetNameValue("SWAP", stack => stack.Swap());
            calc.SetNameValue("DEPTH", stack => stack.Push(stack.Count));
            calc.SetNameValue("ROT", stack => stack.Rotate(3));
            calc.SetNameValue("ROLL", stack => stack.Roll(1));
            calc.SetNameValue("OVER", StackExtensions.Over);
            calc.SetNameValue("CLST", CLEAR_STACK);
            calc.SetNameValue("CLV", st => CLEAR_VAR(calc, st));
            calc.SetNameValue("STO", st => STORE(calc, st, RPN.Scope.Default));
            calc.SetNameValue("RCL", st => RECALL(calc, st, RPN.Scope.Default));
            calc.SetNameValue("GSTO", st => STORE(calc, st, RPN.Scope.Global));
            calc.SetNameValue("GRCL", st => RECALL(calc, st, RPN.Scope.Global));
            calc.SetNameValue("LSTO", st => STORE(calc, st, RPN.Scope.Local));
            calc.SetNameValue("LRCL", st => RECALL(calc, st, RPN.Scope.Local));

            calc.SetNameValue("IP", INTEGER_PART);
            calc.SetNameValue("FP", FRACTION_PART);
            calc.SetNameValue("FLOOR", stack => stack.Func(x => Math.Floor(x.GetRealNumber())));
            calc.SetNameValue("CEIL", stack => stack.Func(x => Math.Ceiling(x.GetRealNumber())));

            calc.SetNameValue("IFT", st => IF_THEN(calc, st));
            calc.SetNameValue("IFTE", st => IF_THEN_ELSE(calc, st));
            calc.SetNameValue("WHILE", st => WHILE(calc, st));
            calc.SetNameValue("FOR", st => FOR(calc, st));
            calc.SetNameValue("LOOP", st => LOOP(calc, st));
            calc.SetNameValue("END", _ => calc.StopProgram = true);
            calc.SetNameValue("BREAK", RPNTools.CreateMacroInstructions($"{FLAG_STOP_LOOP} SF END"));

            calc.SetNameValue("==", stack => stack.Func((x, y) => y == x));
            calc.SetNameValue("!=", stack => stack.Func((x, y) => y != x));
            calc.SetNameValue("<", stack => stack.Func((x, y) => y.GetRealNumber() < x));
            calc.SetNameValue("<=", stack => stack.Func((x, y) => y.GetRealNumber() <= x));
            calc.SetNameValue(">", stack => stack.Func((x, y) => y.GetRealNumber() > x));
            calc.SetNameValue(">=", stack => stack.Func((x, y) => y.GetRealNumber() >= x));
            calc.SetNameValue("NOT", stack => stack.Func(x => !x.GetBool()));

            calc.SetNameValue("HEAD", HEAD);
            calc.SetNameValue("TAIL", TAIL);
            calc.SetNameValue("CONTAIN", CONTAIN);
            calc.SetNameValue("POS", POSTION);
            calc.SetNameValue("GET", GET_FROM_LIST);
            calc.SetNameValue("GETI", GET_INC_FROM_LIST);
            calc.SetNameValue("PUT", PUT_TO_LIST);
            calc.SetNameValue("PUTI", PUT_INC_TO_LIST);
            calc.SetNameValue(">LIST", TO_LIST);
            calc.SetNameValue("LIST>", EXPLODE_LIST);
            calc.SetNameValue("SIZE", SIZE);

            calc.SetNameValue("TYPE", TYPE);
            calc.SetNameValue(">STR", TO_STRING);
            calc.SetNameValue("STR>", stack => FROM_STRING(calc, stack));

            calc.SetCollectionGenerator("[", "]", st => new ListItem(st));
            calc.SetCollectionGenerator("{", "}", st => new ProgramItem(st));
            calc.SetCollectionGenerator("(", ")", CreateComplexNumber);

            calc.SetNameValue("SF", stack => SET_FLAG(calc, stack, true), true);
            calc.SetNameValue("CF", stack => SET_FLAG(calc, stack, false), true);
            calc.SetNameValue("FS?", stack => READ_FLAG(calc, stack, true, false), true);
            calc.SetNameValue("FC?", stack => READ_FLAG(calc, stack, false, false), true);
            calc.SetNameValue("FS?C", stack => READ_FLAG(calc, stack, true, true), true);
            calc.SetNameValue("FC?C", stack => READ_FLAG(calc, stack, false, true), true);

            calc.Flags.AddIndexedFlags(10, true); // 10 user flags
            calc.SetNameValue(FLAG_STOP_LOOP, calc.Flags.AddNamedFlag(FLAG_STOP_LOOP, false), RPN.Scope.Protected);
            calc.SetNameValue(FLAG_COMPLEX_ROOT, calc.Flags.AddNamedFlag(FLAG_COMPLEX_ROOT, false), RPN.Scope.Protected);
        }

        internal static void EVAL(RPN calc, Stack<AItem> stack)
        {
            AItem item = stack.Pop();
            if (item is StringItem expression)
            {
                var tokens = AlgebraicTools.GetTokens(expression.value);
                tokens = AlgebraicTools.InfixToPostfix(tokens);
                var items = RPNTools.TokensToItems(tokens);
                foreach (var _item in items)
                {
                    if (_item is NameItem name)
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

        internal static void PLUS(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is RealNumberItem && y is RealNumberItem) stack.Push(y.GetRealNumber() + x.GetRealNumber());
            else if (x is ListItem xList && y is ListItem yList) stack.Push(ListItem.Combine(yList, xList));
            else if (x is ListItem listX) stack.Push(ListItem.Combine(y, listX));
            else if (y is ListItem listY) stack.Push(ListItem.Combine(listY, x));
            else if (x is StringItem || y is StringItem) stack.Push(y.AsString() + x.AsString());
            else if (x is ComplexNumberItem || y is ComplexNumberItem)
                stack.Push(y.AsComplexNumber() + x.AsComplexNumber());
            else throw UndefinedResult;
        }

        internal static void MINUS(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is RealNumberItem && y is RealNumberItem) stack.Push(y.GetRealNumber() - x.GetRealNumber());
            else if (x is ComplexNumberItem || y is ComplexNumberItem)
                stack.Push(y.AsComplexNumber() - x.AsComplexNumber());
            else throw UndefinedResult;
        }

        internal static void MUL(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is RealNumberItem && y is RealNumberItem) stack.Push(y.GetRealNumber() * x.GetRealNumber());
            else if (x is ComplexNumberItem || y is ComplexNumberItem)
                stack.Push(y.AsComplexNumber() * x.AsComplexNumber());
            else throw UndefinedResult;
        }

        internal static void DIV(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is RealNumberItem && y is RealNumberItem) stack.Push(y.GetRealNumber() / x.GetRealNumber());
            else if (x is ComplexNumberItem || y is ComplexNumberItem)
                stack.Push(y.AsComplexNumber() / x.AsComplexNumber());
            else throw UndefinedResult;
        }

        internal static void POW(FlagCollection flags, Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is RealNumberItem && y is RealNumberItem)
            {
                double yReal = y.GetRealNumber();
                double xReal = x.GetRealNumber();
                if (yReal >= 0 || xReal >= 1)
                {
                    stack.Push(Math.Pow(yReal, xReal));
                }
                else if (flags[FLAG_COMPLEX_ROOT])
                {
                    if (Math.Abs(xReal).Equals(.5))
                    {
                        double realResult = Math.Sqrt(Math.Abs(yReal));
                        Complex complexResult = Math.Sign(xReal) < 0
                            ? new Complex(0, 1 / realResult * Math.Sign(yReal))
                            : new Complex(0, realResult);
                        stack.Push(complexResult);
                    }
                    else
                    {
                        stack.Push(Complex.Pow(yReal, xReal));
                    }
                }
                else
                {
                    throw UndefinedRoot;
                }
            }
            else if (x is ComplexNumberItem || y is ComplexNumberItem)
            {
                stack.Push(Complex.Pow(y.AsComplexNumber(), x.AsComplexNumber()));
            }
            else
            {
                throw UndefinedResult;
            }
        }

        internal static void NEG(Stack<AItem> stack)
        {
            NumberOperation(
                stack,
                x => -x.GetRealNumber(),
                x => -x.GetComplexNumber()
            );
        }

        internal static void SQUARE(Stack<AItem> stack)
        {
            NumberOperation(
                stack,
                x => x.GetRealNumber() * x.GetRealNumber(),
                x => x.GetComplexNumber() * x.GetComplexNumber()
            );
        }

        internal static void SQUARE_ROOT(RPN calc, Stack<AItem> stack)
        {
            AItem x = stack.Pop();
            if (x is RealNumberItem xReal)
            {
                double number = xReal.GetRealNumber();
                if (number >= 0) stack.Push(Math.Sqrt(xReal));
                else if (calc.Flags[FLAG_COMPLEX_ROOT]) stack.Push(Complex.Sqrt(number));
                else throw UndefinedRoot;
            }
            else if (x is ComplexNumberItem xComplex)
            {
                stack.Push(Complex.Sqrt(xComplex));
            }
            else
            {
                throw UndefinedResult;
            }
        }

        internal static void ONE_OVER_X(Stack<AItem> stack)
        {
            NumberOperation(
                stack,
                x => 1d / x.GetRealNumber(),
                x => new Complex(1, 0) / x.GetComplexNumber()
            );
        }

        internal static void STORE(RPN calc, Stack<AItem> stack, RPN.Scope scope)
        {
            string name = stack.Pop();
            var value = stack.Pop();
            calc.SetNameValue(name, value, scope);
        }

        internal static void RECALL(RPN calc, Stack<AItem> stack, RPN.Scope scope)
        {
            string name = stack.Pop();
            AItem item = calc.GetNameValue(name, scope);
            stack.Push(item);
        }

        internal static void CLEAR_STACK(Stack<AItem> stack) => stack.Clear();

        internal static void CLEAR_VAR(RPN calc, Stack<AItem> stack)
        {
            string name = stack.Pop();
            //EnsureValidName(name);
            calc.RemoveName(name);
        }

        /// <summary>
        /// number { nonzero branch } IFT
        /// </summary>
        internal static void IF_THEN(RPN calc, Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            bool predicate = y;
            var branch = x.GetProgram();
            if (predicate) calc.EvalItem(branch, true);
        }

        /// <summary>
        /// number { nonzero branch } { zerobranch } IFTE
        /// </summary>
        internal static void IF_THEN_ELSE(RPN calc, Stack<AItem> stack)
        {
            var (x, y, z) = stack.Pop3();
            bool condition = z;
            var trueBranch = y.GetProgram();
            var falseBranch = x.GetProgram();
            if (condition) calc.EvalItem(trueBranch, true);
            else calc.EvalItem(falseBranch, true);
        }

        /// <summary>
        /// { condition } { program loop } WHILE
        /// </summary>
        internal static void WHILE(RPN calc, Stack<AItem> stack)
        {
            ClearStopLoopFlag(calc);
            var (x, y) = stack.Pop2();
            var program = x.GetProgram();
            var condition = y.GetProgram();
            while (evalCondition())
            {
                calc.EvalItem(program, true);
                if (StopLoopFlagIsSetAndClear(calc)) break;
            }

            bool evalCondition()
            {
                calc.EvalItem(condition, true);
                return stack.Pop();
            }
        }

        /// <summary>
        /// <para>more complex 'for' loop</para>
        /// <para>'variable' { condition } { step } { program loop } FOR</para>
        /// </summary>
        internal static void FOR(RPN calc, Stack<AItem> stack)
        {
            ClearStopLoopFlag(calc);
            var (x, y, z, t) = stack.Pop4();
            var programLoop = x.GetProgram();
            var stepProgram = y.GetProgram();
            var conditionProgram = z.GetProgram();
            string variableName = t;
            calc.GetNameValue(variableName).EnsureType<RealNumberItem>();
            for (; condition(); step())
            {
                calc.EvalItem(programLoop, true);
                if (StopLoopFlagIsSetAndClear(calc)) break;
            }

            bool condition()
            {
                ExpectedDepthEval<RealNumberItem>(calc, conditionProgram, "condition");
                return stack.Pop();
            }

            void step()
            {
                ExpectedDepthEval<RealNumberItem>(calc, stepProgram, "step");
                double varValue = calc.GetNameValue(variableName);
                calc.SetNameValue(variableName, varValue + stack.Pop());
            }
        }

        /// <summary>
        /// <para>simple 'for' loop</para>
        /// <para>'variable' end_num step_num { program loop } LOOP</para>
        /// </summary>
        internal static void LOOP(RPN calc, Stack<AItem> stack)
        {
            ClearStopLoopFlag(calc);
            var (x, y, z, t) = stack.Pop4();
            var programLoop = x.GetProgram();
            double stepValue = y;
            double endValue = z;
            if (!y) throw new RPNArgumentException("Step value is zero");
            string variableName = t;
            for (; condition(); step())
            {
                calc.EvalItem(programLoop, true);
                if (StopLoopFlagIsSetAndClear(calc)) break;
            }

            bool condition()
            {
                double varValue = calc.GetNameValue(variableName);
                if (stepValue > 0) return varValue < endValue;
                else return varValue > endValue;
            }

            void step()
            {
                double varValue = calc.GetNameValue(variableName);
                calc.SetNameValue(variableName, varValue + stepValue);
            }
        }

        /// <summary>
        /// <para>[ a b c d ] -> a</para>
        /// <para>'string' -> 's'</para>
        /// </summary>
        internal static void HEAD(Stack<AItem> stack)
        {
            AItem item = stack.Pop();
            if (item is StringItem str)
            {
                if (str.value.Length == 0) throw new RPNArgumentException("Empty string");
                stack.Push(str.value[0].ToString());
                return;
            }
            AItem[] array = item.GetArray();
            if (array.Length == 0) throw new RPNArgumentException("Empty list");
            stack.Push(array[0]);
        }

        /// <summary>
        /// <para>[ a b c d ] -> [ b c d]</para>
        /// <para>'string' -> 'tring'</para>
        /// </summary>
        internal static void TAIL(Stack<AItem> stack)
        {
            AItem item = stack.Pop();
            if (item is StringItem str)
            {
                if (str.value.Length == 0)
                {
                    stack.Push(string.Empty);
                    return;
                }
                stack.Push(str.value.Substring(1));
                return;
            }
            AItem[] array = item.GetArray();
            if (array.Length == 0)
            {
                stack.Push(new ListItem());
                return;
            }
            AItem[] tail = new AItem[array.Length - 1];
            Array.Copy(array, 1, tail, 0, tail.Length);
            stack.Push(tail);
        }

        /// <summary>
        /// <para>[ list ] item CONTAIN</para>
        /// <para>'string' 'string' CONTAIN</para>
        /// </summary>
        internal static void CONTAIN(Stack<AItem> stack)
        {
            stack.Push(Position(stack) >= 0);
        }

        internal static void POSTION(Stack<AItem> stack)
        {
            stack.Push(Position(stack));
        }

        private static int Position(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (y is StringItem str)
            {
                string subStr = x.GetString();
                return str.value.IndexOf(subStr, StringComparison.InvariantCulture);
            }
            AItem[] array = y.GetArray();
            return Array.IndexOf(array, x);
        }

        /// <summary>
        /// a b c d 2 -> a b [ c d ]
        /// </summary>
        internal static void TO_LIST(Stack<AItem> stack)
        {
            int count = GetInteger(stack.Pop());
            AItem[] array = new AItem[count];
            for (int i = 0; i < count; i++)
            {
                array[array.Length - i - 1] = stack.Pop();
            }
            stack.Push(array);
        }

        /// <summary>
        /// [ a b c ] -> a b c 3
        /// </summary>
        internal static void EXPLODE_LIST(Stack<AItem> stack)
        {
            AItem[] array = stack.Pop().GetArray();
            foreach (AItem item in array)
            {
                stack.Push(item);
            }
            stack.Push(array.Length);
        }

        /// <summary>
        /// 0.6789 2 -> 0.68
        /// </summary>
        internal static void ROUND(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            int digits = GetInteger(x);
            double rounded = Math.Round(y.GetRealNumber(), digits, MidpointRounding.AwayFromZero);
            stack.Push(rounded);
        }

        /// <summary>
        /// [ a b c d ] 2 -> c
        /// </summary>
        internal static void GET_FROM_LIST(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            int index = GetInteger(x);
            if (y is StringItem str)
            {
                if (str.value.Length <= index || index < 0) throw IndexOutOfRange;
                stack.Push(str.value[index].ToString());
                return;
            }
            var array = y.GetArray();
            if (array.Length <= index || index < 0) throw IndexOutOfRange;
            stack.Push(array[index]);
        }

        /// <summary>
        /// [ a b c d ] 2 -> [ a b c d ] 3 c
        /// </summary>
        internal static void GET_INC_FROM_LIST(Stack<AItem> stack)
        {
            var index = GetInteger(stack.Pop());
            var y = stack.Peek();
            if (y is StringItem str)
            {
                if (str.value.Length <= index || index < 0) throw IndexOutOfRange;
                stack.Push((index + 1) % str.value.Length);
                stack.Push(str.value[index].ToString());
                return;
            }
            var array = y.GetArray();
            if (array.Length <= index || index < 0) throw IndexOutOfRange;
            stack.Push((index + 1) % array.Length);
            stack.Push(array[index]);
        }

        /// <summary>
        /// [ a b c d ] 1 X -> [ a X c d ]
        /// </summary>
        internal static void PUT_TO_LIST(Stack<AItem> stack)
        {
            var (x, y, z) = stack.Pop3();
            int index = GetInteger(y);
            var array = z.GetArray();
            if (array.Length <= index || index < 0) throw IndexOutOfRange;
            array[index] = x;
            stack.Push(array);
        }

        /// <summary>
        /// [ a b c d ] 1 X -> [ a X c d ] 2
        /// </summary>
        internal static void PUT_INC_TO_LIST(Stack<AItem> stack)
        {
            var (x, y, z) = stack.Pop3();
            int index = GetInteger(y);
            var array = z.GetArray();
            if (array.Length <= index || index < 0) throw IndexOutOfRange;
            array[index] = x;
            stack.Push(array);
            stack.Push((index + 1) % array.Length);
        }

        internal static void TYPE(Stack<AItem> stack)
        {
            stack.Push((int)stack.Pop().type);
        }

        internal static void SIZE(Stack<AItem> stack)
        {
            int size = stack.Pop() switch
            {
                ListItem list  => list.value.Length,
                StringItem str => str.value.Length,
                _              => 1,
            };
            stack.Push(size);
        }

        internal static void TO_STRING(Stack<AItem> stack)
        {
            AItem item = stack.Pop();
            stack.Push(item.AsString());
        }

        internal static void FROM_STRING(RPN calc, Stack<AItem> stack)
        {
            string str = stack.Pop().GetString();
            calc.Eval(str);
        }

        internal static void SET_FLAG(RPN calc, Stack<AItem> stack, bool value)
        {
            var x = stack.Pop();
            if (x is RealNumberItem real)
            {
                int flagIndex = GetInteger(real);
                calc.Flags[flagIndex] = value;
                return;
            }
            if (x is StringItem str)
            {
                string flagName = str.GetString();
                calc.Flags[flagName] = value;
                return;
            }
            throw new RPNArgumentException("Invalid flag type");
        }

        internal static void READ_FLAG(RPN calc, Stack<AItem> stack, bool expectedValue, bool clearFlag)
        {
            var x = stack.Pop();
            if (x is RealNumberItem real)
            {
                int flagIndex = GetInteger(real);
                stack.Push(calc.Flags[flagIndex] == expectedValue);
                if (clearFlag)
                {
                    calc.Flags[flagIndex] = false;
                }
                return;
            }
            if (x is StringItem str)
            {
                string flagName = str.GetString();
                stack.Push(calc.Flags[flagName] == expectedValue);
                if (clearFlag)
                {
                    calc.Flags[flagName] = false;
                }
            }
        }

        private static AItem CreateComplexNumber(Stack<AItem> stack)
        {
            if (stack.Count != 2) throw new RPNArgumentException("Invalid syntax of complex number");
            var (i, r) = stack.Pop2();
            return new ComplexNumberItem(r, i);
        }

        private static void ExpectedDepthEval<T>(RPN calc, ProgramItem programInstructions, string programName, int expectedDepth = 1) where T : AItem
        {
            expectedDepth = calc.StackView.Count + expectedDepth;
            calc.EvalItem(programInstructions, true);
            if (expectedDepth != calc.StackView.Count)
                throw new RPNArgumentException($"Unexpected behavior of {programName} program");
            if (expectedDepth > 0 && calc.StackView[0] is not T)
                throw new RPNArgumentException($"Unexpected type of return value of {programName} program");
        }

        private static void AddToVarOnStack(RPN calc, Stack<AItem> stack, double valueToAdd)
        {
            string varName = stack.Pop().GetString();
            var number = calc.GetNameValue(varName).EnsureType<RealNumberItem>();
            number.value += valueToAdd;
        }

        private static int GetInteger(AItem item)
        {
            return (int)Math.Round(item.GetRealNumber(), MidpointRounding.AwayFromZero);
        }

        private static bool StopLoopFlagIsSetAndClear(RPN calc)
        {
            bool set = calc.Flags[FLAG_STOP_LOOP];
            calc.Flags[FLAG_STOP_LOOP] = false;
            return set;
        }

        private static void ClearStopLoopFlag(RPN calc)
        {
            calc.Flags[FLAG_STOP_LOOP] = false;
        }

        internal static void INTEGER_PART(Stack<AItem> stack)
        {
            double number = stack.Pop().GetRealNumber();
            number = Math.Truncate(number);
            stack.Push(number);
        }

        internal static void FRACTION_PART(Stack<AItem> stack)
        {
            double number = stack.Pop().GetRealNumber();
            number = number - Math.Truncate(number);
            stack.Push(number);
        }

        internal static void SIN(Stack<AItem> stack) => NumberOperation(stack, Math.Sin, Complex.Sin);

        internal static void COS(Stack<AItem> stack) => NumberOperation(stack, Math.Cos, Complex.Cos);

        internal static void TAN(Stack<AItem> stack) => NumberOperation(stack, Math.Tan, Complex.Tan);

        internal static void LOG(Stack<AItem> stack)
        {
            var (x, y) = stack.Pop2();
            if (x is not RealNumberItem logBase)
            {
                throw UndefinedResult;
            }
            if (y is RealNumberItem number)
            {
                stack.Push(Math.Log(number.GetRealNumber(), logBase.GetRealNumber()));
            }
            else if (y is ComplexNumberItem complex)
            {
                stack.Push(Complex.Log(complex.GetComplexNumber(), logBase.GetRealNumber()));
            }
            else
            {
                throw UndefinedResult;
            }
        }

        internal static void LN(Stack<AItem> stack) => NumberOperation(stack, Math.Log, Complex.Log);

        internal static void EXP(Stack<AItem> stack) => NumberOperation(stack, Math.Exp, Complex.Exp);

        internal static void NumberOperation(
            Stack<AItem> stack,
            Func<RealNumberItem, RealNumberItem> numberOperation,
            Func<ComplexNumberItem, ComplexNumberItem> complexOperation
        )
        {
            var item = stack.Pop();
            if (item is RealNumberItem number)
            {
                stack.Push(numberOperation(number));
            }
            else if (item is ComplexNumberItem complex)
            {
                stack.Push(complexOperation(complex));
            }
            else
            {
                throw UndefinedResult;
            }
        }

        internal static void NumberOperation(
            Stack<AItem> stack,
            Func<double, double> numberOperation,
            Func<Complex, Complex> complexOperation
        )
        {
            var item = stack.Pop();
            if (item is RealNumberItem number)
            {
                stack.Push(numberOperation(number.GetRealNumber()));
            }
            else if (item is ComplexNumberItem complex)
            {
                stack.Push(complexOperation(complex.GetComplexNumber()));
            }
            else
            {
                throw UndefinedResult;
            }
        }
    }
}
