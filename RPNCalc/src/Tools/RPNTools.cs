using System;
using System.Globalization;
using System.Text.RegularExpressions;
using RPNCalc.StackItems;

namespace RPNCalc.Tools
{
    public static class RPNTools
    {
        private static readonly Regex numberToken = new(@"^\-?\.?\d+?(?:\.\d+)?(?:e\-?\d+)?$");
        private static readonly Regex stringToken = new(@"^'([^'\\]*(?:\\.[^'\\]*)*)'$");
        private static readonly Regex variableNameToken = new(@"^[^'\.][^'\s]*$");
        private static readonly Regex[] matchers = new[] { numberToken, stringToken, variableNameToken };
        private static readonly Regex expressionSplit = new(@"(?<!\\)'.*?(?<!\\)'|[^\s']+");

        /// <summary>
        /// Splits string to array of string tokens.
        /// </summary>
        public static string[] GetTokens(string rpnExpression)
        {
            bool escapeSymbol = false;
            int count = 0;
            for (int i = 0; i < rpnExpression.Length; i++)
            {
                if (rpnExpression[i] == '\\')
                {
                    escapeSymbol = true;
                    continue;
                }
                else if (!escapeSymbol && rpnExpression[i] == '\'')
                {
                    count++;
                }
                escapeSymbol = false;
            }
            if (count % 2 == 1) throw new RPNArgumentException("Uneven ' string symbols");
            MatchCollection matches = expressionSplit.Matches(rpnExpression);
            string[] tokens = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                tokens[i] = matches[i].Value;
            }
            return tokens;
        }

        /// <summary>
        /// <para>Convert string tokens to strongly typed tokens.</para>
        /// <para>Expects RPN expression.</para>
        /// </summary>
        /// <param name="postfixTokens">Can be output of <see cref="AlgebraicTools.InfixToPostfix(string[], bool)"/> method</param>
        public static AStackItem[] TokensToItems(params string[] postfixTokens)
        {
            AStackItem[] items = new AStackItem[postfixTokens.Length];
            for (int i = 0; i < postfixTokens.Length; i++)
            {
                items[i] = MatchToken(postfixTokens[i]);
            }
            return items;
        }

        private static AStackItem MatchToken(string token)
        {
            token = token.Trim();
            foreach (var matcher in matchers)
            {
                Match match;
                if (!(match = matcher.Match(token)).Success) continue;
                if (matcher == numberToken) return new StackReal(NumberConvert(match.Groups[0].Value));
                if (matcher == stringToken) return new StackString(match.Groups[1].Value.Replace("\\'", "'"));
                if (matcher == variableNameToken) return new StackName(match.Groups[0].Value);
            }
            throw new RPNArgumentException("Invalid syntax, unknown token " + token);
        }

        private static double NumberConvert(string num)
        {
            return double.Parse(num, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
