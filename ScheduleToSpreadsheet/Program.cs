using OfficeOpenXml;
using CommandLine;
using System;
using System.IO;
using Camp;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.VBA;

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

				CreateWorkbook(opts, activitySchedule);

				Console.Out.Write($"Generated {opts.ScheduleExcelPath} " +
					$"from {opts.ActivityScheduleCsvPath}");
				if (!String.IsNullOrEmpty(opts.ActivityDefinitionsPath))
				{
					Console.Out.Write($" and {opts.ActivityDefinitionsPath}");
				}
				Console.Out.WriteLine();
				Environment.Exit(0);
			}).WithNotParsed(opts =>
			{
				Environment.Exit(-1);
			});
		}

		private static void CreateWorkbook(Options opts, List<ActivityDefinition> activitySchedule)
		{
			//Creates a blank workbook. Use the using statment, so the package is disposed when we are done.
			using (var excelPackage = new ExcelPackage())
			{
				ActivitySheet activitySheet = new ActivitySheet(activitySchedule, excelPackage.Workbook);

				activitySheet.BuildWorksheet();
				activitySheet.AddMacros();

				CamperSheet camperSheet = new CamperSheet(activitySchedule, excelPackage.Workbook);

				camperSheet.BuildWorksheet();
				camperSheet.AddMacros();

				try
				{
					//Save the new workbook. We haven't specified the filename so use the Save as method.
					excelPackage.SaveAs(new FileInfo(opts.ScheduleExcelPath));
				}
				catch (InvalidOperationException e)
				{
					Console.Error.WriteLine($"Could not create spreadsheet file due to '{e.Message}'");
					Environment.Exit(-2);
				}
			}
		}
	}
}
