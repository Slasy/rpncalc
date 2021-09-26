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
            Assert.AreEqual(10, stack.Pop());
        }

        [Test]
        public void FailPop1Value()
        {
            Assert.Throws<RPNEmptyStackException>(() => stack.Pop());
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
            Assert.Throws<RPNEmptyStackException>(() => stack.Pop2());
            stack.Push(123);
            Assert.Throws<RPNEmptyStackException>(() => stack.Pop2());
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
            Assert.Throws<RPNEmptyStackException>(() => stack.Pop3());
            stack.Push(123);
            Assert.Throws<RPNEmptyStackException>(() => stack.Pop3());
            stack.Push(321);
            Assert.Throws<RPNEmptyStackException>(() => stack.Pop3());
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
            Assert.Throws<RPNEmptyStackException>(() => stack.Swap());
            stack.Push(123);
            Assert.Throws<RPNEmptyStackException>(() => stack.Swap());
        }

        [Test]
        public void RollStack()
        {
            stack.Push(10, 20, 30);
            CollectionAssert.AreEqual(new[] { 30, 20, 10 }, stack);
            stack.RollDown();
            CollectionAssert.AreEqual(new[] { 20, 10, 30 }, stack);
            stack.RollDown();
            CollectionAssert.AreEqual(new[] { 10, 30, 20 }, stack);
            stack.Roll(-2); // back to top
            CollectionAssert.AreEqual(new[] { 30, 20, 10 }, stack);
        }

        [Test]
        public void RotateTop3()
        {
            stack.Push(30, 20, 10);
            stack.Push(999);
            CollectionAssert.AreEqual(new[] { 999, 10, 20, 30 }, stack);
            stack.Rotate();
            CollectionAssert.AreEqual(new[] { 10, 20, 999, 30 }, stack);
            stack.Rotate();
            CollectionAssert.AreEqual(new[] { 20, 999, 10, 30 }, stack);
        }

        [Test]
        public void RotateTop3OtherWay()
        {
            stack.Push(30, 20, 10);
            stack.Push(999);
            CollectionAssert.AreEqual(new[] { 999, 10, 20, 30 }, stack);
            stack.Rotate(-3);
            CollectionAssert.AreEqual(new[] { 20, 999, 10, 30 }, stack);
            stack.Rotate(-3);
            CollectionAssert.AreEqual(new[] { 10, 20, 999, 30 }, stack);
        }

        [Test]
        public void DumpStack()
        {
            stack.Push(10, 20, 30);
            string dump = stack.DumpStack();
            Assert.AreEqual("003: 10\n002: 20\n001: 30\n", dump);
        }
    }
}
