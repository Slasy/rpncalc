using System;
using System.Diagnostics;
using System.Globalization;
using RPNCalc.Extensions;

namespace RPNCalc.Items
{
    [DebuggerDisplay("RealNumber({value})")]
    public class RealNumberItem : AItem<double>,
        IEquatable<double>,
        IEquatable<float>,
        IEquatable<long>,
        IEquatable<ulong>,
        IEquatable<int>,
        IEquatable<uint>
    {
        public RealNumberItem(double value) : base(Type.RealNumber, value) { }

        public static bool operator >(RealNumberItem left, RealNumberItem right) => left.value > right.value;
        public static bool operator <(RealNumberItem left, RealNumberItem right) => left.value < right.value;
        public static bool operator >=(RealNumberItem left, RealNumberItem right) => left.value >= right.value;
        public static bool operator <=(RealNumberItem left, RealNumberItem right) => left.value <= right.value;

        public static implicit operator RealNumberItem(double number) => new(number);
        public static implicit operator RealNumberItem(float number) => new(number);
        public static implicit operator RealNumberItem(int number) => new(number);
        public static implicit operator RealNumberItem(uint number) => new(number);
        public static implicit operator RealNumberItem(long number) => new(number);
        public static implicit operator RealNumberItem(ulong number) => new(number);
        public static implicit operator RealNumberItem(bool boolean) => new(boolean ? 1 : 0);
        public static implicit operator double(RealNumberItem number) => number.value;
        public static implicit operator float(RealNumberItem number) => (float)number.value;
        public static implicit operator int(RealNumberItem number) => (int)number.value;
        public static implicit operator uint(RealNumberItem number) => (uint)number.value;
        public static implicit operator long(RealNumberItem number) => (long)number.value;
        public static implicit operator ulong(RealNumberItem number) => (ulong)number.value;
        public static implicit operator bool(RealNumberItem number) => number.GetBool();

        public bool Equals(double other) => value == other;
        public bool Equals(float other) => value == other;
        public bool Equals(long other) => value == other;
        public bool Equals(ulong other) => value == other;
        public bool Equals(int other) => value == other;
        public bool Equals(uint other) => value == other;

        public override bool Equals(AItem other) => (other is RealNumberItem number && number.value == value) || (other is ComplexNumberItem complex && complex.value == value);

        public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
    }
}
