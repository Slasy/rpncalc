using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    [DebuggerDisplay("String({ToString()})")]
    public class StackString : AStackItem<string>, IEquatable<string>
    {
        public StackString(string str) : base(Type.String, str) { }

        public static implicit operator StackString(string str) => new(str);
        public static implicit operator string(StackString str) => str.value;

        public bool Equals(string other) => value == other;

        public override string ToString() => $"'{value}'";
    }

    [DebuggerDisplay("Program({ToString()})")]
    public class StackProgram : AStackItem<AStackItem[]>
    {
        public StackProgram(AStackItem[] array) : base(Type.Program, array) { }
        public StackProgram(Stack<AStackItem> stack) : base(Type.Program, stack.ToReverseArray()) { }

        public static StackProgram From(params AStackItem[] array) => new(array);

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{ ");
            foreach (var item in value)
            {
                sb.Append(item.ToString());
                sb.Append(' ');
            }
            sb.Append('}');
            return sb.ToString();
        }
    }

    [DebuggerDisplay("List({ToString()})")]
    public class StackList : AStackItem<AStackItem[]>
    {
        /// <summary>Empty list.</summary>
        public StackList() : base(Type.List, Array.Empty<AStackItem>()) { }
        public StackList(AStackItem[] array) : base(Type.List, array) { }
        /// <summary>Items in list will be in revers order - bottom/last item in stack will be first.</summary>
        public StackList(Stack<AStackItem> stack) : base(Type.List, stack.ToReverseArray()) { }

        public static StackList operator +(StackList list, AStackItem item)
        {
            AStackItem[] newArray = new AStackItem[list.value.Length + 1];
            Array.Copy(list.value, newArray, list.value.Length);
            newArray[list.value.Length] = item;
            return new StackList(newArray);
        }

        public static implicit operator StackList(AStackItem[] array) => new(array);
        public static implicit operator AStackItem[](StackList item) => item.value;

        public static StackList From(params AStackItem[] array) => new(array);

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[ ");
            foreach (var item in value)
            {
                sb.Append(item.ToString());
                sb.Append(' ');
            }
            sb.Append(']');
            return sb.ToString();
        }

        public bool Equals(AStackItem[] other) => value.SequenceEqual(other);
    }

    [DebuggerDisplay("Function({name})")]
    public class StackFunction : AStackItem<RPN.Function>
    {
        public readonly string name;
        public StackFunction(string name, RPN.Function function) : base(Type.Function, function) => this.name = name;

        public override string ToString() => name;
    }

    [DebuggerDisplay("Name({value})")]
    public class StackName : AStackItem<string>
    {
        public StackName(string referenceName) : base(Type.Name, referenceName) { }

        public override string ToString() => value;
    }
}
