using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Baseline;
using Marten.Linq.Fields;
using Marten.Linq.Filters;
using Marten.Linq.Parsing;
using Marten.Linq.SqlGeneration;
using Marten.Schema;

namespace Marten.Linq.SoftDeletes
{
    public class DeletedBeforeParser: IMethodCallParser
    {
        private static readonly MethodInfo _method =
            typeof(SoftDeletedExtensions).GetMethod(nameof(SoftDeletedExtensions.DeletedBefore));

        public bool Matches(MethodCallExpression expression)
        {
            return Equals(expression.Method, _method);
        }

        public ISqlFragment Parse(IFieldMapping mapping, ISerializer serializer, MethodCallExpression expression)
        {
            if (mapping.DeleteStyle != DeleteStyle.SoftDelete)
                throw new NotSupportedException($"Document DeleteStyle must be {DeleteStyle.SoftDelete}");

            var time = expression.Arguments.Last().Value().As<DateTimeOffset>();

            return new WhereFragment($"d.{SchemaConstants.DeletedColumn} and d.{SchemaConstants.DeletedAtColumn} < ?", time);
        }
    }
}
