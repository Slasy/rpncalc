using System;
using System.Collections;
using System.Collections.Generic;
using Nito.Collections;

namespace RPNCalc
{
    public class Stack<T> : IReadOnlyList<T>
    {
        private readonly Deque<T> queue;

        public Stack() => queue = new Deque<T>();
        public Stack(int capacity) => queue = new Deque<T>(capacity);

        public T this[int index]
        {
            get => queue[index];
            set => queue[index] = value;
        }

        public int Count => queue.Count;

        public void Clear() => queue.Clear();

        /// <summary>Pops only one value from stack.</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public T Pop()
        {
            ThrowIfTooSmall(1);
            return queue.RemoveFromFront();
        }

        /// <summary>Pops last two values from stack.</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public (T x, T y) Pop2()
        {
            ThrowIfTooSmall(2);
            return (queue.RemoveFromFront(), queue.RemoveFromFront());
        }

        /// <summary>Pops last three values from stack.</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public (T x, T y, T z) Pop3()
        {
            ThrowIfTooSmall(3);
            return (queue.RemoveFromFront(), queue.RemoveFromFront(), queue.RemoveFromFront());
        }

        /// <summary>Pops last four values from stack.</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public (T x, T y, T z, T t) Pop4()
        {
            ThrowIfTooSmall(4);
            return (queue.RemoveFromFront(), queue.RemoveFromFront(), queue.RemoveFromFront(), queue.RemoveFromFront());
        }

        public void Push(T x)
        {
            if (x is null) throw new ArgumentNullException(nameof(x), "Pushing null to stack is not allowed");
            queue.AddToFront(x);
        }

        /// <summary>
        /// Return last value from stack without poping it.
        /// </summary>
        /// <exception cref="RPNEmptyStackException"/>
        public T Peek()
        {
            ThrowIfTooSmall(1);
            return queue[0];
        }

        /// <summary>
        /// Return last two values from stack without poping them.
        /// </summary>
        /// <exception cref="RPNEmptyStackException"/>
        public (T x, T y) Peek2()
        {
            ThrowIfTooSmall(2);
            return (queue[0], queue[1]);
        }

        /// <summary>
        /// Return last three values from stack without poping them.
        /// </summary>
        /// <exception cref="RPNEmptyStackException"/>
        public (T x, T y, T z) Peek3()
        {
            ThrowIfTooSmall(3);
            return (queue[0], queue[1], queue[2]);
        }

        /// <summary>Swap position of top two values on stack</summary>
        /// <exception cref="RPNEmptyStackException"/>
        public void Swap()
        {
            ThrowIfTooSmall(2);
            T x = queue[0];
            queue[0] = queue[1];
            queue[1] = x;
        }

        /// <summary>
        /// Rotate top N values.
        /// </summary>
        /// <param name="n">positive number moves top value to Nth position,
        /// negative moves Nth value to top</param>
        public void Rotate(int n = 3)
        {
            if (n == 0) return;
            ThrowIfTooSmall(Math.Abs(n));
            T[] topStack = new T[Math.Abs(n)];
            if (n > 0)
            {
                topStack[n - 1] = queue.RemoveFromFront();
                for (int i = 1; i < n; i++)
                {
                    topStack[i - 1] = queue.RemoveFromFront();
                }
            }
            else
            {
                n = -n;
                for (int i = 1; i < n; i++)
                {
                    topStack[i] = queue.RemoveFromFront();
                }
                topStack[0] = queue.RemoveFromFront();
            }
            for (int i = n - 1; i >= 0; i--)
            {
                queue.AddToFront(topStack[i]);
            }
        }

        /// <summary>
        /// Roll over whole stack.
        /// </summary>
        /// <param name="n">positive number moves top values to bottom,
        /// negative moves back values from bottom to top</param>
        public void Roll(int n = 1)
        {
            if (n == 0) return;
            if (Count == Math.Abs(n)) return;
            if (n > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    T value = queue.RemoveFromFront();
                    queue.AddToBack(value);
                }
            }
            else
            {
                n = -n;
                for (int i = 0; i < n; i++)
                {
                    T value = queue.RemoveFromBack();
                    queue.AddToFront(value);
                }
            }
        }

        public IEnumerator<T> GetEnumerator() => queue.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => queue.GetEnumerator();

        /// <summary>
        /// Check stack size and throw exception if doesn't contain enough items.
        /// </summary>
        /// <exception cref="RPNEmptyStackException"/>
        public void ThrowIfTooSmall(int minSize)
        {
            if (Count < minSize) throw new RPNEmptyStackException("Too few arguments");
        }
    }
}
