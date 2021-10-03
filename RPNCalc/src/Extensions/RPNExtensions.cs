using System;
using RPNCalc.Tools;

namespace RPNCalc.Extensions
{
    public static class RPNExtensions
    {
        /// <summary>
        /// Converts string to instructions and evaluates them.
        /// </summary>
        /// <param name="calc">RPN calculator instance</param>
        /// <param name="rpnExpression">whole RPN expression as one string</param>
        /// <returns>top value on stack or null if stack is empty</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="RPNUndefinedNameException"/>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        /// <exception cref="RPNArgumentException"/>
        public static AStackItem Eval(this RPN calc, string rpnExpression)
        {
            string[] tokens = RPNTools.GetTokens(rpnExpression);
            AStackItem[] instructions = RPNTools.ConvertTokens(tokens);
            return calc.Eval(instructions);
        }

        /// <summary>
        /// Evaluates instruction(s) and returns top value from stack.
        /// </summary>
        /// <param name="calc">RPN calculator instance</param>
        /// <param name="algebraicExpression">one or more instructions</param>
        /// <returns>top value on stack or null if stack is empty</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="RPNUndefinedNameException"/>
        /// <exception cref="RPNEmptyStackException"/>
        /// <exception cref="RPNFunctionException"/>
        /// <exception cref="RPNArgumentException"/>
        public static AStackItem EvalAlgebraic(this RPN calc, string algebraicExpression)
        {
            string[] tokens = AlgebraicTools.AlgebraicTokenizer(algebraicExpression);
            tokens = AlgebraicTools.InfixToPostfix(tokens);
            AStackItem[] instructions = RPNTools.ConvertTokens(tokens);
            return calc.Eval(instructions);
        }
    }
}
