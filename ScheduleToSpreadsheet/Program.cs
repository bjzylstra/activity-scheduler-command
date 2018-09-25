using OfficeOpenXml;
using CommandLine;
using System;
using System.IO;
using Camp;
using System.Collections.Generic;
using System.Linq;

namespace ScheduleToSpreadsheet
{
    class Program
    {
        class Options
        {
            [Option('a', "ActivityScheduleCsvPath", Required = true, HelpText = "Path to the CSV file with the activity schedules")]
            public String ActivityScheduleCsvPath { get; set; }

            [Option('s', "ScheduleExcelPath", Required = true, HelpText = "Path to the output Excel spreadsheet file")]
            public String ScheduleExcelPath { get; set; }

            [Option('d', "ActivityDefinitionsPath", Required = false, HelpText = "Path to the XML file with the activity definitions")]
            public String ActivityDefinitionsPath { get; set; }

        }
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(opts =>
            {
                List<ActivityDefinition> activitySchedule =
                    ActivityDefinition.ReadScheduleFromCsvFile(opts.ActivityScheduleCsvPath);

                if (activitySchedule == null) Environment.Exit(-2);

                // If activity definition is included, fold in the limit numbers into the schedule
                if (!String.IsNullOrEmpty(opts.ActivityDefinitionsPath))
                {
                    var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(opts.ActivityDefinitionsPath);

                    if (activityDefinitions == null) Environment.Exit(-2);

                    foreach (var activityDefinition in activityDefinitions)
                    {
                        var matchingSchedule = activitySchedule
                            .First(activity => activity.Name.Equals(activityDefinition.Name));

                        if (matchingSchedule != null)
                        {
                            matchingSchedule.MaximumCapacity = activityDefinition.MaximumCapacity;
                            matchingSchedule.OptimalCapacity = activityDefinition.OptimalCapacity;
                            matchingSchedule.MinimumCapacity = activityDefinition.MinimumCapacity;
                        }
                    }
                }

                //Creates a blank workbook. Use the using statment, so the package is disposed when we are done.
                using (var excelApplication = new ExcelPackage())
                {
                    ActivitySheet activitySheet = new ActivitySheet(activitySchedule);

                    activitySheet.AddToWorkbook(excelApplication.Workbook);

                    CamperSheet camperSheet = new CamperSheet(activitySchedule);

                    camperSheet.AddToWorkbook(excelApplication.Workbook);

                    //Save the new workbook. We haven't specified the filename so use the Save as method.
                    excelApplication.SaveAs(new FileInfo(opts.ScheduleExcelPath));
                }

                Console.Out.WriteLine($"Ran with {opts.ActivityScheduleCsvPath}");
                Environment.Exit(0);
            }).WithNotParsed(opts =>
            {
                Environment.Exit(-1);
            });
        }
    }
}
