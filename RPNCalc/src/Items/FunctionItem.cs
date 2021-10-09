using System.Diagnostics;

namespace RPNCalc.Items
{
    [DebuggerDisplay("Function({name})")]
    public class FunctionItem : AItem<RPN.Function>
    {
        public readonly string name;
        public FunctionItem(string name, RPN.Function function) : base(Type.Function, function) => this.name = name;

        public override bool Equals(AItem other) => other is FunctionItem function && name == function.name && value == function.value;

        public override string ToString() => name;
    }
}
