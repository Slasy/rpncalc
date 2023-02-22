using System.Diagnostics;

namespace RPNCalc.Items
{
    [DebuggerDisplay("Name({value})")]
    public sealed class NameItem : AItem<string>
    {
        public NameItem(string referenceName) : base(Type.Name, referenceName) { }

        public override bool Equals(AItem? other) => other is NameItem name && value == name.value;

        public override string ToString() => value;
    }
}
