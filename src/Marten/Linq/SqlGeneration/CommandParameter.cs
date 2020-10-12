using System.Linq.Expressions;
using Marten.Util;
using Npgsql;
using NpgsqlTypes;

namespace Marten.Linq.SqlGeneration
{
    public class CommandParameter : ISqlFragment
    {
        public CommandParameter()
        {
        }

        public CommandParameter(ConstantExpression expression)
        {
            Value = expression.Value;
            DbType = TypeMappings.ToDbType(expression.Type);
        }

        public CommandParameter(object value)
        {
            Value = value;
            if (value != null)
            {
                DbType = TypeMappings.TryGetDbType(value.GetType());
            }
        }

        public CommandParameter(object value, NpgsqlDbType npgsqlDbType)
        {
            Value = value;
            DbType = npgsqlDbType;
        }

        public object Value { get; }
        public NpgsqlDbType? DbType { get; }

        public NpgsqlParameter AddParameter(CommandBuilder builder)
        {
            return builder.AddParameter(Value, DbType);
        }

        public void Apply(CommandBuilder builder)
        {
            var parameter = AddParameter(builder);
            builder.Append(":");
            builder.Append(parameter.ParameterName);
        }

        public bool Contains(string sqlText)
        {
            return false;
        }
    }
}
