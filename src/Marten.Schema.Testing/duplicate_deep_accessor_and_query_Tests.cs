﻿using System.Linq;
using Baseline;
using Marten.Linq;
using Marten.Schema.Testing.Documents;
using Marten.Services;
using Shouldly;
using Xunit;

namespace Marten.Schema.Testing
{
    public class duplicate_deep_accessor_and_query_Tests : IntegrationContext
    {
        [Fact]
        public void duplicate_and_search_off_of_deep_accessor_by_number()
        {
            var targets = Target.GenerateRandomData(10).ToArray();
            StoreOptions(_ =>
            {
                _.Schema.For<Target>().Duplicate(x => x.Inner.Number);
            });

            targets.Each(x => theSession.Store(x));
            theSession.SaveChanges();

            var thirdTarget = targets.ElementAt(2);

            var results = theSession.Query<Target>().Where(x => x.Inner.Number == thirdTarget.Inner.Number).ToArray();
            results
                .Any(x => x.Id == thirdTarget.Id).ShouldBeTrue();
        }

        [Fact]
        public void duplicate_and_search_off_of_deep_accessor_by_date()
        {
            var targets = Target.GenerateRandomData(10).ToArray();
            StoreOptions(_ =>
            {
                _.Schema.For<Target>().Duplicate(x => x.Inner.Date);
            });

            targets.Each(x => theSession.Store(x));
            theSession.SaveChanges();

            var thirdTarget = targets.ElementAt(2);

            var queryable = theSession.Query<Target>().Where(x => x.Inner.Date == thirdTarget.Inner.Date);
            var results = queryable.ToArray();
            results
                .Any(x => x.Id == thirdTarget.Id).ShouldBeTrue();

            queryable.ToCommand(FetchType.FetchMany).CommandText.ShouldContain("inner_date = :p0");
        }

    }
}
