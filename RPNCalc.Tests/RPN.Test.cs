using System;
using System.Linq;
using NUnit.Framework;
using RPNCalc.Extensions;

namespace RPNCalc.Tests
{
    public class RPNTest
    {
        private RPN calc;

        [SetUp]
        public void Setup()
        {
            calc = new RPN(alwaysClearStack: false);
        }

        [Test]
        public void PushToStack()
        {
            Assert.AreEqual(3, calc.Eval("10 20 30 40 50 1 2 3"));
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 50, 40, 30, 20, 10 }, calc.StackView);
        }

        [Test]
        public void ReturnTopValue()
        {
            Assert.AreEqual(10, calc.Eval("10"));
            Assert.AreEqual(20, calc.Eval("20"));
            Assert.AreEqual(30, calc.Eval("30"));
            Assert.AreEqual(50, calc.Eval("40 50"));
        }

        [Test]
        public void BasicOperations()
        {
            Assert.AreEqual(30, calc.Eval("10 20 +"));
            Assert.AreEqual(200, calc.Eval("10 20 *"));
            Assert.AreEqual(-10, calc.Eval("10 20 -"));
            Assert.AreEqual(0.5, calc.Eval("10 20 /"));
            Assert.AreEqual(1000, calc.Eval("10 3 ^"));
            Assert.AreEqual(100, calc.Eval("10 sq"));
            Assert.AreEqual(3, calc.Eval("9 sqrt"));
        }

        [Test]
        public void BasicOperationsIterative()
        {
            calc.Eval("10");
            calc.Eval("20");
            Assert.AreEqual(30, calc.Eval("+"));
        }

        [Ignore("There is no support for this (yet?)")]
        [Test]
        public void EvaluateWithImperfectSpaces()
        {
            Assert.AreEqual(30, calc.Eval("10 20+"));
            calc.SetVariable("x", 20);
            Assert.AreEqual(30, calc.Eval("10 x+ "));
            Assert.AreEqual(30, calc.Eval("  10  20+ "));
        }

        [Test]
        public void SettingNotCaseSensitiveVariablesAndFunctions()
        {
            calc = new RPN(false);

            calc.SetVariable("Foo", 123);
            Assert.IsTrue(calc.VariablesView.ContainsKey("foo"));
            calc.SetVariable("bAr", 456);
            Assert.IsTrue(calc.VariablesView.ContainsKey("bar"));
            calc.SetFunction("PlUs", st => st.Push((double)st.Pop() + st.Pop()));
            Assert.IsTrue(calc.FunctionsView.Contains("plus"));
            Assert.AreEqual(123 + 456, calc.Eval("foO BAR plus"));
        }

        [Test]
        public void SettingCaseSensitiveVariablesAndFunctions()
        {
            calc = new RPN(true);

            calc.SetVariable("Foo", 123);
            Assert.IsTrue(calc.VariablesView.ContainsKey("Foo"));
            calc.SetVariable("bAr", 456);
            Assert.IsTrue(calc.VariablesView.ContainsKey("bAr"));
            calc.SetFunction("PlUs", st => st.Push((double)st.Pop() + st.Pop()));
            Assert.IsTrue(calc.FunctionsView.Contains("PlUs"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("foO BAR plus"));
            Assert.AreEqual(123 + 456, calc.Eval("Foo bAr PlUs"));
        }

        [Test]
        public void ExceptionOnSameFunctionVariableName()
        {
            calc.SetVariable("foo", 123);
            Assert.Throws<ArgumentException>(() => calc.SetFunction("foo", _ => { }));
            calc.SetFunction("bar", _ => { });
            Assert.Throws<ArgumentException>(() => calc.SetVariable("bar", 123));
        }

        [Test]
        public void UnsetFunctionAndVariable()
        {
            calc.SetVariable("foo", 123);
            calc.SetFunction("bar", _ => { });
            CollectionAssert.AreEqual(new[] { "foo" }, calc.VariablesView.Keys);
            CollectionAssert.Contains(calc.FunctionsView, "bar");
            calc.RemoveVariable("foo");
            CollectionAssert.IsEmpty(calc.VariablesView.Keys);
            CollectionAssert.Contains(calc.FunctionsView, "bar");
            calc.RemoveFunction("bar");
            CollectionAssert.IsEmpty(calc.VariablesView.Keys);
            CollectionAssert.DoesNotContain(calc.FunctionsView, "bar");
        }

        [Test]
        public void Pythagoras()
        {
            // a^2 + b^2 = c^2
            calc.SetVariable("a", 3);
            calc.SetVariable("b", 4);
            calc.SetVariable("c", 5);
            calc.SetFunction("=", st =>
            {
                var (x, y) = st.Pop2();
                st.Push(y);
                st.Push(x);
                st.Push(x == y ? 1 : 0);
            });
            Assert.AreEqual(1, calc.Eval("a sq b sq + sqrt c sq sqrt ="));
            CollectionAssert.AreEqual(new[] { 1, 5, 5 }, calc.StackView);
        }

        [Test]
        public void Quadratic()
        {
            // 2x^2 - 5x - 3 = 0
            calc.SetVariable("a", 2);
            calc.SetVariable("b", -5);
            calc.SetVariable("c", -3);
            double root1 = calc.Eval("b +- b sq 4 a c * * - sqrt + 2 a * /");
            double root2 = calc.Eval("b +- b sq 4 a c * * - sqrt - 2 a * /");
            Assert.AreEqual(3, root1);
            Assert.AreEqual(-0.5, root2);
        }

        [Test]
        public void LoadFloatingPointNumbers()
        {
            calc.Eval("1.1 100.001 3.1415 -0.69 3.33 +-");
            CollectionAssert.AreEqual(new[] { -3.33, -0.69, 3.1415, 100.001, 1.1 }, calc.StackView);
        }

        [Test]
        public void LoadExponentNumbers()
        {
            calc.Eval("1.1e3 -1.1e3 -1.1e-3 1.1e-3");
            CollectionAssert.AreEqual(new[] { 0.0011, -0.0011, -1100, 1100 }, calc.StackView);
        }

        [Test]
        public void SetMacro()
        {
            calc.SetFunction("macro", "1 2 + dup 2 / swap");
            CollectionAssert.Contains(calc.FunctionsView, "macro");
            calc.Eval("10 20 macro");
            CollectionAssert.AreEqual(new[] { 3, 1.5, 20, 10 }, calc.StackView);
        }

        [Test]
        public void KeepStackInMacro()
        {
            calc = new RPN(true, true);
            calc.Eval("1 2");
            calc.SetFunction("foo", "dup * +");
            calc.Eval("10 20 foo");
            CollectionAssert.AreEqual(new[] { 410 }, calc.StackView);
        }

        [Test]
        public void RotRollOver()
        {
            calc.SetFunction("eq", st => { var (x, y) = st.Peek2(); st.Push(x == y ? 1 : 0); });
            calc.Eval("5 20 over / 4 eq roll drop drop +");
            CollectionAssert.AreEqual(new[] { 6 }, calc.StackView);
            calc.Eval("1 2 3 clear 10 20 30 rot + eq");
            CollectionAssert.AreEqual(new[] { 1, 30, 30 }, calc.StackView);
            calc.Eval("clear 1 2 3 10 20 30 rot over 2 * eq swap drop");
            CollectionAssert.AreEqual(new[] { 1, 20, 10, 30, 3, 2, 1 }, calc.StackView);
        }
    }
}
