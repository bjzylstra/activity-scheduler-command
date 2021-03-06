using ActivitySchedulerFrontEnd.Services;
using Camp;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class SchedulerServiceTests
	{
		private string _applicationName = Guid.NewGuid().ToString();
		private ActivityDefinitionService _activityDefinitionService;
		private ILogger<SchedulerService> _logger;

		private const string DefaultActivitySetName = "DefaultActivities";


		private DirectoryInfo ApplicationDirectoryInfo
		{
			get
			{
				DirectoryInfo dataDirectoryInfo = new DirectoryInfo(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
				DirectoryInfo applicationDirectoryInfo = dataDirectoryInfo.GetDirectories().FirstOrDefault(d =>
					d.Name.Equals(_applicationName, StringComparison.OrdinalIgnoreCase));
				return applicationDirectoryInfo;
			}
		}

		[SetUp]
		public void SetupLogger()
		{
			_logger = Substitute.For<ILogger<SchedulerService>>();
			_activityDefinitionService = new ActivityDefinitionService(
				_applicationName, Substitute.For<ILogger<ActivityDefinitionService>>());
		}

		[TearDown]
		public void RemoveApplicationData()
		{
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			if (applicationDirectoryInfo != null)
			{
				applicationDirectoryInfo.Delete(true);
			}
		}

		[Test]
		public void Construct_NoAppData_CreatesAppDataWithDefaults()
		{
			// Arrange - make sure directory is gone
			RemoveApplicationData();

			// Act - construct the activity service
			SchedulerService service = new SchedulerService(_applicationName, _logger);

			// Assert - verify the directory is created
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			Assert.That(applicationDirectoryInfo, Is.Not.Null,
				"application directory info after constructor");
		}

		[Test]
		public void Construct_HasAppData_SchedulesSetIncludesAllValidSchedules()
		{
			// Arrange - generate a schedule and save a couple of copies
			// in the application directory
			string[] expectedScheduleIds = new[] { "Schedule1", "Another schedule", "2020.04.01" };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			// Throw a garbage file into the app data folder
			string junkFileName = $"{ApplicationDirectoryInfo.FullName}\\Junk{SchedulerService.ScheduleFileExtension}";
			using (StreamWriter junkFileWriter = new StreamWriter(junkFileName))
			{
				junkFileWriter.WriteLine("Junk content");
			}

			// Act - create the schedule service
			SchedulerService service = new SchedulerService(_applicationName, _logger);

			// Assert - scheduler should have the expected schedule Ids
			List<string> scheduleIds = service.GetScheduleIds();
			Assert.That(scheduleIds, Is.EquivalentTo(expectedScheduleIds),
				"Schedule IDs in the service");
		}

		[Test]
		public void GetSchedule_KnownId_ReturnsSchedule()
		{
			// Arrange - generate a schedule and save a couple of copies
			string[] expectedScheduleIds = new[] { "Schedule1", "Another schedule", "2020.04.01" };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);

			// Act
			string scheduleId = expectedScheduleIds.Last();
			List<ActivityDefinition> retrievedSchedule = service.GetSchedule(scheduleId);

			// Assert - a schedule was retrieved
			Assert.That(retrievedSchedule, Is.Not.Null, "Retrieved schedule");
			Assert.That(retrievedSchedule, Has.Count.GreaterThan(0),
				"Activities in the retrieved schedule");
		}

		[Test]
		public void GetSchedule_UnknownId_ReturnsEmptySchedule()
		{
			// Arrange - generate a schedule and save a couple of copies
			string[] expectedScheduleIds = new[] { "Schedule1", "Another schedule", "2020.04.01" };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);

			// Act
			string scheduleId = "NoSuchSchedule";
			List<ActivityDefinition> retrievedSchedule = service.GetSchedule(scheduleId);

			// Assert - a schedule was retrieved
			Assert.That(retrievedSchedule, Is.Not.Null, "Retrieved schedule");
			Assert.That(retrievedSchedule, Has.Count.EqualTo(0),
				"Activities in the retrieved schedule");
		}

		[Test]
		public void UpdateSchedule_NewSchedule_ServiceHasNewSchedule()
		{
			// Arrange - Start with an empty schedule set
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			Assert.That(service.GetScheduleIds, Has.Count.EqualTo(0), "Initial schedule set");

			// Act - Add a schedule
			string scheduleId = "MySchedule";
			var scheduleData = GenerateSchedule();
			service.UpdateSchedule(scheduleId, scheduleData.activityDefinitions, 
				scheduleData.camperGroups, scheduleData.camperPreferences);

			// Arrange - read the schedule back.
			List<ActivityDefinition> retrievedSchedule = service.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(scheduleData.activityDefinitions.Count), 
				"Number of activities in retrieved schedule");
			List<HashSet<Camper>> retrievedCamperGroups = service.GetCamperGroupsForScheduleId(scheduleId);
			AssertCamperGroupsAreEqual(retrievedCamperGroups, scheduleData.camperGroups);
		}

		[Test]
		public void UpdateSchedule_NewSchedule_ServicePersistsNewSchedule()
		{
			// Arrange - Start with an empty schedule set
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			Assert.That(service.GetScheduleIds, Has.Count.EqualTo(0), "Initial schedule set");

			// Act - Add a schedule
			string scheduleId = "MySchedule";
			var scheduleData = GenerateSchedule();
			service.UpdateSchedule(scheduleId, scheduleData.activityDefinitions, 
				scheduleData.camperGroups, scheduleData.camperPreferences);

			// Arrange - Create another scheduler service and read the schedule back.
			SchedulerService freshService = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> retrievedSchedule = freshService.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(scheduleData.activityDefinitions.Count),
				"Number of activities in retrieved schedule");
			List<HashSet<Camper>> retrievedCamperGroups = service.GetCamperGroupsForScheduleId(scheduleId);
			AssertCamperGroupsAreEqual(retrievedCamperGroups, scheduleData.camperGroups);
		}

		[Test]
		public void UpdateSchedule_UpdatedSchedule_ServiceHasUpdatedSchedule()
		{
			// Arrange - generate a schedule in the service
			string scheduleId = "MySchedule";
			string[] expectedScheduleIds = new[] { scheduleId };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);

			// Act - Modify and update the schedule
			List<ActivityDefinition> schedule = service.GetSchedule(scheduleId);
			List<HashSet<Camper>> camperGroups = service.GetCamperGroupsForScheduleId(scheduleId);
			Dictionary<Camper, List<ActivityDefinition>> camperPreferences =
				service.GetCamperPreferencesForScheduleId(scheduleId);
			schedule.RemoveAt(0);
			service.UpdateSchedule(scheduleId, schedule, camperGroups, camperPreferences);

			// Arrange - read the schedule back.
			List<ActivityDefinition> retrievedSchedule = service.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(schedule.Count),
				"Number of activities in retrieved schedule");
			List<HashSet<Camper>> retrievedCamperGroups = service.GetCamperGroupsForScheduleId(scheduleId);
			AssertCamperGroupsAreEqual(retrievedCamperGroups, camperGroups);
		}

		[Test]
		public void UpdateSchedule_UpdatedSchedule_ServicePersistsUpdatedSchedule()
		{
			// Arrange - generate a schedule in the service
			string scheduleId = "MySchedule";
			string[] expectedScheduleIds = new[] { scheduleId };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);

			// Act - Modify and update the schedule
			List<ActivityDefinition> schedule = service.GetSchedule(scheduleId);
			List<HashSet<Camper>> camperGroups = service.GetCamperGroupsForScheduleId(scheduleId);
			Dictionary<Camper, List<ActivityDefinition>> camperPreferences =
				service.GetCamperPreferencesForScheduleId(scheduleId);
			schedule.RemoveAt(0);
			service.UpdateSchedule(scheduleId, schedule, camperGroups, camperPreferences);

			// Arrange - Create another scheduler service and read the schedule back.
			SchedulerService freshService = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> retrievedSchedule = freshService.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(schedule.Count),
				"Number of activities in retrieved schedule");
			List<HashSet<Camper>> retrievedCamperGroups = freshService.GetCamperGroupsForScheduleId(scheduleId);
			AssertCamperGroupsAreEqual(retrievedCamperGroups, camperGroups);
		}

		[Test]
		public void MoveCamperToBlock_ValidMove_CamperMoved()
		{
			// Arrange - generate a schedule in the service
			string scheduleId = "MySchedule";
			string[] expectedScheduleIds = new[] { scheduleId };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> schedule = service.GetSchedule(scheduleId);

			// Act - move a camper
			ActivityDefinition sourceActivity = schedule[0];
			ActivityDefinition targetActivity = schedule[1];
			int timeSlot = 0;
			string camperName = sourceActivity.ScheduledBlocks[timeSlot].AssignedCampers[0].FullName;
			service.MoveCamperToBlock(scheduleId, camperName, timeSlot, targetActivity.Name);

			// Assert - camper is moved.
			schedule = service.GetSchedule(scheduleId);
			List<string> assignedCampersByName = sourceActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.None.EqualTo(camperName),
				"Assigned campers on move source");
			assignedCampersByName = targetActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.One.EqualTo(camperName),
				"Assigned campers on move target");
		}

		[Test]
		public void MoveCamperToBlock_BadScheduleId_CamperNotMoved()
		{
			// Arrange - generate a schedule in the service
			string scheduleId = "MySchedule";
			string[] expectedScheduleIds = new[] { scheduleId };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> schedule = service.GetSchedule(scheduleId);

			// Act/Assert - move a camper
			ActivityDefinition sourceActivity = schedule[0];
			ActivityDefinition targetActivity = schedule[1];
			int timeSlot = 0;
			string camperName = sourceActivity.ScheduledBlocks[timeSlot].AssignedCampers[0].FullName;
			ArgumentException exception = Assert.Throws<ArgumentException>(() =>
				service.MoveCamperToBlock("bogusScheduleId", camperName, timeSlot, targetActivity.Name));
			Assert.That(exception.ParamName, Is.EqualTo("scheduleId"), "Exception parameter");

			// Verify camper is not moved.
			schedule = service.GetSchedule(scheduleId);
			List<string> assignedCampersByName = sourceActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.One.EqualTo(camperName),
				"Assigned campers on move source");
			assignedCampersByName = targetActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.None.EqualTo(camperName),
				"Assigned campers on move target");
		}

		[Test]
		public void MoveCamperToBlock_BadTargetActivity_CamperNotMoved()
		{
			// Arrange - generate a schedule in the service
			string scheduleId = "MySchedule";
			string[] expectedScheduleIds = new[] { scheduleId };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> schedule = service.GetSchedule(scheduleId);

			// Act - move a camper
			ActivityDefinition sourceActivity = schedule[0];
			ActivityDefinition targetActivity = schedule[1];
			int timeSlot = 0;
			string camperName = sourceActivity.ScheduledBlocks[timeSlot].AssignedCampers[0].FullName;
			ArgumentException exception = Assert.Throws<ArgumentException>(() =>
				service.MoveCamperToBlock(scheduleId, camperName, timeSlot, "No Such Activity"));
			Assert.That(exception.ParamName, Is.EqualTo("newActivityName"), "Exception parameter");

			// Verify camper is not moved.
			schedule = service.GetSchedule(scheduleId);
			List<string> assignedCampersByName = sourceActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.One.EqualTo(camperName),
				"Assigned campers on move source");
			assignedCampersByName = targetActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.None.EqualTo(camperName),
				"Assigned campers on move target");
		}

		[Test]
		public void MoveCamperToBlock_BadTimeSlot_CamperNotMoved()
		{
			// Arrange - generate a schedule in the service
			string scheduleId = "MySchedule";
			string[] expectedScheduleIds = new[] { scheduleId };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> schedule = service.GetSchedule(scheduleId);

			// Act - move a camper
			ActivityDefinition sourceActivity = schedule[0];
			ActivityDefinition targetActivity = schedule[1];
			int timeSlot = 0;
			string camperName = sourceActivity.ScheduledBlocks[timeSlot].AssignedCampers[0].FullName;
			ArgumentException exception = Assert.Throws<ArgumentException>(() =>
				service.MoveCamperToBlock(scheduleId, camperName, Int16.MaxValue, targetActivity.Name));
			Assert.That(exception.ParamName, Is.EqualTo("timeSlot"), "Exception parameter");

			// Verify camper is not moved.
			schedule = service.GetSchedule(scheduleId);
			List<string> assignedCampersByName = sourceActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.One.EqualTo(camperName),
				"Assigned campers on move source");
			assignedCampersByName = targetActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.None.EqualTo(camperName),
				"Assigned campers on move target");
		}

		[Test]
		public void MoveCamperToBlock_UnknownCamper_CamperNotMoved()
		{
			// Arrange - generate a schedule in the service
			string scheduleId = "MySchedule";
			string[] expectedScheduleIds = new[] { scheduleId };
			LoadSchedulesIntoAppData(expectedScheduleIds);
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> schedule = service.GetSchedule(scheduleId);

			// Act - move a camper
			ActivityDefinition sourceActivity = schedule[0];
			ActivityDefinition targetActivity = schedule[1];
			int timeSlot = 0;
			string camperName = sourceActivity.ScheduledBlocks[timeSlot].AssignedCampers[0].FullName;
			ArgumentException exception = Assert.Throws<ArgumentException>(() =>
				service.MoveCamperToBlock(scheduleId, "Unknown camper", timeSlot, targetActivity.Name));
			Assert.That(exception.ParamName, Is.EqualTo("camperName"), "Exception parameter");

			// Verify camper is not moved.
			schedule = service.GetSchedule(scheduleId);
			List<string> assignedCampersByName = sourceActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.One.EqualTo(camperName),
				"Assigned campers on move source");
			assignedCampersByName = targetActivity.ScheduledBlocks[timeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.None.EqualTo(camperName),
				"Assigned campers on move target");
		}

		/// <summary>
		/// Create a set of schedule files with the provided IDS
		/// </summary>
		/// <param name="scheduleIds">Schedule IDs to create files for</param>
		private void LoadSchedulesIntoAppData(string[] scheduleIds)
		{
			var scheduleData = GenerateSchedule();
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			ISchedulerService loaderScheduler = new SchedulerService(_applicationName, _logger);
			foreach (string scheduleId in scheduleIds)
			{
				loaderScheduler.UpdateSchedule(scheduleId, scheduleData.activityDefinitions, 
					scheduleData.camperGroups, scheduleData.camperPreferences);
			}
		}

		/// <summary>
		/// Generate a schedule from the built-in test data for camper requests.
		/// </summary>
		/// <returns>A successful schedule from the built-in test data</returns>
		private (List<ActivityDefinition> activityDefinitions, 
			List<HashSet<Camper>> camperGroups, 
			Dictionary<Camper,List<ActivityDefinition>> camperPreferences) GenerateSchedule()
		{
			Assembly assembly = typeof(SchedulerServiceTests).Assembly;
			using (Stream camperRequestFile = assembly.GetManifestResourceStream(
				"ActivitySchedulerFrontEnd.Tests.CamperRequests.csv"))
			{
				List<ActivityDefinition> activityDefinitions = _activityDefinitionService.GetActivityDefinition(
					DefaultActivitySetName).ToList();
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(camperRequestFile,
					activityDefinitions);
				Scheduler.ScheduleActivities(camperRequests, false, _logger);
				List<HashSet<Camper>> camperGroups = CamperRequests.GenerateCamperMateGroups(camperRequests);
				Dictionary<Camper, List<ActivityDefinition>> camperPreferences = 
					CamperRequests.GenerateCamperActivityPreferences(camperRequests);
				// Activity definitions now has the schedule
				return (activityDefinitions,camperGroups,camperPreferences);
			}
		}

		private void AssertCamperGroupsAreEqual(List<HashSet<Camper>> actualCamperGroups,
			List<HashSet<Camper>> expectedCamperGroups)
		{
			Assert.That(actualCamperGroups, Has.Count.EqualTo(expectedCamperGroups.Count),
				"Retrieved camper groups");
			foreach (var expectedCamperGroup in expectedCamperGroups)
			{
				// Find the original camper group
				var actualCamperGroup = actualCamperGroups.First(rcg => rcg.Contains(expectedCamperGroup.First()));
				// Last name equivalency because groups only give last name
				foreach (var camperLastName in expectedCamperGroup.Select(c => c.LastName))
				{
					Assert.That(actualCamperGroup.Select(c => c.LastName), Has.One.EqualTo(camperLastName),
						"Retrieved camper group");
				}
			}
		}

	}
}
