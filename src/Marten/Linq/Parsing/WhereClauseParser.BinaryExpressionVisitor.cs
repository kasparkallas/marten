using System.Linq.Expressions;
using Marten.Exceptions;
using Marten.Linq.Fields;
using Marten.Linq.SqlGeneration;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Marten.Linq.Parsing
{
    internal partial class WhereClauseParser
    {
        internal class BinaryExpressionVisitor: RelinqExpressionVisitor
        {
            private readonly WhereClauseParser _parent;
            private BinarySide _left;
            private BinarySide _right;

            public BinaryExpressionVisitor(WhereClauseParser parent)
            {
                _parent = parent;
            }

            public ISqlFragment BuildWhereFragment(BinaryExpression node, string op)
            {
                _left = analyze(node.Left);
                _right = analyze(node.Right);

                return _left.CompareTo(_right, op);
            }

            private BinarySide analyze(Expression expression)
            {
                switch (expression)
                {
                    case ConstantExpression c:
                        return new BinarySide
                        {
                            Constant = c
                        };
                    case PartialEvaluationExceptionExpression p:
                    {
                        var inner = p.Exception;

                        throw new BadLinqExpressionException($"Error in value expression inside of the query for '{p.EvaluatedExpression}'. See the inner exception:", inner);
                    }
                    case SubQueryExpression subQuery:
                    {
                        var parser = new SubQueryFilterParser(_parent, subQuery);

                        return new BinarySide
                        {
                            Comparable = parser.BuildCountComparisonStatement()
                        };
                    }
                    case QuerySourceReferenceExpression source:
                        return new BinarySide
                        {
                            Field = new SimpleDataField(source.Type)
                        };
                    case BinaryExpression binary when binary.NodeType == ExpressionType.Modulo:
                        return new BinarySide{Comparable = new ModuloFragment(binary, _parent._statement.Fields)};
                    case BinaryExpression ne when ne.NodeType == ExpressionType.NotEqual:
                        if (ne.Right is ConstantExpression v && v.Value == null)
                        {
                            var field = _parent._statement.Fields.FieldFor(ne.Left);
                            return new BinarySide
                            {
                                Comparable = new HasValueField(field)
                            };
                        }

                        throw new BadLinqExpressionException($"Invalid Linq Where() clause with expression: " + ne);

                        break;
                    case BinaryExpression binary:
                        throw new BadLinqExpressionException($"Unsupported nested operator '{binary.NodeType}' as an operand in a binary expression");
                    case UnaryExpression u when u.NodeType == ExpressionType.Not:
                        return new BinarySide{Comparable = new NotField(_parent._statement.Fields.FieldFor(u.Operand))};
                    default:
                        return new BinarySide{Field = _parent._statement.Fields.FieldFor(expression)};
                }
            }
        }
    }
}
