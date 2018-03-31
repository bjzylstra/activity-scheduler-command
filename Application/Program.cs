using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;

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

                // Sort the campers by difficulty to resolve activity list.
                // Most difficult go first.
                camperRequestsList.Sort();

                List<CamperRequests> unsuccessfulCamperRequests = Scheduler.ScheduleActivities(camperRequestsList);
                foreach (var unhappyCamper in unsuccessfulCamperRequests)
                {
                    List<string> unscheduledActivities = unhappyCamper.ActivityRequests
                        .Where(ar => !unhappyCamper.Camper.ScheduledBlocks.Select(sb => sb.ActivityDefinition).Contains(ar))
                        .Select(ar => ar.Name).ToList();
                    Console.Error.WriteLine($"Failed to place {unhappyCamper.Camper} in {String.Join(',', unscheduledActivities)} " +
                        $"or alternate {unhappyCamper.AlternateActivity.Name}");
                }

                foreach (var activity in activityDefinitions)
                {
                    foreach (var activityBlock in activity.ScheduledBlocks)
                    {
                        Console.Out.WriteLine($"Scheduled '{activity.Name}' " +
                            $"in block {activityBlock.TimeSlot} " +
                            $"with {activityBlock.AssignedCampers.Count} campers");
                    }
                }

                if (unsuccessfulCamperRequests.Count == 0)
                {
                    Console.Out.WriteLine($"Successfully scheduled {camperRequestsList.Count} " +
                        $"campers into {activityDefinitions.Count} activities");
                    Environment.Exit(0);
                }
                else
                {
                    Console.Error.WriteLine($"Failed to schedule {unsuccessfulCamperRequests.Count} " +
                        $"of {camperRequestsList.Count} campers into " +
                        $"{activityDefinitions.Count} activities");
                    Environment.Exit(-4);
                }
            }).WithNotParsed(opts =>
            {
                Environment.Exit(-1);
            });
        }
    }
}
