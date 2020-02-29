using ActivitySchedulerFrontEnd.Services;
using Camp;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class ActivityDefinitionServiceTests
	{
		private const string DefaultSetName = "DefaultActivities";
		private string _applicationName = Guid.NewGuid().ToString();
		private ILogger<ActivityDefinitionService> _logger;

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
			_logger = NSubstitute.Substitute.For<ILogger<ActivityDefinitionService>>();
			ActivityDefinitionService service = new ActivityDefinitionService(_applicationName, _logger);

			// Assert - verify the directory is created
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			Assert.That(applicationDirectoryInfo, Is.Not.Null,
				"application directory info after constructor");

			// Verify the default XML is present
			FileInfo[] activityFiles = applicationDirectoryInfo.GetFiles();
			Assert.That(activityFiles, Has.Length.EqualTo(1), "Activity file set");
			Assert.That(activityFiles[0].Name, Is.EqualTo($"{DefaultSetName}.xml"), "ActivityFile");

			// Verify activity file is readable and not empty
			List<ActivityDefinition> defaultActivities = ActivityDefinition.ReadActivityDefinitions(
				activityFiles[0].FullName, _logger);
			Assert.That(defaultActivities, Has.Count.GreaterThan(0), "Number of default activities");

			// Verify the activity set contains just the default
			List<string> activitySets = new List<string>(service.GetActivitySetNames());
			Assert.That(activitySets, Is.EquivalentTo(new List<string> { DefaultSetName }),
				"Initial activity sets");
		}

		[Test]
		public void Construct_HasAppData_ActivitySetsIncludesAllFiles()
		{
			// Arrange - use constructor to create directory with 1 file.
			new ActivityDefinitionService(_applicationName, _logger);
			// Create a couple copies of the default.
			List<string> expectedActivitySets = new List<string>
			{
				DefaultSetName,
				"AnotherSet",
				"OneMore"
			};
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			foreach (string addSet in expectedActivitySets.Skip(1))
			{
				File.Copy($"{applicationDirectoryInfo.FullName}\\{DefaultSetName}.xml",
					$"{applicationDirectoryInfo.FullName}\\{addSet}.xml");
			}

			// Act - create the activity service
			ActivityDefinitionService service = new ActivityDefinitionService(
				_applicationName, _logger);

			// Assert - activity sets should match the expected
			List<string> activitySets = new List<string>(service.GetActivitySetNames());
			Assert.That(activitySets, Is.EquivalentTo(expectedActivitySets),
				"Activity sets on preloaded application");
		}

	}
}
