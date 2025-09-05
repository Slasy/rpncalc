using System;
using System.Diagnostics;

namespace RPNCalc.Items
{
    [DebuggerDisplay("String({ToString()})")]
    public sealed class StringItem : AItem<string>, IEquatable<string>
    {
        public StringItem(string str) : base(Type.String, str) { }

        public static implicit operator StringItem(string str) => new(str);
        public static implicit operator string(StringItem str) => str.value;

        public bool Equals(string? other) => value == other;
        public override string ToString() => $"'{value.Replace("'", "\\'")}'";
        public override bool Equals(AItem? other) => other is StringItem str && value == str.value;
    }
}
