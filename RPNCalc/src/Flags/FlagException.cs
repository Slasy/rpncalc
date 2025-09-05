using System;
using System.Runtime.Serialization;

namespace RPNCalc.Flags
{
    [Serializable]
    public class FlagException : Exception
    {
        public FlagException() { }
        public FlagException(string message) : base(message) { }
        public FlagException(string message, Exception inner) : base(message, inner) { }
    }
}
