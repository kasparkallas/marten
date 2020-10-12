#if NET461
using System.Transactions;
#endif

namespace Marten.Testing.Bugs
{
#if NET461
    public class Bug_949_connection_closing_issue_with_transaction_scope : IntegratedFixture
    {
        [Fact]
        public void do_not_blow_up_with_too_many_open_connections()
        {
            for (int i = 0; i < 1000; i++)
            {
                // this reaches 200, than crashes

                using (var scope = new TransactionScope())
                {
                    using (var session = theStore.OpenSession(SessionOptions.ForCurrentTransaction()))
                    {
                        session.Store(new EntityToSave());
                        session.SaveChanges();
                    }

                    scope.Complete();
                }
            }

            using (var session = theStore.QuerySession())
            {
                session.Query<EntityToSave>().Count().ShouldBe(1000);
            }
        }
    }

    class EntityToSave
    {
        public Guid Id { get; set; }
    }
#endif
}
