using System;
using System.IO;
using Baseline;
using Oakton;

namespace Marten.CommandLine.Commands.Dump
{
    [Description("Dumps the entire DDL for the configured Marten database")]
    public class DumpCommand: MartenCommand<DumpInput>
    {
        public DumpCommand()
        {
            Usage("Writes the complete DDL for the entire Marten configuration to the named file")
                .Arguments(x => x.FileName);
        }

        protected override bool execute(IDocumentStore store, DumpInput input)
        {
            if (input.ByTypeFlag)
            {
                input.WriteLine("Writing DDL files to " + input.FileName);
                store.Schema.WriteDDLByType(input.FileName, input.TransactionalScriptFlag);

                // You only need to clean out the existing folder when dumping
                // by type
                try
                {
                    if (Directory.Exists(input.FileName))
                    {
                        new FileSystem().CleanDirectory(input.FileName);
                    }
                }
                catch (Exception)
                {
                    input.WriteLine(ConsoleColor.Yellow, $"Unable to clean the directory at {input.FileName} before writing new files");
                }
            }
            else
            {
                input.WriteLine("Writing DDL file to " + input.FileName);



                store.Schema.WriteDDL(input.FileName, input.TransactionalScriptFlag);
            }

            return true;
        }
    }
}
