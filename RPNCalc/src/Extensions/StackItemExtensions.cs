using System.Numerics;
using RPNCalc.Items;

namespace RPNCalc.Extensions
{
    public static class StackItemExtensions
    {
        public static double GetRealNumber(this AItem item)
        {
            EnsureType<RealNumberItem>(item, out var number);
            return number.value;
        }

        public static string GetString(this AItem item)
        {
            EnsureType<StackStringItem>(item, out var str);
            return str.value;
        }

        /// <summary>
        /// Returns item as a string (if compatible).
        /// </summary>
        public static string AsString(this AItem item)
        {
            if (item is StackStringItem str) return str.value;
            return item.ToString();
        }

        public static ProgramItem GetProgram(this AItem item)
        {
            EnsureType<ProgramItem>(item, out var program);
            return program;
        }

        public static AItem[] GetProgramInstructions(this AItem item)
        {
            EnsureType<ProgramItem>(item, out var program);
            return program.value;
        }

        public static bool GetBool(this AItem item)
        {
            EnsureType<RealNumberItem>(item, out var number);
            return number > double.Epsilon || number < -double.Epsilon;
        }

        public static AItem[] GetArray(this AItem item)
        {
            EnsureType<ListItem>(item, out var list);
            return list.value;
        }

        public static Complex GetComplex(this AItem item)
        {
            EnsureType<ComplexNumberItem>(item, out var complex);
            return complex.value;
        }

        /// <summary>
        /// Returns complex number or converts real to complex number.
        /// </summary>
        public static Complex AsComplex(this AItem item)
        {
            EnsureNotNull(item);
            if (item is ComplexNumberItem complex) return complex.value;
            if (item is RealNumberItem real) return real.value;
            throw BadArgumentException;
        }

        private static void EnsureNotNull(AItem item)
        {
            if (item is null) throw new RPNArgumentException("Missing stack item");
        }

        private static void EnsureType<T>(AItem item, out T typedItem) where T : AItem
        {
            EnsureNotNull(item);
            item.EnsureType(out typedItem);
        }

        private static RPNArgumentException BadArgumentException => new("Bad argument type");
    }
}
