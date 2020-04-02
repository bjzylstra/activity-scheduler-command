using Camp;
using GridMvc.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ActivitySchedulerFrontEnd.Services
{
	public class SchedulerService : ISchedulerService
	{
		public const string UnscheduledActivityName = " Unscheduled";

		private readonly string _applicationName;
		private readonly ILogger<SchedulerService> _logger;
		private Dictionary<string,List<ActivityDefinition>> _schedulesById = new Dictionary<string,List<ActivityDefinition>>();
		private const string ScheduleFileExtension = ".csv";

		/// <summary>
		/// Default constructor used by dependency injection.
		/// </summary>
		/// <param name="logger">Logger</param>
		public SchedulerService(ILogger<SchedulerService> logger)
		{
			_logger = logger;
			_applicationName = Assembly.GetEntryAssembly().GetName().Name;
			_schedulesById = LoadSchedulesFromPersistence(_applicationName);
		}

		/// <summary>
		/// Constructor with a fixed application name for testing
		/// </summary>
		/// <param name="folderName">Application name for local application data folder</param>
		/// <param name="logger">Logger</param>
		public SchedulerService(string folderName, ILogger<SchedulerService> logger)
		{
			_logger = logger;
			_applicationName = folderName;
			_schedulesById = LoadSchedulesFromPersistence(_applicationName);
		}

		/// <summary>
		/// Load the schedules from the local application data folder.
		/// Creates the folder if it is not found.
		/// </summary>
		/// <param name="applicationName">Application name for local applications data folder</param>
		/// <returns>Dictionary of schedules by schedule Id</returns>
		private Dictionary<string, List<ActivityDefinition>> LoadSchedulesFromPersistence(string applicationName)
		{
			Dictionary<string, List<ActivityDefinition>> schedulesById = new Dictionary<string, List<ActivityDefinition>>();
			try
			{
				DirectoryInfo dataDirectoryInfo = new DirectoryInfo(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
				DirectoryInfo applicationDirectoryInfo = dataDirectoryInfo.GetDirectories().FirstOrDefault(d =>
					d.Name.Equals(applicationName, StringComparison.OrdinalIgnoreCase));
				if (applicationDirectoryInfo == null)
				{
					// First time - load default from the embedded resource
					applicationDirectoryInfo = dataDirectoryInfo.CreateSubdirectory(applicationName);
				}

				foreach (var scheduleFile in applicationDirectoryInfo.EnumerateFiles()
					.Where(f => f.Extension.Equals(ScheduleFileExtension, StringComparison.OrdinalIgnoreCase)))
				{
					List<ActivityDefinition> schedule = ActivityDefinition.ReadScheduleFromCsvFile(
						scheduleFile.FullName, _logger);
					if (schedule != null)
					{
						string scheduleId = scheduleFile.Name.Substring(0,
							scheduleFile.Name.Length - ScheduleFileExtension.Length);
						schedulesById.Add(scheduleId, schedule);
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "LoadSchedulesFromPersistence failed");
			}
			return schedulesById;
		}

		public List<string> GetScheduleIds()
		{
			return _schedulesById.Keys.ToList();
		}

		public List<ActivityDefinition> GetSchedule(string scheduleId)
		{
			return LookupScheduleById(scheduleId);
		}

		public List<ActivityDefinition> CreateSchedule(string scheduleId, List<CamperRequests> camperRequests, 
			List<ActivityDefinition> activityDefinitions)
		{
			List<CamperRequests> unscheduledCamperRequests = Scheduler.ScheduleActivities(camperRequests, activityDefinitions, _logger);
			if (unscheduledCamperRequests.Any())
			{
				// Put the unscheduled blocks into a special unscheduled activity
				ActivityDefinition unscheduledActivity = new ActivityDefinition
				{
					Name = UnscheduledActivityName,
					MaximumCapacity = int.MaxValue,
					OptimalCapacity = 0
				};
				unscheduledActivity.PreloadBlocks();
				foreach (Camper unscheduledCamper in unscheduledCamperRequests.Select(cr => cr.Camper))
				{
					int[] blockIds = { 0, 1, 2, 3 };
					foreach (int unscheduledBlockId in blockIds
						.Except(unscheduledCamper.ScheduledBlocks.Select(b => b.TimeSlot)))
					{
						unscheduledActivity.TryAssignCamperToExistingActivityBlock(unscheduledCamper, false);
					}
				}
				// Put unscheduled activity at the top of the grid
				activityDefinitions.Insert(0, unscheduledActivity);
			}

			// Generate the schedule ID from the date.
			_schedulesById[scheduleId] = activityDefinitions;
			UpdateSchedule(scheduleId, activityDefinitions);

			return activityDefinitions;
		}

		public void UpdateSchedule(string scheduleId, List<ActivityDefinition> schedule)
		{
			DirectoryInfo dataDirectoryInfo = new DirectoryInfo(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
			// Created in constructor so it really should be there.
			DirectoryInfo applicationDirectoryInfo = dataDirectoryInfo.GetDirectories().FirstOrDefault(d =>
				d.Name.Equals(_applicationName, StringComparison.OrdinalIgnoreCase));
			string fileName = $"{applicationDirectoryInfo.FullName}\\{scheduleId}{ScheduleFileExtension}";
			ActivityDefinition.WriteScheduleToCsvFile(schedule, fileName, _logger);
			_schedulesById[scheduleId] = schedule;
		}

		private List<ActivityDefinition> LookupScheduleById(string scheduleId)
		{
			if (!_schedulesById.TryGetValue(scheduleId, out List<ActivityDefinition> schedule))
			{
				// Schedule is not found, use an empty schedule.
				schedule = new List<ActivityDefinition>();
			}
			return schedule;
		}

		public ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId, Action<IGridColumnCollection<IActivityBlock>> columns, QueryDictionary<StringValues> query)
		{
			var server = new GridServer<IActivityBlock>(
				LookupScheduleById(scheduleId).SelectMany(ad => ad.ScheduledBlocks),
				new QueryCollection(query), true,
			"activityScheduleGrid", columns);

			return server.ItemsToDisplay;
		}

		public ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId, QueryDictionary<StringValues> query)
		{
			var server = new GridServer<IActivityBlock>(
				LookupScheduleById(scheduleId).SelectMany(ad => ad.ScheduledBlocks),
				new QueryCollection(query), true,
			"activityScheduleGrid", null).AutoGenerateColumns();

			return server.ItemsToDisplay;
		}

		public string WriteActivityScheduleToCsv(string scheduleId)
		{
			return ActivityDefinition.WriteScheduleToCsvString(
				LookupScheduleById(scheduleId), _logger);
		}

		public string WriteCamperScheduleToCsv(string scheduleId)
		{
			List<Camper> campers = LookupScheduleById(scheduleId)
				.SelectMany(ad => ad.ScheduledBlocks.SelectMany(b => b.AssignedCampers))
				.Distinct().ToList();
			return Camper.WriteScheduleToCsvString(campers, _logger);
		}
	}

	public interface ISchedulerService
	{
		/// <summary>
		/// Get the list of known scheduleIds
		/// </summary>
		/// <returns>List of known scheduleIds</returns>
		List<string> GetScheduleIds();

		/// <summary>
		/// Return the schedule with the given ID. If not found returns an empty schedule
		/// </summary>
		/// <param name="scheduleId">Id for the schedule</param>
		/// <returns>Return the schedule with the given ID. If not found returns an empty schedule</returns>
		List<ActivityDefinition> GetSchedule(string scheduleId);

		/// <summary>
		/// Update a schedule in persistence
		/// </summary>
		/// <param name="scheduleId">Id for the schedule</param>
		/// <param name="schedule">Schedule details</param>
		void UpdateSchedule(string scheduleId, List<ActivityDefinition> schedule);

		/// <summary>
		/// Generate a schedule for the camper requests
		/// </summary>
		/// <param name="camperRequests">Camper requests - updated by scheduling</param>
		/// <param name="activityDefinitions">Activity definitions</param>
		/// <param name="scheduleId">Id for the schedule</param>
		/// <returns>Activity definitions with schedule information</returns>
		List<ActivityDefinition> CreateSchedule(string scheduleId, List<CamperRequests> camperRequests,
			List<ActivityDefinition> activityDefinitions);

		/// <summary>
		/// Generates the CSV for the activity schedule.
		/// </summary>
		/// <param name="scheduleId">Id of schedule to write the activity schedule for</param>
		/// <returns>CSV text of the activity schedule</returns>
		string WriteActivityScheduleToCsv(string scheduleId);

		/// <summary>
		/// Generates the CSV for the camper schedule.
		/// </summary>
		/// <param name="scheduleId">Id of schedule to write the camper CSV for</param>
		/// <returns>CSV text of the camper schedule.</returns>
		string WriteCamperScheduleToCsv(string scheduleId);

		/// <summary>
		/// Get grid rows for a schedule
		/// </summary>
		/// <param name="scheduleId">Id of schedule to show</param>
		/// <param name="columns">Column details</param>
		/// <param name="query">Query</param>
		/// <returns></returns>
		ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId,
			Action<IGridColumnCollection<IActivityBlock>> columns,
			QueryDictionary<StringValues> query);

		/// <summary>
		/// Get grid rows for a schedule
		/// </summary>
		/// <param name="scheduleId">Id of schedule to show</param>
		/// <param name="query">Query</param>
		/// <returns></returns>
		ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId,
			QueryDictionary<StringValues> query);

	}
}
