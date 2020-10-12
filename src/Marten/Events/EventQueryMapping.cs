using System;
using System.Linq;
using System.Linq.Expressions;
using Marten.Linq;
using Marten.Linq.Fields;
using Marten.Linq.Parsing;
using Marten.Schema;

namespace Marten.Events
{
    public class EventQueryMapping : DocumentMapping
    {
        public EventQueryMapping(StoreOptions storeOptions) : base(typeof(IEvent), storeOptions)
        {
            Selector = storeOptions.Events.StreamIdentity == StreamIdentity.AsGuid
                ? (IEventSelector)new EventSelector(storeOptions.Events, storeOptions.Serializer())
                : new StringIdentifiedEventSelector(storeOptions.Events, storeOptions.Serializer());

            DatabaseSchemaName = storeOptions.Events.DatabaseSchemaName;

            TableName = new DbObjectName(DatabaseSchemaName, "mt_events");

            duplicateField(x => x.Sequence, "seq_id");
            if (storeOptions.Events.StreamIdentity == StreamIdentity.AsGuid)
            {
                duplicateField(x => x.StreamId, "stream_id");
            }
            else
            {
                duplicateField(x => x.StreamKey, "stream_id");
            }

            duplicateField(x => x.Version, "version");
            duplicateField(x => x.Timestamp, "timestamp");
        }

        internal IEventSelector Selector { get; }

        public override DbObjectName TableName { get; }

        private DuplicatedField duplicateField(Expression<Func<IEvent, object>> property, string columnName)
        {
            var finder = new FindMembers();
            finder.Visit(property);

            return DuplicateField(finder.Members.ToArray(), columnName: columnName);
        }

    }
}
