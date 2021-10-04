using System.Diagnostics;

namespace RPNCalc
{
    [DebuggerDisplay("Name({value})")]
    public class StackName : AStackItem<string>
    {
        public StackName(string referenceName) : base(Type.Name, referenceName) { }

        public override string ToString() => value;
    }
}
