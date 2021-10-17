using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RPNCalc.Flags
{
    [Serializable]
    public class FlagException : Exception
    {
        public FlagException() { }
        public FlagException(string message) : base(message) { }
        public FlagException(string message, Exception inner) : base(message, inner) { }
        protected FlagException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class FlagCollection
    {
        private const string UNKNOWN_INDEX = "Unknown flag index ";
        private const string UNKNOWN_NAME = "Unknown flag name ";

        private readonly Dictionary<string, bool> namedFlags = new();
        private readonly SortedList<int, bool> indexedFlags = new();
        private readonly SortedList<int, string> indexToName = new();

        public int Count => indexedFlags.Count + namedFlags.Count;

        public bool this[int index]
        {
            get
            {
                if (indexToName.TryGetValue(index, out string key)) return namedFlags[key];
                if (indexedFlags.TryGetValue(index, out bool value)) return value;
                throw new FlagException();
            }
            set
            {
                if (indexToName.TryGetValue(index, out string key))
                {
                    namedFlags[key] = value;
                    return;
                }
                else if (indexedFlags.ContainsKey(index))
                {
                    indexedFlags[index] = value;
                    return;
                }
                throw new FlagException(UNKNOWN_INDEX + index);
            }
        }

        public bool this[string name]
        {
            get
            {
                if (namedFlags.TryGetValue(name, out bool flag)) return flag;
                throw new FlagException();
            }
            set
            {
                if (namedFlags.ContainsKey(name))
                {
                    namedFlags[name] = value;
                    return;
                }
                throw new FlagException(UNKNOWN_NAME + name);
            }
        }

        /// <summary>
        /// Adds set of new unnamed flags.
        /// </summary>
        /// <param name="flagCount">how many flags to add</param>
        /// <returns>index of last new flag</returns>
        public int AddIndexedFlags(int flagCount)
        {
            int firstNewIndex = Count;
            for (int i = 0; i < flagCount; i++)
            {
                indexedFlags.Add(firstNewIndex + i, false);
            }
            return Count - 1;
        }

        /// <summary>
        /// Adds set of new named flags
        /// </summary>
        /// <param name="flagNames">flag names in order</param>
        /// <returns>index of last new flag</returns>
        public int AddNamedFlags(IEnumerable<string> flagNames)
        {
            int firstNewIndex = Count;
            foreach (string name in flagNames)
            {
                AddNamedFlag(name);
            }
            return Count - 1;
        }

        public void AddIndexedFlag()
        {
            indexedFlags.Add(Count, false);
        }

        public void AddNamedFlag(string name)
        {
            if (!namedFlags.ContainsKey(name))
            {
                indexToName.Add(Count, name);
                namedFlags.Add(name, false);
            }
        }

        public bool TryGetFlagName(int index, out string name)
        {
            return indexToName.TryGetValue(index, out name);
        }
    }
}
