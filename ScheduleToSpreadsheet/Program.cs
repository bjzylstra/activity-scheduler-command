using OfficeOpenXml;
using CommandLine;
using System;
using System.IO;
using Camp;
using System.Collections.Generic;

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
        }
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(opts =>
            {
                List<ActivityDefinition> activitySchedule =
                    ActivityDefinition.ReadScheduleFromCsvFile(opts.ActivityScheduleCsvPath);

                if (activitySchedule == null) Environment.Exit(-2);

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
