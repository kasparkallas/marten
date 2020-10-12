using System.Linq;
using Marten.Events.Projections;
using Marten.Testing.Events.Projections;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Events
{
    public class aggregates_should_be_registered_as_document_mappings_automatically: IntegrationContext
    {
        [Fact]
        public void aggregations_are_registered()
        {
            StoreOptions(_ =>
            {
                _.Events.AggregateFor<QuestParty>();
            });

            theStore.Storage.AllDocumentMappings.Select(x => x.DocumentType)
                .ShouldContain(typeof(QuestParty));
        }

        [Fact]
        public void aggregations_added_manually_are_registered()
        {
            StoreOptions(_ =>
            {
                _.Events.AddAggregator(new Aggregator<QuestParty>());
            });

            theStore.Storage.AllDocumentMappings.Select(x => x.DocumentType)
                .ShouldContain(typeof(QuestParty));
        }

        [Fact]
        public void inline_aggregations_are_registered()
        {
            StoreOptions(_ =>
            {
                _.Events.InlineProjections.AggregateStreamsWith<QuestParty>();
            });

            theStore.Storage.AllDocumentMappings.Select(x => x.DocumentType)
                .ShouldContain(typeof(QuestParty));
        }

        [Fact]
        public void async_aggregations_are_registered()
        {
            StoreOptions(_ =>
            {
                _.Events.AsyncProjections.AggregateStreamsWith<QuestParty>();
            });

            theStore.Storage.AllDocumentMappings.Select(x => x.DocumentType)
                .ShouldContain(typeof(QuestParty));
        }

        [Fact]
        public void inline_transformations_are_registered()
        {
            StoreOptions(_ =>
            {
                _.Events.InlineProjections.TransformEvents(new MonsterDefeatedTransform());
            });

            theStore.Storage.AllDocumentMappings.Select(x => x.DocumentType)
                .ShouldContain(typeof(MonsterDefeated));
        }

        [Fact]
        public void async_transformations_are_registered()
        {
            StoreOptions(_ =>
            {
                _.Events.AsyncProjections.TransformEvents(new MonsterDefeatedTransform());
            });

            theStore.Storage.AllDocumentMappings.Select(x => x.DocumentType)
                .ShouldContain(typeof(MonsterDefeated));
        }

        public aggregates_should_be_registered_as_document_mappings_automatically(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }
}
