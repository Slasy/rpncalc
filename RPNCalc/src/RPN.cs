using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using RPNCalc.Extensions;

namespace RPNCalc
{
    /// <summary>
    /// Extensible RPN calculator
    /// </summary>
    public class RPN
    {
        protected enum State
        {
            Normal,
            ReadingString,
            ReadingProgram,
        }

        /// <summary>
        /// Delegate for all RPN functions/operations, expects to directly operates on stack.
        /// </summary>
        public delegate void Function(Stack<AStackItem> stack);

        /// <summary>
        /// Fires after each instruction.
        /// </summary>
        public event Action StepProcessed = null;

        /// <summary>
        /// If handler is attached calculator will try to reroute all exception to this event instead of simple throw.
        /// </summary>
        public event Action<Exception> OnError = null;

        /// <summary>
        /// Collection of variables accessible and usable in this calculator.
        /// </summary>
        public IReadOnlyDictionary<string, AStackItem> VariablesView => variables;

        /// <summary>
        /// <para>Read only collection of functions available to use in this calculator.</para>
        /// <para>Also includes all default functions - can be overridden/removed.</para>
        /// </summary>
        public IReadOnlyCollection<string> FunctionsView => functions.Keys;

        /// <summary>
        /// Read only access to current stack.
        /// </summary>
        public IReadOnlyCollection<AStackItem> StackView => stack;

        /// <summary>Automatically clear stack before each <see cref="Eval(string)"/> call.</summary>
        public bool AlwaysClearStack { get; set; } = true;
        /// <summary>Variable and function names are case sensitive.</summary>
        public bool CaseSensitiveNames { get; }

        protected Stack<AStackItem> stack = new();
        protected readonly StringBuilder buffer = new();
        protected readonly Dictionary<string, AStackItem> variables = new();
        protected readonly Dictionary<string, Function> functions = new();

        /// <summary>
        /// <para>RPN calculator, setup default set of functions:</para>
        /// <para>+ - * / ^ addition, subtraction, multiplication, division, exponentiation</para>
        /// <para>+- ++ -- change sign, +1 to variable, -1 from variable</para>
        /// <para>SQ, SQRT : square, square root</para>
        /// <para>DROP, DUP, SWAP : drop, duplicate, swap values on stack</para>
        /// <para>ROT, ROLL : rottate top 3 values, roll/rotate whole stack</para>
        /// <para>DEPTH, CLV, CLST : number of values on stack, clear variable, clear stack</para>
        /// <para>STO, RCL, EVAL : store variable, recall variable, evaluate/execute program on stack</para>
        /// <para>IFT, IFTE, WHILE : if then, if then else, while loop</para>
        /// </summary>
        /// <param name="caseSensitiveNames">Set true if you want variable and function names to be case sensitive.</param>
        /// <param name="alwaysClearStack">Automatically clear stack before each <see cref="Eval(string)"/> call.</param>
        public RPN(bool caseSensitiveNames = false, bool alwaysClearStack = true)
        {
            CaseSensitiveNames = caseSensitiveNames;
            AlwaysClearStack = alwaysClearStack;
            LoadDefaultFunctions();
        }

        /// <summary>
        /// Evaluates expression(s) and returns top value from stack.
        /// </summary>
        /// <param name="expression">one or more expressions as a single string</param>
        /// <returns>top value on stack or null if stack is empty</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="RPNUndefinedNameException"/>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        /// <exception cref="RPNArgumentException"/>
        public AStackItem Eval(string expression)
        {
            return InternalEval(expression, false);
        }

