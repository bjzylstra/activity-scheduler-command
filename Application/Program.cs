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
            [Option('r', "CamperRequestsPath", Required = true, HelpText = "Path to a CSV file describing the camper activity requests")]
            public String CamperRequestsPath { get; set; }

            [Option('d', "ActivityDefinitionsPath", Required = true, HelpText = "Path to the XML file with the activity definitions")]
            public String ActivityDefinitionsPath { get; set; }

            [Option('a', "ActivityScheduleCsvPath", HelpText = "Path to where to write the CSV file with the activity schedules")]
            public String ActivityScheduleCsvPath { get; set; }

            [Option('c', "CamperScheduleCsvPath", HelpText = "Path to where to write the CSV file with the camper schedules")]
            public String CamperScheduleCsvPath { get; set; }
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

                // Preload the activity blocks
                foreach (var activity in activityDefinitions)
                {
                    activity.PreloadBlocks();
                }

                List<CamperRequests> unsuccessfulCamperRequests = Scheduler.ScheduleActivities(camperRequestsList);
                foreach (var unhappyCamper in unsuccessfulCamperRequests)
                {
                    List<ActivityRequest> unscheduledActivities = unhappyCamper.ActivityRequests
                        .Where(ar => !unhappyCamper.Camper.ScheduledBlocks.Select(sb => sb.ActivityDefinition).Contains(ar.Activity))
                        .ToList();
                    if (unscheduledActivities.Any(ar => ar.Rank < 3))
                    {
                        Console.Error.WriteLine($"Failed to place {unhappyCamper.Camper} in {String.Join(',', unscheduledActivities.Select(ar => ar.ToString()))} ");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Failed to place {unhappyCamper.Camper} in " +
                            $"{String.Join(',', unscheduledActivities.Select(ar => ar.ToString()))} " +
                            $"or alternate '{unhappyCamper.AlternateActivity.Name}'");
                    }
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

                if (!String.IsNullOrWhiteSpace(opts.ActivityScheduleCsvPath))
                {
                    ActivityDefinition.WriteScheduleToCsvFile(activityDefinitions, opts.ActivityScheduleCsvPath);
                    Console.Out.WriteLine($"Wrote the activity schedule file to '{opts.ActivityScheduleCsvPath}'");
                }

                if (!String.IsNullOrWhiteSpace(opts.CamperScheduleCsvPath))
                {
                    Camper.WriteScheduleToCsvFile(camperRequestsList.Select(cr => cr.Camper), opts.CamperScheduleCsvPath);
                    Console.Out.WriteLine($"Wrote the camper schedule file to '{opts.CamperScheduleCsvPath}'");
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
