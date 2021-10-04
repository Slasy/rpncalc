using System.Diagnostics;
using System.Linq;
using System.Text;
using RPNCalc.Extensions;

namespace RPNCalc.StackItems
{
    [DebuggerDisplay("Program({ToString()})")]
    public class StackProgram : AStackItem<AStackItem[]>
    {
        public StackProgram(AStackItem[] array) : base(Type.Program, array) { }
        public StackProgram(Stack<AStackItem> stack) : base(Type.Program, stack.ToReverseArray()) { }

        public static StackProgram From(params AStackItem[] array) => new(array);

        public override bool Equals(AStackItem other)=>other is StackProgram program && value.SequenceEqual(program.value);

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
