using System;
using System.Runtime.Serialization;

// ReSharper disable InconsistentNaming
namespace RPNCalc
{
    /// <summary>
    /// Base RPN exception.
    /// </summary>
    [Serializable]
    public class RPNException : Exception
    {
        public RPNException() { }
        public RPNException(string message) : base(message) { }
        public RPNException(string message, Exception inner) : base(message, inner) { }
        protected RPNException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// When function or variable is not defined.
    /// </summary>
    [Serializable]
    public class RPNUndefinedNameException : RPNException
    {
        public RPNUndefinedNameException() { }
        public RPNUndefinedNameException(string message) : base(message) { }
        public RPNUndefinedNameException(string message, Exception inner) : base(message, inner) { }
        protected RPNUndefinedNameException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// When there is not enough values on stack to evaluate function.
    /// </summary>
    [Serializable]
    public class RPNEmptyStackException : RPNException
    {
        public RPNEmptyStackException() { }
        public RPNEmptyStackException(string message) : base(message) { }
        public RPNEmptyStackException(string message, Exception inner) : base(message, inner) { }
        protected RPNEmptyStackException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// When user defined function throw exception.
    /// </summary>
    [Serializable]
    public class RPNFunctionException : RPNException
    {
        public RPNFunctionException() { }
        public RPNFunctionException(string message) : base(message) { }
        public RPNFunctionException(string message, Exception inner) : base(message, inner) { }
        protected RPNFunctionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// When function argument has unexpected type or value.
    /// </summary>
    [Serializable]
    public class RPNArgumentException : RPNException
    {
        public RPNArgumentException() { }
        public RPNArgumentException(string message) : base(message) { }
        public RPNArgumentException(string message, Exception inner) : base(message, inner) { }
        protected RPNArgumentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
