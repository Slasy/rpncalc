using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPNCalc.Extensions;

namespace RPNCalc.Tests
{
    public class StackExtensionsTest
    {
        private Stack<double> stack;

        [SetUp]
        public void Setup()
        {
            stack = new Stack<double>();
        }

        [Test]
        public void Pop1Value()
        {
            stack.Push(10);
            Assert.AreEqual(10, stack.Pop1());
        }

        [Test]
        public void FailPop1Value()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Pop1());
        }

        [Test]
        public void Pop2Values()
        {
            stack.Push(10);
            stack.Push(20);
            Assert.AreEqual((20, 10), stack.Pop2());
        }

        [Test]
        public void FailPop2Values()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Pop2());
            stack.Push(123);
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Pop2());
        }

        [Test]
        public void Pop3Values()
        {
            stack.Push(10);
            stack.Push(20);
            stack.Push(55);
            Assert.AreEqual((55, 20, 10), stack.Pop3());
        }

        [Test]
        public void FailPop3Values()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Pop3());
            stack.Push(123);
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Pop3());
            stack.Push(321);
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Pop3());
        }

        [Test]
        public void SwapValues()
        {
            stack.Push(10);
            stack.Push(20);
            stack.Push(55);
            stack.Swap();
            CollectionAssert.AreEqual(new[] { 20, 55, 10 }, stack);
        }

        [Test]
        public void FailSwapValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Swap());
            stack.Push(123);
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.Swap());
        }
    }
}
