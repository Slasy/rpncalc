using System;
using System.Numerics;

namespace RPNCalc.Extensions
{
    public static class StackItemExtensions
    {
        public static double AsNumber(this AStackItem item)
        {
            EnsureType<StackNumber>(item, out var number);
            return number.value;
        }

        public static string AsString(this AStackItem item)
        {
            EnsureType<StackString>(item, out var str);
            return str.value;
        }

        public static AStackItem[] AsProgramInstructions(this AStackItem item)
        {
            EnsureType<StackProgram>(item, out var program);
            return program.value;
        }

        public static bool AsBool(this AStackItem item)
        {
            EnsureType<StackNumber>(item, out var number);
            return number > double.Epsilon || number < -double.Epsilon;
        }

        public static AStackItem[] AsArray(this AStackItem item)
        {
            EnsureType<StackList>(item, out var list);
            return list.value;
        }

        public static Complex AsComplex(this AStackItem item)
        {
            EnsureType<StackComplex>(item, out var complex);
            return complex.value;
        }

        private static void EnsureType(AStackItem item, AStackItem.Type type)
        {
            if (item is null) throw new ArgumentNullException(nameof(item), "Missing stack item");
            if (item.type != type) throw new RPNArgumentException("Bad argument type");
        }

        private static void EnsureType<T>(AStackItem item, out T typedItem) where T : AStackItem
        {
            if (item is null) throw new ArgumentNullException(nameof(item), "Missing stack item");
            if (item is T realType) typedItem = realType;
            else throw new RPNArgumentException("Bad argument type");
        }
    }
}
