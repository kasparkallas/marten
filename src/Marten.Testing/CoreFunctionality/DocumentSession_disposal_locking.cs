﻿using System;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Xunit;

namespace Marten.Testing.CoreFunctionality
{
    public class DocumentSession_disposal_locking
    {
        [Fact]
        public void throw_disposed_ex_after_disposed()
        {
            var store = DocumentStore.For(_ => _.Connection(ConnectionSource.ConnectionString));

            var session = store.OpenSession();
            session.Dispose();

            Exception<ObjectDisposedException>.ShouldBeThrownBy(() =>
            {
                session.Load<User>(Guid.NewGuid());
            });


        }
    }
}
