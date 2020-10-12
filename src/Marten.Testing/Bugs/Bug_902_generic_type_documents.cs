using System;
using System.Collections.Generic;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Bugs
{
    public class Bug_902_generic_type_documents: IntegrationContext
    {
        public class MartenStoredState<T>
        {
            public Guid Id = Guid.NewGuid();

            public T Value { get; set; }
        }

        [Fact]
        public void can_create_object_name()
        {
            var doc2 = new MartenStoredState<Dictionary<string, string>>
            {
                Value = new Dictionary<string, string> { { "color", "blue" } }
            };

            using (var session = theStore.LightweightSession())
            {
                session.Store(doc2);
                session.SaveChanges();
            }

            using (var query = theStore.QuerySession())
            {
                query.Load<MartenStoredState<Dictionary<string, string>>>(doc2.Id)
                    .Value["color"].ShouldBe("blue");
            }
        }

        public Bug_902_generic_type_documents(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }
}
