using System;
using System.Numerics;
using RPNCalc.Extensions;

namespace RPNCalc.Items
{
    /// <summary>
    /// Base item class.
    /// </summary>
    public abstract class AItem
    {
        public enum Type
        {
            RealNumber,
            ComplexNumber,
            /// <summary>Simple text string.</summary>
            String,
            /// <summary>A reference to any item.</summary>
            Name,
            List,
            /// <summary>An expression object, contains data and/or functions and/or other programs.</summary>
            Program,
            /// <summary>C# code or macro.</summary>
            Function,
        }

        public readonly Type type;

        protected AItem(Type type)
        {
            this.type = type;
        }

        public static bool operator ==(AItem left, AItem right) => left.Equals(right);
        public static bool operator !=(AItem left, AItem right) => !left.Equals(right);

        public static implicit operator AItem(double number) => new RealNumberItem(number);
        public static implicit operator AItem(float number) => new RealNumberItem(number);
        public static implicit operator AItem(int number) => new RealNumberItem(number);
        public static implicit operator AItem(uint number) => new RealNumberItem(number);
        public static implicit operator AItem(long number) => new RealNumberItem(number);
        public static implicit operator AItem(ulong number) => new RealNumberItem(number);
        public static implicit operator AItem(string str) => new StringItem(str);
        public static implicit operator AItem(Complex complex) => new ComplexNumberItem(complex);
        public static implicit operator double(AItem item) => item.GetRealNumber();
        public static implicit operator float(AItem item) => (float)item.GetRealNumber();
        public static implicit operator int(AItem item) => (int)item.GetRealNumber();
        public static implicit operator uint(AItem item) => (uint)item.GetRealNumber();
        public static implicit operator long(AItem item) => (long)item.GetRealNumber();
        public static implicit operator ulong(AItem item) => (ulong)item.GetRealNumber();
        public static implicit operator string(AItem item) => item.GetString();
        public static implicit operator bool(AItem item) => item.GetBool();
        public static implicit operator AItem(bool condition) => (RealNumberItem)condition;
        public static implicit operator AItem(AItem[] list) => new ListItem(list);
        public static implicit operator Complex(AItem item) => item.GetComplexNumber();

        public T EnsureType<T>() where T : AItem
        {
            EnsureType(out T typed);
            return typed;
        }

        public void EnsureType<T>(out T typedItem) where T : AItem
        {
            if (this is T realType) typedItem = realType;
            else throw new RPNArgumentException("Bad argument type");
        }

        public override bool Equals(object obj) => throw new InvalidOperationException($"Use generic {nameof(AItem)}");
        public override int GetHashCode() => throw new InvalidOperationException($"Use generic {nameof(AItem)}");
    }

    /// <summary>
    /// Base class containing a strongly typed value.
    /// </summary>
    public abstract class AItem<T> : AItem, IEquatable<AItem> where T : notnull
    {
        public T value;

        protected AItem(Type type) : base(type) { }
        protected AItem(Type type, T value) : base(type) => this.value = value;

        public abstract bool Equals(AItem other);

        public override bool Equals(object obj)
        {
            if (obj is not AItem item) return false;
            return Equals(item);
        }

        public override int GetHashCode() => value.GetHashCode();
    }
}