        protected AStackItem InternalEval(string expression, bool forceKeepStack = true)
        {
            if (expression is null) throw new ArgumentNullException(nameof(expression), "RPN expression is null");
            buffer.Clear();
            if (AlwaysClearStack && !forceKeepStack) ClearStack();
            State currentState = State.Normal;
            int programDepth = 0;
            int i;
            for (i = 0; i < expression.Length; i++)
            {
                char ch = expression[i];
                if (ch == ' ' && currentState == State.Normal)
                {
                    if (buffer.Length == 0) continue;
                    processBufferContent();
                }
                else if (ch == '\'' && currentState != State.ReadingProgram)
                {
                    if (currentState != State.ReadingString)
                    {
                        currentState = State.ReadingString;
                        buffer.Append(ch);
                    }
                    else
                    {
                        buffer.Append(ch);
                        processBufferContent();
                        currentState = State.Normal;
                    }
                }
                else if (ch == '{' && currentState != State.ReadingString)
                {
                    currentState = State.ReadingProgram;
                    programDepth++;
                    buffer.Append(ch);
                }
                else if (ch == '}' && currentState == State.ReadingProgram && --programDepth == 0)
                {
                    buffer.Append(ch);
                    processBufferContent();
                    currentState = State.Normal;
                }
                else
                {
                    buffer.Append(ch);
                }
            }
            if (currentState == State.ReadingString) throw new RPNArgumentException("Unclosed string quote");
            if (currentState == State.ReadingProgram) throw new RPNArgumentException("Unclosed program bracket");
            processBufferContent();
            if (stack.Count == 0) return null;
            return stack.Peek();

            void processBufferContent()
            {
                try
                {
                    string bufferValue = buffer.ToString().Trim();
                    if (string.IsNullOrEmpty(bufferValue)) return;
                    if (currentState == State.Normal)
                    {
                        if (TryGetBufferAsNumberOrVariable(bufferValue, out var number))
                            stack.Push(number);
                        else if (!TryRunFunction(bufferValue, i))
                            throw new RPNUndefinedNameException($"Unknown variable/function on position {i - buffer.Length}: {buffer}");
                    }
                    else if (currentState == State.ReadingString)
                    {
                        if (TryGetBufferAsString(bufferValue, out var str))
                            stack.Push(str);
                        else
                            throw new RPNArgumentException("Invalid string");
                    }
                    else if (currentState == State.ReadingProgram)
                    {
                        if (TryGetBufferAsProgram(bufferValue, out var prog))
                            stack.Push(prog);
                        else
                            throw new RPNArgumentException("Invalid program");
                    }
                    buffer.Clear();
                }
                catch (Exception e) when (OnError is not null)
                {
                    OnError(e);
                }
                StepProcessed?.Invoke();
            }
        }

        /// <summary>
        /// Returns stack as formated string for easy view of stack content.
        /// </summary>
        public string DumpStack() => stack.DumpStack();

        /// <summary>
        /// Clear stack memory.
        /// </summary>
        public void ClearStack() => stack.Clear();

        /// <summary>
        /// Clear all variables
        /// </summary>
        public void ClearVariables() => variables.Clear();

        /// <summary>
        /// Restore default functions and clear the rest
        /// </summary>
        public void ClearFunctions()
        {
            functions.Clear();
            LoadDefaultFunctions();
        }

        /// <summary>
        /// Set custom variable for this calculator instance.
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">value or null to remove variable</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetVariable(string name, AStackItem value)
        {
            if (name is null) throw new ArgumentNullException(nameof(name), "Variable name is null");
            if (!IsValidName(name)) throw new ArgumentException(nameof(name), $"Invalid variable name {name}");
            if (functions.ContainsKey(GetKeyName(name))) throw new ArgumentException(nameof(name), $"Name is already used for a function {name}");
            if (value is null) variables.Remove(name);
            else variables[GetKeyName(name)] = value;
        }

        /// <summary>
        /// Set custom C# function for this calculator instance.
        /// </summary>
        /// <param name="name">function name</param>
        /// <param name="function">function or null to remove function</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetFunction(string name, Function function)
        {
            if (name is null) throw new ArgumentNullException(nameof(name), "Function name is null");
            if (!IsValidName(name)) throw new ArgumentException(nameof(name), $"Invalid function name {name}");
            if (variables.ContainsKey(GetKeyName(name))) throw new ArgumentException(nameof(name), $"Name is already used for a variable {name}");
            if (function is null) functions.Remove(name);
            else functions[GetKeyName(name)] = function;
        }

        /// <summary>
        /// Set custom macro as function for this calculator instance.
        /// </summary>
        /// <param name="name">function name</param>
        /// <param name="macroExpression">macro expression or null to remove function</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetFunction(string name, string macroExpression)
        {
            if (macroExpression is null) SetFunction(name, (Function)null);
            else SetFunction(name, _ => InternalEval(macroExpression));
        }

