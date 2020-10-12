﻿using System.Threading.Tasks;
using Marten.Testing.Documents;
using Marten.Testing.Examples;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.MultiTenancy
{
    public class fetch_tenant_id_as_part_of_metadata : IntegrationContext
    {
        [Fact]
        public void tenant_id_on_metadata()
        {
            StoreOptions(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);
                _.Policies.AllDocumentsAreMultiTenanted();
            });

            var user1 = new User();
            var user2 = new User();

            theStore.BulkInsert("Green", new User[] {user1});
            theStore.BulkInsert("Purple", new User[] {user2});

            using var session = theStore.QuerySession();

            session.MetadataFor(user1)
                .TenantId.ShouldBe("Green");
        }

        [Fact]
        public async Task tenant_id_on_metadata_async()
        {
            StoreOptions(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);
                _.Policies.AllDocumentsAreMultiTenanted();
            });

            var user1 = new User();
            var user2 = new User();

            theStore.BulkInsert("Green", new User[] { user1 });
            theStore.BulkInsert("Purple", new User[] { user2 });

            using var session = theStore.QuerySession();

            var metadata = await session.MetadataForAsync(user1);
            metadata.TenantId.ShouldBe("Green");

        }

        public fetch_tenant_id_as_part_of_metadata(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }
}
