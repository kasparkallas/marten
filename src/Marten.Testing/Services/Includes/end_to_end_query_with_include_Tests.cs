﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Baseline;
using Marten.Linq;
using Marten.Services;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Marten.Testing.Linq;
using Marten.Util;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Issue = Marten.Testing.Documents.Issue;
using User = Marten.Testing.Documents.User;

namespace Marten.Testing.Services.Includes
{
    public class end_to_end_query_with_include_Tests : IntegrationContext
    {
        private readonly ITestOutputHelper _output;

        [Fact]
        public async Task include_within_batch_query()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted #1" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted #2" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted #3" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            await theSession.SaveChangesAsync();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var list = new List<User>();
                var dict = new Dictionary<Guid, User>();

                // SAMPLE: batch_include
                var batch = query.CreateBatchQuery();

                var found = batch.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Title == issue1.Title)
                    .Single();
                // ENDSAMPLE

                var toList = batch.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, list).ToList();

                var toDict = batch.Query<Issue>()
                    .Include(x => x.AssigneeId, dict).ToList();

                await batch.Execute().ConfigureAwait(false);


                (await found).Id.ShouldBe(issue1.Id);

                included.ShouldNotBeNull();
                included.Id.ShouldBe(user1.Id);

                (await toList).Count.ShouldBe(3);

                list.Count.ShouldBe(2); // Only 2 users


                (await toDict).Count.ShouldBe(3);

                dict.Count.ShouldBe(2);

                dict.ContainsKey(user1.Id).ShouldBeTrue();
                dict.ContainsKey(user2.Id).ShouldBeTrue();

            }
        }

        // SAMPLE: simple_include
        [Fact]
        public void simple_include_for_a_single_document()
        {
            var user = new User();
            var issue = new Issue {AssigneeId = user.Id, Title = "Garage Door is busted"};

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query
                    .Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Single(x => x.Title == issue.Title);

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }
        // ENDSAMPLE

        [Fact]
        public void include_with_containment_where_for_a_single_document()
        {
            var user = new User();
            var issue = new Issue {AssigneeId = user.Id, Tags = new []{"DIY"}, Title = "Garage Door is busted"};

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query
                    .Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Single(x => Enumerable.Contains(x.Tags, "DIY"));

                included.ShouldNotBeNull();
                included.Id.ShouldBe(user.Id);

                issue2.ShouldNotBeNull();
            }
        }


        [Fact]
        public void include_with_containment_where_for_a_single_document_with_camel_casing()
        {
            StoreOptions(_ => _.UseDefaultSerialization(casing:Casing.CamelCase));

            var user = new User();
            var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Contains("DIY"))
                    .Single();

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void include_with_any_containment_where_for_a_single_document()
        {
            var user = new User();
            var issue = new Issue {AssigneeId = user.Id, Tags = new []{"DIY"}, Title = "Garage Door is busted"};

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Any(t=>t=="DIY"))
                    .Single();

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void include_with_any_containment_where_for_a_single_document_with_camel_casing_2()
        {
            StoreOptions(_ => _.UseDefaultSerialization(EnumStorage.AsString, Casing.CamelCase));

            var user = new User();
            var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Any(t => t == "DIY"))
                    .Single();

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void include_with_any_containment_where_for_a_single_document_with_snake_casing_2()
        {
            StoreOptions(_ => _.UseDefaultSerialization(EnumStorage.AsString, Casing.SnakeCase));

            var user = new User();
            var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Any(t => t == "DIY"))
                    .Single();

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void include_with_any_containment_where_for_a_single_document_with_camel_casing()
        {
            StoreOptions(_ => _.UseDefaultSerialization(casing:Casing.CamelCase));

            var user = new User();
            var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Any(t => t == "DIY"))
                    .Single();

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void include_with_any_containment_where_for_a_single_document_with_snake_casing()
        {
            StoreOptions(_ => _.UseDefaultSerialization(casing: Casing.SnakeCase));

            var user = new User();
            var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Any(t => t == "DIY"))
                    .Single();

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void include_with_any_array_containment_where_for_a_single_document()
        {
            var user  = new User();
            var issue1 = new Issue {AssigneeId = user.Id, Tags = new []{"DIY"}, Title = "Garage Door is busted"};
            var issue2 = new Issue {AssigneeId = user.Id, Tags = new []{"TAG"}, Title = "Garage Door is busted"};
            var issue3 = new Issue {AssigneeId = user.Id, Tags = new string[] { }, Title = "Garage Door is busted"};

            var requestedTags = new[] {"DIY", "TAG"};

            theSession.Store(user);
            theSession.Store(issue1, issue2, issue3);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var users = new List<User>();
                var issues = query.Query<Issue>()
                                  .Include(x => x.AssigneeId, users)
                                  .Where(x => x.Tags.Any(t => requestedTags.Contains(t)))
                                  .ToList();

                users.Count.ShouldBe(1);
                SpecificationExtensions.ShouldContain(users, x => x.Id == user.Id);

                issues.Count.ShouldBe(2);
                SpecificationExtensions.ShouldContain(issues, x => x.Id == issue1.Id);
                SpecificationExtensions.ShouldContain(issues, x => x.Id == issue2.Id);
            }
        }

        [Fact]
        public void include_with_generic_type()
        {
            var user = new UserWithInterface { Id = Guid.NewGuid(), UserName = "Jens" };
            var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            IncludeGeneric<UserWithInterface>(user);
        }

        private void IncludeGeneric<T>(UserWithInterface userToCompareAgainst) where T : IUserWithInterface
        {
            using (var query = theStore.QuerySession())
            {
                T included = default(T);
                var issue2 = query.Query<Issue>()
                    .Include<T>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Any(t => t == "DIY"))
                    .Single();

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(userToCompareAgainst.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void simple_include_for_a_single_document_using_outer_join()
        {
            var issue = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

            theSession.Store(issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Title == issue.Title)
                    .Single();

                SpecificationExtensions.ShouldBeNull(included);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public void include_to_list()
        {
            var user1 = new User{FirstName = "Travis", LastName = "Kelce"};
            var user2 = new User{FirstName = "Tyrann", LastName = "Mathieu"};

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted 1" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted 2" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted 3" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                var issues = query.Query<Issue>().Include<User>(x => x.AssigneeId, list).ToArray();

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id).ShouldBeTrue();
                list.Any(x => x.Id == user2.Id).ShouldBeTrue();
            }
        }

        [Fact]
        public void include_to_list_using_inner_join()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3, issue4);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                var issues = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, list)
                    .Where(x => x.AssigneeId.HasValue)
                    .ToArray();

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id).ShouldBeTrue();
                list.Any(x => x.Id == user2.Id).ShouldBeTrue();

                issues.Length.ShouldBe(3);
            }
        }

        [Fact]
        public void include_to_list_using_outer_join()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3, issue4);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                var issues = query.Query<Issue>().Include<User>(x => x.AssigneeId, list).ToArray();

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id).ShouldBeTrue();
                list.Any(x => x.Id == user2.Id).ShouldBeTrue();
                list.Any(x => x == null).ShouldBeFalse();

                issues.Length.ShouldBe(4);
            }
        }

        [Fact]
        public void include_is_running_through_identitymap()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            theSession.SaveChanges();

            // This will only work with a non-NulloIdentityMap
            using (var query = theStore.OpenSession())
            {
                var dict = new Dictionary<Guid, User>();

                query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

                query.Load<User>(user1.Id).ShouldBeSameAs(dict[user1.Id]);
                query.Load<User>(user2.Id).ShouldBeSameAs(dict[user2.Id]);
            }
        }

        // SAMPLE: dictionary_include
        [Fact]
        public void include_to_dictionary()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var dict = new Dictionary<Guid, User>();

                query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

                dict.Count.ShouldBe(2);
                dict.ContainsKey(user1.Id).ShouldBeTrue();
                dict.ContainsKey(user2.Id).ShouldBeTrue();
            }
        }
        // ENDSAMPLE

        [Fact]
        public void include_to_dictionary_using_inner_join()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

            theSession.Store(user1,  user2);
            theSession.Store(issue1, issue2, issue3, issue4);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var dict = new Dictionary<Guid, User>();

                var issues = query
                    .Query<Issue>()
                    .Include(x => x.AssigneeId, dict)
                    .Where(x => x.AssigneeId.HasValue).ToArray();

                dict.Count.ShouldBe(2);
                dict.ContainsKey(user1.Id).ShouldBeTrue();
                dict.ContainsKey(user2.Id).ShouldBeTrue();

                issues.Length.ShouldBe(3);
            }
        }

        [Fact]
        public void include_to_dictionary_using_outer_join()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

            theSession.Store(user1,  user2);
            theSession.Store(issue1, issue2, issue3, issue4);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var dict = new Dictionary<Guid, User>();

                var issues = query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

                dict.Count.ShouldBe(2);
                dict.ContainsKey(user1.Id).ShouldBeTrue();
                dict.ContainsKey(user2.Id).ShouldBeTrue();

                issues.Length.ShouldBe(4);
            }
        }

        [Fact]
        public async Task simple_include_for_a_single_document_async()
        {
            var user = new User();
            var issue = new Issue { AssigneeId = user.Id, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            await theSession.SaveChangesAsync().ConfigureAwait(false);

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = await query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Title == issue.Title)
                    .SingleAsync().ConfigureAwait(false);

                SpecificationExtensions.ShouldNotBeNull(included);
                included.Id.ShouldBe(user.Id);

                SpecificationExtensions.ShouldNotBeNull(issue2);
            }
        }

        [Fact]
        public async Task include_to_list_async()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            await theSession.SaveChangesAsync().ConfigureAwait(false);

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                await query.Query<Issue>().Include(x => x.AssigneeId, list).ToListAsync().ConfigureAwait(false);

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id);
                list.Any(x => x.Id == user2.Id);
            }
        }

        [Fact]
        public async Task include_to_list_with_orderby_descending_async()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            await theSession.SaveChangesAsync().ConfigureAwait(false);

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                await query.Query<Issue>().Include(x => x.AssigneeId, list)
                           .OrderByDescending(x => x.Id)
                           .ToListAsync().ConfigureAwait(false);

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id);
                list.Any(x => x.Id == user2.Id);
            }
        }

        [Fact]
        public async Task include_to_list_with_orderby_ascending_async()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            await theSession.SaveChangesAsync().ConfigureAwait(false);

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                await query.Query<Issue>().Include(x => x.AssigneeId, list)
                           .OrderBy(x => x.Id)
                           .ToListAsync().ConfigureAwait(false);

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id);
                list.Any(x => x.Id == user2.Id);
            }
        }

        [Fact]
        public async Task include_to_dictionary_async()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            await theSession.SaveChangesAsync().ConfigureAwait(false);

            using (var query = theStore.QuerySession())
            {
                var dict = new Dictionary<Guid, User>();

                await query.Query<Issue>().Include(x => x.AssigneeId, dict).ToListAsync().ConfigureAwait(false);

                dict.Count.ShouldBe(2);
                dict.ContainsKey(user1.Id).ShouldBeTrue();
                dict.ContainsKey(user2.Id).ShouldBeTrue();
            }


        }

        // SAMPLE: multiple_include
        [Fact]
        public void multiple_includes()
        {
            var assignee = new User();
            var reporter = new User();

            var issue1 = new Issue { AssigneeId = assignee.Id, ReporterId = reporter.Id, Title = "Garage Door is busted" };

            theSession.Store(assignee, reporter);
            theSession.Store(issue1);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User assignee2 = null;
                User reporter2 = null;

                query
                        .Query<Issue>()
                        .Include<User>(x => x.AssigneeId, x => assignee2 = x)
                        .Include<User>(x => x.ReporterId, x => reporter2 = x)
                        .Single()
                        .ShouldNotBeNull();

                assignee2.Id.ShouldBe(assignee.Id);
                reporter2.Id.ShouldBe(reporter.Id);

            }
        }
        // ENDSAMPLE

        public class Group
        {
            public string Name { get; set; }
            public Guid Id { get; set; }
            public Guid[] Users { get; set; }
        }

        [Fact]
        public void include_many_to_list()
        {
            var user1 = new User { };
            var user2 = new User { };
            var user3 = new User { };
            var user4 = new User { };
            var user5 = new User { };
            var user6 = new User { };
            var user7 = new User { };

            theStore.BulkInsert(new User[]{user1, user2, user3, user4, user5, user6, user7});

            var group1 = new Group
            {
                Name = "Odds",
                Users = new []{user1.Id, user3.Id, user5.Id, user7.Id}
            };

            var group2 = new Group {Name = "Evens", Users = new[] {user2.Id, user4.Id, user6.Id}};

            using (var session = theStore.OpenSession())
            {
                session.Store(group1, group2);
                session.SaveChanges();
            }

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                query.Query<Group>()
                    .Include(x => x.Users, list)
                    .Where(x => x.Name == "Odds")
                    .ToList()
                    .Single()
                    .Name.ShouldBe("Odds");

                list.Count.ShouldBe(4);
                list.Any(x => x.Id == user1.Id).ShouldBeTrue();
                list.Any(x => x.Id == user3.Id).ShouldBeTrue();
                list.Any(x => x.Id == user5.Id).ShouldBeTrue();
                list.Any(x => x.Id == user7.Id).ShouldBeTrue();
            }
        }

        public end_to_end_query_with_include_Tests(DefaultStoreFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _output = output;

            DocumentTracking = DocumentTracking.IdentityOnly;
        }
    }
}
