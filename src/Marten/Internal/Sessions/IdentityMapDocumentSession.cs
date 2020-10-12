using System;
using Marten.Internal.CodeGeneration;
using Marten.Internal.Storage;
using Marten.Services;
using Marten.Storage;

namespace Marten.Internal.Sessions
{
    public class IdentityMapDocumentSession: DocumentSessionBase
    {
        public IdentityMapDocumentSession(DocumentStore store, SessionOptions sessionOptions, IManagedConnection database, ITenant tenant) : base(store, sessionOptions, database, tenant)
        {
        }

        protected override IDocumentStorage<T> selectStorage<T>(DocumentProvider<T> provider)
        {
            return provider.IdentityMap;
        }

        protected override void ejectById<T>(long id)
        {
            StorageFor<T>().EjectById(this, id);
        }

        protected override void ejectById<T>(int id)
        {
            StorageFor<T>().EjectById(this, id);
        }

        protected override void ejectById<T>(Guid id)
        {
            StorageFor<T>().EjectById(this, id);
        }

        protected override void ejectById<T>(string id)
        {
            StorageFor<T>().EjectById(this, id);
        }
    }
}
