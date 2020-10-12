using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Baseline;
using Marten.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace Marten.Transforms
{
    public class TransformToJsonMatcher: IMethodCallMatcher
    {
        public bool TryMatch(MethodCallExpression expression, ExpressionVisitor selectorVisitor,
            out ResultOperatorBase op)
        {
            if (expression.Method.Name == nameof(TransformExtensions.TransformToJson))
            {
                var transformName = (string)expression.Arguments.Last().As<ConstantExpression>().Value;
                op = new TransformToJsonResultOperator(transformName);
                return true;
            }

            op = null;
            return false;
        }
    }

    public class TransformToJsonNode: ResultOperatorExpressionNodeBase
    {
        public static MethodInfo[] SupportedMethods =
            typeof(TransformExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name == nameof(TransformExtensions.TransformToJson)).ToArray();

        private readonly TransformToJsonResultOperator _operator;

        public TransformToJsonNode(MethodCallExpressionParseInfo parseInfo, Expression transform, Expression optionalSelector) : base(parseInfo, transform as LambdaExpression, optionalSelector as LambdaExpression)
        {
            var name = transform.As<ConstantExpression>().Value.As<string>();
            _operator = new TransformToJsonResultOperator(name);
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            return _operator;
        }

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved,
            ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(
                inputParameter,
                expressionToBeResolved,
                clauseGenerationContext);
        }


    }
}
