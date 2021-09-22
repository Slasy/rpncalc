using System;
using System.Collections.Generic;

namespace RPNCalc.Extensions
{
    public static class StackExtensions
    {
        /// <summary>Pops only one value from stack</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static double Pop1(this Stack<double> stack)
        {
            CheckStackSize(stack, 1);
            return stack.Pop();
        }

        /// <summary>Pops last two values from stack</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static (double x, double y) Pop2(this Stack<double> stack)
        {
            CheckStackSize(stack, 2);
            return (stack.Pop(), stack.Pop());
        }

        /// <summary>Pops last two values from stack</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static (double x, double y, double z) Pop3(this Stack<double> stack)
        {
            CheckStackSize(stack, 3);
            return (stack.Pop(), stack.Pop(), stack.Pop());
        }

        /// <summary>
        /// Push two values to stack in order Y, X
        /// </summary>
        public static void Push(this Stack<double> stack, double y, double x)
        {
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>
        /// Push three values to stack in order Z, Y, X
        /// </summary>
        public static void Push(this Stack<double> stack, double z, double y, double x)
        {
            stack.Push(z);
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>Swap position of top two values on stack</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static void Swap(this Stack<double> stack)
        {
            var (x, y) = stack.Pop2();
            stack.Push(x);
            stack.Push(y);
        }

        /// <summary>Duplicate last value on stack</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static void Dup(this Stack<double> stack)
        {
            CheckStackSize(stack, 1);
            stack.Push(stack.Peek());
        }

        /// <summary>Drop last value on stack</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static void Drop(this Stack<double> stack)
        {
            CheckStackSize(stack, 1);
            stack.Pop();
        }

        /// <summary>no pops, 1x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<double> func)
        {
            double result = func();
            stack.Push(result);
        }

        /// <summary>1x pop, 1x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<double, double> func)
        {
            double value = stack.Pop1();
            double result = func(value);
            stack.Push(result);
        }

        /// <summary>2x pop, 1x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<double, double, double> func)
        {
            var (x, y) = stack.Pop2();
            double result = func(x, y);
            stack.Push(result);
        }

        /// <summary>3x pop, 1x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<double, double, double, double> func)
        {
            var (x, y, z) = stack.Pop3();
            double result = func(x, y, z);
            stack.Push(result);
        }

        /// <summary>no pops, 2x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<(double, double)> func)
        {
            var (y, x) = func();
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>1x pop, 2x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<double, (double, double)> func)
        {
            double value = stack.Pop1();
            var (y, x) = func(value);
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>2x pop, 2x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<double, double, (double, double)> func)
        {
            var (x, y) = stack.Pop2();
            (y, x) = func(x, y);
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>3x pop, 2x push</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="Exception"/>
        public static void Func(this Stack<double> stack, Func<double, double, double, (double, double)> func)
        {
            var (x, y, z) = stack.Pop3();
            (y, x) = func(x, y, z);
            stack.Push(y);
            stack.Push(x);
        }

        private static void CheckStackSize(Stack<double> stack, int minSize)
        {
            if (stack.Count < minSize) throw new ArgumentOutOfRangeException(nameof(stack), "Too few arguments");
        }
    }
}
