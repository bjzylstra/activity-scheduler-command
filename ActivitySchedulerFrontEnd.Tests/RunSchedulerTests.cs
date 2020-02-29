using ActivitySchedulerFrontEnd.Pages;
using ActivitySchedulerFrontEnd.Services;
using Blazor.FileReader;
using Blazored.LocalStorage;
using Camp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class RunSchedulerTests
	{
		private const string DefaultSetName = "DefaultActivities";
		private const string ActivitySetKey = "activitySet";
		private string _applicationName = Guid.NewGuid().ToString();
		private TestHost _host = new TestHost();
		private Dictionary<string, List<ActivityDefinition>> _expectedActivitySets =
			new Dictionary<string, List<ActivityDefinition>>();
		private ILocalStorageService _localStorage;
		private IFileReaderService _fileReaderService;
		private ILogger<ActivityDefinitionService> _logger;

		private byte[] _missingActivityCamperRequestsBuffer;
		private byte[] _overSubscribedCamperRequestsBuffer;
		private byte[] _validCamperRequestsBuffer;

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

		[OneTimeSetUp]
		public void PreloadActivityService()
		{
			// Arrange - use constructor to create directory with 1 file.
			_logger = Substitute.For<ILogger<ActivityDefinitionService>>();
			ActivityDefinitionService service = new ActivityDefinitionService(_applicationName, _logger);
			// Create a couple copies of the default.
			List<string> expectedActivitySets = new List<string>
			{
				DefaultSetName,
				"AnotherSet",
				"OneMore"
			};
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				service.GetActivityDefinition(DefaultSetName));
			_expectedActivitySets.Add(DefaultSetName, new List<ActivityDefinition>(activityDefinitions));
			foreach (string addSet in expectedActivitySets.Skip(1))
			{
				activityDefinitions.RemoveAt(0);
				string content = ActivityDefinition.WriteActivityDefinitionsToString(activityDefinitions, _logger);
				File.WriteAllText($"{ApplicationDirectoryInfo.FullName}\\{addSet}.xml", content);
				_expectedActivitySets.Add(addSet, new List<ActivityDefinition>(activityDefinitions));
			}

			ServiceSetup();
			LoadTestFiles();
		}

		[OneTimeTearDown]
		public void CleanupApplicationData()
		{
			DirectoryInfo applicationDirectoryInfo = ApplicationDirectoryInfo;
			if (applicationDirectoryInfo != null)
			{
				applicationDirectoryInfo.Delete(true);
			}
		}

		private void ServiceSetup()
		{
			IActivityDefinitionService activityDefinitionService = new ActivityDefinitionService(
				_applicationName, _logger);
			_host.AddService(activityDefinitionService);
			ILogger<SchedulerService> schedulerServiceLogger = Substitute.For<ILogger<SchedulerService>>();
			ISchedulerService schedulerService = new SchedulerService(schedulerServiceLogger);
			_host.AddService(schedulerService);
			IJSRuntime jsRuntime = Substitute.For<IJSRuntime>();
			_host.AddService(jsRuntime);
			_fileReaderService = Substitute.For<IFileReaderService>();
			_host.AddService(_fileReaderService);
			_localStorage = Substitute.For<ILocalStorageService>();
			_host.AddService(_localStorage);
		}

		private void LoadTestFiles()
		{
			Assembly assembly = typeof(RunSchedulerTests).Assembly;

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

		[Test]
		public void RunScheduler_InitializeEmptyLocalStorage_ShowDefaultActivitySet()
		{
			// Arrange / Act
			_localStorage.ClearReceivedCalls();
			_localStorage.GetItemAsync<string>(Arg.Any<string>())
				.Returns(Task.FromResult(string.Empty));
			RenderedComponent<RunScheduler> component =
				_host.AddComponent<RunScheduler>();

			// Assert
			// Verify activity set selector is initialized to default
			Assert.That(component.Instance.ActivitySet, Is.EqualTo(DefaultSetName),
				"ActivitySet initial value");

			// Verify activity set is persisted.
			Received.InOrder(async () =>
			{
				await _localStorage.SetItemAsync(ActivitySetKey, DefaultSetName);
			});
		}

		[Test]
		public void RunScheduler_InitializeFromLocalStorage_ShowsStoredActivitySet()
		{
			// Arrange / Act
			string activitySetName = "testy";
			_localStorage.GetItemAsync<string>(Arg.Any<string>())
				.Returns(Task.FromResult(activitySetName));
			RenderedComponent<RunScheduler> component =
				_host.AddComponent<RunScheduler>();

			// Assert
			// Verify activity set selector is initialized to default
			Assert.That(component.Instance.ActivitySet, Is.EqualTo(activitySetName),
				"ActivitySet initial value");
		}

		[Test]
		public void RunScheduler_SetActivitySet_UpdatesActivitySet()
		{
			// Arrange
			_localStorage.ClearReceivedCalls();
			RenderedComponent<RunScheduler> component =
				_host.AddComponent<RunScheduler>();

			// Act - change set
			string activitySetName = "testy";
			HtmlAgilityPack.HtmlNode setSelector = component.Find("select");
			setSelector.Change(activitySetName);

			// Assert
			// Verify activity set selector is updated
			Assert.That(component.Instance.ActivitySet, Is.EqualTo(activitySetName),
				"ActivitySet selected value");

			// Verify activity set is persisted.
			Received.InOrder(async () =>
			{
				// Empty comes from original load.
				await _localStorage.SetItemAsync(ActivitySetKey, activitySetName);
			});
		}

		[Test]
		public void RunSchedule_ScheduleValidFile_GeneratesSchedule()
		{
			// Arrange
			RenderedComponent<RunScheduler> component =
				_host.AddComponent<RunScheduler>();
			MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer);
			IFileReference camperRequestFile = Substitute.For<IFileReference>();
			camperRequestFile.OpenReadAsync().Returns(camperRequestStream);
			IFileReaderRef fileReaderRef = Substitute.For<IFileReaderRef>();
			fileReaderRef.EnumerateFilesAsync().Returns(new IFileReference[] { camperRequestFile });
			_fileReaderService.CreateReference(Arg.Any<ElementReference>()).Returns(fileReaderRef);

			// Act - execute scheduler
			HtmlAgilityPack.HtmlNode runSchedulerButton = component.Find("button");
			runSchedulerButton.Click();

			// Assert file is loaded
			Assert.That(component.Instance.Output, Contains.Substring("Loaded 9 activity definitions from DefaultActivities"),
				"Messages after scheduling");
			Assert.That(component.Instance.Output, Contains.Substring("Loaded 98 camper requests"),
				"Messages after scheduling");
			Assert.That(component.Instance.Output, Contains.Substring("98 campers scheduled"),
				"Messages after scheduling");
		}

		[Test]
		public void RunSchedule_ScheduleMissingActivityFile_IndicatesUnknownActivity()
		{
			// Arrange
			RenderedComponent<RunScheduler> component =
				_host.AddComponent<RunScheduler>();
			MemoryStream camperRequestStream = new MemoryStream(_missingActivityCamperRequestsBuffer);
			IFileReference camperRequestFile = Substitute.For<IFileReference>();
			camperRequestFile.OpenReadAsync().Returns(camperRequestStream);
			IFileReaderRef fileReaderRef = Substitute.For<IFileReaderRef>();
			fileReaderRef.EnumerateFilesAsync().Returns(new IFileReference[] { camperRequestFile });
			_fileReaderService.CreateReference(Arg.Any<ElementReference>()).Returns(fileReaderRef);

			// Act - execute scheduler
			HtmlAgilityPack.HtmlNode runSchedulerButton = component.Find("button");
			runSchedulerButton.Click();

			// Assert error message
			Assert.That(component.Instance.Output, 
				Contains.Substring("requested unknown activity: 'Horseplay'"),
				"Messages after scheduling");
		}

		[Test]
		public void RunSchedule_ScheduleOversubscribed_OutputsUnhappyCampers()
		{
			// Arrange
			RenderedComponent<RunScheduler> component =
				_host.AddComponent<RunScheduler>();
			MemoryStream camperRequestStream = new MemoryStream(_overSubscribedCamperRequestsBuffer);
			IFileReference camperRequestFile = Substitute.For<IFileReference>();
			camperRequestFile.OpenReadAsync().Returns(camperRequestStream);
			IFileReaderRef fileReaderRef = Substitute.For<IFileReaderRef>();
			fileReaderRef.EnumerateFilesAsync().Returns(new IFileReference[] { camperRequestFile });
			_fileReaderService.CreateReference(Arg.Any<ElementReference>()).Returns(fileReaderRef);

			// Act - execute scheduler
			HtmlAgilityPack.HtmlNode runSchedulerButton = component.Find("button");
			runSchedulerButton.Click();

			// Assert file is loaded
			Assert.That(component.Instance.Output, Contains.Substring("3 campers could not be scheduled"),
				"Messages after scheduling");
		}

	}
}
