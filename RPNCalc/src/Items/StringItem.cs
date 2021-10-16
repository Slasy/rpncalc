using System;
using System.Diagnostics;
using RPNCalc.Extensions;

namespace RPNCalc.Items
{
    [DebuggerDisplay("String({ToString()})")]
    public class StringItem : AItem<string>, IEquatable<string>
    {
        public StringItem(string str) : base(Type.String, str) { }

        public static implicit operator StringItem(string str) => new(str);
        public static implicit operator string(StringItem str) => str.value;

        public static StringItem operator +(StringItem strA, StringItem strB)
        {
            return new StringItem(strA.value + strB.value);
        }

        public static StringItem operator +(StringItem str, AItem other)
        {
            return new StringItem(str.value + other.AsString());
        }

        public static StringItem operator +(AItem other, StringItem str)
        {
            return new StringItem(other.AsString() + str.value);
        }

        public bool Equals(string other) => value == other;
        public override string ToString() => $"'{value.Replace("'", "\\'")}'";
        public override bool Equals(AItem other) => other is StringItem str && value == str.value;
    }
}
