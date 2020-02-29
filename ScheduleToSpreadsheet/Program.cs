using OfficeOpenXml;
using CommandLine;
using System;
using System.IO;
using Camp;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

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

		class LoggerConverter : ILogger
		{
			private readonly NLog.ILogger _nlogger;

			public LoggerConverter(NLog.ILogger nlogger)
			{
				_nlogger = nlogger;
			}

			public IDisposable BeginScope<TState>(TState state)
			{
				throw new NotImplementedException();
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				switch (logLevel)
				{
					case LogLevel.Critical:
						return _nlogger.IsFatalEnabled;
					case LogLevel.Error:
						return _nlogger.IsErrorEnabled;
					case LogLevel.Warning:
						return _nlogger.IsWarnEnabled;
					case LogLevel.Information:
						return _nlogger.IsInfoEnabled;
					case LogLevel.Debug:
						return _nlogger.IsDebugEnabled;
					case LogLevel.Trace:
					default:
						return _nlogger.IsTraceEnabled;
				}
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
			{
				string message = formatter.Invoke(state, exception);
				switch (logLevel)
				{
					case LogLevel.Critical:
						_nlogger.Fatal(message);
						break;
					case LogLevel.Error:
						_nlogger.Error(message);
						break;
					case LogLevel.Warning:
						_nlogger.Warn(message);
						break;
					case LogLevel.Information:
						_nlogger.Info(message);
						break;
					case LogLevel.Debug:
						_nlogger.Debug(message);
						break;
					case LogLevel.Trace:
					default:
						_nlogger.Trace(message);
						break;
				}
			}
		}

		static void Main(string[] args)
		{
			var logger = new LoggerConverter(NLog.Web.NLogBuilder
				.ConfigureNLog("nlog.config")
				.GetCurrentClassLogger());
			Parser.Default.ParseArguments<Options>(args).WithParsed(opts =>
			{
				List<ActivityDefinition> activitySchedule =
					ActivityDefinition.ReadScheduleFromCsvFile(
						opts.ActivityScheduleCsvPath, logger);

				if (activitySchedule == null) Environment.Exit(-2);

				// If activity definition is included, fold in the limit numbers into the schedule
				if (!String.IsNullOrEmpty(opts.ActivityDefinitionsPath))
				{
					var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(
						opts.ActivityDefinitionsPath, logger);

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
				activitySheet.AddVisualBasicCode();
				activitySheet.AddCommands();

				CamperSheet camperSheet = new CamperSheet(activitySchedule, excelPackage.Workbook);

				camperSheet.BuildWorksheet();
				camperSheet.AddVisualBasicCode();
				camperSheet.AddCommands();

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
