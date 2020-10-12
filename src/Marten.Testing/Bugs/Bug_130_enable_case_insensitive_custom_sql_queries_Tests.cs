using System.Linq;
using Marten.Services;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Bugs
{
    public class Bug_130_enable_case_insensitive_custom_sql_queries_Tests: BugIntegrationContext
    {
        [Fact]
        public void query()
        {
            var entity = new Target();
            theSession.Store(entity);
            theSession.SaveChanges();

            theSession.Query<Target>($"SELECT data FROM {SchemaName}.mt_doc_target").Single().Id.ShouldBe(entity.Id);
        }
    }
}
