using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten.Schema.Testing.Documents;
using Marten.Services;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Marten.Schema.Testing.Hierarchies
{
    public class delete_by_where_for_hierarchy_Tests: end_to_end_document_hierarchy_usage_Tests
    {
        private readonly ITestOutputHelper _output;

        public delete_by_where_for_hierarchy_Tests(ITestOutputHelper output)
        {
            _output = output;
            DocumentTracking = DocumentTracking.None;
        }

        [Fact]
        public void can_delete_all_subclass()
        {
            loadData();

            theSession.DeleteWhere<SuperUser>(x => true);
            theSession.SaveChanges();

            theSession.Query<SuperUser>().Count().ShouldBe(0);
            theSession.Query<AdminUser>().Count().ShouldBe(2);
            theSession.Query<User>().Count().ShouldBe(4);
        }

        [Fact]
        public void can_delete_by_subclass()
        {
            loadData();

            theSession.DeleteWhere<SuperUser>(x => x.FirstName.StartsWith("D"));
            theSession.SaveChanges();

            theSession.Query<SuperUser>().Count().ShouldBe(1);
            theSession.Query<AdminUser>().Count().ShouldBe(2);
            theSession.Query<User>().Count().ShouldBe(5);
        }

        [Fact]
        public void can_delete_by_the_hierarchy()
        {
            loadData();

            theSession.DeleteWhere<User>(x => x.FirstName.StartsWith("D"));
            theSession.SaveChanges();

            // Should delete one SuperUser and one AdminUser
            theSession.Query<SuperUser>().Count().ShouldBe(1);
            theSession.Query<AdminUser>().Count().ShouldBe(1);
            theSession.Query<User>().Count().ShouldBe(4);
        }
    }

    public class persist_and_load_for_hierarchy_Tests: end_to_end_document_hierarchy_usage_Tests
    {
        public persist_and_load_for_hierarchy_Tests()
        {
            DocumentTracking = DocumentTracking.IdentityOnly;
        }

        [Fact]
        public void persist_and_delete_subclass()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            theSession.Delete(admin1);

            theSession.SaveChanges();

            theSession.Load<User>(admin1.Id).ShouldBeNull();
            theSession.Load<AdminUser>(admin1.Id).ShouldBeNull();
        }


        [Fact]
        public void persist_and_delete_subclass_2()
        {
            theSession.Logger = new TestOutputMartenLogger(_output);

            theSession.Store(admin1);
            theSession.SaveChanges();

            theSession.Delete<AdminUser>(admin1.Id);

            theSession.SaveChanges();

            theSession.Load<User>(admin1.Id).ShouldBeNull();
            theSession.Load<AdminUser>(admin1.Id).ShouldBeNull();
        }

        [Fact]
        public void persist_and_delete_top()
        {
            theSession.Store(user1);
            theSession.SaveChanges();

            theSession.Delete<User>(user1.Id);
            theSession.SaveChanges();

            theSession.Load<User>(user1.Id).ShouldBeNull();
        }

        [Fact]
        public void persist_and_delete_top_2()
        {
            theSession.Store(user1);
            theSession.SaveChanges();

            theSession.Delete(user1);
            theSession.SaveChanges();

            theSession.Load<User>(user1.Id).ShouldBeNull();
        }


        [Fact]
        public void persist_and_load_subclass()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            theSession.Load<User>(admin1.Id).ShouldBeTheSameAs(admin1);
            theSession.Load<AdminUser>(admin1.Id).ShouldBeTheSameAs(admin1);

            using (var session = theStore.QuerySession())
            {
                session.Load<AdminUser>(admin1.Id).ShouldNotBeTheSameAs(admin1).ShouldNotBeNull();
                session.Load<User>(admin1.Id).ShouldNotBeTheSameAs(admin1).ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task persist_and_load_subclass_async()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            (await theSession.LoadAsync<User>(admin1.Id).ConfigureAwait(false)).ShouldBeTheSameAs(admin1);
            (await theSession.LoadAsync<AdminUser>(admin1.Id).ConfigureAwait(false)).ShouldBeTheSameAs(admin1);

            using (var session = theStore.QuerySession())
            {
                (await session.LoadAsync<AdminUser>(admin1.Id).ConfigureAwait(false)).ShouldNotBeTheSameAs(admin1)
                    .ShouldNotBeNull();
                (await session.LoadAsync<User>(admin1.Id).ConfigureAwait(false)).ShouldNotBeTheSameAs(admin1)
                    .ShouldNotBeNull();
            }
        }

        [Fact]
        public void persist_and_load_top_level()
        {
            theSession.Store(user1);
            theSession.SaveChanges();

            theSession.Load<User>(user1.Id).ShouldBeTheSameAs(user1);

            using (var session = theStore.QuerySession())
            {
                session.Load<User>(user1.Id).ShouldNotBeTheSameAs(user1).ShouldNotBeNull();
            }
        }

    }

    public class query_through_mixed_population_Tests: end_to_end_document_hierarchy_usage_Tests
    {
        public query_through_mixed_population_Tests()
        {
            DocumentTracking = DocumentTracking.IdentityOnly;
            loadData();

        }

        [Fact]
        public void clean_by_subclass_only_deletes_the_one_subclass()
        {
            theStore.Advanced.Clean.DeleteDocumentsFor(typeof(AdminUser));

            theSession.Query<User>().Any().ShouldBeTrue();
            theSession.Query<SuperUser>().Any().ShouldBeTrue();

            theSession.Query<AdminUser>().Any().ShouldBeFalse();
        }


        [Fact]
        public void identity_map_usage_from_select()
        {
            var users = theSession.Query<User>().OrderBy(x => x.FirstName).ToArray();
            users[0].ShouldBeTheSameAs(admin1);
            users[1].ShouldBeTheSameAs(super1);
            users[5].ShouldBeTheSameAs(user2);
        }

        [Fact]
        public void load_by_id_keys_from_base_class_clean()
        {
            using (var session = theStore.QuerySession())
            {
                session.LoadMany<AdminUser>(admin1.Id, admin2.Id)
                    .Select(x => x.Id)
                    .ShouldHaveTheSameElementsAs(admin1.Id, admin2.Id);
            }
        }

        [Fact]
        public void load_by_id_keys_from_base_class_resolved_from_identity_map()
        {
            theSession.LoadMany<AdminUser>(admin1.Id, admin2.Id)
                .ShouldHaveTheSameElementsAs(admin1, admin2);
        }

        [Fact]
        public async Task load_by_id_keys_from_base_class_resolved_from_identity_map_async()
        {
            var users = await theSession.LoadManyAsync<AdminUser>(admin1.Id, admin2.Id).ConfigureAwait(false);
            users.ShouldHaveTheSameElementsAs(admin1, admin2);
        }

        [Fact]
        public void load_by_id_with_mixed_results_fresh()
        {
            using (var session = theStore.QuerySession())
            {
                session.LoadMany<User>(admin1.Id, super1.Id, user1.Id)
                    .ToArray()
                    .OrderBy(x => x.FirstName)
                    .Select(x => x.Id)
                    .ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, user1.Id);
            }
        }

        [Fact]
        public async Task load_by_id_with_mixed_results_fresh_async()
        {
            using (var session = theStore.QuerySession())
            {
                var users = await session.LoadManyAsync<User>(admin1.Id, super1.Id, user1.Id).ConfigureAwait(false);

                users.OrderBy(x => x.FirstName)
                    .Select(x => x.Id)
                    .ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, user1.Id);
            }
        }

        [Fact]
        public void load_by_id_with_mixed_results_from_identity_map()
        {
            theSession.LoadMany<User>(admin1.Id, super1.Id, user1.Id)
                .ToArray().ShouldHaveTheSameElementsAs(admin1, super1, user1);
        }

        [Fact]
        public async Task load_by_id_with_mixed_results_from_identity_map_async()
        {
            var users = await theSession.LoadManyAsync<User>(admin1.Id, super1.Id, user1.Id).ConfigureAwait(false);
            users.OrderBy(x => x.FirstName).ShouldHaveTheSameElementsAs(admin1, super1, user1);
        }

        [Fact]
        public void query_against_all_with_no_where()
        {
            var users = theSession.Query<User>().OrderBy(x => x.FirstName).ToArray();
            users
                .Select(x => x.Id)
                .ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, admin2.Id, user1.Id, super2.Id, user2.Id);

            users.Select(x => x.GetType())
                .ShouldHaveTheSameElementsAs(typeof(AdminUser), typeof(SuperUser), typeof(AdminUser), typeof(User),
                    typeof(SuperUser), typeof(User));
        }

        [Fact]
        public void query_against_all_with_where_clause()
        {
            theSession.Query<User>().OrderBy(x => x.FirstName).Where(x => x.UserName.StartsWith("A"))
                .ToArray().Select(x => x.Id)
                .ShouldHaveTheSameElementsAs(admin1.Id, super1.Id, user1.Id);
        }

        [Fact]
        public void query_for_only_a_subclass_with_no_where_clause()
        {
            theSession.Query<AdminUser>().OrderBy(x => x.FirstName).ToArray()
                .Select(x => x.Id).ShouldHaveTheSameElementsAs(admin1.Id, admin2.Id);
        }

        [Fact]
        public void query_for_only_a_subclass_with_where_clause()
        {
            theSession.Query<AdminUser>().Where(x => x.FirstName == "Eric").Single()
                .Id.ShouldBe(admin2.Id);
        }
    }

    public class query_through_mixed_population_Tests_tenanted: IntegrationContext
    {

        public query_through_mixed_population_Tests_tenanted(ITestOutputHelper output) : base(output)
        {
            EnableCommandLogging = true;

            StoreOptions(
                _ =>
                {
                    _.Policies.AllDocumentsAreMultiTenanted();
                    _.Schema.For<User>().AddSubClass<SuperUser>().AddSubClass<AdminUser>().Duplicate(x => x.UserName);
                });

            loadData();
        }

        private void loadData()
        {
            using (var session = theStore.OpenSession("tenant_1"))
            {
                session.Store(new User(), new AdminUser());
                session.SaveChanges();
            }
        }

        [Fact]
        public void query_tenanted_data_with_any_tenant_predicate()
        {
            using (var session = theStore.OpenSession())
            {
                var users = session.Query<AdminUser>().Where(u => u.AnyTenant()).ToArray();
                users.Length.ShouldBeGreaterThan(0);
            }
        }
    }

    public class Bug_1247_end_to_end_query_with_include_and_document_hierarchy_Tests: end_to_end_document_hierarchy_usage_Tests
    {
        private readonly ITestOutputHelper _output;

        public Bug_1247_end_to_end_query_with_include_and_document_hierarchy_Tests(ITestOutputHelper output)
        {
            _output = output;
            DocumentTracking = DocumentTracking.IdentityOnly;
        }

        [Fact]
        public void include_to_list_using_outer_join()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted1" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted2" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted3" };
            var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted4" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3, issue4);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                query.Logger = new TestOutputMartenLogger(_output);

                var list = new List<User>();

                var issues = query.Query<Issue>().Include<User>(x => x.AssigneeId, list).ToArray();

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id).ShouldBeTrue();
                list.Any(x => x.Id == user2.Id).ShouldBeTrue();

                issues.Length.ShouldBe(4);
            }
        }

        [Fact]
        public async Task include_to_list_using_outer_join_async()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted1" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted2" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted3" };
            var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted4" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3, issue4);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                var issues = await query.Query<Issue>().Include<User>(x => x.AssigneeId, list).ToListAsync();

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id).ShouldBeTrue();
                list.Any(x => x.Id == user2.Id).ShouldBeTrue();

                issues.Count.ShouldBe(4);
            }
        }

    }

    public class Bug_1484_store_overloads_Tests: end_to_end_document_hierarchy_usage_Tests
    {
        public Bug_1484_store_overloads_Tests()
        {
            DocumentTracking = DocumentTracking.IdentityOnly;
        }

        [Fact]
        public void persist_and_count_single_entity()
        {
            theSession.Store(admin1);
            theSession.SaveChanges();

            theSession.Query<AdminUser>().Count().ShouldBe(1);
        }

        [Fact]
        public void persist_mutliple_entites_as_params_and_count()
        {
            theSession.Store(admin1, admin2);
            theSession.SaveChanges();

            theSession.Query<AdminUser>().Count().ShouldBe(2);
        }

        [Fact]
        public void persist_mutliple_entites_as_array_and_count()
        {
            theSession.Store(new[] { admin1, admin2 });
            theSession.SaveChanges();

            theSession.Query<AdminUser>().Count().ShouldBe(2);
        }
        [Fact]
        public void persist_mutliple_entites_as_enumerable_and_count()
        {
            theSession.Store(new[] { admin1, admin2 }.AsEnumerable());
            theSession.SaveChanges();

            theSession.Query<AdminUser>().Count().ShouldBe(2);
        }
    }

    public class end_to_end_document_hierarchy_usage_Tests: IntegrationContext
    {
        protected AdminUser admin1 = new AdminUser
        {
            UserName = "A2", FirstName = "Derrick", LastName = "Johnson", Region = "Midwest"
        };

        protected AdminUser admin2 = new AdminUser
        {
            UserName = "B2", FirstName = "Eric", LastName = "Berry", Region = "West Coast"
        };

        protected SuperUser super1 = new SuperUser
        {
            UserName = "A3", FirstName = "Dontari", LastName = "Poe", Role = "Expert"
        };

        protected SuperUser super2 = new SuperUser
        {
            UserName = "B3", FirstName = "Sean", LastName = "Smith", Role = "Master"
        };

        protected User user1 = new User {UserName = "A1", FirstName = "Justin", LastName = "Houston"};
        protected User user2 = new User {UserName = "B1", FirstName = "Tamba", LastName = "Hali"};

        protected end_to_end_document_hierarchy_usage_Tests()
        {
            StoreOptions(
                _ =>
                {
                    _.Schema.For<User>().AddSubClass<SuperUser>().AddSubClass<AdminUser>().Duplicate(x => x.UserName);
                });
        }

        protected void loadData()
        {
            theSession.Store(user1, user2, admin1, admin2, super1, super2);

            theSession.SaveChanges();
        }
    }
}
