using Ookii.CommandLine;
using System;
using System.ComponentModel;

namespace ActivityScheduler
{
    class ProgramArguments
    {
        [CommandLineArgument(IsRequired = true), Alias("i"), Description("Path to a CSV file describing the camper activity requests")]
        public String InputCSV { get; set; }

        [CommandLineArgument, Alias("?"), Description("Displays this help message.")]
        public bool Help { get; set; }

        public static ProgramArguments Create(string[] args)
        {
            CommandLineParser parser = new CommandLineParser(typeof(ProgramArguments));
            // The ArgumentParsed event is used by this sample to stop parsing after the -Help argument is specified.
            parser.ArgumentParsed += CommandLineParser_ArgumentParsed;
            try
            {
                // The Parse function returns null only when the ArgumentParsed event handler cancelled parsing.
                ProgramArguments result = (ProgramArguments)parser.Parse(args);
                if (result != null)
                    return result;
            }
            catch (CommandLineArgumentException ex)
            {
                // We use the LineWrappingTextWriter to neatly wrap console output.
                using (LineWrappingTextWriter writer = LineWrappingTextWriter.ForConsoleError())
                {
                    // Tell the user what went wrong.
                    writer.WriteLine(ex.Message);
                    writer.WriteLine();
                }
            }

            // If we got here, we should print usage information to the console.
            // By default, aliases and default values are not included in the usage descriptions; for this sample, I do want to include them.
            WriteUsageOptions options = new WriteUsageOptions() { IncludeDefaultValueInDescription = true, IncludeAliasInDescription = true };
            // WriteUsageToConsole automatically uses a LineWrappingTextWriter to properly word-wrap the text.
            parser.WriteUsageToConsole(options);
            return null;
        }

        private static void CommandLineParser_ArgumentParsed(object sender, ArgumentParsedEventArgs e)
        {
            // When the -Help argument (or -? using its alias) is specified, parsing is immediately cancelled. That way, CommandLineParser.Parse will
            // return null, and the Create method will display usage even if the correct number of positional arguments was supplied.
            // Try it: just call the sample with "CommandLineSampleCS.exe foo bar -Help", which will print usage even though both the Source and Destination
            // arguments are supplied.
            if (e.Argument.ArgumentName == "Help") // The name is always Help even if the alias was used to specify the argument
                e.Cancel = true;
        }
    }
}