        public void RemoveVariable(string name) => variables.Remove(name);
        public void RemoveFunction(string name) => functions.Remove(name);

        private AStackItem GetVariable(string varName)
        {
            if (!TryGetBufferAsNumberOrVariable(varName, out var item, true))
            {
                throw new RPNUndefinedNameException($"Unknown variable name {varName}");
            }
            return item;
        }

        private bool TryGetBufferAsNumberOrVariable(string value, out AStackItem item, bool forceVariableOnly = false)
        {
            item = null;
            if (value is null) return false;
            if (!forceVariableOnly && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double _number))
            {
                item = new StackNumber(_number);
                return true;
            }
            else if (variables.TryGetValue(GetKeyName(value), out item))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryGetBufferAsString(string value, out StackString str)
        {
            str = null;
            if (value is null) return false;
            if (value.Length < 2) return false;
            if (value[0] != '\'' || value[value.Length - 1] != '\'') return false;
            str = new StackString(value.Substring(1, value.Length - 2));
            return true;
        }

        private bool TryGetBufferAsProgram(string value, out StackProgram program)
        {
            program = null;
            if (value is null) return false;
            if (value.Length < 2) return false;
            if (value[0] != '{' || value[value.Length - 1] != '}') return false;
            program = new StackProgram(value.Substring(1, value.Length - 2));
            return true;
        }

        private bool TryRunFunction(string name, int expressionIndex)
        {
            if (name is null) return false;
            name = GetKeyName(name);
            if (!functions.TryGetValue(name, out Function function)) return false;
            try
            {
                function(stack);
            }
            catch (RPNException e)
            {
                throw new RPNFunctionException($"Function error on position {expressionIndex - name.Length}: {name}: {e.Message}", e);
            }
            return true;
        }

        private string GetKeyName(string name) => CaseSensitiveNames ? name : name.ToLowerInvariant();
        private bool IsValidName(string name)
        {
            foreach (char ch in name)
            {
                if (ch == ' ' || ch == '\'' || ch == '{' || ch == '}') return false;
            }
            return true;
        }

        private void LoadDefaultFunctions()
        {
            SetFunction("+", PLUS);
            SetFunction("-", stack => stack.Func((x, y) => y.AsNumber() - x));
            SetFunction("*", stack => stack.Func((x, y) => y.AsNumber() * x));
            SetFunction("/", stack => stack.Func((x, y) => y.AsNumber() / x));
            SetFunction("^", stack => stack.Func((x, y) => Math.Pow(y, x)));
            SetFunction("+-", stack => stack.Func(x => -x.AsNumber()));
            SetFunction("SQ", stack => stack.Func(x => x.AsNumber() * x));
            SetFunction("SQRT", stack => stack.Func(x => Math.Sqrt(x)));
            SetFunction("DROP", StackExtensions.Drop);
            SetFunction("DUP", StackExtensions.Dup);
            SetFunction("SWAP", stack => stack.Swap());
            SetFunction("DEPTH", stack => stack.Push(stack.Count));
            SetFunction("ROT", stack => stack.Rotate(3));
            SetFunction("ROLL", stack => stack.Roll(1));
            SetFunction("OVER", StackExtensions.Over);
            SetFunction("CLST", CLEAR_STACK);
            SetFunction("CLV", CLEAR_VAR);
            SetFunction("EVAL", stack => InternalEval(stack.Pop().AsProgram()));
            SetFunction("STO", STO);
            SetFunction("RCL", RCL);
            SetFunction("IFT", IFT);
            SetFunction("IFTE", IFTE);
            SetFunction("WHILE", WHILE);
            SetFunction("FOR", FOR);
            SetFunction("LOOP", LOOP);
            SetFunction("==", stack => stack.Func((x, y) => y == x));
            SetFunction("!=", stack => stack.Func((x, y) => y != x));
            SetFunction("<", stack => stack.Func((x, y) => y.AsNumber() < x));
            SetFunction("<=", stack => stack.Func((x, y) => y.AsNumber() <= x));
            SetFunction(">", stack => stack.Func((x, y) => y.AsNumber() > x));
            SetFunction(">=", stack => stack.Func((x, y) => y.AsNumber() >= x));
            SetFunction("++", stack => AddToVarOnStack(stack, 1));
            SetFunction("--", stack => AddToVarOnStack(stack, -1));
            SetFunction("1/X", stack => stack.Func(x => 1d / x));
        }

