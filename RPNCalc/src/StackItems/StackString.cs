using System;
using System.Diagnostics;

namespace RPNCalc.StackItems
{
    [DebuggerDisplay("String({ToString()})")]
    public class StackString : AStackItem<string>, IEquatable<string>
    {
        public StackString(string str) : base(Type.String, str) { }

        public static implicit operator StackString(string str) => new(str);
        public static implicit operator string(StackString str) => str.value;

        public bool Equals(string other) => value == other;

        public override string ToString() => $"'{value}'";

        public override bool Equals(AStackItem other) => other is StackString str && value == str.value;
    }
}
