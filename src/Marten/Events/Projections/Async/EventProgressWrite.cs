using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Marten.Internal;
using Marten.Internal.Operations;
using Marten.Schema;
using Marten.Util;
using NpgsqlTypes;

namespace Marten.Events.Projections.Async
{
    public class EventProgressWrite: IStorageOperation
    {
        private readonly string _key;
        private readonly long _number;
        private readonly DbObjectName _sproc;

        public EventProgressWrite(EventGraph events, string key, long number)
        {
            _sproc = new DbObjectName(events.DatabaseSchemaName, "mt_mark_event_progression");
            _key = key;
            _number = number;
        }

        public void ConfigureCommand(CommandBuilder builder, IMartenSession session)
        {
            var nameArg = builder.AddParameter(_key, NpgsqlDbType.Varchar);
            var numberArg = builder.AddParameter(_number, NpgsqlDbType.Bigint);
            builder.Append($"select {_sproc}(:{nameArg.ParameterName}, :{numberArg.ParameterName})");
        }

        public Type DocumentType => null;

        public void Postprocess(DbDataReader reader, IList<Exception> exceptions)
        {
            // Nothing
        }

        public Task PostprocessAsync(DbDataReader reader, IList<Exception> exceptions, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public OperationRole Role()
        {
            return OperationRole.Other;
        }
    }
}
