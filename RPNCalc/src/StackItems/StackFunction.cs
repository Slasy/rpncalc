using System.Diagnostics;

namespace RPNCalc.StackItems
{
    [DebuggerDisplay("Function({name})")]
    public class StackFunction : AStackItem<RPN.Function>
    {
        public readonly string name;
        public StackFunction(string name, RPN.Function function) : base(Type.Function, function) => this.name = name;

        public override bool Equals(AStackItem other) => other is StackFunction function && name == function.name && value == function.value;

        public override string ToString() => name;
    }
}
