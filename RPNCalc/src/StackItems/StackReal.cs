using System;
using System.Diagnostics;
using System.Globalization;
using RPNCalc.Extensions;

namespace RPNCalc.StackItems
{
    [DebuggerDisplay("RealNumber({value})")]
    public class StackReal : AStackItem<double>,
        IEquatable<double>,
        IEquatable<float>,
        IEquatable<long>,
        IEquatable<ulong>,
        IEquatable<int>,
        IEquatable<uint>
    {
        public StackReal(double value) : base(Type.RealNumber, value) { }

        public static bool operator >(StackReal left, StackReal right) => left.value > right.value;
        public static bool operator <(StackReal left, StackReal right) => left.value < right.value;
        public static bool operator >=(StackReal left, StackReal right) => left.value >= right.value;
        public static bool operator <=(StackReal left, StackReal right) => left.value <= right.value;

        public static implicit operator StackReal(double number) => new(number);
        public static implicit operator StackReal(float number) => new(number);
        public static implicit operator StackReal(int number) => new(number);
        public static implicit operator StackReal(uint number) => new(number);
        public static implicit operator StackReal(long number) => new(number);
        public static implicit operator StackReal(ulong number) => new(number);
        public static implicit operator StackReal(bool boolean) => new(boolean ? 1 : 0);
        public static implicit operator double(StackReal number) => number.value;
        public static implicit operator float(StackReal number) => (float)number.value;
        public static implicit operator int(StackReal number) => (int)number.value;
        public static implicit operator uint(StackReal number) => (uint)number.value;
        public static implicit operator long(StackReal number) => (long)number.value;
        public static implicit operator ulong(StackReal number) => (ulong)number.value;
        public static implicit operator bool(StackReal number) => number.GetBool();

        public bool Equals(double other) => value == other;
        public bool Equals(float other) => value == other;
        public bool Equals(long other) => value == other;
        public bool Equals(ulong other) => value == other;
        public bool Equals(int other) => value == other;
        public bool Equals(uint other) => value == other;

        public override bool Equals(AStackItem other) => (other is StackReal number && number.value == value) || (other is StackComplex complex && complex.value == value);

        public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
    }
}
