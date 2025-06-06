using RPNCalc.Items;
using RPNCalc.Tools;

namespace RPNCalc.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static class RPNExtensions
    {
        /// <summary>
        /// Converts string to instructions and evaluates them.
        /// </summary>
        /// <param name="calc">RPN calculator instance</param>
        /// <param name="rpnExpression">whole RPN expression as one string</param>
        /// <returns>top value on stack or null if stack is empty</returns>
        public static AItem? Eval(this RPN calc, string rpnExpression)
        {
            string[] tokens = RPNTools.GetTokens(rpnExpression);
            AItem[] instructions = RPNTools.TokensToItems(tokens);
            return calc.Eval(instructions);
        }

        /// <summary>
        /// Evaluates instruction(s) and returns top value from stack.
        /// </summary>
        /// <param name="calc">RPN calculator instance</param>
        /// <param name="algebraicExpression">one or more instructions</param>
        /// <returns>top value on stack or null if stack is empty</returns>
        public static AItem? EvalAlgebraic(this RPN calc, string algebraicExpression)
        {
            string[] tokens = AlgebraicTools.GetTokens(algebraicExpression);
            tokens = AlgebraicTools.InfixToPostfix(tokens);
            AItem[] instructions = RPNTools.TokensToItems(tokens);
            return calc.Eval(instructions);
        }

        /// <summary>
        /// Returns stack as formatted string for easy view of stack content.
        /// </summary>
        public static string DumpStack(this RPN calc) => calc.StackView.DumpStack();
    }
}
