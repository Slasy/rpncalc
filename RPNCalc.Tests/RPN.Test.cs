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
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());
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

        [Test]
        public void SettingNotCaseSensitiveVariablesAndFunctions()
        {
            calc = new RPN(false);
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());

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
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());
            calc.Eval("1 2");
            calc.SetFunction("foo", "DUP * +");
            calc.Eval("10 20 foo");
            CollectionAssert.AreEqual(new[] { 410 }, calc.StackView);
        }

        [Test]
        public void RotRollOver()
        {
            calc.SetFunction("eq", st => { var (x, y) = st.Peek2(); st.Push(x == y ? 1 : 0); });
            calc.Eval("5 20 over / 4 eq roll drop drop +");
            CollectionAssert.AreEqual(new[] { 6 }, calc.StackView);
            calc.Eval("1 2 3 clst 10 20 30 rot + eq");
            CollectionAssert.AreEqual(new[] { 1, 30, 30 }, calc.StackView);
            calc.Eval("clst 1 2 3 10 20 30 rot over 2 * eq swap drop");
            CollectionAssert.AreEqual(new[] { 1, 20, 10, 30, 3, 2, 1 }, calc.StackView);
        }

        [Test]
        public void PushAStringsToStack()
        {
            calc.SetVariable("foo", 1234);
            string top = null;
            Assert.DoesNotThrow(() => top = calc.Eval("foo 'foo'"));
            Assert.AreEqual("foo", top);
            CollectionAssert.AreEqual(new AStackItem[] { "foo", 1234 }, calc.StackView);
        }

        [Test]
        public void StringWithSpace()
        {
            Assert.DoesNotThrow(() => calc.Eval("'foo bar'"));
            CollectionAssert.AreEqual(new[] { "foo bar" }, calc.StackView);
        }

        [Test]
        public void FailOnIncompleteString()
        {
            Assert.Throws<RPNArgumentException>(() => calc.Eval("12 'foo bar 10"));
        }

        [Test]
        public void PushProgramToStack()
        {
            AStackItem top = null;
            Assert.DoesNotThrow(() => top = calc.Eval("1 2 3 {10 20 dup + *}"));
            Assert.AreEqual(AStackItem.Type.Program, top.type);
            Assert.AreEqual("10 20 dup + *", top.value);
        }

        [Test]
        public void ProgramInsideProgram()
        {
            AStackItem p = calc.Eval("{{1 2 +} 3 *}");
            Assert.AreEqual("{1 2 +} 3 *", p.value);
        }

        [Test]
        public void EvalSimpleProgram()
        {
            Assert.DoesNotThrow(() => calc.Eval("{1 2 +} eval"));
            CollectionAssert.AreEqual(new[] { 3 }, calc.StackView);
        }

        [Test]
        public void EvalProgramInsideProgram()
        {
            Assert.DoesNotThrow(() => calc.Eval("{ { 1 2 + } eval 7 + } eval"));
            CollectionAssert.AreEqual(new[] { 10 }, calc.StackView);
        }

        [Test]
        public void ProgramInsideString()
        {
            calc.Eval("'{10 20 *} eval'");
            CollectionAssert.AreEqual(new[] { "{10 20 *} eval" }, calc.StackView);
        }

        [Test]
        public void StringsInsideProgram()
        {
            calc.Eval("{'foo' 'bar'}");
            CollectionAssert.AreEqual(new[] { new StackProgram("'foo' 'bar'") }, calc.StackView);
            calc.Eval("eval");
            CollectionAssert.AreEqual(new[] { "bar", "foo" }, calc.StackView);
        }

        [Test]
        public void FailEvalString()
        {
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'foo' eval"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'{foo}' eval"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'{10}' eval"));
        }

        [Test]
        public void FailEvalNumber()
        {
            Assert.Throws<RPNFunctionException>(() => calc.Eval("42 eval"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("-42.1337e2 eval"));
        }

        [Test]
        public void SetStringVariable()
        {
            calc.SetVariable("foo", "foobar");
            string s = calc.Eval("foo").AsString();
            Assert.AreEqual("foobar", s);
        }

        [Test]
        public void SetProgramVariable()
        {
            calc.SetVariable("foo", new StackProgram("dup * +"));
            double n = calc.Eval("10 20 foo eval").AsNumber();
            Assert.AreEqual(410, n);
        }

        [Test]
        public void StoNumber()
        {
            calc.Eval("999 123 'foo' sto");
            CollectionAssert.AreEqual(new[] { 999 }, calc.StackView);
            CollectionAssert.Contains(calc.VariablesView.Keys, "foo");
            Assert.AreEqual(123, calc.VariablesView["foo"]);
            Assert.AreEqual(123, calc.Eval("foo").AsNumber());
        }

        [Test]
        public void StoString()
        {
            calc.Eval("'foobar' dup 'foo' sto");
            CollectionAssert.AreEqual(new[] { "foobar" }, calc.StackView);
            CollectionAssert.Contains(calc.VariablesView.Keys, "foo");
            Assert.AreEqual("foobar", calc.VariablesView["foo"]);
        }

        [Test]
        public void StoProgram()
        {
            calc.Eval("{dummy program} 'foo' sto");
            CollectionAssert.IsEmpty(calc.StackView);
            CollectionAssert.Contains(calc.VariablesView.Keys, "foo");
            Assert.AreEqual("dummy program", calc.VariablesView["foo"].AsProgram());
        }

        [Test]
        public void RclNumber()
        {
            calc.SetVariable("foo", 1234);
            double n = calc.Eval("'foo' rcl").AsNumber();
            Assert.AreEqual(1234, n);
            CollectionAssert.AreEqual(new[] { 1234 }, calc.StackView);
        }

        [Test]
        public void RclString()
        {
            calc.SetVariable("foo", "foobar");
            string s = calc.Eval("'foo' rcl").AsString();
            Assert.AreEqual("foobar", s);
            CollectionAssert.AreEqual(new[] { "foobar" }, calc.StackView);
        }

        [Test]
        public void RclProgram()
        {
            calc.SetVariable("foo", new StackProgram("dup +"));
            string s = calc.Eval("'foo' rcl").AsProgram();
            Assert.AreEqual("dup +", s);
            CollectionAssert.AreEqual(new[] { new StackProgram("dup +") }, calc.StackView);
        }

        [Test]
        public void RclSto()
        {
            calc.SetVariable("foo", "foobar");
            var top = calc.Eval("'foo' rcl dup sto");
            Assert.IsNull(top);
            CollectionAssert.AreEquivalent(new[] { "foo", "foobar" }, calc.VariablesView.Keys);
            Assert.AreEqual("foobar", calc.VariablesView["foo"]);
            Assert.AreEqual("foobar", calc.VariablesView["foobar"]);
        }

        [Test]
        public void ClearVariable()
        {
            calc = new RPN(alwaysClearStack: false);
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());
            calc.SetVariable("foo", "foobar");
            CollectionAssert.IsNotEmpty(calc.VariablesView);
            Assert.AreEqual("foobar", calc.VariablesView["foo"]);
            Assert.DoesNotThrow(() => calc.Eval("foo 'foo' clv"));
            CollectionAssert.IsEmpty(calc.VariablesView);
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("foo"));
        }

        [Test]
        public void LazyEvaluation()
        {
            CollectionAssert.IsEmpty(calc.VariablesView);
            int value = calc.Eval("{a b +} 12 'a' sto 8 'b' sto eval");
            Assert.AreEqual(12 + 8, value);
        }

        [Test]
        public void ConnectStrings()
        {
            string value = calc.Eval("'foo' 'bar' +");
            Assert.AreEqual("foobar", value);
        }

        [Test]
        public void FailSummingIncompatibleTypes()
        {
            calc = new RPN(alwaysClearStack: true);
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());
            Assert.Throws<RPNFunctionException>(() => calc.Eval("{dup} {sto} +"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("{dup} 'var' +"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("12356 'var' +"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'var' 98765 +"));
        }

        [Test]
        public void IfThen()
        {
            calc = new RPN(alwaysClearStack: true);
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());
            string result = calc.Eval("10 10 == { 'yes 10 == 10' } ift");
            Assert.AreEqual("yes 10 == 10", result);
            var emptyResult = calc.Eval(" 10 10 != { 'yes 10 != 10' } ift");
            Assert.IsNull(emptyResult);
        }

        [Test]
        public void IfThenElse()
        {
            calc = new RPN(alwaysClearStack: true);
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());
            string result = calc.Eval("10 10 == { 'yes 10 == 10' } {'nope 10 != 10'} ifte");
            Assert.AreEqual("yes 10 == 10", result);
            var emptyResult = calc.Eval("10 10 != { 'yes 10 != 10' } {'nope 10 == 10'} ifte");
            Assert.AreEqual("nope 10 == 10", emptyResult);
        }

        [Test]
        public void While()
        {
            calc.Eval("1 0 'i' sto { 10 i != } { 2 * 'i' ++ } while");
            CollectionAssert.AreEqual(new[] { Math.Pow(2, 10) }, calc.StackView);
        }

        [Test]
        public void PlusPlus()
        {
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'unknown_var' ++"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'123' ++"));
            Assert.DoesNotThrow(() => calc.Eval("0 '123' sto '123' ++"));
            Assert.AreEqual(1, calc.VariablesView["123"]);
        }

        [Test]
        public void MinusMinus()
        {
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'unknown_var' --"));
            Assert.Throws<RPNFunctionException>(() => calc.Eval("'123' --"));
            Assert.DoesNotThrow(() => calc.Eval("0 '123' sto '123' --"));
            Assert.AreEqual(-1, calc.VariablesView["123"]);
        }

        [Test]
        public void ForLoop()
        {
            Assert.True(calc.Eval("0 0 'i' sto 'i' {i 10 <} {1} {1 +} for 10 =="));
            CollectionAssert.AreEqual(new[] { 1 }, calc.StackView);
        }

        [Test]
        public void ForLoopInvalidVariable()
        {
            ThrowsInnerMessage<RPNFunctionException, RPNUndefinedNameException>(
                "'i' {} {} {} for",
                "Unknown variable name i");
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "'foo' 'i' sto 'i' {} {} {} for",
                "Bad argument type");
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "{} 'i' sto 'i' {} {} {} for",
                "Bad argument type");
        }

        [Test]
        public void ForLoopInvalidCondition()
        {
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "0 'i' sto 'i' { 1 2 3 } {} {} for",
                "Unexpected behavior of condition program");
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "0 'i' sto 'i' { } {} {} for",
                "Unexpected behavior of condition program");
        }

        [Test]
        public void ForLoopInvalidStep()
        {
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { 1 2 3 } {} for",
                "Unexpected behavior of step program");
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { } {} for",
                "Unexpected behavior of step program");
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { 'foo' } {} for",
                "Unexpected type of return value of step program");
            ThrowsInnerMessage<RPNFunctionException, RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { {} } {} for",
                "Unexpected type of return value of step program");
        }

        [Test]
        public void Loop()
        {
            calc = new RPN(alwaysClearStack: true);
            calc.StepProcessed += () => TestContext.WriteLine(calc.DumpStack());
            Assert.True(calc.Eval("10 'i' sto 'i' 20 1 { i } loop dup 20 =="));
            CollectionAssert.AreEqual(new[] { 1, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10 }, calc.StackView);
            Assert.True(calc.Eval("10 'i' sto 'i' 5 -1 { i } loop dup 5 =="));
            CollectionAssert.AreEqual(new[] { 1, 5, 6, 7, 8, 9, 10 }, calc.StackView);
            Assert.True(calc.Eval("-10 'i' sto 'i' -5 1 { i } loop dup -5 =="));
            CollectionAssert.AreEqual(new[] { 1, -5, -6, -7, -8, -9, -10 }, calc.StackView);
            Assert.True(calc.Eval("-5 'i' sto 'i' -10 -1 { i } loop dup -10 =="));
            CollectionAssert.AreEqual(new[] { 1, -10, -9, -8, -7, -6, -5 }, calc.StackView);
        }

        private void ThrowsInnerMessage<E, InnerE>(string program, string expectedMessage) where E : Exception where InnerE : Exception
        {
            var e = Assert.Throws<E>(() => calc.Eval(program));
            Assert.IsInstanceOf<InnerE>(e.InnerException);
            Assert.IsNotNull(e.InnerException.Message);
            Assert.AreEqual(expectedMessage, e.InnerException.Message);
        }

        [Test]
        public void OneOverX()
        {
            Assert.True(calc.Eval("3.1415 1/x 1 3.1415 / =="));
            CollectionAssert.AreEqual(new[] { 1 }, calc.StackView);
        }

        [Ignore("Not implemented yet")]
        [Test]
        public void Array()
        {
            calc.Eval("[ 10 20 ]");
            calc.Eval("[10 20]");
            calc.Eval("[");
            calc.Eval("10 20");
            calc.Eval("]");
            calc.DumpStack();
        }
    }
}
