using ActivitySchedulerFrontEnd.Pages;
using ActivitySchedulerFrontEnd.Services;
using Blazor.FileReader;
using Blazored.LocalStorage;
using Camp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
		private byte[] _camperRequestsBuffer;

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
			ActivityDefinitionService service = new ActivityDefinitionService(_applicationName);
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
				string content = ActivityDefinition.WriteActivityDefinitionsToString(activityDefinitions);
				File.WriteAllText($"{ApplicationDirectoryInfo.FullName}\\{addSet}.xml", content);
				_expectedActivitySets.Add(addSet, new List<ActivityDefinition>(activityDefinitions));
			}

			ServiceSetup();
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

		public void ServiceSetup()
		{
			IActivityDefinitionService activityDefinitionService = new ActivityDefinitionService(_applicationName);
			_host.AddService(activityDefinitionService);
			IJSRuntime jsRuntime = Substitute.For<IJSRuntime>();
			_host.AddService(jsRuntime);
			_fileReaderService = Substitute.For<IFileReaderService>();
			_host.AddService(_fileReaderService);
			_localStorage = Substitute.For<ILocalStorageService>();
			_host.AddService(_localStorage);

			Assembly assembly = typeof(RunSchedulerTests).Assembly;
			using (Stream camperRequests = assembly.GetManifestResourceStream(
				"ActivitySchedulerFrontEnd.Tests.CamperRequests.csv"))
			{
				_camperRequestsBuffer = new byte[camperRequests.Length];
				int bytesRead = camperRequests.Read(_camperRequestsBuffer, 0, _camperRequestsBuffer.Length);
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
		public void RunSchedule_ReadFile_ReadsFile()
		{
			// Arrange
			RenderedComponent<RunScheduler> component =
				_host.AddComponent<RunScheduler>();
			MemoryStream fakeFile = new MemoryStream(_camperRequestsBuffer);
			IFileReference inputFile = Substitute.For<IFileReference>();
			inputFile.OpenReadAsync().Returns(fakeFile);
			IFileReaderRef fileReaderRef = Substitute.For<IFileReaderRef>();
			fileReaderRef.EnumerateFilesAsync().Returns(new IFileReference[] { inputFile });
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
	}
}
