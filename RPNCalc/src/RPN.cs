using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPNCalc.Extensions;
using RPNCalc.Items;

namespace RPNCalc
{
    /// <summary>
    /// Extensible RPN calculator
    /// </summary>
    public class RPN
    {
        /// <summary>
        /// Calculator options.
        /// </summary>
        public class Options
        {
            /// <summary>Set true if you want variable and function names to be case sensitive.</summary>
            public bool CaseSensitiveNames { get; set; } = false;
            /// <summary>Automatically clear stack before each <see cref="Eval(AItem[])"/> call.</summary>
            public bool AlwaysClearStack { get; set; } = true;
            /// <summary>Load a set of function to new calc instance,
            /// calculator knows very little without any functions.</summary>
            public bool LoadDefaultFunctions { get; set; } = true;

            public static Options Default => new();
        }

        /// <summary>
        /// Delegate for all RPN functions/operations, expects to directly operates on stack.
        /// </summary>
        public delegate void Function(Stack<AItem> stack);

        /// <summary>
        /// Called before each evaluated instruction.
        /// </summary>
        public event Action<RPN, AItem> ProgramStepBefore;

        /// <summary>
        /// Called after each evaluated instruction.
        /// </summary>
        public event Action<RPN, AItem> ProgramStepAfter;

        /// <summary>
        /// Collection of all currently globally accessible names usable in this calculator.
        /// </summary>
        public IReadOnlyDictionary<string, AItem> GlobalNames => globalNames;

        /// <summary>
        /// Collection of current local names only, returns empty dictionary if running in global name space.
        /// </summary>
        public IReadOnlyDictionary<string, AItem> LocalNames => localNames.Count > 0 ? localNames.Peek() : new();

        /// <summary>
        /// <para>From all names only function names available to use in this calculator.</para>
        /// <para>Also includes all default functions.</para>
        /// </summary>
        public IReadOnlyCollection<string> FunctionsView => globalNames
            .Where(x => x.Value.type == AItem.Type.Function)
            .Select(x => x.Key)
            .ToArray();

        /// <summary>
        /// Read only access to current stack. On index 0 is top of the stack.
        /// </summary>
        public IReadOnlyList<AItem> StackView => mainStack;

        /// <summary>Automatically clear stack before each <see cref="Eval(AItem[])"/> call.</summary>
        public bool AlwaysClearStack { get; set; } = true;
        /// <summary>Names (and function names) are case sensitive.</summary>
        public bool CaseSensitiveNames { get; }

        /// <summary>
        /// Set to stop currently running calculator program.
        /// </summary>
        public bool StopProgram { protected get; set; }

        protected readonly Stack<AItem> mainStack = new();
        protected readonly StringBuilder buffer = new();
        /// <summary>Used to "buffer" multiple items to collection (list, program, whatever...)</summary>
        protected readonly Stack<Stack<AItem>> sideStack = new();
        /// <summary>Functions that are always called even when not using main stack</summary>
        protected readonly HashSet<string> functionWhiteList = new();
        /// <summary>Pointer to current stack that is used to push items to</summary>
        protected Stack<AItem> currentStackInUse;
        protected readonly Dictionary<string, AItem> globalNames = new();
        protected readonly Stack<Dictionary<string, AItem>> localNames = new();

        protected bool IsUsingMainStack => currentStackInUse == mainStack;

