using System;
using NUnit.Framework;
using RPNCalc.Tools;

namespace RPNCalc.Tests
{
    public class RPNToolsTest
    {
        private RPN calc;

        [SetUp]
        public void Setup()
        {
            calc = new RPN(true);
        }

        [Test]
        public void TokenizeSimpleExpression()
        {
            CollectionAssert.AreEqual(new[] { "10", "+", "20", "-", "30", "*", "40", "^", "50" }, RPNTools.Tokenize("10 + 20 - 30 * 40 ^ 50"));
        }

        [Test]
        public void PostfixSimpleExpression()
        {
            double expects = 10 + 20 - 30 * Math.Pow(40, 5);
            var tokens = RPNTools.Tokenize("10 + 20 - 30 * 40 ^ 5");
            CollectionAssert.AreEqual(new[] { "10", "+", "20", "-", "30", "*", "40", "^", "5" }, tokens);
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "10", "20", "+", "30", "40", "5", "^", "*", "-" }, tokens);
            Assert.AreEqual(expects, calc.Eval(string.Join(" ", tokens)));
        }

        [Test]
        public void TokenizeSquashedExpression()
        {
            CollectionAssert.AreEqual(new[] { "10", "+", "20", "-", "30", "*", "40", "^", "50" }, RPNTools.Tokenize("10+20-30*40^50"));
        }

        [Test]
        public void TokenizeFloatingNumbers()
        {
            CollectionAssert.AreEqual(new[] { "0.3", "+", "3.14159", "*", "9.0", "-", "666.666" }, RPNTools.Tokenize("0.3+3.14159*9.0-666.666"));
        }

        [Test]
        public void TokenizeBrackets()
        {
            CollectionAssert.AreEqual(new[] { "3", "*", "(", "1", "+", "2", ")" }, RPNTools.Tokenize("3*(1+2)"));
        }

        [Test]
        public void TokenizeMultipleBrackets()
        {
            CollectionAssert.AreEqual(new[] { "3", "*", "(", "1", "+", "2", "-", "(", "10", "+", "20", "^", "2", ")", ")" }, RPNTools.Tokenize("3*(1+2-(10+20^2))"));
        }

        [Test]
        public void TokenizeFunctionCall()
        {
            CollectionAssert.AreEqual(new[] { "(", "(", "1", ")", "(", "10", ")", "Random", ")" }, RPNTools.Tokenize("Random(1,10)"));
        }

        [Test]
        public void PostfixFunctionCall1()
        {
            var tokens = RPNTools.Tokenize("Random(1,10)");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "1", "10", "Random" }, tokens);
        }

        [Test]
        public void PostfixFunctionCall2()
        {
            var tokens = RPNTools.Tokenize("Random((1),(10))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "1", "10", "Random" }, tokens);
        }

        [Test]
        public void TokenizeFunctionCall2()
        {
            CollectionAssert.AreEqual(new[] { "1", "+", "(", "(", "1", ")", "(", "10", ")", "Random", ")", "*", "3" }, RPNTools.Tokenize("1 + Random(1, 10) * 3"));
        }

        [Test]
        public void PostfixFunctionCall3()
        {
            var tokens = RPNTools.Tokenize("1 + Random(1, 10) * 3");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "1", "1", "10", "Random", "3", "*", "+" }, tokens);
        }

        [Test]
        public void TokenizeFunctionCallWithInnerFormula()
        {
            CollectionAssert.AreEqual(
                new[] { "(", "(", "1", ")", "(", "(", "10", "+", "x", "*", "2", "^", "2", ")", ")", "Random", ")" },
                RPNTools.Tokenize("Random(1,(10+x*2^2))"));
        }

        [Test]
        public void PostfixFunctionCallWithInnerFormula()
        {
            var tokens = RPNTools.Tokenize("Random(1,(10+x*2^2))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "1", "10", "x", "2", "2", "^", "*", "+", "Random" }, tokens);
        }

        [Test]
        public void PostfixFunctionCallInsideFunctionCall()
        {
            var tokens = RPNTools.Tokenize("Random(42,Sin(x))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "42", "x", "Sin", "Random" }, tokens);
        }

        [Test]
        public void TokenizeNegativeNumbers1()
        {
            CollectionAssert.AreEqual(new[] { "-1.5", "-", "2", "+", "3" }, RPNTools.Tokenize("(-1.5)-2+3"));
        }

        [Test]
        public void TokenizeNegativeNumbers2()
        {
            CollectionAssert.AreEqual(new[] { "(", "1", "-", "2", ")", "*", "-1.0", "+", "3" }, RPNTools.Tokenize("(1-2)*(-1.0)+3"));
        }

        [Test]
        public void TokenizeNegativeNumbers3()
        {
            CollectionAssert.AreEqual(new[] { "-1", "-", "2", "+", "-3.1415" }, RPNTools.Tokenize("(-1)-2+(-3.1415)"));
        }

        [Test]
        public void PostfixNegativeNumbers()
        {
            var tokens = RPNTools.Tokenize("(-1.5)-2+3");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "1.5", "+-", "2", "-", "3", "+" }, tokens);
        }

        [Test]
        public void PostfixPower()
        {
            var tokens = RPNTools.Tokenize("10^20^x");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "10", "20", "^", "x", "^" }, tokens);
        }

        [Test]
        public void PostfixPower2()
        {
            var tokens = RPNTools.Tokenize("10^20^x^function()^y");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "10", "20", "^", "x", "^", "function", "^", "y", "^" }, tokens);
        }

        [Test]
        public void PostfixPower3()
        {
            var tokens = RPNTools.Tokenize("10^20^x^function(2^2,3^3)^y");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "10", "20", "^", "x", "^", "2", "2", "^", "3", "3", "^", "function", "^", "y", "^" }, tokens);
        }

        [Test]
        public void PostfixPower4()
        {
            var tokens = RPNTools.Tokenize("x^(y^z^a^(10^9))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "x", "y", "z", "^", "a", "^", "10", "9", "^", "^", "^" }, tokens);
        }

        [Test]
        public void IgnoreToManyClosingBrackets1()
        {
            var tokens = RPNTools.Tokenize("10*(3+5)))");
            tokens = RPNTools.InfixToPostfix(tokens, false);
            CollectionAssert.AreEqual(new[] { "10", "3", "5", "+", "*" }, tokens);
        }

        [Test]
        public void IgnoreToManyClosingBrackets2()
        {
            var tokens = RPNTools.Tokenize("10*(3+5)))^20");
            tokens = RPNTools.InfixToPostfix(tokens, false);
            CollectionAssert.AreEqual(new[] { "10", "3", "5", "+", "20", "^", "*" }, tokens);
        }

        [Test]
        public void ExceptionOnTooManyOpeningBrackets1()
        {
            var tokens = RPNTools.Tokenize("10*(((3+5)");
            Assert.Throws<ArgumentException>(() => RPNTools.InfixToPostfix(tokens));
        }

        [Test]
        public void ExceptionOnTooManyOpeningBrackets2()
        {
            var tokens = RPNTools.Tokenize("10*(((3+5)^20");
            Assert.Throws<ArgumentException>(() => RPNTools.InfixToPostfix(tokens));
        }

        [Test]
        public void IgnoreTooManyOpeningBrackets()
        {
            var tokens = RPNTools.Tokenize("10*(((3+5)^20");
            tokens = RPNTools.InfixToPostfix(tokens, false);
            CollectionAssert.AreEqual(new[] { "10", "3", "5", "+", "20", "^", "*" }, tokens);
        }

        [Test]
        public void ExceptionOnTooManyClosingBrackets()
        {
            var tokens = RPNTools.Tokenize("10*(((3+5))))^20)))");
            Assert.Throws<ArgumentException>(() => RPNTools.InfixToPostfix(tokens));
        }

        [Test]
        public void VariableWithUnderscoreMiddle()
        {
            var tokens = RPNTools.Tokenize("1+foo_bar");
            CollectionAssert.AreEqual(new[] { "1", "+", "foo_bar" }, tokens);
        }

        [Test]
        public void VariableWithUnderscoreEnd()
        {
            var tokens = RPNTools.Tokenize("1+foobar_");
            CollectionAssert.AreEqual(new[] { "1", "+", "foobar_" }, tokens);
        }

        [Test]
        public void VariableWithUnderscoreStart()
        {
            var tokens = RPNTools.Tokenize("1+_foobar");
            CollectionAssert.AreEqual(new[] { "1", "+", "_foobar" }, tokens);
        }

        [Test]
        public void VariableWithUnderscoreAll()
        {
            var tokens = RPNTools.Tokenize("1+_foo_bar_");
            CollectionAssert.AreEqual(new[] { "1", "+", "_foo_bar_" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreMiddle()
        {
            var tokens = RPNTools.Tokenize("1+foo_bar(123)");
            CollectionAssert.AreEqual(new[] { "1", "+", "(", "(", "123", ")", "foo_bar", ")" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreEnd()
        {
            var tokens = RPNTools.Tokenize("1+foobar_(123)");
            CollectionAssert.AreEqual(new[] { "1", "+", "(", "(", "123", ")", "foobar_", ")" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreStart()
        {
            var tokens = RPNTools.Tokenize("1+_foobar(123)");
            CollectionAssert.AreEqual(new[] { "1", "+", "(", "(", "123", ")", "_foobar", ")" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreAll()
        {
            var tokens = RPNTools.Tokenize("1+_foo_bar_(123)");
            CollectionAssert.AreEqual(new[] { "1", "+", "(", "(", "123", ")", "_foo_bar_", ")" }, tokens);
        }

        [Test]
        public void ComplexFunction()
        {
            var tokens = RPNTools.Tokenize("42^Random(foo, bar, baz, ((lorem+ipsum)*10), 1* 20) ^ 30+(1+2)");
            CollectionAssert.AreEqual(
                new[] { "42", "^",
                    "(", "(", "foo", ")", "(", "bar", ")", "(", "baz", ")",
                    "(", "(", "(", "lorem", "+", "ipsum", ")", "*", "10", ")", ")", "(", "1", "*", "20", ")", "Random", ")",
                    "^", "30", "+", "(", "1", "+", "2", ")" },
                tokens);
        }

        [Test]
        public void NegativeVariable1()
        {
            var tokens = RPNTools.Tokenize("(-b)");
            CollectionAssert.AreEqual(new[] { "-b" }, tokens);
        }

        [Test]
        public void NegativeVariable2()
        {
            var tokens = RPNTools.Tokenize("(-b)+2");
            CollectionAssert.AreEqual(new[] { "-b", "+", "2" }, tokens);
        }

        [Test]
        public void NegativeVariable3()
        {
            var tokens = RPNTools.Tokenize("(-2)-b");
            CollectionAssert.AreEqual(new[] { "-2", "-", "b", }, tokens);
        }

        [Test]
        public void NegativeVariable4()
        {
            var tokens = RPNTools.Tokenize("((-b)+sqrt(b^2-4*a*c))/2*a");
            CollectionAssert.AreEqual(new[] { "(", "-b", "+", "(", "(", "b", "^", "2", "-", "4", "*", "a", "*", "c", ")", "sqrt", ")", ")", "/", "2", "*", "a" }, tokens);
        }

        [Test]
        public void NegativeFunction()
        {
            var tokens = RPNTools.Tokenize("2*(-function())");
            CollectionAssert.AreEqual(new[] { "2", "*", "(", "(", ")", "-function", ")" }, tokens);
        }

        [Test]
        public void NegativeFunctionToPostfix()
        {
            var tokens = RPNTools.Tokenize("2*(-function())");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "2", "function", "+-", "*" }, tokens);
        }

        [Test]
        public void TokenizeNumberWithExponent()
        {
            var tokens = RPNTools.Tokenize("1.4e3 + 23e-3 + (-1e2) + (-2.2e-2)");
            CollectionAssert.AreEqual(new[] { "1.4e3", "+", "23e-3", "+", "-1e2", "+", "-2.2e-2" }, tokens);
        }

        [Test]
        public void NumberWithExponentToPostfix()
        {
            var tokens = RPNTools.Tokenize("1.4e3 + 23e-3 + (-1e2) + (-2.2e-2)");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEqual(new[] { "1.4e3", "23e-3", "+", "1e2", "+-", "+", "2.2e-2", "+-", "+" }, tokens);
        }
    }
}