        private void PLUS(Stack<AStackItem> stack)
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
                throw new RPNArgumentException("Undefined result");
            }
        }

        private void STO(Stack<AStackItem> stack)
        {
            string name = stack.Pop();
            var value = stack.Pop();
            SetVariable(name, value);
        }

        private void RCL(Stack<AStackItem> stack)
        {
            string name = stack.Pop();
            AStackItem item = GetVariable(name);
            stack.Push(item);
        }

        private void CLEAR_STACK(Stack<AStackItem> stack) => stack.Clear();

        private void CLEAR_VAR(Stack<AStackItem> stack)
        {
            string name = stack.Pop();
            if (!IsValidName(name)) throw new RPNArgumentException($"Invalid variable name {name}");
            RemoveVariable(name);
        }

        /// <summary>
        /// number { nonzero branch } IFT
        /// </summary>
        private void IFT(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            bool predicate = y;
            string branch = x.AsProgram();
            if (predicate) InternalEval(branch);
        }

        /// <summary>
        /// number { nonzero branch } { zerobranch } IFTE
        /// </summary>
        private void IFTE(Stack<AStackItem> stack)
        {
            var (x, y, z) = stack.Pop3();
            bool condition = z;
            string trueBranch = y.AsProgram();
            string falseBranch = x.AsProgram();
            if (condition) InternalEval(trueBranch);
            else InternalEval(falseBranch);
        }

        /// <summary>
        /// { condition } { program loop } WHILE
        /// </summary>
        private void WHILE(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            string program = x.AsProgram();
            string condition = y.AsProgram();
            while (evalCondition())
            {
                InternalEval(program);
            }

            bool evalCondition()
            {
                InternalEval(condition);
                return stack.Pop();
            }
        }

        /// <summary>
        /// <para>more complex 'for' loop</para>
        /// <para>'variable' { condition } { step } { program loop } FOR</para>
        /// </summary>
        private void FOR(Stack<AStackItem> stack)
        {
            var (x, y, z, t) = stack.Pop4();
            string programLoop = x.AsProgram();
            string stepProgram = y.AsProgram();
            string conditionProgram = z.AsProgram();
            string variableName = t;
            GetVariable(variableName).AsNumber(); // to check type and/or throw exception
            for (; condition(); step())
            {
                InternalEval(programLoop);
            }

            bool condition()
            {
                ExpectedDepthEval<StackNumber>(conditionProgram, "condition");
                return stack.Pop();
            }

            void step()
            {
                ExpectedDepthEval<StackNumber>(stepProgram, "step");
                double varValue = GetVariable(variableName);
                variables[variableName] = varValue + stack.Pop();
            }
        }

        /// <summary>
        /// <para>simple 'for' loop</para>
        /// <para>'variable' end_num step_num { program loop } LOOP</para>
        /// </summary>
        private void LOOP(Stack<AStackItem> stack)
        {
            var (x, y, z, t) = stack.Pop4();
            string programLoop = x.AsProgram();
            double stepValue = y;
            double endValue = z;
            if (!z) throw new RPNArgumentException("Step value is zero");
            string variableName = t;
            for (; condition(); step())
            {
                InternalEval(programLoop);
            }

            bool condition()
            {
                double varValue = GetVariable(variableName);
                if (stepValue > 0) return varValue <= endValue;
                else return varValue >= endValue;
            }

            void step()
            {
                double varValue = GetVariable(variableName);
                variables[variableName] += stepValue;
            }
        }

        private void ExpectedDepthEval<T>(string program, string programName, int expectedDepth = 1) where T : AStackItem
        {
            expectedDepth = stack.Count + expectedDepth;
            InternalEval(program);
            if (expectedDepth != stack.Count) throw new RPNArgumentException($"Unexpected behavior of {programName} program");
            if (stack.Peek() is not T) throw new RPNArgumentException($"Unexpected type of return value of {programName} program");
        }

        private void AddToVarOnStack(Stack<AStackItem> stack, double valueToAdd)
        {
            string varName = stack.Pop().AsString();
            double number = GetVariable(varName);
            SetVariable(varName, number + valueToAdd);
        }
    }
}
