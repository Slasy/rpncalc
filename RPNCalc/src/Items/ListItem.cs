using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using RPNCalc.Extensions;

namespace RPNCalc.Items
{
    [DebuggerDisplay("List({ToString()})")]
    public class ListItem : AItem<AItem[]>
    {
        /// <summary>Empty list.</summary>
        public ListItem() : base(Type.List, Array.Empty<AItem>()) { }
        public ListItem(AItem[] array) : base(Type.List, array) { }
        /// <summary>Items in list will be in revers order - bottom/last item in stack will be first.</summary>
        public ListItem(Stack<AItem> stack) : base(Type.List, stack.ToReverseArray()) { }

        public static ListItem operator +(ListItem list, AItem item)
        {
            AItem[] newArray = new AItem[list.value.Length + 1];
            Array.Copy(list.value, newArray, list.value.Length);
            newArray[list.value.Length] = item;
            return new ListItem(newArray);
        }

        public static implicit operator ListItem(AItem[] array) => new(array);
        //public static implicit operator AStackItem[](StackList item) => item.value;

        public static ListItem From(params AItem[] array) => new(array);

        public bool Equals(AItem[] other) => value.SequenceEqual(other);

        public override bool Equals(AItem other) => other is ListItem list && value.SequenceEqual(list.value);

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
