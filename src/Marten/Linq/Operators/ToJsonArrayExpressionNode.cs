using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace Marten.Linq.Operators
{
    public class ToJsonArrayExpressionNode: ResultOperatorExpressionNodeBase
    {
        public static MethodInfo[] SupportedMethods =
            typeof(CompiledQueryExtensions).GetMethods().Where(m => m.Name == nameof(CompiledQueryExtensions.ToJsonArray)).ToArray();

        public ToJsonArrayExpressionNode(
            MethodCallExpressionParseInfo parseInfo, LambdaExpression parameter)
            : base(parseInfo, null, null) { }

        protected override ResultOperatorBase CreateResultOperator(
            ClauseGenerationContext clauseGenerationContext)
        {
            return new ToJsonArrayResultOperator(null);
        }

        public override Expression Resolve(
            ParameterExpression inputParameter,
            Expression expressionToBeResolved,
            ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(
                inputParameter,
                expressionToBeResolved,
                clauseGenerationContext);
        }
    }
}
