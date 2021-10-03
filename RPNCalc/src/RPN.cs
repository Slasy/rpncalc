using System;
using System.Collections.Generic;
using System.Linq;
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
        public delegate void Function(Stack<AStackItem> stack);

        /// <summary>
        /// Collection of all currently accessible names and usable in this calculator.
        /// </summary>
        public IReadOnlyDictionary<string, AStackItem> Names => names;

        /// <summary>
        /// <para>From all names only function names available to use in this calculator.</para>
        /// <para>Also includes all default functions.</para>
        /// </summary>
        public IReadOnlyCollection<string> FunctionsView => names
            .Where(x => x.Value.type == AStackItem.Type.Function)
            .Select(x => x.Key)
            .ToArray();

        /// <summary>
        /// Read only access to current stack. On index 0 is top of the stack.
        /// </summary>
        public IReadOnlyList<AStackItem> StackView => mainStack;

        /// <summary>Automatically clear stack before each <see cref="Eval(AStackItem[])"/> call.</summary>
        public bool AlwaysClearStack { get; set; } = true;
        /// <summary>Names (and function names) are case sensitive.</summary>
        public bool CaseSensitiveNames { get; }

        protected readonly Stack<AStackItem> mainStack = new();
        protected readonly StringBuilder buffer = new();
        /// <summary>Used to "buffer" multiple items to collection (list, program, whatever...)</summary>
        protected readonly Stack<Stack<AStackItem>> sideStack = new();
        /// <summary>Functions that are always called even when not using main stack</summary>
        protected readonly HashSet<string> functionWhiteList = new();
        /// <summary>Pointer to current stack that is used to push items to</summary>
        protected Stack<AStackItem> currentStackInUse;
        protected readonly Dictionary<string, AStackItem> names = new();

        protected bool IsUsingMainStack => currentStackInUse == mainStack;

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
        /// <param name="alwaysClearStack">Automatically clear stack before each <see cref="Eval(AStackItem[])"/> call.</param>
        /// <param name="loadDefaultFunctions">Calculator knows very little without any functions.</param>
        public RPN(bool caseSensitiveNames = false, bool alwaysClearStack = true, bool loadDefaultFunctions = true)
        {
            CaseSensitiveNames = caseSensitiveNames;
            AlwaysClearStack = alwaysClearStack;
            currentStackInUse = mainStack;
            if (loadDefaultFunctions) this.LoadDefaultFunctions();
        }

        /// <summary>
        /// Evaluates instruction(s) and returns top value from stack.
        /// </summary>
        /// <param name="instruction">one or more instructions</param>
        /// <returns>top value on stack or null if stack is empty</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="RPNUndefinedNameException"/>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        /// <exception cref="RPNArgumentException"/>
        public AStackItem Eval(AStackItem[] instruction)
        {
            if (AlwaysClearStack) ClearStack();
            // this is already processing a "program" so it starts at level 1
            AStackItem top = EvalItems(instruction, false);
            if (AlwaysClearStack && sideStack.Count != 0) throw new RPNFunctionException("A collection-creating function didn't finished buffering items");
            return top;
        }

        /// <summary>
        /// Primary use is for writing new functions.
        /// </summary>
        /// <param name="items">Set of instructions/items for evaluation</param>
        /// <param name="evalPrograms">Set to false to just push a program or name to stack without evaluating</param>
        public AStackItem EvalItems(AStackItem[] items, bool evalPrograms)
        {
            foreach (AStackItem item in items)
            {
                EvalItem(item, evalPrograms);
            }
            if (mainStack.Count == 0) return null;
            return mainStack.Peek();
        }

        /// <summary>
        /// Primary use is for writing new functions.
        /// </summary>
        /// <param name="item">One instruction/item for evaluation</param>
        /// <param name="evalPrograms">Set to false to just push a program or name to stack without evaluating</param>
        public void EvalItem(AStackItem item, bool evalPrograms)
        {
            switch (item)
            {
                case StackProgram prog when evalPrograms:
                    EvalItems(prog.value, false);
                    break;
                case StackProgram:
                    currentStackInUse.Push(item);
                    break;
                case StackFunction function:
                    if (IsUsingMainStack || IsWhiteListFunction(function.name)) function.value(currentStackInUse);
                    else currentStackInUse.Push(new StackName(function.name));
                    break;
                case StackName name:
                    if (IsUsingMainStack || IsWhiteListFunction(name.value)) EvalItem(GetNameValue(name.value), true);
                    else currentStackInUse.Push(name);
                    break;
                // everything else goes to stack
                default:
                    currentStackInUse.Push(item);
                    break;
            }
        }

        /// <summary>
        /// Clear stack memory.
        /// </summary>
        public void ClearStack()
        {
            mainStack.Clear();
            sideStack.Clear();
            currentStackInUse = mainStack;
        }

        /// <summary>
        /// Clear ALL names, also clears ALL built-in functions
        /// </summary>
        public void ClearAllNames() => names.Clear();

        /// <summary>
        /// Set custom variable for this calculator instance.
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">value or null to remove variable</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetName(string name, AStackItem value)
        {
            EnsureValidName(name);
            name = GetKeyName(name);
            if (value is null) names.Remove(name);
            else names[name] = value;
        }

        /// <summary>
        /// Set custom C# function for this calculator instance.
        /// </summary>
        /// <param name="name">function name</param>
        /// <param name="function">function or null to remove function</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetName(string name, Function function) => SetFunction(name, function, false);

        /// <summary>
        /// Set custom macro as function for this calculator instance.
        /// </summary>
        /// <param name="name">function name</param>
        /// <param name="instructions">macro expression or null to remove function</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetName(string name, AStackItem[] instructions)
        {
            if (instructions is null) SetName(name, (Function)null);
            else SetName(name, _ => EvalItems(instructions, false));
        }

        public void SetCollectionGenerator(string startSymbol, string endSymbol, Func<Stack<AStackItem>, AStackItem> collectionGenerator)
        {
            //if (startSymbol == endSymbol) throw new RPNArgumentException("Start and end symbols must be different");
            SetFunction(startSymbol, GenerateStartCollectionStack(startSymbol), true);
            SetFunction(endSymbol, GenerateEndCollectionStack(startSymbol, endSymbol, collectionGenerator), true);
        }

        public void RemoveName(string name)
        {
            names.Remove(GetKeyName(name));
            functionWhiteList.Remove(GetKeyName(name));
        }

        public AStackItem GetNameValue(string varName)
        {
            if (!names.TryGetValue(GetKeyName(varName), out var item))
            {
                throw new RPNUndefinedNameException($"Unknown name {varName}");
            }
            return item;
        }

        protected void SetFunction(string name, Function function, bool alsoAddToWhiteList)
        {
            EnsureValidName(name);
            name = GetKeyName(name);
            if (function is null)
            {
                names.Remove(name);
                functionWhiteList.Remove(name);
            }
            else
            {
                names[name] = new StackFunction(name, function);
                if (alsoAddToWhiteList) functionWhiteList.Add(name);
            }
        }

        protected bool IsWhiteListFunction(string name) => functionWhiteList.Contains(GetKeyName(name));
        protected void EnsureValidName(string name)
        {
            //if (name is null) throw new RPNArgumentException($"Name is null");
            if (!IsValidName(name)) throw new RPNArgumentException($"Invalid name {name}");
        }
        protected string GetKeyName(string name) => CaseSensitiveNames ? name : name.ToLowerInvariant();
        protected bool IsValidName(string name)
        {
            foreach (char ch in name)
            {
                if (ch == ' ' || ch == '\'') return false;
            }
            return true;
        }

        private Function GenerateStartCollectionStack(string startSymbol)
        {
            return _ =>
            {
                sideStack.Push(currentStackInUse = new());
                currentStackInUse.Push(startSymbol);
            };
        }

        private Function GenerateEndCollectionStack(string startSymbol, string endSymbol, Func<Stack<AStackItem>, AStackItem> collectionConstructor)
        {
            return _ =>
            {
                var tmpStack = sideStack.Pop();
                tmpStack.Roll(-1);
                string expectedSymbol = tmpStack.Pop().AsString();
                if (startSymbol != expectedSymbol) throw new RPNFunctionException($"Unexpected symbol, was expecting {endSymbol}");
                AStackItem newCollection = collectionConstructor(tmpStack);
                if (sideStack.Count > 0)
                {
                    sideStack.Peek().Push(newCollection);
                    currentStackInUse = sideStack.Peek();
                }
                else
                {
                    currentStackInUse = mainStack;
                    mainStack.Push(newCollection);
                }
            };
        }
    }
}
