using System;

namespace RPNCalc.Extensions
{
    public static class StackExtensions
    {
        /// <summary>
        /// Push two values to stack in order Y, X
        /// </summary>
        public static void Push<T>(this Stack<T> stack, T y, T x)
        {
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>
        /// Push three values to stack in order Z, Y, X
        /// </summary>
        public static void Push<T>(this Stack<T> stack, T z, T y, T x)
        {
            stack.Push(z);
            stack.Push(y);
            stack.Push(x);
        }

        /// <summary>Duplicate last value on stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public static void Dup<T>(this Stack<T> stack)
        {
            stack.ThrowIfTooSmall(1);
            stack.Push(stack.Peek());
        }

        public static void Over<T>(this Stack<T> stack)
        {
            stack.ThrowIfTooSmall(2);
            var (_, y) = stack.Peek2();
            stack.Push(y);
        }

        /// <summary>Drop last value on stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public static void Drop<T>(this Stack<T> stack)
        {
            stack.ThrowIfTooSmall(1);
            stack.Pop();
        }

        /// <summary>no pops, 1x push</summary>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        public static void Func<T>(this Stack<T> stack, Func<T> func)
        {
            T result;
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
        public static void Func<T>(this Stack<T> stack, Func<T, T> func)
        {
            T value = stack.Pop();
            T result;
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
        public static void Func<T>(this Stack<T> stack, Func<T, T, T> func)
        {
            var (x, y) = stack.Pop2();
            T result;
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
        public static void Func<T>(this Stack<T> stack, Func<T, T, T, T> func)
        {
            var (x, y, z) = stack.Pop3();
            T result;
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
        public static void Func<T>(this Stack<T> stack, Func<(T, T)> func)
        {
            T x, y;
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
        public static void Func<T>(this Stack<T> stack, Func<T, (T, T)> func)
        {
            T value = stack.Pop();
            T x, y;
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
        public static void Func<T>(this Stack<T> stack, Func<T, T, (T, T)> func)
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
        public static void Func<T>(this Stack<T> stack, Func<T, T, T, (T, T)> func)
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

        /// <summary>
        /// Moves top of the stack to bottom.
        /// </summary>
        public static void RollDown<T>(this Stack<T> stack) => stack.Roll(1);

        /// <summary>
        /// Moves bottom of the stack to top.
        /// </summary>
        public static void RotateUp<T>(this Stack<T> stack) => stack.Roll(-1);
    }
}
