using System;
using System.Collections.Generic;

namespace RPNCalc.Extensions
{
    public static class StackExtensions
    {
        /// <summary>Pops only one value from stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public static double Pop1(this Stack<double> stack)
        {
            CheckStackSize(stack, 1);
            return stack.Pop();
        }

        /// <summary>Pops last two values from stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public static (double x, double y) Pop2(this Stack<double> stack)
        {
            CheckStackSize(stack, 2);
            return (stack.Pop(), stack.Pop());
        }

        /// <summary>Pops last two values from stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
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
        /// <exception cref="RPNEmptyStackException"/>
        public static void Swap(this Stack<double> stack)
        {
            var (x, y) = stack.Pop2();
            stack.Push(x);
            stack.Push(y);
        }

        /// <summary>Duplicate last value on stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public static void Dup(this Stack<double> stack)
        {
            CheckStackSize(stack, 1);
            stack.Push(stack.Peek());
        }

        /// <summary>Drop last value on stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public static void Drop(this Stack<double> stack)
        {
            CheckStackSize(stack, 1);
            stack.Pop();
        }

        /// <summary>no pops, 1x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<double> func)
        {
            double result;
            try
            {
                result = func();
            }
            catch (Exception e)
            {
                throw new RPNFunctionException("No input arguments", e);
            }
            stack.Push(result);
        }

        /// <summary>1x pop, 1x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<double, double> func)
        {
            double value = stack.Pop1();
            double result;
            try
            {
                result = func(value);
            }
            catch (Exception e)
            {
                throw new RPNFunctionException($"Input arguments: {value}", e);
            }
            stack.Push(result);
        }

        /// <summary>2x pop, 1x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<double, double, double> func)
        {
            var (x, y) = stack.Pop2();
            double result;
            try
            {
                result = func(x, y);
            }
            catch (Exception e)
            {
                throw new RPNFunctionException($"Input arguments: {x}, {y}", e);
            }
            stack.Push(result);
        }

        /// <summary>3x pop, 1x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<double, double, double, double> func)
        {
            var (x, y, z) = stack.Pop3();
            double result;
            try
            {
                result = func(x, y, z);
            }
            catch (Exception e)
            {
                throw new RPNFunctionException($"Input arguments: {x} {y} {z}", e);
            }
            stack.Push(result);
        }

        /// <summary>no pops, 2x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<(double, double)> func)
        {
            double x, y;
            try
            {
                (y, x) = func();
            }
            catch (Exception e)
            {
                throw new RPNFunctionException("No input arguments", e);
            }
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>1x pop, 2x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<double, (double, double)> func)
        {
            double value = stack.Pop1();
            double x, y;
            try
            {
                (y, x) = func(value);
            }
            catch (Exception e)
            {
                throw new RPNFunctionException($"Input arguments: {value}", e);
            }
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>2x pop, 2x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<double, double, (double, double)> func)
        {
            var (x, y) = stack.Pop2();
            try
            {
                (y, x) = func(x, y);
            }
            catch (Exception e)
            {
                throw new RPNFunctionException($"Input arguments: {x}, {y}", e);
            }
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>3x pop, 2x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func(this Stack<double> stack, Func<double, double, double, (double, double)> func)
        {
            var (x, y, z) = stack.Pop3();
            try
            {
                (y, x) = func(x, y, z);
            }
            catch (Exception e)
            {
                throw new RPNFunctionException($"Input arguments: {x} {y} {z}", e);
            }
            stack.Push(y);
            stack.Push(x);
        }

        private static void CheckStackSize(Stack<double> stack, int minSize)
        {
            if (stack.Count < minSize) throw new RPNEmptyStackException("Too few arguments");
        }
    }
}
