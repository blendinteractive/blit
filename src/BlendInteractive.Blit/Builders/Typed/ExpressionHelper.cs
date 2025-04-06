using System.Linq.Expressions;

namespace BlendInteractive.Blit.Builders.Typed;

public static class ExpressionHelper
{
    public static string GetMemberName<V>(this Expression<V> expression) =>
        expression.Body switch
        {
            MemberExpression m =>
                m.Member.Name,
            UnaryExpression u when u.Operand is MemberExpression m =>
                m.Member.Name,
            _ =>
                throw new NotImplementedException($"Cannot resolve expression of {expression.GetType().GetShortTypeName()}")
        };
}