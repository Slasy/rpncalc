using System.Diagnostics;
using System.Linq;
using System.Text;
using RPNCalc.Extensions;

namespace RPNCalc.Items
{
    [DebuggerDisplay("Program({ToString()})")]
    public sealed class ProgramItem : AItem<AItem[]>
    {
        public ProgramItem(AItem[] array) : base(Type.Program, array) { }
        public ProgramItem(Stack<AItem> stack) : base(Type.Program, stack.ToReverseArray()) { }

        public static ProgramItem From(params AItem[] array) => new(array);

        public override bool Equals(AItem? other) => other is ProgramItem program && value.SequenceEqual(program.value);

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
}
