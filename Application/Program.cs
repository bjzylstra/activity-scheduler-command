using CommandLine;
using System;

namespace ActivityScheduler
{
    class Program
    {
        class Options
        {
            [Option('c', "CamperRequestsPath", Required = true, HelpText = "Path to a CSV file describing the camper activity requests")]
            public String CamperRequestsPath { get; set; }

            [Option('a', "ActivityDefinitionsPath", Required = true, HelpText = "Path to the XML file with the activity definitions")]
            public String ActivityDefinitionsPath { get; set; }
        }

        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args).WithParsed(opts =>
            {
                var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(opts.ActivityDefinitionsPath);

                if (activityDefinitions == null) Environment.Exit(-2);

                Console.WriteLine("Found {0} activity definitions in the file: {1}",
                    activityDefinitions.Count, opts.ActivityDefinitionsPath);

                var camperRequestsList = CamperRequests.ReadCamperRequests(opts.CamperRequestsPath,
                    activityDefinitions);

                if (camperRequestsList == null) Environment.Exit(-2);

                Console.WriteLine("Found {0} campers in the file: {1}",
                    camperRequestsList.Count, opts.CamperRequestsPath);

                Environment.Exit(0);
            }).WithNotParsed(opts =>
            {
                Environment.Exit(-1);
            });
        }
    }
}
