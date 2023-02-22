using Microsoft.CodeAnalysis;

namespace AwesomeAnalyzer
{
    public static class Helper
    {
        public static T HasParent<T>(this SyntaxNode syntaxNode)
        {
            var method = syntaxNode.Parent;
            while (true)
            {
                if (method is T || method == null)
                {
                    break;
                }

                method = method.Parent;
            }

            return method is T node ? node : default;
        }
    }
}