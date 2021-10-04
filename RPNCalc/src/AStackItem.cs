using System;
using System.Numerics;
using RPNCalc.Extensions;

namespace RPNCalc
{
    /// <summary>
    /// Base item class.
    /// </summary>
    public abstract class AStackItem
    {
        public enum Type
        {
            /// <summary>Real number, internally type double</summary>
            Number,
            ComplexNumber,
            /// <summary>Simple text string, can be used pass reference on variable/function</summary>
            String,
            /// <summary>An expression object, contains data and/or functions and/or other programs</summary>
            Program,
            Function,
            /// <summary>A reference to any item</summary>
            Name,
            List,
        }

        public readonly Type type;

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
        public static implicit operator AStackItem(Complex complex) => new StackComplex(complex);
        public static implicit operator double(AStackItem item) => item.AsNumber();
        public static implicit operator float(AStackItem item) => (float)item.AsNumber();
        public static implicit operator int(AStackItem item) => (int)item.AsNumber();
        public static implicit operator uint(AStackItem item) => (uint)item.AsNumber();
        public static implicit operator long(AStackItem item) => (long)item.AsNumber();
        public static implicit operator ulong(AStackItem item) => (ulong)item.AsNumber();
        public static implicit operator string(AStackItem item) => item.AsString();
        public static implicit operator bool(AStackItem item) => item.AsBool();
        public static implicit operator AStackItem(bool condition) => (StackNumber)condition;
        public static implicit operator AStackItem(AStackItem[] list) => new StackList(list);
        public static implicit operator Complex(AStackItem item) => item.AsComplex();

        public override bool Equals(object obj) => throw new InvalidOperationException($"Use generic {nameof(AStackItem)}");
        public override int GetHashCode() => throw new InvalidOperationException($"Use generic {nameof(AStackItem)}");
    }

    /// <summary>
    /// Base class containing a strongly typed value.
    /// </summary>
    public abstract class AStackItem<T> : AStackItem, IEquatable<AStackItem> where T : notnull
    {
        public T value;

        protected AStackItem(Type type) : base(type) { }
        protected AStackItem(Type type, T value) : base(type) => this.value = value;

        public abstract bool Equals(AStackItem other);

        public override bool Equals(object obj)
        {
            if (obj is not AStackItem item) return false;
            return Equals(item);
        }

        public override int GetHashCode() => value.GetHashCode();
    }
}
