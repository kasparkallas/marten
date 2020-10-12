﻿using Marten.Services;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.CoreFunctionality
{

    public class persist_and_deleting_multiple_documents_Tests : IntegrationContext
    {
        [Theory]
        [SessionTypes]
        public void multiple_documents(DocumentTracking tracking)
        {
            DocumentTracking = tracking;

            // SAMPLE: mixed-docs-to-store
            var user1 = new User {FirstName = "Jeremy", LastName = "Miller"};
            var issue1 = new Issue {Title = "TV won't turn on"}; // unfortunately true as I write this...
            var company1 = new Company{Name = "Widgets, inc."};
            var company2 = new Company{Name = "BigCo"};
            var company3 = new Company{Name = "SmallCo"};

            theSession.Store<object>(user1, issue1, company1, company2, company3);
            // ENDSAMPLE

            theSession.SaveChanges();

            using (var session = theStore.OpenSession())
            {
                var user = session.Load<User>(user1.Id);
                user.FirstName = "Max";

                session.Store(user);

                session.Delete(company2);

                session.SaveChanges();
            }

            using (var session = theStore.QuerySession())
            {
                session.Load<User>(user1.Id).FirstName.ShouldBe("Max");
                session.Load<Company>(company1.Id).Name.ShouldBe("Widgets, inc.");
                SpecificationExtensions.ShouldBeNull(session.Load<Company>(company2.Id));
                session.Load<Company>(company3.Id).Name.ShouldBe("SmallCo");
            }
        }

        public persist_and_deleting_multiple_documents_Tests(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }
}
