using System.Diagnostics;

namespace RPNCalc
{
    [DebuggerDisplay("Function({name})")]
    public class StackFunction : AStackItem<RPN.Function>
    {
        public readonly string name;
        public StackFunction(string name, RPN.Function function) : base(Type.Function, function) => this.name = name;

        public override string ToString() => name;
    }
}
