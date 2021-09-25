using NUnit.Framework;
using RPNCalc.Extensions;

namespace RPNCalc.Tests
{
    public class StackTest
    {
        private Stack<AStackItem> stack;

        [SetUp]
        public void Setup()
        {
            stack = new();
        }

        [Test]
        public void CompareItemsAbstract()
        {
            AStackItem n1 = 1234.5;
            AStackItem n2 = 9999;
            AStackItem n3 = 1234.5;
            AStackItem s1 = "foo";
            AStackItem s2 = "bar";
            AStackItem s3 = "foo";
            AStackItem p1 = new StackProgram("foo");
            AStackItem p2 = new StackProgram("foo");
            Assert.True(n1 != n2);
            Assert.False(n1 == n2);
            Assert.True(n1 == n3);
            Assert.AreNotEqual(n1, n2);
            Assert.AreEqual(n1, n3);
            Assert.True(s1 != s2);
            Assert.False(s1 == s2);
            Assert.True(s1 == s3);
            Assert.AreNotEqual(s1, s2);
            Assert.AreEqual(s1, s3);
            Assert.True(n1 < n2);
            Assert.True(n1 <= n2);
            Assert.False(n1 > n2);
            Assert.False(n1 >= n2);
            Assert.False(s1 == p1);
            Assert.AreNotEqual(s1, p1);
            Assert.AreEqual(p1, p2);
        }

        [Test]
        public void CompareItems()
        {
            StackNumber n1 = 1234.5;
            StackNumber n2 = 9999;
            StackNumber n3 = 1234.5;
            StackString s1 = "foo";
            StackString s2 = "bar";
            StackString s3 = "foo";
            StackProgram p1 = new StackProgram("foo");
            StackProgram p2 = new StackProgram("foo");
            Assert.True(n1 != n2);
            Assert.False(n1 == n2);
            Assert.True(n1 == n3);
            Assert.AreNotEqual(n1, n2);
            Assert.AreEqual(n1, n3);
            Assert.True(s1 != s2);
            Assert.False(s1 == s2);
            Assert.True(s1 == s3);
            Assert.AreNotEqual(s1, s2);
            Assert.AreEqual(s1, s3);
            Assert.True(n1 < n2);
            Assert.True(n1 <= n2);
            Assert.False(n1 > n2);
            Assert.False(n1 >= n2);
            Assert.False(s1 == p1);
            Assert.AreNotEqual(s1, p1);
            Assert.AreEqual(p1, p2);
        }
    }
}
