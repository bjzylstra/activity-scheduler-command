using OfficeOpenXml;
using CommandLine;
using System;
using System.IO;

namespace ScheduleToSpreadsheet
{
    class Program
    {
        class Options
        {
            [Option('a', "ActivityScheduleCsvPath", Required = true, HelpText = "Path to the CSV file with the activity schedules")]
            public String ActivityScheduleCsvPath { get; set; }

            [Option('c', "CamperScheduleCsvPath", Required = true, HelpText = "Path to the CSV file with the camper schedules")]
            public String CamperScheduleCsvPath { get; set; }

            [Option('s', "ScheduleExcelPath", Required = true, HelpText = "Path to the output Excel spreadsheet file")]
            public String ScheduleExcelPath { get; set; }
        }
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(opts =>
            {
                //Creates a blank workbook. Use the using statment, so the package is disposed when we are done.
                using (var p = new ExcelPackage())
                {
                    FileInfo activityCsv = new FileInfo(opts.ActivityScheduleCsvPath);
                    if (activityCsv.Exists)
                    {
                        ExcelWorksheet ws = p.Workbook.Worksheets.Add("Activities");
                        //To set values in the spreadsheet use the Cells indexer.
                        ws.Cells.LoadFromText(activityCsv);

                    }

                    FileInfo camperCsv = new FileInfo(opts.CamperScheduleCsvPath);
                    if (camperCsv.Exists)
                    {
                        ExcelWorksheet ws = p.Workbook.Worksheets.Add("Campers");
                        //To set values in the spreadsheet use the Cells indexer.
                        ws.Cells.LoadFromText(camperCsv);

                    }
                    //Save the new workbook. We haven't specified the filename so use the Save as method.
                    p.SaveAs(new FileInfo(opts.ScheduleExcelPath));
                }

                Console.Out.WriteLine($"Ran with {opts.ActivityScheduleCsvPath} and {opts.CamperScheduleCsvPath}");
                Environment.Exit(0);
            }).WithNotParsed(opts =>
            {
                Environment.Exit(-1);
            });
        }
    }
}
