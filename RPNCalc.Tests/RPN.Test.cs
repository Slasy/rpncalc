using System;
using System.Linq;
using NUnit.Framework;
using RPNCalc.Extensions;
using RPNCalc.Tools;

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

        [Test]
        public void SettingNotCaseSensitiveVariablesAndFunctions()
        {
            calc = new RPN(false);

            calc.SetName("Foo", 123);
            Assert.IsTrue(calc.Names.ContainsKey("foo"));
            calc.SetName("bAr", 456);
            Assert.IsTrue(calc.Names.ContainsKey("bar"));
            calc.SetName("PlUs", st => st.Push((double)st.Pop() + st.Pop()));
            Assert.IsTrue(calc.FunctionsView.Contains("plus"));
            Assert.AreEqual(123 + 456, calc.Eval("foO BAR plus"));
        }

        [Test]
        public void SettingCaseSensitiveVariablesAndFunctions()
        {
            calc = new RPN(true);

            calc.SetName("Foo", 123);
            Assert.IsTrue(calc.Names.ContainsKey("Foo"));
            calc.SetName("bAr", 456);
            Assert.IsTrue(calc.Names.ContainsKey("bAr"));
            calc.SetName("PlUs", st => st.Push((double)st.Pop() + st.Pop()));
            Assert.IsTrue(calc.FunctionsView.Contains("PlUs"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("foO BAR plus"));
            Assert.AreEqual(123 + 456, calc.Eval("Foo bAr PlUs"));
        }

        [Test]
        public void NoExceptionOnSameFunctionVariableName()
        {
            calc.SetName("foo", 123);
            Assert.DoesNotThrow(() => calc.SetName("foo", _ => { }));
            calc.SetName("bar", _ => { });
            Assert.DoesNotThrow(() => calc.SetName("bar", 123));
        }

        [Test]
        public void UnsetFunctionAndVariable()
        {
            calc.SetName("foo", 123);
            calc.SetName("bar", _ => { });
            CollectionAssert.IsSubsetOf(new[] { "foo", "bar" }, calc.Names.Keys);
            CollectionAssert.Contains(calc.FunctionsView, "bar");
            CollectionAssert.DoesNotContain(calc.FunctionsView, "foo");
            calc.RemoveName("foo");
            CollectionAssert.Contains(calc.FunctionsView, "bar");
            calc.RemoveName("bar");
            CollectionAssert.DoesNotContain(calc.FunctionsView, "bar");
        }

        [Test]
        public void Pythagoras()
        {
            // a^2 + b^2 = c^2
            calc.SetName("a", 3);
            calc.SetName("b", 4);
            calc.SetName("c", 5);
            calc.SetName("=", st =>
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
            calc.SetName("a", 2);
            calc.SetName("b", -5);
            calc.SetName("c", -3);
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
            calc.SetName("macro", new AStackItem[] { 1, 2, new StackName("+"), new StackName("dup"), 2, new StackName("/"), new StackName("swap") });
            CollectionAssert.Contains(calc.FunctionsView, "macro");
            calc.Eval("10 20 macro");
            CollectionAssert.AreEqual(new[] { 3, 1.5, 20, 10 }, calc.StackView);
        }

        [Test]
        public void KeepStackInMacro()
        {
            calc = new RPN(true, true);
            calc.Eval("1 2");
            calc.SetName("foo", new[] { new StackName("DUP"), new StackName("*"), new StackName("+") });
            calc.Eval("10 20 foo");
            CollectionAssert.AreEqual(new[] { 410 }, calc.StackView);
        }

        [Test]
        public void RotRollOver()
        {
            calc.SetName("eq", st => { var (x, y) = st.Peek2(); st.Push(x == y ? 1 : 0); });
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
            calc.SetName("foo", 1234);
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
            Assert.DoesNotThrow(() => top = calc.Eval("1 2 3 { 10 20 dup + * }"));
            Assert.AreEqual(AStackItem.Type.Program, top.type);
            Assert.AreEqual("{ 10 20 dup + * }", (top as StackProgram).ToString());
        }

        [Test]
        public void ProgramInsideProgram()
        {
            AStackItem p = calc.Eval("{ { 1 2 + } 3 * }");
            Assert.AreEqual("{ { 1 2 + } 3 * }", (p as StackProgram).ToString());
        }

        [Test]
        public void EvalSimpleProgram()
        {
            Assert.DoesNotThrow(() => calc.Eval("{ 1 2 + } eval"));
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
            calc.Eval("'{ 10 20 * } eval'");
            CollectionAssert.AreEqual(new[] { "{ 10 20 * } eval" }, calc.StackView);
        }

        [Test]
        public void StringsInsideProgram()
        {
            calc.Eval("{ 'foo' 'bar' }");
            CollectionAssert.AreEqual(new[] { StackProgram.From("foo", "bar") }, calc.StackView);
            calc.Eval("eval");
            CollectionAssert.AreEqual(new[] { "bar", "foo" }, calc.StackView);
        }

        [Test]
        public void DoNothingEvalString()
        {
            AStackItem result = null;
            Assert.DoesNotThrow(() => result = calc.Eval("'foo' eval"));
            Assert.AreEqual("foo", result.AsString());
            Assert.DoesNotThrow(() => result = calc.Eval("'{ foo }' eval"));
            Assert.AreEqual("{ foo }", result.AsString());
            Assert.DoesNotThrow(() => result = calc.Eval("'{ 10 }' eval"));
            Assert.AreEqual("{ 10 }", result.AsString());
        }

        [Test]
        public void DoNothingEvalNumber()
        {
            AStackItem result = null;
            Assert.DoesNotThrow(() => result = calc.Eval("42 eval"));
            Assert.AreEqual(42, result.AsNumber());
            Assert.DoesNotThrow(() => result = calc.Eval("-42.1337e2 eval"));
            Assert.AreEqual(-42.1337e2, result.AsNumber());
        }

        [Test]
        public void SetStringVariable()
        {
            calc.SetName("foo", "foobar");
            string s = calc.Eval("foo").AsString();
            Assert.AreEqual("foobar", s);
        }

        [Test]
        public void SetProgramVariable()
        {
            calc.SetName("foo", StackProgram.From(new StackName("dup"), new StackName("*"), new StackName("+")));
            double n = calc.Eval("10 20 foo eval").AsNumber();
            Assert.AreEqual(410, n);
        }

        [Test]
        public void StoNumber()
        {
            calc.Eval("999 123 'foo' sto");
            CollectionAssert.AreEqual(new[] { 999 }, calc.StackView);
            CollectionAssert.Contains(calc.Names.Keys, "foo");
            Assert.AreEqual(123, calc.Names["foo"]);
            Assert.AreEqual(123, calc.Eval("foo").AsNumber());
        }

        [Test]
        public void StoString()
        {
            calc.Eval("'foobar' dup 'foo' sto");
            CollectionAssert.AreEqual(new[] { "foobar" }, calc.StackView);
            CollectionAssert.Contains(calc.Names.Keys, "foo");
            Assert.AreEqual("foobar", calc.Names["foo"]);
        }

        [Test]
        public void StoProgram()
        {
            calc.Eval("{ dummy program } 'foo' sto");
            CollectionAssert.IsEmpty(calc.StackView);
            CollectionAssert.Contains(calc.Names.Keys, "foo");
            Assert.AreEqual("{ dummy program }", calc.Names["foo"].ToString());
        }

        [Test]
        public void RclNumber()
        {
            calc.SetName("foo", 1234);
            double n = calc.Eval("'foo' rcl").AsNumber();
            Assert.AreEqual(1234, n);
            CollectionAssert.AreEqual(new[] { 1234 }, calc.StackView);
        }

        [Test]
        public void RclString()
        {
            calc.SetName("foo", "foobar");
            string s = calc.Eval("'foo' rcl").AsString();
            Assert.AreEqual("foobar", s);
            CollectionAssert.AreEqual(new[] { "foobar" }, calc.StackView);
        }

        [Test]
        public void RclProgram()
        {
            calc.SetName("foo", StackProgram.From(new StackName("dup"), new StackName("+")));
            var s = calc.Eval("'foo' rcl").AsProgramInstructions();
            Assert.AreEqual(StackProgram.From(new StackName("dup"), new StackName("+")), new StackProgram(s));
            CollectionAssert.AreEqual(new[] { StackProgram.From(new StackName("dup"), new StackName("+")) }, calc.StackView);
        }

        [Test]
        public void RclSto()
        {
            calc.SetName("foo", "foobar");
            var top = calc.Eval("'foo' rcl dup sto");
            Assert.IsNull(top);
            CollectionAssert.IsSubsetOf(new[] { "foo", "foobar" }, calc.Names.Keys);
            Assert.AreEqual("foobar", calc.Names["foo"]);
            Assert.AreEqual("foobar", calc.Names["foobar"]);
        }

        [Test]
        public void ClearVariable()
        {
            calc = new RPN(alwaysClearStack: false);
            calc.SetName("foo", "foobar");
            CollectionAssert.Contains(calc.Names.Keys, "foo");
            Assert.AreEqual("foobar", calc.Names["foo"]);
            Assert.DoesNotThrow(() => calc.Eval("foo 'foo' clv"));
            CollectionAssert.DoesNotContain(calc.Names.Keys, "foo");
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("foo"));
        }

        [Test]
        public void LazyEvaluation()
        {
            CollectionAssert.DoesNotContain(calc.Names.Keys, "a");
            CollectionAssert.DoesNotContain(calc.Names.Keys, "b");
            int value = calc.Eval("{ a b + } 12 'a' sto 8 'b' sto eval");
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
            Assert.Throws<RPNFunctionException>(() => calc.Eval("{ dup } { sto } +"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("{ dup } 'var' +"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("12356 'var' +"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("'var' 98765 +"));
        }

        [Test]
        public void IfThen()
        {
            calc = new RPN(alwaysClearStack: true);
            string result = calc.Eval("10 10 == { 'yes 10 == 10' } ift");
            Assert.AreEqual("yes 10 == 10", result);
            var emptyResult = calc.Eval(" 10 10 != { 'yes 10 != 10' } ift");
            Assert.IsNull(emptyResult);
        }

        [Test]
        public void IfThenElse()
        {
            calc = new RPN(alwaysClearStack: true);
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
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'unknown_var' ++"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'123' ++"));
            Assert.DoesNotThrow(() => calc.Eval("0 '123' sto '123' ++"));
            Assert.AreEqual(1, calc.Names["123"]);
        }

        [Test]
        public void MinusMinus()
        {
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'unknown_var' --"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'123' --"));
            Assert.DoesNotThrow(() => calc.Eval("0 '123' sto '123' --"));
            Assert.AreEqual(-1, calc.Names["123"]);
        }

        [Test]
        public void ForLoop()
        {
            Assert.True(calc.Eval("0 0 'i' sto 'i' { i 10 < } { 1 } { 1 + } for 10 =="));
            CollectionAssert.AreEqual(new[] { 1 }, calc.StackView);
        }

        [Test]
        public void ForLoopInvalidVariable()
        {
            ThrowsMessage<RPNUndefinedNameException>(
                "'i' { } { } { } for",
                "Unknown name i");
            ThrowsMessage<RPNArgumentException>(
                "'foo' 'i' sto 'i' { } { } { } for",
                "Bad argument type");
            ThrowsMessage<RPNArgumentException>(
                "{ } 'i' sto 'i' { } { } { } for",
                "Bad argument type");
        }

        [Test]
        public void ForLoopInvalidCondition()
        {
            ThrowsMessage<RPNArgumentException>(
                "0 'i' sto 'i' { 1 2 3 } { } { } for",
                "Unexpected behavior of condition program");
            ThrowsMessage<RPNArgumentException>(
                "0 'i' sto 'i' { } { } { } for",
                "Unexpected behavior of condition program");
        }

        [Test]
        public void ForLoopInvalidStep()
        {
            ThrowsMessage<RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { 1 2 3 } { } for",
                "Unexpected behavior of step program");
            ThrowsMessage<RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { } { } for",
                "Unexpected behavior of step program");
            ThrowsMessage<RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { 'foo' } { } for",
                "Unexpected type of return value of step program");
            ThrowsMessage<RPNArgumentException>(
                "0 'i' sto 'i' { i 10 < } { { } } { } for",
                "Unexpected type of return value of step program");
        }

        private void ThrowsMessage<E>(string program, string expectedMessage) where E : Exception
        {
            var e = Assert.Throws<E>(() => calc.Eval(program));
            Assert.IsNotNull(e.Message);
            Assert.AreEqual(expectedMessage, e.Message);
        }

        [Test]
        public void Loop()
        {
            calc = new RPN(alwaysClearStack: true);
            Assert.True(calc.Eval("10 'i' sto 'i' 20 1 { i } loop dup 20 =="));
            CollectionAssert.AreEqual(new[] { 1, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10 }, calc.StackView);
            Assert.True(calc.Eval("10 'i' sto 'i' 5 -1 { i } loop dup 5 =="));
            CollectionAssert.AreEqual(new[] { 1, 5, 6, 7, 8, 9, 10 }, calc.StackView);
            Assert.True(calc.Eval("-10 'i' sto 'i' -5 1 { i } loop dup -5 =="));
            CollectionAssert.AreEqual(new[] { 1, -5, -6, -7, -8, -9, -10 }, calc.StackView);
            Assert.True(calc.Eval("-5 'i' sto 'i' -10 -1 { i } loop dup -10 =="));
            CollectionAssert.AreEqual(new[] { 1, -10, -9, -8, -7, -6, -5 }, calc.StackView);
        }

        [Test]
        public void OneOverX()
        {
            Assert.True(calc.Eval("3.1415 1/x 1 3.1415 / =="));
            CollectionAssert.AreEqual(new[] { 1 }, calc.StackView);
        }

        [Test]
        public void ListAndLists()
        {
            Assert.AreEqual(new StackList(new StackNumber[] { 10, 20 }), calc.Eval("[ 10 20 ]"));
            var list = new AStackItem[] { 10, new StackList(new StackNumber[] { 20 }), 30, new StackList(new AStackItem[] { 40, 50, 60 }) };
            Assert.AreEqual(list, calc.Eval("[ 10 [ 20 ] 30 [ 40 50 60 ] ]").AsArray());
            Assert.AreEqual(list, calc.Eval("[").AsArray());
            Assert.AreEqual(list, calc.Eval("10 20").AsArray());
            Assert.AreEqual(new StackList(new AStackItem[] { 10, 20 }), calc.Eval("]"));
        }

        [Test]
        public void ThrowsOnIncorrectCollectionSymbols()
        {
            calc = new RPN(false, true);
            Assert.Throws<RPNFunctionException>(() => calc.Eval("[ 10 >>"));
            Assert.DoesNotThrow(() => calc.Eval("[ << 10 >> ]"));
        }

        [Test]
        public void EvalTypedExpression1()
        {
            var result = calc.Eval(new AStackItem[] { 10, 20 });
            Assert.IsInstanceOf<StackNumber>(result);
            Assert.AreEqual(20, (result as StackNumber).value);
        }

        [Test]
        public void EvalTypedExpression2()
        {
            var result = calc.Eval(new AStackItem[] { 1, 2, 3, new StackList(new AStackItem[] { 10, 20 }) });
            Assert.IsInstanceOf<StackList>(result);
            Assert.AreEqual(4, calc.StackView.Count);
            Assert.AreEqual(20, (result as StackList).value[1].AsNumber());
        }

        [Test]
        public void EvalTypedExpression3()
        {
            var result = calc.Eval(new AStackItem[] { 1, 2, 3, new StackFunction("foo", st => st.Push(st.Pop().AsNumber() * 2)) });
            Assert.IsInstanceOf<StackNumber>(result);
            Assert.AreEqual(3, calc.StackView.Count);
            Assert.AreEqual(6, (result as StackNumber).value);
        }

        [Test]
        public void EvalTypedExpression4()
        {
            var result = calc.Eval(new AStackItem[] { 1, 2, 3, new StackList(new AStackItem[] { new StackFunction("foo", st => st.Push(st.Pop().AsNumber() * 2)) }) });
            Assert.IsInstanceOf<StackList>(result);
            Assert.AreEqual(4, calc.StackView.Count);
            Assert.IsInstanceOf<StackFunction>((result as StackList).value[0]);
            StackFunction function = (result as StackList).value[0] as StackFunction;
            Assert.AreEqual("foo", function.name);
            var st = new Stack<AStackItem>();
            st.Push(5);
            function.value(st);
            Assert.AreEqual(10, st.Pop().AsNumber());
        }

        [Test]
        public void CallFunctionUsingVariable()
        {
            calc.Eval(new AStackItem[] { StackList.From(10, 20, new StackName("+")), 30, 40, new StackName("*") });
            Assert.AreEqual(StackList.From(10, 20, new StackName("+")), calc.StackView.Skip(1).Single());
            Assert.IsInstanceOf<StackNumber>(calc.StackView.First());
            Assert.AreEqual(1200, calc.StackView.First());
        }

        [Test]
        public void CreateAndCallProgramLikeFunction()
        {
            var x2 = StackProgram.From(2, new StackName("^"));
            var top = calc.Eval(new AStackItem[] { x2, new StackString("X2"), new StackName("STO"), StackProgram.From(10, new StackName("X2")), new StackName("EVAL") });
            Assert.IsInstanceOf<StackNumber>(top);
            Assert.AreEqual(100, top);
        }

        [Test]
        public void ShouldKeepInnerProgramOnStack()
        {
            calc.SetName("plus2", new StackProgram(new AStackItem[] { 2, new StackName("+") }));
            var expression = StackProgram.From(10, new StackName("plus2"));
            var top = calc.Eval(new AStackItem[] { expression, new StackName("EVAL") });
            Assert.IsInstanceOf<StackNumber>(top);
            Assert.AreEqual(12, top);

            calc.ClearStack();
            calc.Eval(new[] { expression });
            TestContext.WriteLine(calc.DumpStack());
        }

        [Test]
        public void InnerPrograms()
        {
            var ifTrue = StackProgram.From(new AStackItem[] { 10 });
            var ifFalse = StackProgram.From(new AStackItem[] { 20 });
            var prog = StackProgram.From(42, new StackName("=="), ifTrue, ifFalse, new StackName("ifte"));
            var result = calc.Eval(new AStackItem[] { 42, prog, new StackName("eval") });
            TestContext.WriteLine(calc.DumpStack());
            Assert.AreEqual(10, result);

            result = calc.Eval(new AStackItem[] { 0, prog, new StackName("eval") });
            TestContext.WriteLine(calc.DumpStack());
            Assert.AreEqual(20, result);
        }

        [Test]
        public void BuildAndProgram()
        {
            var prog = calc.Eval(new AStackItem[] { new StackName("{"), 10, 20, new StackName("+"), new StackName("}") });
            Assert.IsInstanceOf<StackProgram>(prog);
            Assert.AreEqual(StackProgram.From(10, 20, new StackName("+")), prog);
            var num = calc.Eval(new[] { new StackName("eval") });
            Assert.IsInstanceOf<StackNumber>(num);
            Assert.AreEqual(30, num);
        }

        [Test]
        public void RunStringTokens()
        {
            var result = calc.Eval(RPNTools.TokensToItems("10", "20", "+", "3", "^"));
            Assert.AreEqual(Math.Pow(10 + 20, 3), result);
        }

        [Test]
        public void SqueakyCleanCalc()
        {
            calc = new RPN(false, false, false);
            calc.Eval("10 20 3.14");
            CollectionAssert.AreEqual(new[] { 3.14, 20, 10 }, calc.StackView);
            calc.Eval("'foo bar'");
            CollectionAssert.AreEqual(new AStackItem[] { "foo bar", 3.14, 20, 10 }, calc.StackView);
            Assert.AreEqual("Unknown name {", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("{ 1 2 }")).Message);
            Assert.AreEqual("Unknown name [", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("[ 1 2 ]")).Message);
            Assert.AreEqual("Unknown name +", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 +")).Message);
            Assert.AreEqual("Unknown name ^", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 ^")).Message);
            Assert.AreEqual("Unknown name sto", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 sto")).Message);
            Assert.AreEqual("Unknown name ift", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 ift")).Message);
            Assert.AreEqual("Unknown name while", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 while")).Message);
            Assert.AreEqual("Unknown name clst", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("clst")).Message);
            CollectionAssert.IsEmpty(calc.Names);
        }

        [Test]
        public void MinimalCalc()
        {
            calc = new RPN(false, false, false);
            calc.SetName("add2numbers", st => { var (x, y) = st.Pop2(); st.Push(x.AsNumber() + y); });
            calc.SetName("giv4pls", st => st.Push(4)); // https://xkcd.com/221/
            calc.EvalAlgebraic("add2numbers(giv4pls(),giv4pls())");
            CollectionAssert.AreEqual(new[] { 8 }, calc.StackView);
            Assert.AreEqual("Unknown name +", Assert.Throws<RPNUndefinedNameException>(() => calc.EvalAlgebraic("giv4pls() + giv4pls()")).Message);
        }
    }
}