        /// <summary>
        /// <para>RPN calculator, some of default functions:</para>
        /// <para>+ - * / ^ addition, subtraction, multiplication, division, exponentiation</para>
        /// <para>+- ++ -- change sign, +1 to variable, -1 from variable</para>
        /// <para>SQ, SQRT : square, square root</para>
        /// <para>DROP, DUP, SWAP : drop, duplicate, swap values on stack</para>
        /// <para>ROT, ROLL : rottate top 3 values, roll/rotate whole stack</para>
        /// <para>DEPTH, CLV, CLST : number of values on stack, clear variable, clear stack</para>
        /// <para>STO, RCL, EVAL : store variable, recall variable, evaluate/execute program on stack</para>
        /// <para>IFT, IFTE, WHILE : if then, if then else, while loop</para>
        /// </summary>
        public RPN(Options options = null)
        {
            options ??= Options.Default;
            CaseSensitiveNames = options.CaseSensitiveNames;
            AlwaysClearStack = options.AlwaysClearStack;
            currentStackInUse = mainStack;
            if (options.LoadDefaultFunctions) this.LoadDefaultFunctions();
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
        public AItem Eval(AItem[] instruction)
        {
            if (AlwaysClearStack) ClearStack();
            // this is already processing a "program" so it starts at level 1
            AItem top = EvalItems(instruction, false);
            if (AlwaysClearStack && sideStack.Count != 0) throw new RPNFunctionException("A collection-creating function didn't finished buffering items");
            return top;
        }

        /// <summary>
        /// Primary use is for writing new functions.
        /// </summary>
        /// <param name="items">Set of instructions/items for evaluation</param>
        /// <param name="evalPrograms">Set to false to just push a program or name to stack without evaluating</param>
        public AItem EvalItems(AItem[] items, bool evalPrograms)
        {
            foreach (AItem item in items)
            {
                EvalItem(item, evalPrograms);
                if (StopProgram)
                {
                    StopProgram = false;
                    break;
                }
            }
            if (mainStack.Count == 0) return null;
            return mainStack.Peek();
        }

        /// <summary>
        /// Primary use is for writing new functions.
        /// </summary>
        /// <param name="item">One instruction/item for evaluation</param>
        /// <param name="evalPrograms">Set to false to just push a program or name to stack without evaluating</param>
        public void EvalItem(AItem item, bool evalPrograms)
        {
            ProgramStepBefore?.Invoke(this, item);
            switch (item)
            {
                case ProgramItem prog when evalPrograms:
                    localNames.Push(new());
                    EvalItems(prog.value, false);
                    localNames.Pop().Clear();
                    break;
                case ProgramItem:
                    currentStackInUse.Push(item);
                    break;
                case Items.FunctionItem function:
                    if (IsUsingMainStack || IsWhiteListFunction(function.name)) function.value(currentStackInUse);
                    else currentStackInUse.Push(new NameItem(function.name));
                    break;
                case NameItem name:
                    if (IsUsingMainStack || IsWhiteListFunction(name.value)) EvalItem(GetNameValue(name.value), true);
                    else currentStackInUse.Push(name);
                    break;
                // everything else goes to stack
                default:
                    currentStackInUse.Push(item);
                    break;
            }
            ProgramStepAfter?.Invoke(this, item);
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
        public void ClearAllNames() => globalNames.Clear();

        /// <summary>
        /// Set custom variable for this calculator instance.
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">value or null to remove variable</param>
        /// <param name="globalNamesOnly">force seting global name space even when running in local name space</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public void SetName(string name, AItem value, bool globalNamesOnly = false)
        {
            EnsureValidName(name);
            name = GetKeyName(name);
            if (value is null)
            {
                if (globalNamesOnly || localNames.Count == 0 || !localNames.Peek().Remove(name))
                {
                    globalNames.Remove(name);
                }
            }
            else
            {
                if (!globalNamesOnly && localNames.Count > 0)
                {
                    localNames.Peek()[name] = value;
                }
                else
                {
                    globalNames[name] = value;
                }
            }
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
        public void SetName(string name, AItem[] instructions)
        {
            if (instructions is null) SetName(name, (Function)null);
            else SetName(name, _ => EvalItems(instructions, false));
        }

        public void SetCollectionGenerator(string startSymbol, string endSymbol, Func<Stack<AItem>, AItem> collectionGenerator)
        {
            //if (startSymbol == endSymbol) throw new RPNArgumentException("Start and end symbols must be different");
            SetFunction(startSymbol, GenerateStartCollectionStack(startSymbol), true);
            SetFunction(endSymbol, GenerateEndCollectionStack(startSymbol, endSymbol, collectionGenerator), true);
        }

        public void RemoveName(string name)
        {
            globalNames.Remove(GetKeyName(name));
            functionWhiteList.Remove(GetKeyName(name));
        }

        public AItem GetNameValue(string varName, bool globalNamesOnly = false)
        {
            if (!globalNamesOnly && localNames.Count > 0 && localNames.Peek().TryGetValue(varName, out var localItem))
            {
                return localItem;
            }
            if (globalNames.TryGetValue(GetKeyName(varName), out var globalItem))
            {
                return globalItem;
            }
            throw new RPNUndefinedNameException($"Unknown name {varName}");
        }

        protected void SetFunction(string name, Function function, bool alsoAddToWhiteList)
        {
            EnsureValidName(name);
            name = GetKeyName(name);
            if (function is null)
            {
                globalNames.Remove(name);
                functionWhiteList.Remove(name);
            }
            else
            {
                globalNames[name] = new Items.FunctionItem(name, function);
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

        protected Function GenerateStartCollectionStack(string startSymbol)
        {
            return _ =>
            {
                sideStack.Push(currentStackInUse = new());
                currentStackInUse.Push(startSymbol);
            };
        }

        protected Function GenerateEndCollectionStack(string startSymbol, string endSymbol, Func<Stack<AItem>, AItem> collectionConstructor)
        {
            return _ =>
            {
                var tmpStack = sideStack.Pop();
                tmpStack.Roll(-1);
                string expectedSymbol = tmpStack.Pop().GetString();
                if (startSymbol != expectedSymbol) throw new RPNFunctionException($"Unexpected symbol, was expecting {endSymbol}");
                AItem newCollection = collectionConstructor(tmpStack);
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
