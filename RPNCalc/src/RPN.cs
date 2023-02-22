using System;
using System.Collections.Generic;
using System.Linq;
using RPNCalc.Extensions;
using RPNCalc.Flags;
using RPNCalc.Items;

namespace RPNCalc
{
    /// <summary>
    /// Extensible RPN calculator
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public sealed class RPN
    {
        /// <summary>
        /// Calculator options.
        /// </summary>
        public class Options
        {
            /// <summary>Set true if you want variable and function names to be case sensitive.</summary>
            public bool CaseSensitiveNames { get; set; }

            /// <summary>Automatically clear stack before each <see cref="Eval(AItem[])"/> call.</summary>
            public bool AlwaysClearStack { get; set; } = true;

            /// <summary>Load a set of function to new calc instance,
            /// calculator knows very little without any functions.</summary>
            public bool LoadDefaultFunctions { get; set; } = true;

            public static Options Default => new();
        }

        /// <summary>
        /// <para>Name resolution type.</para>
        /// </summary>
        public enum Scope
        {
            /// <summary>Traverse local scope from inside out with fallback to global scope.</summary>
            Default,
            /// <summary>Access global scope only.</summary>
            Global,
            /// <summary>Access only current local scope, or global if not running inside a program.</summary>
            Local,
            /// <summary>Global scope but can't be overridden with different scope option.</summary>
            Protected,
        }

        /// <summary>
        /// Delegate for all RPN functions/operations, expects to directly operates on stack.
        /// </summary>
        public delegate void Function(Stack<AItem> stack);

        /// <summary>
        /// Called before each evaluated instruction.
        /// </summary>
        public event Action<RPN, AItem>? ProgramStepBefore;

        /// <summary>
        /// Called after each evaluated instruction.
        /// </summary>
        public event Action<RPN, AItem>? ProgramStepAfter;

        public IReadOnlyCollection<string> ProtectedNames => protectedNames;

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

        /// <summary>
        /// General purpose collection of flags
        /// </summary>
        public FlagCollection Flags { get; }

        /// <summary>Automatically clear stack before each <see cref="Eval(AItem[])"/> call.</summary>
        public bool AlwaysClearStack { get; set; }

        /// <summary>Names (and function names) are case sensitive.</summary>
        public bool CaseSensitiveNames { get; }

        /// <summary>
        /// Set to stop currently running calculator program.
        /// </summary>
        public bool StopProgram { private get; set; }

        private readonly Stack<AItem> mainStack = new();

        /// <summary>Used to "buffer" multiple items to collection (list, program, whatever...)</summary>
        private readonly Stack<Stack<AItem>> sideStack = new();

        /// <summary>Functions that are always called even when not using main stack</summary>
        private readonly HashSet<string> functionWhiteList = new();

        /// <summary>Pointer to current stack that is used to push items to</summary>
        private Stack<AItem> currentStackInUse;

        private readonly Dictionary<string, AItem> globalNames = new();
        private readonly Stack<Dictionary<string, AItem>> localNames = new();
        private readonly HashSet<string> protectedNames = new();

        private bool IsUsingMainStack => currentStackInUse == mainStack;
        private int macroCounter;

        /// <summary>
        /// <para>RPN calculator, some of default functions:</para>
        /// <para>+ - * / ^ addition, subtraction, multiplication, division, exponentiation</para>
        /// <para>+- ++ -- change sign, +1 to variable, -1 from variable</para>
        /// <para>SQ, SQRT : square, square root</para>
        /// <para>DROP, DUP, SWAP : drop, duplicate, swap values on stack</para>
        /// <para>ROT, ROLL : rotate top 3 values, roll/rotate whole stack</para>
        /// <para>DEPTH, CLV, CLST : number of values on stack, clear variable, clear stack</para>
        /// <para>STO, RCL, EVAL : store variable, recall variable, evaluate/execute program on stack</para>
        /// <para>IFT, IFTE, WHILE : if then, if then else, while loop</para>
        /// </summary>
        public RPN(Options? options = null)
        {
            options ??= Options.Default;
            Flags = new(options.CaseSensitiveNames);
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
        public AItem? Eval(AItem[] instruction)
        {
            if (AlwaysClearStack) ClearStack();
            // this is already processing a "program" so inner programs will be just pushed to stack
            AItem? top = EvalItems(instruction, false);
            if (AlwaysClearStack && sideStack.Count != 0) throw new RPNFunctionException("A collection-creating function didn't finished buffering items");
            return top;
        }

        /// <summary>
        /// Primary use is for writing new functions.
        /// </summary>
        /// <param name="items">Set of instructions/items for evaluation</param>
        /// <param name="evalPrograms">Set to false to just push a program or name to stack without evaluating</param>
        public AItem? EvalItems(AItem[] items, bool evalPrograms)
        {
            foreach (AItem item in items)
            {
                EvalItem(item, evalPrograms);
                if (StopProgram)
                {
                    if (macroCounter == 0) StopProgram = false;
                    else macroCounter--;
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
                case FunctionItem function:
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
        /// Set custom variable for this calculator instance.
        /// </summary>
        /// <param name="name">variable name</param>
        /// <param name="value">value</param>
        /// <param name="scopeType"></param>
        /// <returns>name used to save value</returns>
        public string SetNameValue(string name, AItem value, Scope scopeType = Scope.Default)
        {
            EnsureValidName(name);
            if (value is null) throw new RPNArgumentException($"Can't set {name} to null");
            name = GetKeyName(name);
            if (scopeType == Scope.Default)
            {
                if (localNames.Count > 0)
                {
                    Dictionary<string, AItem>? scope = GetScopeContainingName(name);
                    if (scope is null || scope == globalNames)
                    {
                        localNames.Peek()[name] = value;
                    }
                    else
                    {
                        if (scope == globalNames) EnsureNotProtected(name, scopeType);
                        scope[name] = value;
                    }
                }
                else
                {
                    EnsureNotProtected(name, scopeType);
                    globalNames[name] = value;
                }
            }
            else if (scopeType == Scope.Local)
            {
                if (localNames.Count > 0)
                {
                    localNames.Peek()[name] = value;
                }
                else
                {
                    EnsureNotProtected(name, scopeType);
                    globalNames[name] = value;
                }
            }
            else if (scopeType == Scope.Protected)
            {
                protectedNames.Add(name);
                globalNames[name] = value;
            }
            else
            {
                EnsureNotProtected(name, scopeType);
                globalNames[name] = value;
            }
            return name;
        }

        /// <summary>
        /// Set custom C# function for this calculator instance.
        /// </summary>
        /// <param name="name">function name</param>
        /// <param name="function">C# function</param>
        /// <param name="setProtected">function will be protected against overriding and removing</param>
        /// <returns>name used to save function</returns>
        public string SetNameValue(string name, Function function, bool setProtected = false) => SetFunction(name, function, false, setProtected);

        /// <summary>
        /// Set custom macro-like function for this calculator instance.
        /// </summary>
        /// <param name="name">macro name</param>
        /// <param name="instructions">macro expression as a collection of instructions</param>
        /// <param name="setProtected">macro will be protected against overriding and removing</param>
        /// <returns>name used to save macro</returns>
        public string SetNameValue(string name, AItem[] instructions, bool setProtected = false)
        {
            EnsureValidName(name);
            if (instructions is null) throw new RPNArgumentException("Instructions are null");
            return SetNameValue(name, macro, setProtected);

            void macro(Stack<AItem> _)
            {
                macroCounter++;
                EvalItems(instructions, false);
            }
        }

        public void SetCollectionGenerator(string startSymbol, string endSymbol, Func<Stack<AItem>, AItem> collectionGenerator, bool setProtected = false)
        {
            if (startSymbol == endSymbol) throw new RPNArgumentException("Start and end symbols must be different");
            SetFunction(startSymbol, GenerateStartCollectionStack(startSymbol), true, setProtected);
            SetFunction(endSymbol, GenerateEndCollectionStack(startSymbol, endSymbol, collectionGenerator), true, setProtected);
        }

        public bool RemoveName(string name, Scope scopeType = Scope.Default)
        {
            EnsureValidName(name);
            EnsureNotProtected(name, scopeType);
            name = GetKeyName(name);
            bool removed = false;
            if (scopeType == Scope.Default)
            {
                Dictionary<string, AItem>? scope = GetScopeContainingName(name);
                if (scope is null) return false;
                removed = scope.Remove(name);
                if (scope == globalNames) functionWhiteList.Remove(name);
            }
            else if (scopeType == Scope.Global)
            {
                removed = globalNames.Remove(name);
                functionWhiteList.Remove(name);
            }
            else if (LocalNames.Count > 0)
            {
                removed = localNames.Peek().Remove(name);
            }
            return removed;
        }

        /// <summary>
        /// Get item this name refers to.
        /// </summary>
        public AItem GetNameValue(string name, Scope scopeType = Scope.Default)
        {
            EnsureValidName(name);
            name = GetKeyName(name);
            if (scopeType is Scope.Global or Scope.Protected)
            {
                if (globalNames.TryGetValue(name, out var value)) return value;
            }
            else if (scopeType == Scope.Default)
            {
                Dictionary<string, AItem>? scope = GetScopeContainingName(name);
                if (scope is not null) return scope[name];
            }
            else if (scopeType == Scope.Local && localNames.Count > 0)
            {
                if (localNames.Peek().TryGetValue(name, out var value)) return value;
            }
            throw new RPNUndefinedNameException($"Unknown name {name}");
        }

        private Dictionary<string, AItem>? GetScopeContainingName(string name)
        {
            EnsureValidName(name);
            name = GetKeyName(name);
            for (int i = 0; i < localNames.Count; i++)
            {
                if (localNames[i].ContainsKey(name))
                {
                    return localNames[i];
                }
            }
            if (globalNames.ContainsKey(name)) return globalNames;
            return null;
        }

        private string SetFunction(string name, Function function, bool alsoAddToWhiteList, bool alsoAddToProtected)
        {
            EnsureValidName(name);
            if (function is null) throw new RPNArgumentException($"Can't set function {name} to null");
            name = GetKeyName(name);
            globalNames[name] = new FunctionItem(name, function);
            if (alsoAddToWhiteList) functionWhiteList.Add(name);
            if (alsoAddToProtected) protectedNames.Add(name);
            return name;
        }

        private bool IsWhiteListFunction(string name) => functionWhiteList.Contains(GetKeyName(name)) && GetScopeContainingName(name) == globalNames;
        private bool IsProtectedName(string name) => protectedNames.Contains(GetKeyName(name));

        private void EnsureNotProtected(string name, Scope scopeType)
        {
            if (scopeType != Scope.Protected && IsProtectedName(name))
            {
                throw new RPNArgumentException($"Name {name} is protected");
            }
        }

        private void EnsureValidName(string name)
        {
            if (name is null) throw new RPNArgumentException("Name is null");
            if (!IsValidName(name)) throw new RPNArgumentException($"Invalid name {name}");
        }

        private string GetKeyName(string name) => CaseSensitiveNames ? name : name.ToLowerInvariant();

        private bool IsValidName(string name)
        {
            foreach (char ch in name)
            {
                if (ch is ' ' or '\'') return false;
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

        private Function GenerateEndCollectionStack(string startSymbol, string endSymbol, Func<Stack<AItem>, AItem> collectionConstructor)
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
