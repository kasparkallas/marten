using System;
using System.Linq;
using Marten.Events.Projections;
using Marten.Services;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Events.Projections
{
    public class view_projection_for_view_with_non_public_id_setter: IntegrationContext
    {
        private readonly MonsterSlayed slayed1 = new MonsterSlayed { Name = "Troll" };
        private readonly MonsterSlayed slayed2 = new MonsterSlayed { Name = "Dragon" };
        private readonly Guid streamId;

        public view_projection_for_view_with_non_public_id_setter(DefaultStoreFixture fixture) : base(fixture)
        {
            StoreOptions(_ =>
            {
                _.AutoCreateSchemaObjects = AutoCreate.All;
                _.UseDefaultSerialization(nonPublicMembersStorage: NonPublicMembersStorage.NonPublicSetters);
                _.Events.InlineProjections.Add<ViewProjectionForViewWithPrivateIdSetter>();
                _.Events.InlineProjections.Add<ViewProjectionForViewWithProtectedIdSetter>();
            });

            streamId = theSession.Events
                .StartStream<QuestMonstersWithBaseClass>(slayed1, slayed2).Id;

            theSession.SaveChanges();
        }

        [Fact]
        public void run_view_projection_with_private_id_setter()
        {
            VerifyProjection<QuestMonstersWithPrivateIdSetter>();
        }

        [Fact]
        public void run_view_projection_with_protected_id_setter()
        {
            VerifyProjection<QuestMonstersWithProtectedIdSetter>();
        }

        private void VerifyProjection<T>() where T : IMonstersView
        {
            var loadedView = theSession.Load<T>(streamId);

            loadedView.Id.ShouldBe(streamId);
            loadedView.Monsters.ShouldHaveTheSameElementsAs("Troll", "Dragon");

            var queriedView = theSession.Query<T>()
                .Single(x => x.Id == streamId);

            queriedView.Id.ShouldBe(streamId);
            queriedView.Monsters.ShouldHaveTheSameElementsAs("Troll", "Dragon");
        }

        public class ViewProjectionForViewWithPrivateIdSetter: ViewProjection<QuestMonstersWithPrivateIdSetter, Guid>
        {
            public ViewProjectionForViewWithPrivateIdSetter()
            {
                ProjectEvent<MonsterSlayed>(Project);
            }

            private void Project(QuestMonstersWithPrivateIdSetter view, MonsterSlayed @event)
            {
                view.Apply(@event);
            }
        }

        public class ViewProjectionForViewWithProtectedIdSetter: ViewProjection<QuestMonstersWithProtectedIdSetter, Guid>
        {
            public ViewProjectionForViewWithProtectedIdSetter()
            {
                ProjectEvent<MonsterSlayed>(Project);
            }

            private void Project(QuestMonstersWithProtectedIdSetter view, MonsterSlayed @event)
            {
                view.Apply(@event);
            }
        }
    }
}
