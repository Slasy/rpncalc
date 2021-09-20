using System;
using NUnit.Framework;
using RPNCalc.Tools;

namespace RPNCalc.Tests
{
    public class RPNToolsTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TokenizeSimpleExpression()
        {
            CollectionAssert.AreEquivalent(new[] { "10", "+", "20", "-", "30", "*", "40", "^", "50" }, RPNTools.Tokenize("10 + 20 - 30 * 40 ^ 50"));
        }

        [Test]
        public void PostFixSimpleExpression()
        {
            var tokens = RPNTools.Tokenize("10 + 20 - 30 * 40 ^ 50");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "10", "20", "30", "40", "50", "^", "*", "-", "+" }, tokens);
        }

        [Test]
        public void TokenizeSquashedExpression()
        {
            CollectionAssert.AreEquivalent(new[] { "10", "+", "20", "-", "30", "*", "40", "^", "50" }, RPNTools.Tokenize("10+20-30*40^50"));
        }

        [Test]
        public void TokenizeFloatingNumbers()
        {
            CollectionAssert.AreEquivalent(new[] { "0.3", "+", "3.14159", "*", "9.0", "-", "666.666" }, RPNTools.Tokenize("0.3+3.14159*9.0-666.666"));
        }

        [Test]
        public void TokenizeBrackets()
        {
            CollectionAssert.AreEquivalent(new[] { "3", "*", "(", "1", "+", "2", ")" }, RPNTools.Tokenize("3*(1+2)"));
        }

        [Test]
        public void TokenizeMultipleBrackets()
        {
            CollectionAssert.AreEquivalent(new[] { "3", "*", "(", "1", "+", "2", "-", "(", "10", "+", "20", "^", "2", ")", ")" }, RPNTools.Tokenize("3*(1+2-(10+20^2))"));
        }

        [Test]
        public void TokenizeFunctionCall()
        {
            CollectionAssert.AreEquivalent(new[] { "1", "10", "Random" }, RPNTools.Tokenize("Random(1,10)"));
        }

        [Test]
        public void PostFixFunctionCall1()
        {
            var tokens = RPNTools.Tokenize("Random(1,10)");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "1", "10", "Random" }, tokens);
        }

        [Test]
        public void PostFixFunctionCall2()
        {
            var tokens = RPNTools.Tokenize("Random((1),(10))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "1", "10", "Random" }, tokens);
        }

        [Test]
        public void TokenizeFunctionCall2()
        {
            CollectionAssert.AreEquivalent(new[] { "1", "+", "1", "10", "Random", "*", "3" }, RPNTools.Tokenize("1 + Random(1, 10) * 3"));
        }

        [Test]
        public void PostFixFunctionCall3()
        {
            var tokens = RPNTools.Tokenize("1 + Random(1, 10) * 3");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "1", "1", "10", "Random", "+", "3", "*" }, tokens);
        }

        [Test]
        public void TokenizeFunctionCallWithInnerFormula()
        {
            CollectionAssert.AreEquivalent(new[] { "Random", "1", "(", "10", "+", "x", "*", "2", "^", "2", ")" }, RPNTools.Tokenize("Random(1,(10+x*2^2))"));
        }

        [Test]
        public void PostFixFunctionCallWithInnerFormula()
        {
            var tokens = RPNTools.Tokenize("Random(1,(10+x*2^2))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "1", "10", "x", "2", "2", "^", "*", "+", "Random" }, tokens);
        }

        [Test]
        public void PostFixFunctionCallInsideFunctionCall()
        {
            var tokens = RPNTools.Tokenize("Random(42,Sin(x))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "42", "x", "Sin", "Random" }, tokens);
        }

        [Test]
        public void TokenizeNegativeNumbers1()
        {
            CollectionAssert.AreEquivalent(new[] { "-1.5", "-", "2", "+", "3" }, RPNTools.Tokenize("(-1.5)-2+3"));
        }

        [Test]
        public void TokenizeNegativeNumbers2()
        {
            CollectionAssert.AreEquivalent(new[] { "(", "1", "-", "2", ")", "*", "-1.0", "+", "3" }, RPNTools.Tokenize("(1-2)*(-1.0)+3"));
        }

        [Test]
        public void TokenizeNegativeNumbers3()
        {
            CollectionAssert.AreEquivalent(new[] { "-1", "-", "2", "+", "-3.1415" }, RPNTools.Tokenize("(-1)-2+(-3.1415)"));
        }

        [Test]
        public void PostFixNegativeNumbers()
        {
            var tokens = RPNTools.Tokenize("Random(42,Sin(x))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "42", "x", "Sin", "Random" }, tokens);
        }

        [Test]
        public void PostFixPower()
        {
            var tokens = RPNTools.Tokenize("10^20^x");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "10", "20", "x", "^", "^" }, tokens);
        }

        [Test]
        public void PostFixPower2()
        {
            var tokens = RPNTools.Tokenize("10^20^x^function()^y");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "10", "20", "x", "function", "y", "^", "^", "^", "^" }, tokens);
        }

        [Test]
        public void PostFixPower3()
        {
            var tokens = RPNTools.Tokenize("10^20^x^function(2^2,3^3)^y");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "10", "20", "x", "2", "2", "^", "3", "3", "^", "function", "y", "^", "^", "^", "^" }, tokens);
        }

        [Test]
        public void PostFixPower4()
        {
            var tokens = RPNTools.Tokenize("x^(y^z^a^(10^9))");
            tokens = RPNTools.InfixToPostfix(tokens);
            CollectionAssert.AreEquivalent(new[] { "x", "y", "z", "a", "10", "9", "^", "^", "^", "^", "^" }, tokens);
        }

        [Test]
        public void IgnoreToManyClosingBrackets1()
        {
            var tokens = RPNTools.Tokenize("10*(3+5)))");
            tokens = RPNTools.InfixToPostfix(tokens, false);
            CollectionAssert.AreEquivalent(new[] { "10", "3", "5", "+", "*" }, tokens);
        }

        [Test]
        public void IgnoreToManyClosingBrackets2()
        {
            var tokens = RPNTools.Tokenize("10*(3+5)))^20");
            tokens = RPNTools.InfixToPostfix(tokens, false);
            CollectionAssert.AreEquivalent(new[] { "10", "3", "5", "+", "20", "^", "*" }, tokens);
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
            CollectionAssert.AreEquivalent(new[] { "10", "3", "5", "+", "20", "^", "*" }, tokens);
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
            CollectionAssert.AreEquivalent(new[] { "1", "+", "foo_bar" }, tokens);
        }

        [Test]
        public void VariableWithUnderscoreEnd()
        {
            var tokens = RPNTools.Tokenize("1+foobar_");
            CollectionAssert.AreEquivalent(new[] { "1", "+", "foobar_" }, tokens);
        }

        [Test]
        public void VariableWithUnderscoreStart()
        {
            var tokens = RPNTools.Tokenize("1+_foobar");
            CollectionAssert.AreEquivalent(new[] { "1", "+", "_foobar" }, tokens);
        }

        [Test]
        public void VariableWithUnderscoreAll()
        {
            var tokens = RPNTools.Tokenize("1+_foo_bar_");
            CollectionAssert.AreEquivalent(new[] { "1", "+", "_foo_bar_" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreMiddle()
        {
            var tokens = RPNTools.Tokenize("1+foo_bar(123)");
            CollectionAssert.AreEquivalent(new[] { "1", "+", "123", "foo_bar" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreEnd()
        {
            var tokens = RPNTools.Tokenize("1+foobar_(123)");
            CollectionAssert.AreEquivalent(new[] { "1", "+", "123", "foobar_" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreStart()
        {
            var tokens = RPNTools.Tokenize("1+_foobar(123)");
            CollectionAssert.AreEquivalent(new[] { "1", "+", "123", "_foobar" }, tokens);
        }

        [Test]
        public void FunctionWithUnderscoreAll()
        {
            var tokens = RPNTools.Tokenize("1+_foo_bar_(123)");
            CollectionAssert.AreEquivalent(new[] { "1", "+", "123", "_foo_bar_" }, tokens);
        }
    }
}
