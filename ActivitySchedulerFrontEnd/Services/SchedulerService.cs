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
using System.Text.Json;

namespace ActivitySchedulerFrontEnd.Services
{
	public class SchedulerService : ISchedulerService
	{
		public const string UnscheduledActivityName = " Unscheduled";

		private readonly string _applicationName;
		private readonly ILogger<SchedulerService> _logger;
		private Dictionary<string,List<ActivityDefinition>> _schedulesById = new Dictionary<string,List<ActivityDefinition>>();
		private Dictionary<string, List<HashSet<Camper>>> _camperGroupsByScheduleId = new Dictionary<string, List<HashSet<Camper>>>();
		public const string ScheduleFileExtension = ".sch";

		/// <summary>
		/// Default constructor used by dependency injection.
		/// </summary>
		/// <param name="logger">Logger</param>
		public SchedulerService(ILogger<SchedulerService> logger)
		{
			_logger = logger;
			_applicationName = Assembly.GetEntryAssembly().GetName().Name;
			(_schedulesById,_camperGroupsByScheduleId) = LoadSchedulesFromPersistence(_applicationName);
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
			(_schedulesById, _camperGroupsByScheduleId) = LoadSchedulesFromPersistence(_applicationName);
		}

		/// <summary>
		/// Load the schedules from the local application data folder.
		/// Creates the folder if it is not found.
		/// </summary>
		/// <param name="applicationName">Application name for local applications data folder</param>
		/// <returns>Dictionary of schedules by schedule Id</returns>
		private (Dictionary<string, List<ActivityDefinition>> schedulesById, 
			Dictionary<string, List<HashSet<Camper>>> camperGroupsByScheduleId) 
			LoadSchedulesFromPersistence(string applicationName)
		{
			Dictionary<string, List<ActivityDefinition>> schedulesById = new Dictionary<string, List<ActivityDefinition>>();
			Dictionary<string, List<HashSet<Camper>>> camperGroupsById = new Dictionary<string, List<HashSet<Camper>>>();
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
					(List<ActivityDefinition> schedule,
						List<HashSet<Camper>> camperGroups) = LoadSchedule(scheduleFile.FullName);
					if (schedule != null)
					{
						string scheduleId = scheduleFile.Name.Substring(0, scheduleFile.Name.Length - ScheduleFileExtension.Length);
						schedulesById.Add(scheduleId, schedule);
						camperGroupsById.Add(scheduleId, camperGroups);
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "LoadSchedulesFromPersistence failed");
			}
			return (schedulesById,camperGroupsById);
		}

