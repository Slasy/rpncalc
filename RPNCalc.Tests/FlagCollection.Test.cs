using NUnit.Framework;
using RPNCalc.Flags;

namespace RPNCalc.Tests
{
    public class FlagCollectionTest
    {
        private FlagCollection flags;

        [SetUp]
        public void Setup()
        {
            flags = new();
        }

        [Test]
        public void AddIndexedFlags()
        {
            Assert.AreEqual(3, flags.AddIndexedFlags(4));
            Assert.AreEqual(4, flags.Count);
            Assert.DoesNotThrow(() => flags[0] = true);
            Assert.DoesNotThrow(() => flags[1] = false);
            Assert.DoesNotThrow(() => flags[2] = false);
            Assert.DoesNotThrow(() => flags[3] = true);
            Assert.Throws<FlagException>(() => flags[4] = true);
            Assert.True(flags[0]);
            Assert.False(flags[1]);
            Assert.False(flags[2]);
            Assert.True(flags[3]);
        }

        [Test]
        public void AddNamedFlags()
        {
            Assert.AreEqual(2, flags.AddNamedFlags(new[] {"foo", "bar", "baz"}));
            Assert.AreEqual(3, flags.Count);
            Assert.DoesNotThrow(() => flags["foo"] = true);
            Assert.DoesNotThrow(() => flags["bar"] = false);
            Assert.DoesNotThrow(() => flags["baz"] = true);
            Assert.Throws<FlagException>(() => flags["ipsum"] = false);
            Assert.True(flags["foo"]);
            Assert.False(flags["bar"]);
            Assert.True(flags["baz"]);
            Assert.True(flags[0]);
            Assert.False(flags[1]);
            Assert.True(flags[2]);
        }

        [Test]
        public void AddIndexedAndNamedFlags()
        {
            Assert.AreEqual(1, flags.AddIndexedFlags(2));
            Assert.AreEqual(3, flags.AddNamedFlags(new[] {"foo", "bar"}));
            Assert.AreEqual(4, flags.AddIndexedFlags(1));
            Assert.AreEqual(5, flags.AddNamedFlags(new[] {"baz"}));
            Assert.AreEqual(6, flags.Count);
            Assert.DoesNotThrow(() => flags[1] = true);
            Assert.DoesNotThrow(() => flags[2] = true);
            Assert.True(flags[1]);
            Assert.True(flags["foo"]);
        }

        [Test]
        public void GetFlagNames()
        {
            flags.AddNamedFlags(new[] {"foo", "bar"});
            flags.AddIndexedFlags(4);
            Assert.True(flags.TryGetFlagName(1, out var name));
            Assert.AreEqual("bar", name);
            Assert.False(flags.TryGetFlagName(2, out _));
            Assert.False(flags.TryGetFlagName(3, out _));
            Assert.False(flags.TryGetFlagName(42, out _));
        }
    }
}
