using System;
using System.Linq;
using System.Numerics;
using System.Text;
using NUnit.Framework;
using RPNCalc.Extensions;
using RPNCalc.Items;
using RPNCalc.Tools;

namespace RPNCalc.Tests
{
    [Timeout(3000)]
    public class RPNTest
    {
        private RPN calc;

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugBreak()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        }

        [SetUp]
        public void Setup()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = false });
            calc.SetNameValue("_DBG_", _ => DebugBreak());
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
            calc = new RPN(new RPN.Options { CaseSensitiveNames = false });

            calc.SetNameValue("Foo", 123);
            Assert.IsTrue(calc.GlobalNames.ContainsKey("foo"));
            calc.SetNameValue("bAr", 456);
            Assert.IsTrue(calc.GlobalNames.ContainsKey("bar"));
            calc.SetNameValue("PlUs", st => st.Push((double)st.Pop() + st.Pop()));
            Assert.IsTrue(calc.FunctionsView.Contains("plus"));
            Assert.AreEqual(123 + 456, calc.Eval("foO BAR plus"));
        }

        [Test]
        public void SettingCaseSensitiveVariablesAndFunctions()
        {
            calc = new RPN(new RPN.Options { CaseSensitiveNames = true });

            calc.SetNameValue("Foo", 123);
            Assert.IsTrue(calc.GlobalNames.ContainsKey("Foo"));
            calc.SetNameValue("bAr", 456);
            Assert.IsTrue(calc.GlobalNames.ContainsKey("bAr"));
            calc.SetNameValue("PlUs", st => st.Push((double)st.Pop() + st.Pop()));
            Assert.IsTrue(calc.FunctionsView.Contains("PlUs"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("foO BAR plus"));
            Assert.AreEqual(123 + 456, calc.Eval("Foo bAr PlUs"));
        }

        [Test]
        public void NoExceptionOnSameFunctionVariableName()
        {
            calc.SetNameValue("foo", 123);
            Assert.DoesNotThrow(() => calc.SetNameValue("foo", _ => { }));
            calc.SetNameValue("bar", _ => { });
            Assert.DoesNotThrow(() => calc.SetNameValue("bar", 123));
        }

        [Test]
        public void UnsetFunctionAndVariable()
        {
            calc.SetNameValue("foo", 123);
            calc.SetNameValue("bar", _ => { });
            CollectionAssert.IsSubsetOf(new[] { "foo", "bar" }, calc.GlobalNames.Keys);
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
            calc.SetNameValue("a", 3);
            calc.SetNameValue("b", 4);
            calc.SetNameValue("c", 5);
            calc.SetNameValue("=", st =>
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
            calc.SetNameValue("a", 2);
            calc.SetNameValue("b", -5);
            calc.SetNameValue("c", -3);
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
            calc.SetNameValue("macro", new AItem[] { 1, 2, new NameItem("+"), new NameItem("dup"), 2, new NameItem("/"), new NameItem("swap") });
            CollectionAssert.Contains(calc.FunctionsView, "macro");
            calc.Eval("10 20 macro");
            CollectionAssert.AreEqual(new[] { 3, 1.5, 20, 10 }, calc.StackView);
        }

        [Test]
        public void KeepStackInMacro()
        {
            calc = new RPN(new RPN.Options { CaseSensitiveNames = true, AlwaysClearStack = true });
            calc.Eval("1 2");
            calc.SetNameValue("foo", new[] { new NameItem("DUP"), new NameItem("*"), new NameItem("+") });
            calc.Eval("10 20 foo");
            CollectionAssert.AreEqual(new[] { 410 }, calc.StackView);
        }

        [Test]
        public void RotRollOver()
        {
            calc.SetNameValue("eq", st => { var (x, y) = st.Peek2(); st.Push(x == y ? 1 : 0); });
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
            calc.SetNameValue("foo", 1234);
            string top = null;
            Assert.DoesNotThrow(() => top = calc.Eval("foo 'foo'"));
            Assert.AreEqual("foo", top);
            CollectionAssert.AreEqual(new AItem[] { "foo", 1234 }, calc.StackView);
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
            AItem top = null;
            Assert.DoesNotThrow(() => top = calc.Eval("1 2 3 { 10 20 dup + * }"));
            Assert.AreEqual(AItem.Type.Program, top.type);
            Assert.AreEqual("{ 10 20 dup + * }", (top as ProgramItem).ToString());
        }

        [Test]
        public void ProgramInsideProgram()
        {
            AItem p = calc.Eval("{ { 1 2 + } 3 * }");
            Assert.AreEqual("{ { 1 2 + } 3 * }", (p as ProgramItem).ToString());
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
            CollectionAssert.AreEqual(new[] { ProgramItem.From("foo", "bar") }, calc.StackView);
            calc.Eval("eval");
            CollectionAssert.AreEqual(new[] { "bar", "foo" }, calc.StackView);
        }

        [Test]
        public void DoNothingEvalNumber()
        {
            AItem result = null;
            Assert.DoesNotThrow(() => result = calc.Eval("42 eval"));
            Assert.AreEqual(42, result.GetRealNumber());
            Assert.DoesNotThrow(() => result = calc.Eval("-42.1337e2 eval"));
            Assert.AreEqual(-42.1337e2, result.GetRealNumber());
        }

        [Test]
        public void SetStringVariable()
        {
            calc.SetNameValue("foo", "foobar");
            string s = calc.Eval("foo").GetString();
            Assert.AreEqual("foobar", s);
        }

        [Test]
        public void SetProgramVariable()
        {
            calc.SetNameValue("foo", ProgramItem.From(new NameItem("dup"), new NameItem("*"), new NameItem("+")));
            double n = calc.Eval("10 20 foo eval").GetRealNumber();
            Assert.AreEqual(410, n);
        }

        [Test]
        public void StoNumber()
        {
            calc.Eval("999 123 'foo' sto");
            CollectionAssert.AreEqual(new[] { 999 }, calc.StackView);
            CollectionAssert.Contains(calc.GlobalNames.Keys, "foo");
            Assert.AreEqual(123, calc.GlobalNames["foo"]);
            Assert.AreEqual(123, calc.Eval("foo").GetRealNumber());
        }

        [Test]
        public void StoString()
        {
            calc.Eval("'foobar' dup 'foo' sto");
            CollectionAssert.AreEqual(new[] { "foobar" }, calc.StackView);
            CollectionAssert.Contains(calc.GlobalNames.Keys, "foo");
            Assert.AreEqual("foobar", calc.GlobalNames["foo"]);
        }

        [Test]
        public void StoProgram()
        {
            calc.Eval("{ dummy program } 'foo' sto");
            CollectionAssert.IsEmpty(calc.StackView);
            CollectionAssert.Contains(calc.GlobalNames.Keys, "foo");
            Assert.AreEqual("{ dummy program }", calc.GlobalNames["foo"].ToString());
        }

        [Test]
        public void RclNumber()
        {
            calc.SetNameValue("foo", 1234);
            double n = calc.Eval("'foo' rcl").GetRealNumber();
            Assert.AreEqual(1234, n);
            CollectionAssert.AreEqual(new[] { 1234 }, calc.StackView);
        }

        [Test]
        public void RclString()
        {
            calc.SetNameValue("foo", "foobar");
            string s = calc.Eval("'foo' rcl").GetString();
            Assert.AreEqual("foobar", s);
            CollectionAssert.AreEqual(new[] { "foobar" }, calc.StackView);
        }

        [Test]
        public void RclProgram()
        {
            calc.SetNameValue("foo", ProgramItem.From(new NameItem("dup"), new NameItem("+")));
            var s = calc.Eval("'foo' rcl").GetProgramInstructions();
            Assert.AreEqual(ProgramItem.From(new NameItem("dup"), new NameItem("+")), new ProgramItem(s));
            CollectionAssert.AreEqual(new[] { ProgramItem.From(new NameItem("dup"), new NameItem("+")) }, calc.StackView);
        }

        [Test]
        public void RclSto()
        {
            calc.SetNameValue("foo", "foobar");
            var top = calc.Eval("'foo' rcl dup sto");
            Assert.IsNull(top);
            CollectionAssert.IsSubsetOf(new[] { "foo", "foobar" }, calc.GlobalNames.Keys);
            Assert.AreEqual("foobar", calc.GlobalNames["foo"]);
            Assert.AreEqual("foobar", calc.GlobalNames["foobar"]);
        }

        [Test]
        public void ClearVariable()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = false });
            calc.SetNameValue("foo", "foobar");
            CollectionAssert.Contains(calc.GlobalNames.Keys, "foo");
            Assert.AreEqual("foobar", calc.GlobalNames["foo"]);
            Assert.DoesNotThrow(() => calc.Eval("foo 'foo' clv"));
            CollectionAssert.DoesNotContain(calc.GlobalNames.Keys, "foo");
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("foo"));
        }

        [Test]
        public void LazyEvaluation()
        {
            CollectionAssert.DoesNotContain(calc.GlobalNames.Keys, "a");
            CollectionAssert.DoesNotContain(calc.GlobalNames.Keys, "b");
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
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.Throws<RPNFunctionException>(() => calc.Eval("{ dup } { sto } +"));
        }

        [Test]
        public void IfThen()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            string result = calc.Eval("10 10 == { 'yes 10 == 10' } ift");
            Assert.AreEqual("yes 10 == 10", result);
            var emptyResult = calc.Eval(" 10 10 != { 'yes 10 != 10' } ift");
            Assert.IsNull(emptyResult);
        }

        [Test]
        public void IfThenElse()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
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
            Assert.AreEqual(1, calc.GlobalNames["123"]);
        }

        [Test]
        public void MinusMinus()
        {
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'unknown_var' --"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'123' --"));
            Assert.DoesNotThrow(() => calc.Eval("0 '123' sto '123' --"));
            Assert.AreEqual(-1, calc.GlobalNames["123"]);
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
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.True(calc.Eval("10 'i' sto 'i' 20 1 { i } loop dup 19 =="));
            CollectionAssert.AreEqual(new[] { 1, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10 }, calc.StackView);
            Assert.True(calc.Eval("10 'i' sto 'i' 5 -1 { i } loop dup 6 =="));
            CollectionAssert.AreEqual(new[] { 1, 6, 7, 8, 9, 10 }, calc.StackView);
            Assert.True(calc.Eval("-10 'i' sto 'i' -5 1 { i } loop dup -6 =="));
            CollectionAssert.AreEqual(new[] { 1, -6, -7, -8, -9, -10 }, calc.StackView);
            Assert.True(calc.Eval("-5 'i' sto 'i' -10 -1 { i } loop dup -9 =="));
            CollectionAssert.AreEqual(new[] { 1, -9, -8, -7, -6, -5 }, calc.StackView);
            Assert.IsNull(calc.Eval("0 'i' sto 'i' 0 1 { i } loop"));
            Assert.AreEqual(0, calc.Eval("0 'i' sto 'i' 1 1 { i } loop"));
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
            Assert.AreEqual(new ListItem(new RealNumberItem[] { 10, 20 }), calc.Eval("[ 10 20 ]"));
            var list = new AItem[] { 10, new ListItem(new RealNumberItem[] { 20 }), 30, new ListItem(new AItem[] { 40, 50, 60 }) };
            Assert.AreEqual(list, calc.Eval("[ 10 [ 20 ] 30 [ 40 50 60 ] ]").GetArray());
            Assert.AreEqual(list, calc.Eval("[").GetArray());
            Assert.AreEqual(list, calc.Eval("10 20").GetArray());
            Assert.AreEqual(new ListItem(new AItem[] { 10, 20 }), calc.Eval("]"));
        }

        [Test]
        public void ThrowsOnIncorrectCollectionSymbols()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.Throws<RPNFunctionException>(() => calc.Eval("[ 10 >>"));
            Assert.DoesNotThrow(() => calc.Eval("[ << 10 >> ]"));
        }

        [Test]
        public void EvalTypedExpression1()
        {
            var result = calc.Eval(new AItem[] { 10, 20 });
            Assert.IsInstanceOf<RealNumberItem>(result);
            Assert.AreEqual(20, (result as RealNumberItem).value);
        }

        [Test]
        public void EvalTypedExpression2()
        {
            var result = calc.Eval(new AItem[] { 1, 2, 3, new ListItem(new AItem[] { 10, 20 }) });
            Assert.IsInstanceOf<ListItem>(result);
            Assert.AreEqual(4, calc.StackView.Count);
            Assert.AreEqual(20, (result as ListItem).value[1].GetRealNumber());
        }

        [Test]
        public void EvalTypedExpression3()
        {
            var result = calc.Eval(new AItem[] { 1, 2, 3, new FunctionItem("foo", st => st.Push(st.Pop().GetRealNumber() * 2)) });
            Assert.IsInstanceOf<RealNumberItem>(result);
            Assert.AreEqual(3, calc.StackView.Count);
            Assert.AreEqual(6, (result as RealNumberItem).value);
        }

        [Test]
        public void EvalTypedExpression4()
        {
            var result = calc.Eval(new AItem[] { 1, 2, 3, new ListItem(new AItem[] { new FunctionItem("foo", st => st.Push(st.Pop().GetRealNumber() * 2)) }) });
            Assert.IsInstanceOf<ListItem>(result);
            Assert.AreEqual(4, calc.StackView.Count);
            Assert.IsInstanceOf<FunctionItem>((result as ListItem).value[0]);
            FunctionItem function = (result as ListItem).value[0] as FunctionItem;
            Assert.AreEqual("foo", function.name);
            var st = new Stack<AItem>();
            st.Push(5);
            function.value(st);
            Assert.AreEqual(10, st.Pop().GetRealNumber());
        }

        [Test]
        public void CallFunctionUsingVariable()
        {
            calc.Eval(new AItem[] { ListItem.From(10, 20, new NameItem("+")), 30, 40, new NameItem("*") });
            Assert.AreEqual(ListItem.From(10, 20, new NameItem("+")), calc.StackView.Skip(1).Single());
            Assert.IsInstanceOf<RealNumberItem>(calc.StackView.First());
            Assert.AreEqual(1200, calc.StackView.First());
        }

        [Test]
        public void CreateAndCallProgramLikeFunction()
        {
            var x2 = ProgramItem.From(2, new NameItem("^"));
            var top = calc.Eval(new AItem[] { x2, new StackStringItem("X2"), new NameItem("STO"), ProgramItem.From(10, new NameItem("X2")), new NameItem("EVAL") });
            Assert.IsInstanceOf<RealNumberItem>(top);
            Assert.AreEqual(100, top);
        }

        [Test]
        public void ShouldKeepInnerProgramOnStack()
        {
            calc.SetNameValue("plus2", new ProgramItem(new AItem[] { 2, new NameItem("+") }));
            var expression = ProgramItem.From(10, new NameItem("plus2"));
            var top = calc.Eval(new AItem[] { expression, new NameItem("EVAL") });
            Assert.IsInstanceOf<RealNumberItem>(top);
            Assert.AreEqual(12, top);

            calc.ClearStack();
            calc.Eval(new[] { expression });
            TestContext.WriteLine(calc.DumpStack());
        }

        [Test]
        public void InnerPrograms()
        {
            var ifTrue = ProgramItem.From(new AItem[] { 10 });
            var ifFalse = ProgramItem.From(new AItem[] { 20 });
            var prog = ProgramItem.From(42, new NameItem("=="), ifTrue, ifFalse, new NameItem("ifte"));
            var result = calc.Eval(new AItem[] { 42, prog, new NameItem("eval") });
            TestContext.WriteLine(calc.DumpStack());
            Assert.AreEqual(10, result);

            result = calc.Eval(new AItem[] { 0, prog, new NameItem("eval") });
            TestContext.WriteLine(calc.DumpStack());
            Assert.AreEqual(20, result);
        }

        [Test]
        public void BuildAndProgram()
        {
            var prog = calc.Eval(new AItem[] { new NameItem("{"), 10, 20, new NameItem("+"), new NameItem("}") });
            Assert.IsInstanceOf<ProgramItem>(prog);
            Assert.AreEqual(ProgramItem.From(10, 20, new NameItem("+")), prog);
            var num = calc.Eval(new[] { new NameItem("eval") });
            Assert.IsInstanceOf<RealNumberItem>(num);
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
            calc = new RPN(new RPN.Options { AlwaysClearStack = false, CaseSensitiveNames = false, LoadDefaultFunctions = false });
            calc.Eval("10 20 3.14");
            CollectionAssert.AreEqual(new[] { 3.14, 20, 10 }, calc.StackView);
            calc.Eval("'foo bar'");
            CollectionAssert.AreEqual(new AItem[] { "foo bar", 3.14, 20, 10 }, calc.StackView);
            Assert.AreEqual("Unknown name {", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("{ 1 2 }")).Message);
            Assert.AreEqual("Unknown name [", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("[ 1 2 ]")).Message);
            Assert.AreEqual("Unknown name +", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 +")).Message);
            Assert.AreEqual("Unknown name ^", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 ^")).Message);
            Assert.AreEqual("Unknown name sto", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 sto")).Message);
            Assert.AreEqual("Unknown name ift", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 ift")).Message);
            Assert.AreEqual("Unknown name while", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("1 2 while")).Message);
            Assert.AreEqual("Unknown name clst", Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("clst")).Message);
            CollectionAssert.IsEmpty(calc.GlobalNames);
        }

        [Test]
        public void MinimalCalc()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = false, CaseSensitiveNames = false, LoadDefaultFunctions = false });
            calc.SetNameValue("add2numbers", st => { var (x, y) = st.Pop2(); st.Push(x.GetRealNumber() + y); });
            calc.SetNameValue("giv4pls", st => st.Push(4)); // https://xkcd.com/221/
            calc.EvalAlgebraic("add2numbers(giv4pls(),giv4pls())");
            CollectionAssert.AreEqual(new[] { 8 }, calc.StackView);
            Assert.AreEqual("Unknown name +", Assert.Throws<RPNUndefinedNameException>(() => calc.EvalAlgebraic("giv4pls() + giv4pls()")).Message);
        }

        [Test]
        public void MoreComplexAlgExpression()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = false, CaseSensitiveNames = false });
            calc.SetNameValue("sin", st => st.Push(Math.Sin(st.Pop())));
            calc.SetNameValue("cos", st => st.Push(Math.Cos(st.Pop())));
            calc.SetNameValue("tan", st => st.Push(Math.Tan(st.Pop())));
            var result = calc.EvalAlgebraic("sin(cos(tan(3)+2)*5)/3");
            Assert.AreEqual(-.329230492289, result, 0.0001);
        }

        [Test]
        public void EvalStringAsAlgExpression()
        {
            calc.SetNameValue("sin", st => st.Push(Math.Sin(st.Pop())));
            calc.SetNameValue("pi", Math.PI);
            var result = calc.Eval("'sin(pi/2)' eval");
            Assert.IsInstanceOf<RealNumberItem>(result);
            Assert.AreEqual(1d, result);
        }

        [Test]
        public void ExceptionOnUnknownNameInAlgString()
        {
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'sin(pi)' eval"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'foo' eval"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'random()' eval"));
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'random(1,10)' eval"));
        }

        [Test]
        public void DefineProgramAndEvalInAlgebraicString()
        {
            var result = calc.Eval("{ dup dup * * } 'cube' sto 'cube(4)*(2+8)' eval");
            Assert.IsInstanceOf<RealNumberItem>(result);
            Assert.AreEqual(640d, result);
        }

        [Test]
        public void ConnectAsStrings()
        {
            Assert.AreEqual("1FOO2BAR", calc.Eval("1 'FOO' + 2 + 'BAR' +"));
            Assert.AreEqual("{ 10 20 + }123", calc.Eval(new AItem[] {
                string.Empty,
                ProgramItem.From(10, 20, new NameItem("+")),
                new NameItem("+"),
                1, new NameItem("+"),
                2, new NameItem("+"),
                3, new NameItem("+") }));
        }

        [Test]
        public void LoadComplexNumbers()
        {
            calc.Eval("( 10 20 ) ( 50 0 )");
            CollectionAssert.AreEqual(new[] { new Complex(50, 0), new Complex(10, 20) }, calc.StackView);
            calc.ClearStack();
            Assert.AreEqual(new ComplexNumberItem(123, 0), calc.Eval("( 123 0 )"));
        }

        [Test]
        public void ComplexNumbersConversion()
        {
            double r;
            Assert.Throws<RPNArgumentException>(() => r = new ComplexNumberItem(1234, 0));
            Complex complex1 = new ComplexNumberItem(1234, 0);
            Complex complex2 = new ComplexNumberItem(1234, 5678);
            ComplexNumberItem complex3 = new Complex(999, 666);
            ComplexNumberItem complex4 = 42;
            ComplexNumberItem complex5 = 42.42;
            Assert.AreEqual(1234, complex1.Real);
            Assert.AreEqual(0, complex1.Imaginary);
            Assert.AreEqual(1234, complex2.Real);
            Assert.AreEqual(5678, complex2.Imaginary);
            Assert.AreEqual(999, complex3.value.Real);
            Assert.AreEqual(666, complex3.value.Imaginary);
            Assert.AreEqual(42, complex4.value.Real);
            Assert.AreEqual(0, complex4.value.Imaginary);
            Assert.AreEqual(42.42, complex5.value.Real);
            Assert.AreEqual(0, complex5.value.Imaginary);
        }

        [Test]
        public void ComplexAndRealNumbersOperations()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.AreEqual(new Complex(10, 20) * 2, calc.Eval("( 10 20 ) 2 *"));
            Assert.AreEqual(new Complex(10, 20) + 2, calc.Eval("( 10 20 ) 2 +"));
            Assert.AreEqual(new Complex(10, 20) - 2, calc.Eval("( 10 20 ) 2 -"));
            Assert.AreEqual(new Complex(10, 20) / 2, calc.Eval("( 10 20 ) 2 /"));
            Assert.AreEqual(Complex.Pow(new Complex(10, 20), 2), calc.Eval("( 10 20 ) 2 ^"));
        }

        [Test]
        public void ComplexAndComplexNumbersOperations()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.AreEqual(new Complex(10, 20) * new Complex(2, 3), calc.Eval("( 10 20 ) ( 2 3 ) *"));
            Assert.AreEqual(new Complex(10, 20) + new Complex(2, 3), calc.Eval("( 10 20 ) ( 2 3 ) +"));
            Assert.AreEqual(new Complex(10, 20) - new Complex(2, 3), calc.Eval("( 10 20 ) ( 2 3 ) -"));
            Assert.AreEqual(new Complex(10, 20) / new Complex(2, 3), calc.Eval("( 10 20 ) ( 2 3 ) /"));
            Assert.AreEqual(Complex.Pow(new Complex(10, 20), new Complex(3, 4)), calc.Eval("( 10 20 ) ( 3 4 ) ^"));
        }

        [Test]
        public void EqualComplexNumbers()
        {
            Assert.IsTrue(calc.Eval("( 10 0 ) 10 =="));
            Assert.IsFalse(calc.Eval("( 10 0 ) 10 !="));
            Assert.IsTrue(calc.Eval("10 ( 10 0 ) =="));
            Assert.IsFalse(calc.Eval("10 ( 10 0 ) !="));
            Assert.IsTrue(calc.Eval("( 10 0 ) ( 10 0 ) =="));
            Assert.IsTrue(calc.Eval("( 10 1 ) ( 10 0 ) !="));
            Assert.IsTrue(calc.Eval("( 10 1 ) ( 10 2 ) !="));
            Assert.IsTrue(calc.Eval("( 10 -5 ) ( 10 -5 ) =="));
        }

        [Test]
        public void ListOperations()
        {
            calc.Eval("[ 1 2 3 'foo' [ 10 ] 'bar' ] 'lst' sto");
            Assert.IsTrue(calc.Eval("'lst' rcl 'foo' contain"));
            Assert.IsFalse(calc.Eval("'lst' rcl '3' contain"));
            Assert.IsTrue(calc.Eval("'lst' rcl 3 contain"));
            Assert.IsTrue(calc.Eval("'lst' rcl [ 10 ] contain"));
            Assert.IsFalse(calc.Eval("'lst' rcl [ 20 ] contain"));
        }

        [Test]
        public void AlgListOperations()
        {
            var result = calc.EvalAlgebraic("contain([1,2,3], 2)");
            Assert.IsTrue(result);
            result = calc.EvalAlgebraic("contain([1,2,3], 'foo')");
            Assert.IsFalse(result);
            result = calc.EvalAlgebraic("contain([1,2,3], 123)");
            Assert.IsFalse(result);
            result = calc.EvalAlgebraic("contain([1,2,3,[10,20]], [10,20])");
            Assert.IsTrue(result);
        }

        [Test]
        public void HeadAndTailList()
        {
            Assert.AreEqual(10, calc.Eval("[ 10 20 30 40 ] head"));
            Assert.AreEqual("foo", calc.Eval("[ 'foo' 'bar' ] head"));
            Assert.AreEqual(ListItem.From("foo", "bar"), calc.Eval("[ [ 'foo' 'bar'] [ 10 20 ] ] head"));

            Assert.AreEqual(ListItem.From(20, 30, 40), calc.Eval("[ 10 20 30 40 ] tail"));
            Assert.AreEqual(ListItem.From("bar"), calc.Eval("[ 'foo' 'bar' ] tail"));
            Assert.AreEqual(ListItem.From(ListItem.From(10, 20)), calc.Eval("[ [ 'foo' 'bar'] [ 10 20 ] ] tail"));

            Assert.AreEqual(new ListItem(), calc.Eval("[ 10 ] tail"));
            Assert.AreEqual(new ListItem(), calc.Eval("[ ] tail"));

            Assert.Throws<RPNArgumentException>(() => calc.Eval("[ ] head"));
        }

        [Test]
        public void HeadAndTailString()
        {
            Assert.AreEqual("s", calc.Eval("'string' head"));
            Assert.AreEqual("tring", calc.Eval("'string' tail"));

            Assert.AreEqual("", calc.Eval("'f' tail"));
            Assert.AreEqual("", calc.Eval("'' tail"));

            Assert.Throws<RPNArgumentException>(() => calc.Eval("'' head"));
        }

        [Test]
        public void EvalMultipleAlgExpressions()
        {
            Assert.AreEqual(5, calc.EvalAlgebraic("sto(3,'a') sto(4,'b') sqrt(a^2+b^2)"));
        }

        [Test]
        public void InlineAlgExpressionWithStringValue()
        {
            Assert.IsTrue(calc.Eval(@"'sto(3,\'a\')' eval a a * 9 =="));
            Assert.DoesNotThrow(() => calc.Eval(@"'\'foo' head"));
        }

        [Test]
        public void NamesWithDots()
        {
            Assert.DoesNotThrow(() => calc.Eval("123.456 'item.id' sto 'item.id' rcl 456.789 'foo.bar.baz' 3 >list"));
            Assert.AreEqual(123.456, calc.Eval("'item.id' rcl"));
            calc.ClearStack();
            Assert.AreEqual(123.456, calc.Eval("item.id"));
        }

        [Test]
        public void NamesWithDotsInAlgebraic()
        {
            Assert.DoesNotThrow(() => calc.EvalAlgebraic("sto(123.456,'item.id')"));
            Assert.DoesNotThrow(() => calc.EvalAlgebraic("sto(123.456, 'item.id') rcl('item.id')"));
            Assert.AreEqual(123.456, calc.EvalAlgebraic("rcl('item.id')"));
            calc.ClearStack();
            Assert.AreEqual(123.456, calc.EvalAlgebraic("item.id"));
        }

        [Test]
        public void Rounding()
        {
            Assert.AreEqual(-2, calc.Eval("-2.49999 0 rnd"));
            Assert.AreEqual(-2, calc.Eval("-2 0 rnd"));
            Assert.AreEqual(-2, calc.Eval("-1.5 0 rnd"));
            Assert.AreEqual(2, calc.Eval("1.5 0 rnd"));
            Assert.AreEqual(2, calc.Eval("2 0 rnd"));
            Assert.AreEqual(2, calc.Eval("2.49999 0 rnd"));

            Assert.AreEqual(-2.5, calc.Eval("-2.49999 2 rnd"));
            Assert.AreEqual(-2, calc.Eval("-2 2 rnd"));
            Assert.AreEqual(-1.5, calc.Eval("-1.5 2 rnd"));
            Assert.AreEqual(1.5, calc.Eval("1.5 2 rnd"));
            Assert.AreEqual(2, calc.Eval("2 2 rnd"));
            Assert.AreEqual(2.5, calc.Eval("2.49999 2 rnd"));
        }

        [Test]
        public void RoundingMacro()
        {
            Assert.AreEqual(-2, calc.Eval("-2.49999 rnd0"));
            Assert.AreEqual(-2, calc.Eval("-2 rnd0"));
            Assert.AreEqual(-2, calc.Eval("-1.5 rnd0"));
            Assert.AreEqual(2, calc.Eval("1.5 rnd0"));
            Assert.AreEqual(2, calc.Eval("2 rnd0"));
            Assert.AreEqual(2, calc.Eval("2.49999 rnd0"));
        }

        [Test]
        public void IndexOfItemInArrayAndString()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.AreEqual(0, calc.Eval("[ 10 20 30 ] 10 pos"));
            Assert.AreEqual(1, calc.Eval("[ 10 20 30 ] 20 pos"));
            Assert.AreEqual(2, calc.Eval("[ 10 20 30 ] 30 pos"));
            Assert.AreEqual(-1, calc.Eval("[ 10 20 30 ] 40 pos"));
            Assert.AreEqual(4, calc.Eval("[ 10 20 30 '40' 40 ] 40 pos"));
            Assert.AreEqual(3, calc.Eval("[ 10 20 30 '40' 40 ] '40' pos"));

            Assert.AreEqual(0, calc.Eval("'foobar123' 'f' pos"));
            Assert.AreEqual(1, calc.Eval("'foobar123' 'o' pos"));
            Assert.AreEqual(2, calc.Eval("'foobar123' 'ob' pos"));
            Assert.AreEqual(7, calc.Eval("'foobar123' '2' pos"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("'foobar123' 2 pos"));
        }

        [Test]
        public void AccessingIndexes()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.AreEqual(10, calc.Eval("[ 10 20 30 ] 0 get"));
            Assert.AreEqual(20, calc.Eval("[ 10 20 30 ] 1 get"));
            Assert.AreEqual(30, calc.Eval("[ 10 20 30 ] 2 get"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("[ 10 20 30 ] 3 get"));

            Assert.AreEqual("f", calc.Eval("'foobar' 0 get"));
            Assert.AreEqual("o", calc.Eval("'foobar' 1 get"));
            Assert.AreEqual("b", calc.Eval("'foobar' 3 get"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("'foobar' 99 get"));
        }

        [Test]
        public void AutoIndexing()
        {
            calc = new RPN(new RPN.Options { AlwaysClearStack = true });
            Assert.AreEqual(10, calc.Eval("[ 10 20 30 ] 0 geti"));
            CollectionAssert.AreEqual(new AItem[] { 10, 1, ListItem.From(10, 20, 30) }, calc.StackView);
            Assert.AreEqual(20, calc.Eval("[ 10 20 30 ] 1 geti"));
            CollectionAssert.AreEqual(new AItem[] { 20, 2, ListItem.From(10, 20, 30) }, calc.StackView);
            Assert.AreEqual(30, calc.Eval("[ 10 20 30 ] 2 geti"));
            CollectionAssert.AreEqual(new AItem[] { 30, 0, ListItem.From(10, 20, 30) }, calc.StackView);
            Assert.Throws<RPNArgumentException>(() => calc.Eval("[ 10 20 30 ] 3 geti"));

            Assert.AreEqual("f", calc.Eval("'foobar' 0 geti"));
            CollectionAssert.AreEqual(new AItem[] { "f", 1, "foobar" }, calc.StackView);
            Assert.AreEqual("o", calc.Eval("'foobar' 1 geti"));
            CollectionAssert.AreEqual(new AItem[] { "o", 2, "foobar" }, calc.StackView);
            Assert.AreEqual("b", calc.Eval("'foobar' 3 geti"));
            CollectionAssert.AreEqual(new AItem[] { "b", 4, "foobar" }, calc.StackView);
            Assert.Throws<RPNArgumentException>(() => calc.Eval("'foobar' 99 geti"));
        }

        [Test]
        public void LocalNames()
        {
            calc.Eval("1234 'global_name' sto");
            calc.SetNameValue("foo", new AItem[] { new NameItem("global_name") });
            Assert.AreEqual(1234, calc.Eval("{ 'global_name' rcl } eval"));
            Assert.AreEqual("foo", calc.Eval("{ 'foo' 'global_name' sto global_name } eval"));
            Assert.AreEqual(1234, calc.Eval("{ 'global_name' rcl } eval"));
            Assert.AreEqual(2468, calc.Eval("global_name 2 * 'global_name' sto global_name"));
            Assert.AreEqual(2468, calc.Eval("{ global_name } eval"));
            Assert.AreEqual(2468, calc.Eval("foo"));
            Assert.AreEqual(2468, calc.Eval("{ foo } eval"));
            calc.Eval("clst { 0 'i' sto 'i' 10 1 { i dup * } loop } eval");
            CollectionAssert.AreEqual(Enumerable.Range(0, 10).Select(x => x * x).Reverse().ToArray(), calc.StackView);
            CollectionAssert.DoesNotContain(calc.GlobalNames.Keys, "i");
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'i' rcl"));
        }

        [Test]
        public void GetGlobalValue()
        {
            calc.Eval("123 'foo' sto { 666 'foo' sto 'foo' rcl 'foo' grcl } eval");
            CollectionAssert.AreEqual(new[] { 123, 666 }, calc.StackView);
        }

        [Test]
        public void SetGlobalValue()
        {
            Assert.AreEqual(123, calc.Eval("123 'foo' sto { 666 'foo' lsto } eval 'foo' rcl"));
            Assert.AreEqual(666, calc.Eval("{ 666 'foo' gsto } eval 'foo' rcl"));
        }

        [Test]
        public void ItemToString()
        {
            Assert.AreEqual("123", calc.Eval("123 >str"));
            Assert.AreEqual("1.23", calc.Eval("1.23 >str"));
            Assert.AreEqual("123", calc.Eval("'123' >str"));
            Assert.AreEqual("foo", calc.Eval("'foo' >str"));
            Assert.AreEqual("[ 123 'foo' ]", calc.Eval(" [   123        'foo' ] >str"));
            Assert.AreEqual("{ 10 20 * }", calc.Eval(" {\n10   20\n*\n} >str"));
            Assert.AreEqual("( -3 3.1415 )", calc.Eval("   (  -3  31415e-4 ) >str"));
            Assert.AreEqual("don't", calc.Eval(@"'don\'t' >str"));
            Assert.AreEqual("don't", calc.Eval(@"'don\'t' >str >str"));
        }

        [Test]
        public void StringToItem()
        {
            Assert.AreEqual(123, calc.Eval("'123' str>"));
            Assert.AreEqual(1.23, calc.Eval("'1.23' str>"));
            Assert.AreEqual("123", calc.Eval(@"'\'123\'' str>"));
            Assert.AreEqual(new ComplexNumberItem(1.23, -3.21), calc.Eval("'( 1.23 -3.21 )' str>"));
            Assert.AreEqual(ListItem.From(123, 456, "foo"), calc.Eval(@"'[ 123 456 \'foo\' ]' str>"));
            Assert.AreEqual(ProgramItem.From(10, 20, new NameItem("+")), calc.Eval("'{ 10 20 + }' str>"));
        }

        [Test]
        public void StringToEvalExpression()
        {
            calc.Eval("'123 345' str>");
            Assert.AreEqual(new[] { 345, 123 }, calc.StackView);
            Assert.AreEqual(12.3, calc.Eval("'1.23 10 *' str>"));
            Assert.AreEqual("123foo", calc.Eval(@"'\'123\' \'foo\' +' str>"));
            Assert.AreEqual(new ComplexNumberItem(2.46, -6.42), calc.Eval("'( 1.23 -3.21 ) 2 *' str>"));
            calc.ClearStack();
            calc.Eval(@"'[ 123 456 \'foo\' ]' str> list>");
            Assert.AreEqual(new AItem[] { 3, "foo", 456, 123 }, calc.StackView);
            Assert.AreEqual(30, calc.Eval("'{ 10 20 + } eval' str>"));
        }

        [Test]
        public void MultiLayerLocalNames()
        {
            calc.Eval(@"
                111 'var' sto
                {
                    222 'var' lsto
                    0 'i' lsto
                    _DBG_
                    'i' 5 1 {
                        _DBG_
                        var i + 'var' sto
                    } loop
                    _DBG_
                    var
                    'var' grcl
                } eval");
            CollectionAssert.AreEqual(new[] { 111, 222 + 0 + 1 + 2 + 3 + 4 }, calc.StackView);
            Assert.Throws<RPNUndefinedNameException>(() => calc.Eval("'i' rcl"));
        }

        [Test]
        public void ReadFlags()
        {
            calc.Eval("_stop_loop FS? _stop_loop FC?");
            CollectionAssert.AreEqual(new[] { 1, 0 }, calc.StackView);
        }

        [Test]
        public void SetFlags()
        {
            calc.Eval("0 SF 0 FS? 1 FS?");
            CollectionAssert.AreEqual(new[] { 0, 1 }, calc.StackView);
        }

        [Test]
        public void FailDestroyFlags()
        {
            Assert.Throws<RPNArgumentException>(() => calc.Eval("'_FLAGS' clv"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("1234 '_FLAGS' sto"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("1234 '_FLAGS' lsto"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("1234 '_FLAGS' gsto"));
        }

        [Test, Timeout(100)]
        public void BreakWhile()
        {
            calc.Eval("{ 1 } { break 123 } while");
            CollectionAssert.IsEmpty(calc.StackView);
        }

        [Test]
        public void BreakLoop()
        {
            calc.Eval("123 'i' sto 'i' 999 1 { i break i } loop");
            CollectionAssert.AreEqual(new[] { 123 }, calc.StackView);
        }

        [Test]
        public void EndProgram()
        {
            Assert.AreEqual(123, calc.Eval("{ 123 end 456 } eval"));
            Assert.AreEqual(666, calc.Eval("666 end 999"));
        }

        [Test]
        public void RunMacroWithoutCreatingNewScope()
        {
            calc.SetNameValue("macro", new AItem[] { 999, new NameItem("END") });
            Assert.AreEqual(999, calc.Eval("{ macro 123 } eval"));
            calc.ClearStack();
            calc.Eval("{ 0 { macro 123 } } eval");
            CollectionAssert.DoesNotContain(calc.StackView, 123);
            CollectionAssert.DoesNotContain(calc.StackView, 999);
            calc.ClearStack();
            calc.Eval("{ 0 { macro 123 } eval } eval");
            CollectionAssert.DoesNotContain(calc.StackView, 123);
            CollectionAssert.Contains(calc.StackView, 999);
            calc.ClearStack();
            Assert.AreEqual(999, calc.Eval("macro 123"));
            calc.ClearStack();
        }

        [Test]
        public void TuringCompletenessBrainfuckTest()
        {
            var bfOutput = new StringBuilder();
            calc.SetNameValue("ORD", st => st.Func(x => (int)x.GetString()[0]));
            calc.SetNameValue("_MSG", st =>
            {
                string r = st.Pop() switch
                {
                    RealNumberItem real => ((char)(int)real.value).ToString(),
                    AItem other => other.AsString() + "\n",
                };
                //Console.Write(r);
                bfOutput.Append(r);
            });
            calc.SetNameValue("bf_input", string.Empty);
            const string bfCodeImplement = @"
{
  0 'i' lsto
  'i' swap 1 { 0 } loop
  'i' lrcl
  >list
  'i' clv
} 'zero_list' sto
{
  dup size 'max_pc' lsto
  0 'pc' lsto
  0 'mc' lsto
  0 'bc' lsto
  16 zero_list 'mem' lsto
  { pc max_pc < }
  {
    dup pc get
    dup '>' == { 'mc' ++ } ift
    dup '<' == { 'mc' -- } ift
    dup '+' == { mem mc mem mc get 1 + put drop } ift
    dup '-' == { mem mc mem mc get 1 - put drop } ift
    dup '.' == { mem mc get _msg } ift
    dup ',' == { mem mc bf_input head bf_input tail 'bf_input' sto ord put drop } ift
    dup '[' == mem mc get 0 == *
    {
      'bc' ++
      { bc 0 > pc max_pc >= * }
      {
        'pc' ++
        dup
        pc get
        dup '[' == { 'bc' ++ break } ift
        dup ']' == { 'bc' -- break } ift
        drop
      } while
      0
    } ift
    dup ']' == mem mc get *
    {
      'bc' ++
      drop
      { bc 0 > pc 0 > * }
      {
        'pc' --
        dup
        pc get
        dup '[' == { 'bc' -- break } ift
        dup ']' == { 'bc' ++ break } ift
        drop
      } while
      0
    } ift
    drop
    'pc' ++
  } while
  mem
} 'bf' sto
";
            const string bfCodeRun = @"
'output: ' _msg
'++++++++++[>+++++++>++++++++++>+++>+<<<<-]>++.>+.+++++++..+++.>++.<<+++++++++++++++.>.+++.------.--------.>+.>.'
bf
";
            Assert.DoesNotThrow(() => calc.Eval(bfCodeImplement));
            Assert.DoesNotThrow(() => calc.Eval(bfCodeRun));
            Assert.AreEqual("output: \nHello World!\n", bfOutput.ToString());
        }

        [Test]
        public void OverrideProtectedFunctionInLocalScope()
        {
            calc.SetNameValue("func", st => st.Push(123), true);
            calc.Eval("{ func 999 'func' lsto func } eval func");
            CollectionAssert.AreEqual(new[] { 123, 999, 123 }, calc.StackView);
            Assert.Throws<RPNArgumentException>(() => calc.Eval("999 'func' sto"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("999 'func' lsto"));
            Assert.Throws<RPNArgumentException>(() => calc.Eval("999 'func' gsto"));
        }
    }
}
