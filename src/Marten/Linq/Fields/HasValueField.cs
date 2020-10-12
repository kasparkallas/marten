using System.Linq.Expressions;
using Marten.Linq.Filters;
using Marten.Linq.SqlGeneration;

namespace Marten.Linq.Fields
{
    public class HasValueField: IComparableFragment
    {
        private readonly IField _field;

        public HasValueField(IField field)
        {
            _field = field;
        }

        public ISqlFragment CreateComparison(string op, ConstantExpression value)
        {
            var hasValue = (bool)value.Value;
            return hasValue
                ? (ISqlFragment) new IsNotNullFilter(_field)
                : new IsNullFilter(_field);
        }
    }
}
