﻿using ActivitySchedulerFrontEnd.Services;
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
			List<ActivityDefinition> schedule = GenerateSchedule();
			service.UpdateSchedule(scheduleId, schedule);

			// Arrange - read the schedule back.
			List<ActivityDefinition> retrievedSchedule = service.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(schedule.Count), 
				"Number of activities in retrieved schedule");
		}

		[Test]
		public void UpdateSchedule_NewSchedule_ServicePersistsNewSchedule()
		{
			// Arrange - Start with an empty schedule set
			SchedulerService service = new SchedulerService(_applicationName, _logger);
			Assert.That(service.GetScheduleIds, Has.Count.EqualTo(0), "Initial schedule set");

			// Act - Add a schedule
			string scheduleId = "MySchedule";
			List<ActivityDefinition> schedule = GenerateSchedule();
			service.UpdateSchedule(scheduleId, schedule);

			// Arrange - Create another scheduler service and read the schedule back.
			SchedulerService freshService = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> retrievedSchedule = freshService.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(schedule.Count),
				"Number of activities in retrieved schedule");
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
			schedule.RemoveAt(0);
			service.UpdateSchedule(scheduleId, schedule);

			// Arrange - read the schedule back.
			List<ActivityDefinition> retrievedSchedule = service.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(schedule.Count),
				"Number of activities in retrieved schedule");
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
			schedule.RemoveAt(0);
			service.UpdateSchedule(scheduleId, schedule);

			// Arrange - Create another scheduler service and read the schedule back.
			SchedulerService freshService = new SchedulerService(_applicationName, _logger);
			List<ActivityDefinition> retrievedSchedule = freshService.GetSchedule(scheduleId);
			Assert.That(retrievedSchedule, Has.Count.EqualTo(schedule.Count),
				"Number of activities in retrieved schedule");
		}

		/// <summary>
		/// Create a set of schedule files with the provided IDS
		/// </summary>
		/// <param name="scheduleIds">Schedule IDs to create files for</param>
		private void LoadSchedulesIntoAppData(string[] scheduleIds)
		{
			List<ActivityDefinition> schedule = GenerateSchedule();
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			ISchedulerService loaderScheduler = new SchedulerService(_applicationName, _logger);
			foreach (string scheduleId in scheduleIds)
			{
				loaderScheduler.UpdateSchedule(scheduleId, schedule);
			}
		}

		/// <summary>
		/// Generate a schedule from the built-in test data for camper requests.
		/// </summary>
		/// <returns>A successful schedule from the built-in test data</returns>
		private List<ActivityDefinition> GenerateSchedule()
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
				// Activity definitions now has the schedule
				return activityDefinitions;
			}
		}
	}
}
