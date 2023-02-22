using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using RPNCalc.Extensions;

namespace RPNCalc.Items
{
    [DebuggerDisplay("List({ToString()})")]
    public sealed class ListItem : AItem<AItem[]>
    {
        /// <summary>Empty list.</summary>
        public ListItem() : base(Type.List, Array.Empty<AItem>()) { }

        /// <summary>Items in list will be in revers order - bottom/last item in stack will be first.</summary>
        public ListItem(Stack<AItem> stack) : this(stack.ToReverseArray()) { }

        public ListItem(AItem[] array) : base(Type.List, array) { }

        public static implicit operator ListItem(AItem[] array) => new(array);

        public static ListItem From(params AItem[] array) => new(array);

        public static ListItem Combine(ListItem listA, ListItem listB)
        {
            AItem[] newArray = new AItem[listA.value.Length + listB.value.Length];
            Array.Copy(listA.value, newArray, listA.value.Length);
            Array.Copy(listB.value, 0, newArray, listA.value.Length, listB.value.Length);
            return new ListItem(newArray);
        }

        public static ListItem Combine(ListItem list, AItem other)
        {
            AItem[] newArray = new AItem[list.value.Length + 1];
            Array.Copy(list.value, newArray, list.value.Length);
            newArray[list.value.Length] = other;
            return new ListItem(newArray);
        }

        public static ListItem Combine(AItem other, ListItem list)
        {
            AItem[] newArray = new AItem[list.value.Length + 1];
            Array.Copy(list.value, 0, newArray, 1, list.value.Length);
            newArray[0] = other;
            return new ListItem(newArray);
        }

        public bool Equals(AItem[] other) => value.SequenceEqual(other);

        public override bool Equals(AItem? other) => other is ListItem list && value.SequenceEqual(list.value);

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
