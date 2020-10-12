using System;
using System.Linq;
using System.Linq.Expressions;
using Baseline;
using Marten.Linq.Fields;
using Marten.Linq.Filters;
using Marten.Linq.SqlGeneration;
using Marten.Util;
using NpgsqlTypes;

namespace Marten.Linq.Parsing.Methods
{
    internal class IsOneOf: IMethodCallParser
    {
        public bool Matches(MethodCallExpression expression)
        {
            return (expression.Method.Name == nameof(LinqExtensions.IsOneOf)
                    || expression.Method.Name == nameof(LinqExtensions.In))
                   && expression.Method.DeclaringType == typeof(LinqExtensions);
        }

        public ISqlFragment Parse(IFieldMapping mapping, ISerializer serializer, MethodCallExpression expression)
        {
            var members = FindMembers.Determine(expression);

            var locator = mapping.FieldFor(members).TypedLocator;
            var values = expression.Arguments.Last().Value();

            if (members.Last().GetMemberType().IsEnum)
            {
                return new EnumIsOneOfWhereFragment(values, serializer.EnumStorage, locator);
            }

            // TODO -- use new WhereInArray
            return new WhereFragment($"{locator} = ANY(?)", values);
        }
    }

    public class EnumIsOneOfWhereFragment: ISqlFragment
    {
        private readonly object _values;
        private readonly string _locator;
        private readonly NpgsqlDbType _dbType;

        public EnumIsOneOfWhereFragment(object values, EnumStorage enumStorage, string locator)
        {
            var array = values.As<Array>();
            if (enumStorage == EnumStorage.AsInteger)
            {
                var numbers = new int[array.Length];

                for (int i = 0; i < array.Length; i++)
                {
                    numbers[i] = array.GetValue(i).As<int>();
                }

                _values = numbers;
                _dbType = NpgsqlDbType.Integer | NpgsqlDbType.Array;
            }
            else
            {
                var strings = new string[array.Length];

                for (int i = 0; i < array.Length; i++)
                {
                    strings[i] = array.GetValue(i).ToString();
                }

                _values = strings;
                _dbType = NpgsqlDbType.Varchar | NpgsqlDbType.Array;
            }

            _locator = locator;
        }

        public void Apply(CommandBuilder builder)
        {
            var param = builder.AddParameter(_values, _dbType);

            builder.Append(_locator);
            builder.Append(" = ANY(:");
            builder.Append(param.ParameterName);
            builder.Append(")");
        }

        public bool Contains(string sqlText)
        {
            return false;
        }
    }
}
