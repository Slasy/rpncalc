using System;
using System.Linq;
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
        public static implicit operator double(AStackItem item) => item.AsNumber();
        public static implicit operator float(AStackItem item) => (float)item.AsNumber();
        public static implicit operator int(AStackItem item) => (int)item.AsNumber();
        public static implicit operator uint(AStackItem item) => (uint)item.AsNumber();
        public static implicit operator long(AStackItem item) => (long)item.AsNumber();
        public static implicit operator ulong(AStackItem item) => (ulong)item.AsNumber();
        public static implicit operator string(AStackItem item) => item.AsString();
        public static implicit operator bool(AStackItem item) => item.AsBool();
        public static implicit operator AStackItem(bool condition) => (StackNumber)condition;
        //public static implicit operator AStackItem[](AStackItem item) => item.AsArray();
        public static implicit operator AStackItem(AStackItem[] list) => new StackList(list);

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

        public override bool Equals(object obj)
        {
            if (obj is not AStackItem item) return false;
            return Equals(item);
        }

        public bool Equals(AStackItem other)
        {
            if (type != other.type) return false;
            if (this is StackNumber selfNumber && other is StackNumber otherNumber) return selfNumber.value == otherNumber.value;
            if (this is StackString selfString && other is StackString otherString) return selfString.value == otherString.value;
            if (this is StackProgram selfProgram && other is StackProgram otherProgram) return selfProgram.value.SequenceEqual(otherProgram.value);
            if (this is StackList selfList && other is StackList otherList) return selfList.value.SequenceEqual(otherList.value);
            if (this is StackFunction selfFunction && other is StackFunction otherFunction) return selfFunction.name == otherFunction.name && selfFunction.value.Equals(otherFunction.value);
            if (this is StackName selfVariable && other is StackName otherVariable) return selfVariable.value == otherVariable.value;
            return false;
        }

        public override int GetHashCode() => value.GetHashCode();
    }
}
