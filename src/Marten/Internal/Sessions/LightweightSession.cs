using System;
using Marten.Internal.CodeGeneration;
using Marten.Internal.Storage;
using Marten.Services;
using Marten.Storage;

namespace Marten.Internal.Sessions
{
    public class LightweightSession: DocumentSessionBase
    {
        public LightweightSession(DocumentStore store, SessionOptions sessionOptions, IManagedConnection database, ITenant tenant) : base(store, sessionOptions, database, tenant)
        {
        }

        protected override IDocumentStorage<T> selectStorage<T>(DocumentProvider<T> provider)
        {
            return provider.Lightweight;
        }

        public override void Eject<T>(T document)
        {
            _unitOfWork.Eject(document);
        }

        public override void EjectAllOfType(Type type)
        {
            // Nothing
        }

        protected override void ejectById<T>(long id)
        {
            // Nothing
        }

        protected override void ejectById<T>(int id)
        {
            // Nothing
        }

        protected override void ejectById<T>(Guid id)
        {
            // Nothing
        }

        protected override void ejectById<T>(string id)
        {
            // Nothing
        }
    }
}
