using BenchmarkDotNet.Running;

namespace MartenBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //BenchmarkStore.Store.Advanced.Clean.DeleteAllDocuments();
            //BenchmarkStore.Store.Schema.ApplyAllConfiguredChangesToDatabase();

            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

            //return;
            BenchmarkRunner.Run<DocumentActions>();
            BenchmarkRunner.Run<BulkLoading>();
            BenchmarkRunner.Run<LinqActions>();
            BenchmarkRunner.Run<EventActions>();

            //BenchmarkRunner.Run<StorageFeatureBench>();
            //BenchmarkRunner.Run<IdentityMapBench>();
            //BenchmarkRunner.Run<UnitOfWorkBench>();
        }
    }
}
