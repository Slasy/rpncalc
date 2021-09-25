namespace RPNCalc.Extensions
{
    public static class StackElementExtensions
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

        public static string AsProgram(this AStackItem item)
        {
            EnsureType<StackProgram>(item, out var program);
            return program.value;
        }

        private static void EnsureType(AStackItem item, AStackItem.Type type)
        {
            if (item.type != type) throw new RPNArgumentException("Bad argument type");
        }

        private static void EnsureType<T>(AStackItem item, out T typedItem) where T : AStackItem
        {
            if (item is T realType) typedItem = realType;
            else throw new RPNArgumentException("Bad argument type");
        }
    }
}
