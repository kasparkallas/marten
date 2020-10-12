using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Marten.Internal.Operations;
using Marten.Linq.Fields;
using Marten.Linq.SqlGeneration;
using Marten.Schema;
using Marten.Services;
using Marten.Storage;
using Remotion.Linq;

namespace Marten.Internal.Storage
{
    public interface IDocumentStorage : ISelectClause
    {
        Type SourceType { get; }

        Type IdType { get; }

        IFieldMapping Fields { get; }

        ISqlFragment FilterDocuments(QueryModel model, ISqlFragment query);

        ISqlFragment DefaultWhereFragment();

        bool UseOptimisticConcurrency { get; }
        IOperationFragment DeleteFragment { get; }
        DuplicatedField[] DuplicatedFields { get; }
        DbObjectName TableName { get; }
        Type DocumentType { get; }

        TenancyStyle TenancyStyle { get; }
    }

    public interface IDocumentStorage<T> : IDocumentStorage
    {
        object IdentityFor(T document);


        Guid? VersionFor(T document, IMartenSession session);

        void Store(IMartenSession session, T document);
        void Store(IMartenSession session, T document, Guid? version);

        void Eject(IMartenSession session, T document);

        IStorageOperation Update(T document, IMartenSession session, ITenant tenant);
        IStorageOperation Insert(T document, IMartenSession session, ITenant tenant);
        IStorageOperation Upsert(T document, IMartenSession session, ITenant tenant);

        IStorageOperation Overwrite(T document, IMartenSession session, ITenant tenant);


        IDeletion DeleteForDocument(T document);


        void EjectById(IMartenSession session, object id);
        void RemoveDirtyTracker(IMartenSession session, object id);
    }

    public interface IDocumentStorage<T, TId> : IDocumentStorage<T>
    {
        IDeletion DeleteForId(TId id);

        T Load(TId id, IMartenSession session);
        Task<T> LoadAsync(TId id, IMartenSession session, CancellationToken token);

        IReadOnlyList<T> LoadMany(TId[] ids, IMartenSession session);
        Task<IReadOnlyList<T>> LoadManyAsync(TId[] ids, IMartenSession session, CancellationToken token);


        TId AssignIdentity(T document, ITenant tenant);
        TId Identity(T document);
        ISqlFragment ByIdFilter(TId id);
    }

}
