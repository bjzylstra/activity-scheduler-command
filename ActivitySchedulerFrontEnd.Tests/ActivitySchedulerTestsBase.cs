using ActivitySchedulerFrontEnd.Services;
using Camp;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ActivitySchedulerFrontEnd.Tests
{
	public class ActivitySchedulerTestsBase
	{
		protected const string DefaultSetName = "DefaultActivities";

		private string _applicationName = Guid.NewGuid().ToString();
		protected Dictionary<string, List<ActivityDefinition>> _expectedActivitySets =
			new Dictionary<string, List<ActivityDefinition>>();

		protected IActivityDefinitionService _activityDefinitionService;
		protected ISchedulerService _schedulerService;

		protected byte[] _overSubscribedCamperRequestsBuffer;
		protected byte[] _missingActivityCamperRequestsBuffer;
		protected byte[] _validCamperRequestsBuffer;

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

		protected void LoadTestCamperRequests()
		{
			Assembly assembly = typeof(ActivitySchedulerTestsBase).Assembly;

			using (Stream camperRequests = assembly.GetManifestResourceStream(
				"ActivitySchedulerFrontEnd.Tests.CamperRequestsUnknownActivity.csv"))
			{
				_missingActivityCamperRequestsBuffer = new byte[camperRequests.Length];
				int bytesRead = camperRequests.Read(_missingActivityCamperRequestsBuffer, 0,
					_missingActivityCamperRequestsBuffer.Length);
			}
			using (Stream camperRequests = assembly.GetManifestResourceStream(
				"ActivitySchedulerFrontEnd.Tests.CamperRequestsOversubscribed.csv"))
			{
				_overSubscribedCamperRequestsBuffer = new byte[camperRequests.Length];
				int bytesRead = camperRequests.Read(_overSubscribedCamperRequestsBuffer, 0,
					_overSubscribedCamperRequestsBuffer.Length);
			}
			using (Stream camperRequests = assembly.GetManifestResourceStream(
				"ActivitySchedulerFrontEnd.Tests.CamperRequests.csv"))
			{
				_validCamperRequestsBuffer = new byte[camperRequests.Length];
				int bytesRead = camperRequests.Read(_validCamperRequestsBuffer, 0,
					_validCamperRequestsBuffer.Length);
			}
		}

		protected void SetUpApplicationServices()
		{
		// Arrange - use constructor to create directory with 1 file.
			ILogger<ActivityDefinitionService> logger = Substitute.For<ILogger<ActivityDefinitionService>>();
			ActivityDefinitionService activityDefinitionService = new ActivityDefinitionService(_applicationName, logger);
			// Create a couple copies of the default.
			List<string> expectedActivitySets = new List<string>
			{
				DefaultSetName,
				"AnotherSet",
				"OneMore"
			};
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				activityDefinitionService.GetActivityDefinition(DefaultSetName));
			_expectedActivitySets.Add(DefaultSetName, new List<ActivityDefinition>(activityDefinitions));
			foreach (string addSet in expectedActivitySets.Skip(1))
			{
				activityDefinitions.RemoveAt(0);
				string content = ActivityDefinition.WriteActivityDefinitionsToString(activityDefinitions, logger);
				File.WriteAllText($"{ApplicationDirectoryInfo.FullName}\\{addSet}.xml", content);
				_expectedActivitySets.Add(addSet, new List<ActivityDefinition>(activityDefinitions));
			}
			_activityDefinitionService = new ActivityDefinitionService(_applicationName, logger);
			_schedulerService = new SchedulerService(_applicationName,
				Substitute.For<ILogger<SchedulerService>>());
		}

		protected void CleanupApplicationServices()
		{
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			if (applicationDirectoryInfo != null)
			{
				applicationDirectoryInfo.Delete(true);
			}
		}

	}
}
