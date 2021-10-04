using System;
using System.Diagnostics;
using RPNCalc.Extensions;

namespace RPNCalc
{
    [DebuggerDisplay("Number({value})")]
    public class StackNumber : AStackItem<double>,
        IEquatable<double>,
        IEquatable<float>,
        IEquatable<long>,
        IEquatable<ulong>,
        IEquatable<int>,
        IEquatable<uint>
    {
        public StackNumber(double value) : base(Type.Number, value) { }

        public static bool operator >(StackNumber left, StackNumber right) => left.value > right.value;
        public static bool operator <(StackNumber left, StackNumber right) => left.value < right.value;
        public static bool operator >=(StackNumber left, StackNumber right) => left.value >= right.value;
        public static bool operator <=(StackNumber left, StackNumber right) => left.value <= right.value;

        public static implicit operator StackNumber(double number) => new(number);
        public static implicit operator StackNumber(float number) => new(number);
        public static implicit operator StackNumber(int number) => new(number);
        public static implicit operator StackNumber(uint number) => new(number);
        public static implicit operator StackNumber(long number) => new(number);
        public static implicit operator StackNumber(ulong number) => new(number);
        public static implicit operator StackNumber(bool boolean) => new(boolean ? 1 : 0);
        public static implicit operator double(StackNumber number) => number.value;
        public static implicit operator float(StackNumber number) => (float)number.value;
        public static implicit operator int(StackNumber number) => (int)number.value;
        public static implicit operator uint(StackNumber number) => (uint)number.value;
        public static implicit operator long(StackNumber number) => (long)number.value;
        public static implicit operator ulong(StackNumber number) => (ulong)number.value;
        public static implicit operator bool(StackNumber number) => number.AsBool();

        public bool Equals(double other) => value == other;
        public bool Equals(float other) => value == other;
        public bool Equals(long other) => value == other;
        public bool Equals(ulong other) => value == other;
        public bool Equals(int other) => value == other;
        public bool Equals(uint other) => value == other;

        public override string ToString() => value.ToString();
    }
}
