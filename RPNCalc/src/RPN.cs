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
        /// <summary>
        /// Delegate for all RPN functions/operations, expects to directly operates on stack.
        /// </summary>
        public delegate void Function(RPNStack<double> stack);

        /// <summary>
        /// Collection of variables accessible and usable in this calculator.
        /// </summary>
        public IReadOnlyDictionary<string, double> VariablesView => variables;

        /// <summary>
        /// <para>Read only collection of functions available to use in this calculator.</para>
        /// <para>Also includes all default functions - can be overridden/removed.</para>
        /// </summary>
        public IReadOnlyCollection<string> FunctionsView => functions.Keys;

        /// <summary>
        /// Read only access to current stack.
        /// </summary>
        public IReadOnlyCollection<double> StackView => stack;

        /// <summary>Automatically clear stack before each <see cref="Eval(string)"/> call.</summary>
        public bool AlwaysClearStack { get; set; } = true;
        /// <summary>Variable and function names are case sensitive.</summary>
        public bool CaseSensitiveNames { get; }

        protected RPNStack<double> stack = new RPNStack<double>();
        protected readonly StringBuilder buffer = new StringBuilder();
        protected readonly Dictionary<string, double> variables = new Dictionary<string, double>();
        protected readonly Dictionary<string, Function> functions = new Dictionary<string, Function>();

        /// <summary>
        /// <para>RPN calculator, setup default set of functions:</para>
        /// <para>+ - * / addition, subtraction, multiplication, division</para>
        /// <para>^ exponentiation</para>
        /// <para>+- change sign</para>
        /// <para>sq, sqrt : square, square root</para>
        /// <para>drop, dup, swap : drop, duplicate, swap values on stack</para>
        /// <para>rot, roll, clear : rottate top 3 values, roll/rotate whole stack, clear stack</para>
        /// <para>depth : number of values on stack</para>
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
        public double? Eval(string expression)
        {
            return InternalEval(expression, false);
        }

        protected double? InternalEval(string expression, bool forceKeepStack)
        {
            if (expression is null) throw new ArgumentNullException(nameof(expression), "RPN expression is null");
            buffer.Clear();
            if (AlwaysClearStack && !forceKeepStack) ClearStack();
            int i;
            for (i = 0; i < expression.Length; i++)
            {
                char ch = expression[i];
                if (ch == ' ')
                {
                    if (buffer.Length == 0) continue;
                    processBufferContent();
                    buffer.Clear();
                }
                else
                {
                    buffer.Append(ch);
                }
            }
            processBufferContent();
            buffer.Clear();
            if (stack.Count == 0) return null;
            return stack.Peek();

            void processBufferContent()
            {
                string bufferValue = buffer.ToString().Trim();
                if (string.IsNullOrEmpty(bufferValue)) return;
                if (TryGetBufferValue(bufferValue, out var number))
                    stack.Push(number);
                else if (!TryRunFunction(bufferValue, i))
                    throw new RPNUndefinedNameException($"Unknown variable/function on position {i - buffer.Length}: {buffer}");
            }
        }

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
        /// <param name="value">number or null to remove variable</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetVariable(string name, double? value)
        {
            if (name is null) throw new ArgumentNullException(nameof(name), "Variable name is null");
            if (!IsValidName(name)) throw new ArgumentException(nameof(name), "Invalid variable name");
            if (functions.ContainsKey(GetKeyName(name))) throw new ArgumentException(nameof(name), "Name is already used for a function");
            if (!value.HasValue) variables.Remove(name);
            else variables[GetKeyName(name)] = value.Value;
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
            if (!IsValidName(name)) throw new ArgumentException(nameof(name), "Invalid function name");
            if (variables.ContainsKey(GetKeyName(name))) throw new ArgumentException(nameof(name), "Name is already used for a variable");
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

        private bool TryGetBufferValue(string value, out double number)
        {
            number = 0;
            if (value is null) return false;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                return true;
            else if (variables.TryGetValue(GetKeyName(value), out number))
                return true;
            else
                return false;
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
        private bool IsValidName(string name) => !name.Contains(" ");

        private void LoadDefaultFunctions()
        {
            functions["+"] = stack => stack.Func((x, y) => y + x);
            functions["-"] = stack => stack.Func((x, y) => y - x);
            functions["*"] = stack => stack.Func((x, y) => y * x);
            functions["/"] = stack => stack.Func((x, y) => y / x);
            functions["^"] = stack => stack.Func((x, y) => Math.Pow(y, x));
            functions["+-"] = stack => stack.Func(x => -x);
            functions["sq"] = stack => stack.Func(x => x * x);
            functions["sqrt"] = stack => stack.Func(x => Math.Sqrt(x));
            functions["drop"] = StackExtensions.Drop;
            functions["dup"] = StackExtensions.Dup;
            functions["swap"] = stack => stack.Swap();
            functions["depth"] = stack => stack.Push(stack.Count);
            functions["rot"] = stack => stack.Rotate(3);
            functions["roll"] = stack => stack.Roll(1);
            functions["over"] = StackExtensions.Over;
            functions["clear"] = stack => stack.Clear();
        }
    }
}
