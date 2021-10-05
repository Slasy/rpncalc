using System;
using System.Numerics;
using RPNCalc.StackItems;

namespace RPNCalc.Extensions
{
    public static class StackItemExtensions
    {
        public static double GetRealNumber(this AStackItem item)
        {
            EnsureType<StackReal>(item, out var number);
            return number.value;
        }

        public static string GetString(this AStackItem item)
        {
            EnsureType<StackString>(item, out var str);
            return str.value;
        }

        /// <summary>
        /// Returns item as a string (if compatible).
        /// </summary>
        public static string AsString(this AStackItem item)
        {
            if (item is StackString str) return str.value;
            return item.ToString();
        }

        public static AStackItem[] GetProgramInstructions(this AStackItem item)
        {
            EnsureType<StackProgram>(item, out var program);
            return program.value;
        }

        public static bool GetBool(this AStackItem item)
        {
            EnsureType<StackReal>(item, out var number);
            return number > double.Epsilon || number < -double.Epsilon;
        }

        public static AStackItem[] GetArray(this AStackItem item)
        {
            EnsureType<StackList>(item, out var list);
            return list.value;
        }

        public static Complex GetComplex(this AStackItem item)
        {
            EnsureType<StackComplex>(item, out var complex);
            return complex.value;
        }

        /// <summary>
        /// Returns complex number or converts real to complex number.
        /// </summary>
        public static Complex AsComplex(this AStackItem item)
        {
            EnsureNotNull(item);
            if (item is StackComplex complex) return complex.value;
            if (item is StackReal real) return real.value;
            throw BadArgumentException;
        }

        private static void EnsureNotNull(AStackItem item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item), "Missing stack item");
        }

        private static void EnsureType<T>(AStackItem item, out T typedItem) where T : AStackItem
        {
            EnsureNotNull(item);
            if (item is T realType) typedItem = realType;
            else throw BadArgumentException;
        }

        private static RPNArgumentException BadArgumentException => new RPNArgumentException("Bad argument type");
    }
}
