using NUnit.Framework;
using RPNCalc.Items;

namespace RPNCalc.Tests
{
    public class StackTest
    {
        private Stack<AItem> stack;

        [SetUp]
        public void Setup()
        {
            stack = new();
        }

        [Test]
        public void CompareItemsAbstract()
        {
            AItem n1 = 1234.5;
            AItem n2 = 9999;
            AItem n3 = 1234.5;
            AItem s1 = "foo";
            AItem s2 = "bar";
            AItem s3 = "foo";
            AItem p1 = ProgramItem.From(new NameItem("foo"));
            AItem p2 = ProgramItem.From(new NameItem("foo"));
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
            RealNumberItem n1 = 1234.5;
            RealNumberItem n2 = 9999;
            RealNumberItem n3 = 1234.5;
            StackStringItem s1 = "foo";
            StackStringItem s2 = "bar";
            StackStringItem s3 = "foo";
            ProgramItem p1 = ProgramItem.From(new NameItem("foo"));
            ProgramItem p2 = ProgramItem.From(new NameItem("foo"));
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
        public void ImplicitListConverts()
        {
            Assert.DoesNotThrow(() => stack.Push(new AItem[] { 10, 20, 30 }));
            Assert.DoesNotThrow(() => stack.Push(new AItem[] { 10, 20, "foo bar", "foo", true, false, ProgramItem.From(new NameItem("foo"), new NameItem("dup"), new NameItem("+")) }));
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual(30, (stack[1] as ListItem).value[2]);
            Assert.AreEqual("foo bar", (stack[0] as ListItem).value[2]);
            Assert.AreEqual(false, (bool)(stack[0] as ListItem).value[5]);
        }

        [Test]
        public void EqualityOfVariables()
        {
            Assert.AreEqual(new NameItem("foobar"), new NameItem("foobar"));
            Assert.AreNotEqual(new NameItem("foobar"), new StackStringItem("foobar"));
            Assert.AreNotEqual(new StackStringItem("foobar"), new NameItem("foobar"));
        }
    }
}
