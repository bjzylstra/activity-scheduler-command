using ActivitySchedulerFrontEnd.Pages;
using ActivitySchedulerFrontEnd.Services;
using Blazor.FileReader;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class RunSchedulerTests : ActivitySchedulerTestsBase
	{
		private const string ActivitySetKey = "activitySet";
		private TestHost _host = new TestHost();
		private ILocalStorageService _localStorage;
		private IFileReaderService _fileReaderService;

		[OneTimeSetUp]
		public void PreloadActivityService()
		{
			SetUpActivityService();
			ServiceSetup();
			LoadTestCamperRequests();
		}

		[OneTimeTearDown]
		public void CleanupApplicationData()
		{
			CleanupActivityService();
		}

		private void ServiceSetup()
		{
			_host.AddService(_activityDefinitionService);
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
