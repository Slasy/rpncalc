using System.Collections.Generic;
using System.Linq;

namespace RPNCalc.Flags
{
    public class FlagCollection
    {
        private const string UNKNOWN_INDEX = "Unknown flag index ";
        private const string UNKNOWN_NAME = "Unknown flag name ";

        private readonly Dictionary<string, bool> namedFlags = new();
        private readonly Dictionary<int, bool> indexedFlags = new();
        private readonly Dictionary<int, string> indexToName = new();

        private int negativeCount;
        private int positiveCount;
        private readonly bool isCaseSensitive;

        public int Count => indexedFlags.Count + namedFlags.Count;

        public FlagCollection(bool isCaseSensitive)
        {
            this.isCaseSensitive = isCaseSensitive;
        }

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
                name = GetName(name);
                if (namedFlags.TryGetValue(name, out bool flag)) return flag;
                throw new FlagException();
            }
            set
            {
                name = GetName(name);
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
        public int AddIndexedFlags(int flagCount, bool positiveIndexes)
        {
            for (int i = 0; i < flagCount; i++)
            {
                AddIndexedFlag(positiveIndexes);
            }
            return positiveIndexes ? positiveCount - 1 : negativeCount;
        }

        /// <summary>
        /// Adds set of new named flags
        /// </summary>
        /// <param name="flagNames">flag names in order</param>
        /// <param name="positiveIndexes">select on which end append new flags</param>
        /// <returns>index of last new flag</returns>
        public int AddNamedFlags(IEnumerable<string> flagNames, bool positiveIndexes)
        {
            int lastIndex = 0;
            foreach (string name in flagNames)
            {
                lastIndex = AddNamedFlag(name, positiveIndexes);
            }
            return lastIndex;
        }

        /// <summary>
        /// Add one flag without name
        /// <param name="positiveIndexes">select on which end append new flags</param>
        /// </summary>
        /// <returns>index of new flag</returns>
        public int AddIndexedFlag(bool positiveIndexes)
        {
            int newIndex = positiveIndexes ? positiveCount++ : --negativeCount;
            indexedFlags.Add(newIndex, false);
            return positiveIndexes ? positiveCount - 1 : negativeCount;
        }

        /// <summary>
        /// Add one flag with name
        /// <param name="name">flag name</param>
        /// <param name="positiveIndexes">select on which end append new flags</param>
        /// </summary>
        /// <returns>index of new flag</returns>
        public int AddNamedFlag(string name, bool positiveIndexes)
        {
            name = GetName(name);
            if (!namedFlags.ContainsKey(name))
            {
                int newIndex = positiveIndexes ? positiveCount++ : --negativeCount;
                indexToName.Add(newIndex, name);
                namedFlags.Add(name, false);
                return newIndex;
            }
            return indexToName.Single(x => x.Value == name).Key;
        }

        public bool TryGetFlagName(int index, out string name)
        {
            return indexToName.TryGetValue(index, out name);
        }

        private string GetName(string name) => isCaseSensitive ? name : name.ToLowerInvariant();
    }
}
