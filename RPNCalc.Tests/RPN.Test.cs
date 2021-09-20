using System;
using System.Linq;
using NUnit.Framework;

namespace RPNCalc.Tests
{
    public class RPNTest
    {
        private RPN calc;

        [SetUp]
        public void Setup()
        {
            calc = new RPN { ClearStack = false };
        }

        [Test]
        public void PushToStack()
        {
            calc.Eval("10 20 30 40 50 1 2 3");
            CollectionAssert.AreEquivalent(new[] { 10, 20, 30, 40, 50, 1, 2, 3 }, calc.StackView);
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
        public void SettingVariablesAndFunctions()
        {
            calc.SetVariable("Foo", 123);
            Assert.IsTrue(calc.VariablesView.ContainsKey("foo"));
            calc.SetVariable("bAr", 456);
            Assert.IsTrue(calc.VariablesView.ContainsKey("bar"));
            calc.SetFunction("PlUs", st => st.Push(st.Pop() + st.Pop()));
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
            calc.SetFunction("PlUs", st => st.Push(st.Pop() + st.Pop()));
            Assert.IsTrue(calc.FunctionsView.Contains("PlUs"));
            Assert.Throws<ArgumentException>(() => calc.Eval("foO BAR plus"));
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
            CollectionAssert.AreEquivalent(new[] { "foo" }, calc.VariablesView.Keys);
            CollectionAssert.Contains(calc.FunctionsView, "bar");
            calc.SetVariable("foo", null);
            CollectionAssert.AreEquivalent(Array.Empty<string>(), calc.VariablesView.Keys);
            CollectionAssert.Contains(calc.FunctionsView, "bar");
            calc.SetFunction("bar", null);
            CollectionAssert.AreEquivalent(Array.Empty<string>(), calc.VariablesView.Keys);
            CollectionAssert.DoesNotContain(calc.FunctionsView, "bar");
        }
    }
}
