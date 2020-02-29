using Camp;
using CommandLine;
using Microsoft.Extensions.Logging;
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

			[Option('o', "UseOptimalLimit", HelpText = "Schedules using optimal activity size and attempts fix ups with maximum size", Default = false)]
			public bool UseOptimalLimit { get; set; }
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
                var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(
                    opts.ActivityDefinitionsPath, logger);

                if (activityDefinitions == null) Environment.Exit(-2);

                logger.LogInformation($"Found {activityDefinitions.Count} activity definitions in the file: {opts.ActivityDefinitionsPath}");

                var camperRequestsList = CamperRequests.ReadCamperRequests(opts.CamperRequestsPath,
                    activityDefinitions, logger);

                if (camperRequestsList == null) Environment.Exit(-2);

                logger.LogInformation($"Found {camperRequestsList.Count} campers in the file: {opts.CamperRequestsPath}");

                // Sort the campers by difficulty to resolve activity list.
                // Most difficult go first.
                camperRequestsList.Sort();

                // Preload the activity blocks
                foreach (var activity in activityDefinitions)
                {
                    activity.PreloadBlocks();
                }

                List<CamperRequests> unsuccessfulCamperRequests = Scheduler.ScheduleActivities(camperRequestsList, 
                    opts.UseOptimalLimit, logger);
				if (opts.UseOptimalLimit && unsuccessfulCamperRequests.Any())
				{
                    logger.LogDebug($"Attempting to resolve {unsuccessfulCamperRequests.Count} " +
						$"unsuccessful camper requests using the activity maximum limits");
					unsuccessfulCamperRequests = Scheduler.ScheduleActivities(unsuccessfulCamperRequests, false, logger);
				}
                foreach (var activity in activityDefinitions)
                {
                    foreach (var activityBlock in activity.ScheduledBlocks)
                    {
                        logger.LogDebug($"Scheduled '{activity.Name}' " +
                            $"in block {activityBlock.TimeSlot} " +
                            $"with {activityBlock.AssignedCampers.Count} campers");
                    }
                }

                if (!String.IsNullOrWhiteSpace(opts.ActivityScheduleCsvPath))
                {
                    ActivityDefinition.WriteScheduleToCsvFile(activityDefinitions, opts.ActivityScheduleCsvPath, logger);
                    logger.LogInformation($"Wrote the activity schedule file to '{opts.ActivityScheduleCsvPath}'");
                }

                if (!String.IsNullOrWhiteSpace(opts.CamperScheduleCsvPath))
                {
                    Camper.WriteScheduleToCsvFile(camperRequestsList.Select(cr => cr.Camper), opts.CamperScheduleCsvPath, logger);
                    logger.LogInformation($"Wrote the camper schedule file to '{opts.CamperScheduleCsvPath}'");
                }

                Console.Out.WriteLine();
                foreach (var unhappyCamper in unsuccessfulCamperRequests)
                {
					List<ActivityRequest> unscheduledActivities = unhappyCamper.UnscheduledActivities;
                    if (unscheduledActivities.Any(ar => ar.Rank < 3))
                    {
                        logger.LogInformation($"Failed to place {unhappyCamper.Camper} in {String.Join(',', unscheduledActivities.Select(ar => ar?.ToString()))} ");
                    }
                    else
                    {
                        logger.LogInformation($"Failed to place {unhappyCamper.Camper} in " +
                            $"{String.Join(',', unscheduledActivities.Select(ar => ar?.ToString()))} " +
                            $"or alternate '{unhappyCamper.AlternateActivity?.Name}'");
                    }
                }

                if (unsuccessfulCamperRequests.Count == 0)
                {
                    logger.LogInformation($"Successfully scheduled {camperRequestsList.Count} " +
                        $"campers into {activityDefinitions.Count} activities");
                    Environment.Exit(0);
                }
                else
                {
                    logger.LogInformation($"Failed to schedule {unsuccessfulCamperRequests.Count} " +
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