		/// <summary>
		/// Load a schedule from persistence.
		/// </summary>
		/// <param name="scheduleFileLocation">Full path to the schedule file</param>
		/// <returns>Schedule if load is successful, otherwise null</returns>
		private (List<ActivityDefinition> schedule,
			List<HashSet<Camper>> camperGroups) LoadSchedule(string scheduleFileLocation)
		{
			using (StreamReader scheduleFileReader = new StreamReader(scheduleFileLocation))
			{
				// Read the length of the definitions section from the first line
				// If anything goes wrong, log and ignore.
				if (!int.TryParse(scheduleFileReader.ReadLine(), out int versionNumber))
				{
					_logger.LogError($"{scheduleFileLocation} is missing the version field");
					return (null, null);
				}
				int definitionLength = 0;
				int scheduleLength = 0;
				int camperGroupLength = 0;
				// Prior to version numbers, the first line was the definition length.
				if (versionNumber > 0)
				{
					definitionLength = versionNumber;
					versionNumber = 0;
				}
				else
				{
					// Version number is stored -ve to differentiate from the definition length
					// in pre-versioned files (rev 1)
					versionNumber = -versionNumber;
					if (versionNumber == 1)
					{
						if (!int.TryParse(scheduleFileReader.ReadLine(), out definitionLength))
						{
							_logger.LogError($"{scheduleFileLocation} is missing the definition length field");
							return (null, null);
						}
						if (!int.TryParse(scheduleFileReader.ReadLine(), out scheduleLength))
						{
							_logger.LogError($"{scheduleFileLocation} is missing the schedule length field");
							return (null, null);
						}
						if (!int.TryParse(scheduleFileReader.ReadLine(), out camperGroupLength))
						{
							_logger.LogError($"{scheduleFileLocation} is missing the camper group length field");
							return (null, null);
						}
					}
					else
					{
						_logger.LogError($"{scheduleFileLocation} is unsupported version '{versionNumber}'");
						return (null, null);
					}
				}

				// Lengths of the sections should be read in now. Read in the sections.
				char[] buffer = new char[definitionLength];
				int charactersRead = scheduleFileReader.Read(buffer, 0, definitionLength);
				if (charactersRead != definitionLength)
				{
					// Ran out of characters
					_logger.LogError($"{scheduleFileLocation} specified definition length of {definitionLength} " +
						$"but found only {charactersRead} characters remaining in the file.");
					return (null,null);
				}
				List<ActivityDefinition> activityDefinitions = ActivityDefinition.ReadActivityDefinitionsFromString(
					new string(buffer), _logger);
				if (activityDefinitions == null || activityDefinitions.Count == 0)
				{
					// Could not read the activity definitions
					_logger.LogError($"{scheduleFileLocation} could not parse the activity definitions");
					return (null, null);
				}

				List<ActivityDefinition> schedule = null;
				if (scheduleLength > 0)
				{
					buffer = new char[scheduleLength];
					charactersRead = scheduleFileReader.Read(buffer, 0, scheduleLength);
					if (charactersRead != scheduleLength)
					{
						// Ran out of characters
						_logger.LogError($"{scheduleFileLocation} specified schedule length of {scheduleLength} " +
							$"but found only {charactersRead} characters remaining in the file.");
						return (null, null);
					}
					schedule = ActivityDefinition.ReadScheduleFromCsvString(
						new string(buffer), _logger);
				}
				else
				{
					// Version 0 has no schedule length. Read to end of file.
					schedule = ActivityDefinition.ReadScheduleFromCsvString(
						scheduleFileReader.ReadToEnd(), _logger);
				}
				if (schedule == null || schedule.Count == 0)
				{
					// Could not read the schedule
					_logger.LogError($"{scheduleFileLocation} could not parse the schedule csv");
					return (null, null);
				}

				List<HashSet<Camper>> camperGroups = new List<HashSet<Camper>>();
				if (camperGroupLength > 0)
				{
					buffer = new char[camperGroupLength];
					charactersRead = scheduleFileReader.Read(buffer, 0, camperGroupLength);
					if (charactersRead != camperGroupLength)
					{
						// Ran out of characters
						_logger.LogError($"{scheduleFileLocation} specified camper group length of {camperGroupLength} " +
							$"but found only {charactersRead} characters remaining in the file.");
						return (null, null);
					}
					string camperGroupJson = new string(buffer);
					try
					{
						JsonSerializerOptions options = new JsonSerializerOptions();
						options.Converters.Add(new CamperJsonConverter());
						camperGroups = JsonSerializer.Deserialize<List<HashSet<Camper>>>(camperGroupJson, options);
						if (camperGroups == null)
						{
							camperGroups = new List<HashSet<Camper>>();
						}
						for (int i = 0; i < camperGroups.Count; i++)
						{
							// Need to put the right comparer on for by name matching to work.
							camperGroups[i] = new HashSet<Camper>(camperGroups[i], new Camper.CamperEqualityCompare());
						}
					}
					catch (JsonException e)
					{
						_logger.LogError(e, $"Failed to parse camper group JSON '{camperGroupJson}'");
					}
				}

				// Merge the limits into the schedule.
				bool mergeSuccessful = true;
				foreach (ActivityDefinition scheduleActivity in schedule)
				{
					ActivityDefinition activityDefinition = activityDefinitions
						.FirstOrDefault(ad => ad.Name.Equals(scheduleActivity.Name));
					if (activityDefinition == null)
					{
						// Did not find the activity definition for a scheduled activity
						mergeSuccessful = false;
						_logger.LogError($"{scheduleFileLocation} did not contain a definition for" +
							$"scheduled activity '{scheduleActivity.Name}'");
						break;
					}
					scheduleActivity.MaximumCapacity = activityDefinition.MaximumCapacity;
					scheduleActivity.MinimumCapacity = activityDefinition.MinimumCapacity;
					scheduleActivity.OptimalCapacity = activityDefinition.OptimalCapacity;
				}
				if (mergeSuccessful)
				{
					return (schedule,camperGroups);
				}
				return (null, null);
			}
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

			// Generate the camper groups
			List<HashSet<Camper>> camperGroups = CamperRequests.GenerateCamperMateGroups(camperRequests);
			_camperGroupsByScheduleId[scheduleId] = camperGroups;

			UpdateSchedule(scheduleId, activityDefinitions, camperGroups);

			return activityDefinitions;
		}

