using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using RPNCalc.Extensions;

namespace RPNCalc.StackItems
{
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

        public bool Equals(AStackItem[] other) => value.SequenceEqual(other);

        public override bool Equals(AStackItem other) => other is StackList list && value.SequenceEqual(list.value);

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
    }
}
