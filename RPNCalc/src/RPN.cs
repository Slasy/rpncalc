using System;
using System.Collections.Generic;
using System.Globalization;
using RPNCalc.Extensions;

namespace RPNCalc
{
    /// <summary>
    /// RPN calculator
    /// </summary>
    public class RPN
    {
        /// <summary>
        /// Delegate for all RPN functions/operations, expects to directly operates on stack.
        /// </summary>
        public delegate void Function(Stack<double> stack);

        /// <summary>
        /// Collection of variables accessible and usable in this calculator.
        /// </summary>
        public IReadOnlyDictionary<string, double> Variables => variables;

        /// <summary>
        /// <para>Collection of functions available to use in this calculator.</para>
        /// <para>Also includes all default functions - can be overridden/removed.</para>
        /// </summary>
        public IReadOnlyCollection<string> Functions => functions.Keys;

        /// <summary>Automatically clear stack before each <see cref="Eval(string)"/> call.</summary>
        public bool ClearStack { get; set; } = true;
        public bool CaseSensitiveNames { get; }

        protected Stack<double> stack = new Stack<double>();
        protected readonly Dictionary<string, double> variables = new Dictionary<string, double>();
        protected readonly Dictionary<string, Function> functions = new Dictionary<string, Function>();

        /// <summary>
        /// Read only access to current stack.
        /// </summary>
        public IReadOnlyCollection<double> StackView => stack;

        /// <summary>
        /// RPN calculator, loads default set of functions.
        /// </summary>
        public RPN(bool caseSensitiveNames = false)
        {
            CaseSensitiveNames = caseSensitiveNames;
            functions["+"] = stack => stack.Func((x, y) => y + x);
            functions["-"] = stack => stack.Func((x, y) => y - x);
            functions["*"] = stack => stack.Func((x, y) => y * x);
            functions["/"] = stack => stack.Func((x, y) => y / x);
            functions["^"] = stack => stack.Func((x, y) => Math.Pow(y, x));
            functions["sq"] = stack => stack.Func((x) => x * x);
            functions["sqrt"] = stack => stack.Func((x) => Math.Sqrt(x));
            functions["drop"] = stack => stack.Drop();
            functions["dup"] = stack => stack.Dup();
        }

        /// <summary>
        /// Evaluates expression(s) and returns top value from stack.
        /// </summary>
        /// <param name="expression">one or more expressions as a single string</param>
        /// <returns>top value on stack</returns>
        /// <exception cref="ArgumentException"/>
        public double? Eval(string expression)
        {
            if (ClearStack) Clear();
            string buffer = string.Empty;
            foreach (char ch in expression)
            {
                if (ch == ' ')
                {
                    if (buffer.Length == 0) continue;
                    processBufferContent();
                    buffer = string.Empty;
                }
                else
                {
                    buffer += ch;
                }
            }
            processBufferContent();
            if (stack.Count == 0) return null;
            return stack.Peek();

            void processBufferContent()
            {
                if (TryGetBufferValue(buffer, out var number))
                    stack.Push(number);
                else if (!TryRunFunction(buffer))
                    throw new ArgumentException($"Unknown variable/function name: {buffer}");
            }
        }

        /// <summary>
        /// Clear stack memory.
        /// </summary>
        public void Clear() => stack.Clear();

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
        /// Set custom function for this calculator instance.
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

        private bool TryGetBufferValue(string value, out double number)
        {
            number = 0;
            if (value is null) return false;
            value = value.Trim();
            if (double.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                return true;
            else if (variables.TryGetValue(GetKeyName(value), out number))
                return true;
            else
                return false;
        }

        private bool TryRunFunction(string name)
        {
            if (name is null) return false;
            name = GetKeyName(name.Trim());
            if (!functions.TryGetValue(name, out Function function)) return false;
            function(stack);
            return true;
        }

        private string GetKeyName(string name) => CaseSensitiveNames ? name : name.ToLowerInvariant();
        private bool IsValidName(string name) => !name.Contains(" ");
    }
}
