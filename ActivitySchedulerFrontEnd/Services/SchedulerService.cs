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

		private class ScheduleDetails
		{
			public List<ActivityDefinition> Schedule { get; set; }
			public List<HashSet<Camper>> CamperGroups { get; set; }
			public Dictionary<Camper, List<ActivityDefinition>> CamperActivityPreferences { get; set; }
		}

		private readonly string _applicationName;
		private readonly ILogger<SchedulerService> _logger;
		private Dictionary<string, ScheduleDetails> _detailsById = new Dictionary<string, ScheduleDetails>();
		public const string ScheduleFileExtension = ".sch";

		/// <summary>
		/// Default constructor used by dependency injection.
		/// </summary>
		/// <param name="logger">Logger</param>
		public SchedulerService(ILogger<SchedulerService> logger)
		{
			_logger = logger;
			_applicationName = Assembly.GetEntryAssembly().GetName().Name;
			_detailsById = LoadSchedulesFromPersistence(_applicationName);
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
			_detailsById = LoadSchedulesFromPersistence(_applicationName);
		}

		/// <summary>
		/// Load the schedules from the local application data folder.
		/// Creates the folder if it is not found.
		/// </summary>
		/// <param name="applicationName">Application name for local applications data folder</param>
		/// <returns>Dictionary of schedules by schedule Id</returns>
		private Dictionary<string, ScheduleDetails> LoadSchedulesFromPersistence(string applicationName)
		{
			Dictionary<string, ScheduleDetails> detailsById = new Dictionary<string, ScheduleDetails>();
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
					try
					{
						ScheduleDetails scheduleData = LoadScheduleData(scheduleFile.FullName);
						string scheduleId = scheduleFile.Name.Substring(0, scheduleFile.Name.Length - ScheduleFileExtension.Length);
						detailsById.Add(scheduleId, scheduleData);
					}
					catch (PersistenceParseException e)
					{
						_logger.LogError(e, $"Failed to load schedule at {scheduleFile}");
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "LoadSchedulesFromPersistence failed");
			}
			return detailsById;
		}

		/// <summary>
		/// Load a schedule from persistence.
		/// </summary>
		/// <param name="scheduleFileLocation">Full path to the schedule file</param>
		/// <returns>Schedule if load is successful, otherwise null</returns>
		private ScheduleDetails LoadScheduleData(string scheduleFileLocation)
		{
			using (StreamReader scheduleFileReader = new StreamReader(scheduleFileLocation))
			{
				// Read the length of the definitions section from the first line
				// If anything goes wrong, log and ignore.
				if (!int.TryParse(scheduleFileReader.ReadLine(), out int versionNumber))
				{
					throw new PersistenceParseException(scheduleFileLocation,
						$"Missing the version field");
				}
				int definitionLength = 0;
				int scheduleLength = 0;
				int camperGroupLength = 0;
				int camperPreferenceLength = 0;
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
					if (versionNumber > 2)
					{
						throw new PersistenceParseException(scheduleFileLocation,
							$"Unsupported version '{versionNumber}'");
					}

					if (versionNumber >= 1)
					{
						if (!int.TryParse(scheduleFileReader.ReadLine(), out definitionLength))
						{
							throw new PersistenceParseException(scheduleFileLocation,
								"Missing the definition length field");
						}
						else if (!int.TryParse(scheduleFileReader.ReadLine(), out scheduleLength))
						{
							throw new PersistenceParseException(scheduleFileLocation,
								"Missing the schedule length field");
						}
						else if (!int.TryParse(scheduleFileReader.ReadLine(), out camperGroupLength))
						{
							throw new PersistenceParseException(scheduleFileLocation,
								"Missing the camper group length field");
						}
					}
					// Version 2 added camper preferences
					if (versionNumber >= 2)
					{
						if (!int.TryParse(scheduleFileReader.ReadLine(), out camperPreferenceLength))
						{
							throw new PersistenceParseException(scheduleFileLocation,
								"Missing the camper preference length field");
						}
					}
				}

				// Lengths of the sections should be read in now. Read in the sections.
				char[] buffer = ReadExpectedCharacters(scheduleFileReader, definitionLength,
					scheduleFileLocation, "Activity Definitions");
				List<ActivityDefinition> activityDefinitions = ActivityDefinition.ReadActivityDefinitionsFromString(
					new string(buffer), _logger);
				if (activityDefinitions == null || activityDefinitions.Count == 0)
				{
					// Could not read the activity definitions
					throw new PersistenceParseException(scheduleFileLocation,
						"Could not parse the activity definitions");
				}

				ScheduleDetails scheduleDetails = new ScheduleDetails();
				if (scheduleLength > 0)
				{
					buffer = ReadExpectedCharacters(scheduleFileReader, scheduleLength,
						scheduleFileLocation, "Schedule");
					scheduleDetails.Schedule = ActivityDefinition.ReadScheduleFromCsvString(
						new string(buffer), _logger);
				}
				else
				{
					// Version 0 has no schedule length. Read to end of file.
					scheduleDetails.Schedule = ActivityDefinition.ReadScheduleFromCsvString(
						scheduleFileReader.ReadToEnd(), _logger);
				}
				if (scheduleDetails.Schedule == null || scheduleDetails.Schedule.Count == 0)
				{
					// Could not read the schedule
					throw new PersistenceParseException(scheduleFileLocation,
						"Could not parse the schedule");
				}

				if (camperGroupLength > 0)
				{
					scheduleDetails.CamperGroups = ReadCamperGroups(
						scheduleFileLocation, scheduleFileReader, camperGroupLength);
				}

				// Deserialize the camper preferences.
				if (camperPreferenceLength > 0)
				{
					scheduleDetails.CamperActivityPreferences = ReadCamperPreferences(
						scheduleFileLocation, scheduleFileReader, camperPreferenceLength);
				}

				// Merge the limits into the schedule.
				foreach (ActivityDefinition scheduleActivity in scheduleDetails.Schedule)
				{
					ActivityDefinition activityDefinition = activityDefinitions
						.FirstOrDefault(ad => ad.Name.Equals(scheduleActivity.Name));
					if (activityDefinition == null)
					{
						// Did not find the activity definition for a scheduled activity
						throw new PersistenceParseException(scheduleFileLocation,
							$"Did not contain a definition for" +
							$"scheduled activity '{scheduleActivity.Name}'");
					}
					scheduleActivity.MaximumCapacity = activityDefinition.MaximumCapacity;
					scheduleActivity.MinimumCapacity = activityDefinition.MinimumCapacity;
					scheduleActivity.OptimalCapacity = activityDefinition.OptimalCapacity;
				}
				return scheduleDetails;
			}
		}

		private Dictionary<Camper,List<ActivityDefinition>> ReadCamperPreferences(string scheduleFileLocation, StreamReader scheduleFileReader, int camperPreferenceLength)
		{
			char[] buffer = ReadExpectedCharacters(scheduleFileReader, camperPreferenceLength,
				scheduleFileLocation, "Camper Activity Preferences");
			string camperPreferencesJson = new string(buffer);
			try
			{
				JsonSerializerOptions options = new JsonSerializerOptions();
				options.Converters.Add(new CamperPreferencesJsonConverter());

				Dictionary<Camper,List<ActivityDefinition>> camperPreferences
					= JsonSerializer.Deserialize<Dictionary<Camper, List<ActivityDefinition>>>(
						camperPreferencesJson, options);
				if (camperPreferences == null)
				{
					camperPreferences = new Dictionary<Camper, List<ActivityDefinition>>();
				}
				return camperPreferences;
			}
			catch (JsonException e)
			{
				throw new PersistenceParseException(scheduleFileLocation,
					$"Could not parse camper preferences JSON '{camperPreferencesJson}'",
					e);
			}
		}

		private List<HashSet<Camper>> ReadCamperGroups(string scheduleFileLocation, StreamReader scheduleFileReader, int camperGroupLength)
		{
			char [] buffer = ReadExpectedCharacters(scheduleFileReader, camperGroupLength,
				scheduleFileLocation, "Camper Groups");
			string camperGroupJson = new string(buffer);
			try
			{
				JsonSerializerOptions options = new JsonSerializerOptions();
				options.Converters.Add(new CamperJsonConverter());

				List<HashSet<Camper>> camperGroups = JsonSerializer.Deserialize<List<HashSet<Camper>>>(camperGroupJson, options);
				if (camperGroups == null)
				{
					camperGroups = new List<HashSet<Camper>>();
				}
				for (int i = 0; i < camperGroups.Count; i++)
				{
					// Need to put the right comparer on for by name matching to work.
					camperGroups[i] = new HashSet<Camper>(camperGroups[i], new Camper.CamperEqualityCompare());
				}
				return camperGroups;
			}
			catch (JsonException e)
			{
				throw new PersistenceParseException(scheduleFileLocation,
					$"Could not parse camper group JSON '{camperGroupJson}'",
					e);
			}
		}

		private char [] ReadExpectedCharacters(StreamReader streamReader, int numberOfCharacters,
			string streamName, string sectionName)
		{
			char[] buffer = new char[numberOfCharacters];
			int charactersRead = streamReader.Read(buffer, 0, numberOfCharacters);
			if (charactersRead != numberOfCharacters)
			{
				throw new PersistenceParseException(streamName,
					$"Specified {sectionName} length of {numberOfCharacters}" +
					$"but found only {charactersRead} characters remaining in the file.");
			}
			return buffer;
		}

		public List<string> GetScheduleIds()
		{
			return _detailsById.Keys.ToList();
		}

		public List<ActivityDefinition> GetSchedule(string scheduleId)
		{
			return LookupScheduleById(scheduleId);
		}

		public void CreateSchedule(string scheduleId, List<CamperRequests> camperRequests, 
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

			ScheduleDetails scheduleDetails = new ScheduleDetails();
			scheduleDetails.Schedule = activityDefinitions;

			// Generate the camper groups
			scheduleDetails.CamperGroups = CamperRequests.GenerateCamperMateGroups(camperRequests);

			// Generate the camper preferences
			scheduleDetails.CamperActivityPreferences = 
				CamperRequests.GenerateCamperActivityPreferences(camperRequests);

			_detailsById[scheduleId] = scheduleDetails;

			UpdateSchedule(scheduleId, scheduleDetails);
		}

		private void UpdateSchedule(string scheduleId, ScheduleDetails scheduleDetails)
		{
			UpdateSchedule(scheduleId, scheduleDetails.Schedule,
				scheduleDetails.CamperGroups, scheduleDetails.CamperActivityPreferences);
		}

		public void UpdateSchedule(string scheduleId, List<ActivityDefinition> schedule,
			List<HashSet<Camper>> camperGroups,
			Dictionary<Camper, List<ActivityDefinition>> camperActivityPreferences)
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

				// Serialize the camper activity preferences.
				string camperPreferencesJson = String.Empty;
				try
				{
					JsonSerializerOptions options = new JsonSerializerOptions
					{
						WriteIndented = true,
						Converters =
						{
							new CamperPreferencesJsonConverter()
						}
					};
					camperPreferencesJson = JsonSerializer.Serialize(camperActivityPreferences, options);
				}
				catch (JsonException e)
				{
					// Serialization failed. Just skip it.
					_logger.LogError(e, $"Failed to serialize {camperActivityPreferences.Count} camper preferences");
					camperPreferencesJson = String.Empty;
				}

				// Use negative version numbers to differentiate older files that don't use the version
				int currentVersion = 2;
				scheduleFileWriter.WriteLine(-currentVersion);
				scheduleFileWriter.WriteLine(definitions.Length);
				scheduleFileWriter.WriteLine(scheduleCsv.Length);
				scheduleFileWriter.WriteLine(camperGroupsJson.Length);
				scheduleFileWriter.WriteLine(camperPreferencesJson.Length);
				scheduleFileWriter.Write(definitions);
				scheduleFileWriter.Write(scheduleCsv);
				scheduleFileWriter.Write(camperGroupsJson);
				scheduleFileWriter.Write(camperPreferencesJson);
			}
			// Generate a fresh copy of the schedule by reloading from persistence.
			// This effectively performs a deep copy so that the client data is
			// kept out of the service.
			_detailsById[scheduleId] = LoadScheduleData(fileName);
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
					UpdateSchedule(scheduleId, _detailsById[scheduleId]);
					_logger.LogDebug($"-{context}: Camper re-assigned");
					return;
				}
			}

			throw new ArgumentException("Unknown camper", nameof(camperName));
		}

		private List<ActivityDefinition> LookupScheduleById(string scheduleId)
		{
			_detailsById.TryGetValue(scheduleId, out ScheduleDetails scheduleDetails);
			return scheduleDetails?.Schedule ?? new List<ActivityDefinition>();
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
			_detailsById.TryGetValue(scheduleId, out ScheduleDetails scheduleDetails);
			return scheduleDetails?.CamperGroups ?? new List<HashSet<Camper>>();
		}

		public Dictionary<Camper,List<ActivityDefinition>> GetCamperPreferencesForScheduleId(string scheduleId)
		{
			_detailsById.TryGetValue(scheduleId, out ScheduleDetails scheduleDetails);
			return scheduleDetails?.CamperActivityPreferences ?? new Dictionary<Camper, List<ActivityDefinition>>();
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
			List<HashSet<Camper>> camperGroups,
			Dictionary<Camper,List<ActivityDefinition>> camperActivityPreferences);

		/// <summary>
		/// Generate a schedule for the camper requests
		/// </summary>
		/// <param name="camperRequests">Camper requests - updated by scheduling</param>
		/// <param name="activityDefinitions">Activity definitions</param>
		/// <param name="scheduleId">Id for the schedule</param>
		/// <returns>Activity definitions with schedule information</returns>
		void CreateSchedule(string scheduleId, List<CamperRequests> camperRequests,
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

		/// <summary>
		/// Get the camper activity preferences for a schedule
		/// </summary>
		/// <param name="scheduleId">Id of schedule to load</param>
		/// <returns>Activity preferences by activity</returns>
		Dictionary<Camper, List<ActivityDefinition>> GetCamperPreferencesForScheduleId(string scheduleId);
	}
}
