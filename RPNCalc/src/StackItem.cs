using System;
using System.Globalization;
using RPNCalc.Extensions;

namespace RPNCalc
{
    /// <summary>
    /// Base item class.
    /// </summary>
    public abstract class AStackItem : IEquatable<AStackItem>
    {
        public enum Type
        {
            Number,
            String,
            Program,
        }

        public readonly Type type;
        public virtual object value { get; protected set; }

        protected AStackItem(Type type)
        {
            this.type = type;
        }

        public static bool operator ==(AStackItem left, AStackItem right) => left.Equals(right);
        public static bool operator !=(AStackItem left, AStackItem right) => !left.Equals(right);

        public static implicit operator AStackItem(double number) => new StackNumber(number);
        public static implicit operator AStackItem(float number) => new StackNumber(number);
        public static implicit operator AStackItem(int number) => new StackNumber(number);
        public static implicit operator AStackItem(uint number) => new StackNumber(number);
        public static implicit operator AStackItem(long number) => new StackNumber(number);
        public static implicit operator AStackItem(ulong number) => new StackNumber(number);
        public static implicit operator AStackItem(string str) => new StackString(str);
        public static implicit operator double(AStackItem item) => item.AsNumber();
        public static implicit operator float(AStackItem item) => (float)item.AsNumber();
        public static implicit operator int(AStackItem item) => (int)item.AsNumber();
        public static implicit operator uint(AStackItem item) => (uint)item.AsNumber();
        public static implicit operator long(AStackItem item) => (long)item.AsNumber();
        public static implicit operator ulong(AStackItem item) => (ulong)item.AsNumber();
        public static implicit operator string(AStackItem item) => item.AsString();

        public override bool Equals(object obj)
        {
            if (obj is not AStackItem item) return false;
            return Equals(item);
        }

        public override int GetHashCode() => value.GetHashCode();
        public override string ToString() => value.ToString();
        public virtual bool Equals(AStackItem other) => other is not null && value.Equals(other.value);
    }

    /// <summary>
    /// Base class containing a strongly typed value.
    /// </summary>
    public abstract class AStackItem<T> : AStackItem, IEquatable<AStackItem<T>> where T : notnull
    {
        protected T _value;
        public new T value
        {
            get => _value;
            protected set
            {
                _value = value;
                base.value = value;
            }
        }

        protected AStackItem(Type type) : base(type) { }
        protected AStackItem(Type type, T value) : base(type) => this.value = value;

        public override bool Equals(object obj)
        {
            if (!(obj is AStackItem item)) return false;
            return Equals(item);
        }

        public override bool Equals(AStackItem other)
        {
            if (type != other.type) return false;
            if (this is StackNumber selfNumber && other is StackNumber otherNumber) return selfNumber.value == otherNumber.value;
            if (this is StackString selfString && other is StackString otherString) return selfString.value == otherString.value;
            if (this is StackProgram selfProgram && other is StackProgram otherProgram) return selfProgram.value == otherProgram.value;
            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public bool Equals(AStackItem<T> other)
        {
            if (type != other.type) return false;
            return value.Equals(other.value);
        }
    }

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

        public static implicit operator StackNumber(double number) => new StackNumber(number);
        public static implicit operator StackNumber(float number) => new StackNumber(number);
        public static implicit operator StackNumber(int number) => new StackNumber(number);
        public static implicit operator StackNumber(uint number) => new StackNumber(number);
        public static implicit operator StackNumber(long number) => new StackNumber(number);
        public static implicit operator StackNumber(ulong number) => new StackNumber(number);
        public static implicit operator double(StackNumber number) => number.value;
        public static implicit operator float(StackNumber number) => (float)number.value;
        public static implicit operator int(StackNumber number) => (int)number.value;
        public static implicit operator uint(StackNumber number) => (uint)number.value;
        public static implicit operator long(StackNumber number) => (long)number.value;
        public static implicit operator ulong(StackNumber number) => (ulong)number.value;

        public bool Equals(double other) => value == other;
        public bool Equals(float other) => value == other;
        public bool Equals(long other) => value == other;
        public bool Equals(ulong other) => value == other;
        public bool Equals(int other) => value == other;
        public bool Equals(uint other) => value == other;

        public override string ToString() => value.ToString();
    }

    public class StackString : AStackItem<string>, IEquatable<string>
    {
        public StackString(string str) : base(Type.String, str) { }

        public static implicit operator StackString(string str) => new StackString(str);
        public static implicit operator string(StackString str) => str.value;

        public bool Equals(string other) => value == other;

        public override string ToString() => $"'{value}'";
    }

    public class StackProgram : AStackItem<string>, IEquatable<string>
    {
        public StackProgram(string program) : base(Type.Program, program) { }

        public bool Equals(string other) => value == other;

        public override string ToString() => $"{{{value}}}";
    }
}
