using System.Collections.Generic;

namespace RPNCalc.Flags
{
    public class FlagCollection
    {
        private const string UNKNOWN_INDEX = "Unknown flag index ";
        private const string UNKNOWN_NAME = "Unknown flag name ";

        private readonly Dictionary<string, bool> namedFlags = new();
        private readonly SortedList<int, bool> indexedFlags = new();
        private readonly SortedList<int, string> indexToName = new();

        public int Count => indexedFlags.Count + namedFlags.Count;
        public int NegativeCount { get; private set; }
        public int PositiveCount { get; private set; }

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
        /// <param name="positiveIndexes">select on which end append new flags</param>
        /// <returns>index of last new flag</returns>
        public int AddIndexedFlags(int flagCount, bool positiveIndexes = true)
        {
            for (int i = 0; i < flagCount; i++)
            {
                AddIndexedFlag(positiveIndexes);
            }
            return positiveIndexes ? PositiveCount - 1 : NegativeCount;
        }

        /// <summary>
        /// Adds set of new named flags
        /// </summary>
        /// <param name="flagNames">flag names in order</param>
        /// <param name="positiveIndexes">select on which end append new flags</param>
        /// <returns>index of last new flag</returns>
        public int AddNamedFlags(IEnumerable<string> flagNames, bool positiveIndexes = true)
        {
            int firstNewIndex = Count;
            foreach (string name in flagNames)
            {
                AddNamedFlag(name, positiveIndexes);
            }
            return positiveIndexes ? PositiveCount - 1 : NegativeCount;
        }

        /// <summary>
        /// Add one flag without name
        /// <param name="positiveIndexes">select on which end append new flags</param>
        /// </summary>
        /// <returns>index of new flag</returns>
        public int AddIndexedFlag(bool positiveIndexes = true)
        {
            indexedFlags.Add(positiveIndexes ? PositiveCount++ : --NegativeCount, false);
            return positiveIndexes ? PositiveCount - 1 : NegativeCount;
        }

        /// <summary>
        /// Add one flag with name
        /// <param name="name">flag name</param>
        /// <param name="positiveIndexes">select on which end append new flags</param>
        /// </summary>
        /// <returns>index of new flag</returns>
        public int AddNamedFlag(string name, bool positiveIndexes = true)
        {
            if (!namedFlags.ContainsKey(name))
            {
                indexToName.Add(positiveIndexes ? PositiveCount++ : --NegativeCount, name);
                namedFlags.Add(name, false);
            }
            return positiveIndexes ? PositiveCount - 1 : NegativeCount;
        }

        public bool TryGetFlagName(int index, out string name)
        {
            return indexToName.TryGetValue(index, out name);
        }
    }
}
