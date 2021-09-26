using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <para>sq, sqrt : square, square root</para>
        /// <para>drop, dup, swap : drop, duplicate, swap values on stack</para>
        /// <para>rot, roll : rottate top 3 values, roll/rotate whole stack</para>
        /// <para>depth, clv, clst : number of values on stack, clear variable, clear stack</para>
        /// <para>sto, rcl, eval : store variable, recall variable, evaluate/execute program on stack</para>
        /// <para>ift, ifte, while : if then, if then else, while loop</para>
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

        protected AStackItem InternalEval(string expression, bool forceKeepStack)
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
                    else throw new RPNArgumentException("Invalid string");
                }
                else if (currentState == State.ReadingProgram)
                {
                    if (TryGetBufferAsProgram(bufferValue, out var prog))
                        stack.Push(prog);
                    else throw new RPNArgumentException("Invalid program");
                }
                buffer.Clear();
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
            else SetFunction(name, _ => InternalEval(macroExpression, true));
        }

        public void RemoveVariable(string name) => variables.Remove(name);
        public void RemoveFunction(string name) => functions.Remove(name);

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
            functions["+"] = PLUS;
            functions["-"] = stack => stack.Func((x, y) => y.AsNumber() - x);
            functions["*"] = stack => stack.Func((x, y) => y.AsNumber() * x);
            functions["/"] = stack => stack.Func((x, y) => y.AsNumber() / x);
            functions["^"] = stack => stack.Func((x, y) => Math.Pow(y, x));
            functions["+-"] = stack => stack.Func(x => -x.AsNumber());
            functions["sq"] = stack => stack.Func(x => x.AsNumber() * x);
            functions["sqrt"] = stack => stack.Func(x => Math.Sqrt(x));
            functions["drop"] = StackExtensions.Drop;
            functions["dup"] = StackExtensions.Dup;
            functions["swap"] = stack => stack.Swap();
            functions["depth"] = stack => stack.Push(stack.Count);
            functions["rot"] = stack => stack.Rotate(3);
            functions["roll"] = stack => stack.Roll(1);
            functions["over"] = StackExtensions.Over;
            functions["clst"] = CLEAR_STACK;
            functions["eval"] = stack => InternalEval(stack.Pop().AsProgram(), true);
            functions["sto"] = STO;
            functions["rcl"] = RCL;
            functions["clv"] = CLEAR_VAR;
            functions["ift"] = IFT;
            functions["ifte"] = IFTE;
            functions["while"] = WHILE;
            functions["=="] = stack => stack.Func((x, y) => y == x);
            functions["!="] = stack => stack.Func((x, y) => y != x);
            functions["++"] = stack => AddToVar(stack, 1);
            functions["--"] = stack => AddToVar(stack, -1);
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
            string name = stack.Pop().AsString();
            var value = stack.Pop();
            SetVariable(name, value);
        }

        private void RCL(Stack<AStackItem> stack)
        {
            var name = stack.Pop().AsString();
            if (!TryGetBufferAsNumberOrVariable(name, out var item)) throw new RPNUndefinedNameException($"Unknown variable name {name}");
            stack.Push(item);
        }

        private void CLEAR_STACK(Stack<AStackItem> stack) => stack.Clear();

        private void CLEAR_VAR(Stack<AStackItem> stack)
        {
            var name = stack.Pop().AsString();
            if (!IsValidName(name)) throw new RPNArgumentException($"Invalid variable name {name}");
            RemoveVariable(name);
        }

        private void IFT(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            bool predicate = y;
            string branch = x.AsProgram();
            if (predicate) InternalEval(branch, true);
        }

        private void IFTE(Stack<AStackItem> stack)
        {
            var (x, y, z) = stack.Pop3();
            bool condition = z;
            string trueBranch = y.AsProgram();
            string falseBranch = x.AsProgram();
            if (condition) InternalEval(trueBranch, true);
            else InternalEval(falseBranch, true);
        }

        private void WHILE(Stack<AStackItem> stack)
        {
            var (x, y) = stack.Pop2();
            string program = x.AsProgram();
            string condition = y.AsProgram();
            while (evalCondition())
            {
                InternalEval(program, true);
            }

            bool evalCondition()
            {
                InternalEval(condition, true);
                return stack.Pop();
            }
        }

        /* private void FOR(Stack<AStackItem> stack)
        {
            var (prog, @var, stop, start) = stack.Pop4();
            string program = prog.AsProgram();
            string varName = @var;
            double startValue = start;
            double endValue;
            string stopProgram;
            if (stop.type == AStackItem.Type.Number) endValue = stop;
            else stopProgram = stop.AsProgram();
        } */

        private void AddToVar(Stack<AStackItem> stack, double valueToAdd)
        {
            string varName = stack.Pop().AsString();
            if (!TryGetBufferAsNumberOrVariable(varName, out var item, true)) throw new RPNArgumentException($"Unknown variable name {varName}");
            double number = item;
            SetVariable(varName, number + valueToAdd);
        }
    }
}