		public void UpdateSchedule(string scheduleId, List<ActivityDefinition> schedule, 
			List<HashSet<Camper>> camperGroups)
		{
			DirectoryInfo dataDirectoryInfo = new DirectoryInfo(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
			// Created in constructor so it really should be there.
			DirectoryInfo applicationDirectoryInfo = dataDirectoryInfo.GetDirectories().FirstOrDefault(d =>
				d.Name.Equals(_applicationName, StringComparison.OrdinalIgnoreCase));
			string fileName = $"{applicationDirectoryInfo.FullName}\\{scheduleId}{ScheduleFileExtension}";
			using (StreamWriter scheduleFileWriter = new StreamWriter(fileName))
			{
				// Empty the file before writing
				scheduleFileWriter.BaseStream.SetLength(0);
				string definitions = ActivityDefinition.WriteActivityDefinitionsToString(schedule, _logger);
				string scheduleCsv = ActivityDefinition.WriteScheduleToCsvString(schedule, _logger);
				string camperGroupsJson = String.Empty;
				try
				{
					JsonSerializerOptions options = new JsonSerializerOptions
					{
						WriteIndented = true,
						Converters =
						{
							new CamperJsonConverter()
						}
					};
					camperGroupsJson = JsonSerializer.Serialize(camperGroups, options);
				}
				catch (JsonException e)
				{
					// Serialization failed. Just skip it.
					_logger.LogError(e, $"Failed to serialize {camperGroups.Count} camper groups");
					camperGroupsJson = String.Empty;
				}

				// Use negative version numbers to differentiate older files that don't use the version
				int currentVersion = 1;
				scheduleFileWriter.WriteLine(-currentVersion);
				scheduleFileWriter.WriteLine(definitions.Length);
				scheduleFileWriter.WriteLine(scheduleCsv.Length);
				scheduleFileWriter.WriteLine(camperGroupsJson.Length);
				scheduleFileWriter.Write(definitions);
				scheduleFileWriter.Write(scheduleCsv);
				scheduleFileWriter.Write(camperGroupsJson);
			}
			// Generate a fresh copy of the schedule by reloading from persistence.
			// This effectively performs a deep copy so that the client data is
			// kept out of the service.
			(_schedulesById[scheduleId],_camperGroupsByScheduleId[scheduleId]) = LoadSchedule(fileName);
		}

		public void MoveCamperToBlock(string scheduleId, string camperName, int timeSlot, string newActivityName)
		{
			string context = $"{nameof(MoveCamperToBlock)} for schedule '{scheduleId}'," +
				$"camper '{camperName}',timeSlot '{timeSlot}',activity '{newActivityName}'";
			_logger.LogDebug($"+{context}");

			List<ActivityDefinition> schedule = LookupScheduleById(scheduleId);
			if (schedule == null || !schedule.Any())
			{
				_logger.LogInformation($"{context}: could not find schedule");
				throw new ArgumentException("Unknown schedule ID", nameof(scheduleId));
			}

			ActivityDefinition targetActivity = schedule.FirstOrDefault(ad => ad.Name.Equals(newActivityName));
			if (targetActivity == null)
			{
				_logger.LogInformation($"{context}: could not find target activity");
				throw new ArgumentException("Unknown activity name", nameof(newActivityName));
			}
			if (!targetActivity.ScheduledBlocks.Select(b => b.TimeSlot).Contains(timeSlot))
			{
				_logger.LogInformation($"{context}: could not find time slot");
				throw new ArgumentException("Unknown time slot", nameof(timeSlot));
			}

			// Find the camper by name and current activity block in the schedule.
			foreach (var sourceBlock in schedule.Select(ad => ad.ScheduledBlocks[timeSlot]))
			{
				Camper camper = sourceBlock.AssignedCampers.FirstOrDefault(c => camperName.Equals(c.FullName));
				if (camper != null)
				{
					// Found the camper and the source block. Make the move.
					camper.ReAssignBlock(targetActivity.ScheduledBlocks[timeSlot]);
					UpdateSchedule(scheduleId, schedule, _camperGroupsByScheduleId[scheduleId]);
					_logger.LogDebug($"-{context}: Camper re-assigned");
					return;
				}
			}

			throw new ArgumentException("Unknown camper", nameof(camperName));
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

		private List<Camper> GetCampersForScheduleId(string scheduleId)
		{
			List<ActivityDefinition> schedule = LookupScheduleById(scheduleId);

			Dictionary<string, Camper> campersByName = new Dictionary<string, Camper>();

			foreach (ActivityDefinition activity in schedule)
			{
				foreach (IActivityBlock activityBlock in activity.ScheduledBlocks)
				{
					foreach (Camper camper in activityBlock.AssignedCampers)
					{
						if (!campersByName.ContainsKey(camper.FullName))
						{
							campersByName.Add(camper.FullName, camper);
						}
					}
				}
			}

			List<Camper> campers = campersByName.Values.ToList();
			campers.Sort((a,b) => a.FullName.CompareTo(b.FullName));
			return campers;
		}

		public List<HashSet<Camper>> GetCamperGroupsForScheduleId(string scheduleId)
		{
			return (_camperGroupsByScheduleId.TryGetValue(scheduleId, out var camperGroup))
				? camperGroup
				: new List<HashSet<Camper>>();
		}

		public ItemsDTO<Camper> GetCampersGridRows(string scheduleId, Action<IGridColumnCollection<Camper>> columns, QueryDictionary<StringValues> query)
		{
			var server = new GridServer<Camper>(
				GetCampersForScheduleId(scheduleId),
				new QueryCollection(query), true,
			"camperScheduleGrid", columns);

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
		/// <param name="camperGroups">Camper groups associated with the schedule</param>
		void UpdateSchedule(string scheduleId, List<ActivityDefinition> schedule,
			List<HashSet<Camper>> camperGroups);

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
		/// Move a camper to a new activity block
		/// </summary>
		/// <param name="scheduleId">Id for the schedule</param>
		/// <param name="camperName">Full cmaper name</param>
		/// <param name="blockNumber">Block number to move</param>
		/// <param name="newActivityName">Activity name to move to</param>
		void MoveCamperToBlock(string scheduleId, string camperName, int blockNumber, string newActivityName);

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
		/// Get grid rows for an activity schedule
		/// </summary>
		/// <param name="scheduleId">Id of schedule to show</param>
		/// <param name="columns">Activity schedule column details</param>
		/// <param name="query">Query</param>
		/// <returns>Activity schedule Items</returns>
		ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId,
			Action<IGridColumnCollection<IActivityBlock>> columns,
			QueryDictionary<StringValues> query);

		/// <summary>
		/// Get grid rows for a camper schedule
		/// </summary>
		/// <param name="scheduleId">Id of schedule to show</param>
		/// <param name="columns">Camper schedule column details</param>
		/// <param name="query">Query</param>
		/// <returns>Camper schedule Items</returns>
		ItemsDTO<Camper> GetCampersGridRows(string scheduleId,
			Action<IGridColumnCollection<Camper>> columns,
			QueryDictionary<StringValues> query);

		/// <summary>
		/// Get the camper groups for an activity schedule
		/// </summary>
		/// <param name="scheduleId">Id of schedule to load groups fro</param>
		/// <returns>List of camper groups</returns>
		List<HashSet<Camper>> GetCamperGroupsForScheduleId(string scheduleId);
	}
}
