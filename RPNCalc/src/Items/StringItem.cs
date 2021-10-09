using System;
using System.Diagnostics;

namespace RPNCalc.Items
{
    [DebuggerDisplay("String({ToString()})")]
    public class StackStringItem : AItem<string>, IEquatable<string>
    {
        public StackStringItem(string str) : base(Type.String, str) { }

        public static implicit operator StackStringItem(string str) => new(str);
        public static implicit operator string(StackStringItem str) => str.value;

        public bool Equals(string other) => value == other;

        public override string ToString() => $"'{value.Replace("'", "\\'")}'";

        public override bool Equals(AItem other) => other is StackStringItem str && value == str.value;
    }
}
