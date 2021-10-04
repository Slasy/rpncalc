using System.Diagnostics;

namespace RPNCalc
{
    [DebuggerDisplay("Name({value})")]
    public class StackName : AStackItem<string>
    {
        public StackName(string referenceName) : base(Type.Name, referenceName) { }

        public override bool Equals(AStackItem other) => other is StackName name && value == name.value;

        public override string ToString() => value;
    }
}
