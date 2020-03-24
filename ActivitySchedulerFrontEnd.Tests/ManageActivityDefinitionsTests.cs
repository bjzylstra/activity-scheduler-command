using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActivitySchedulerFrontEnd.Pages;
using Blazor.FileReader;
using Blazored.LocalStorage;
using Camp;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class ManageActivityDefinitionsTests : ActivitySchedulerTestsBase
	{
		private const string ActivitySetKey = "activitySet";
		private TestHost _host = new TestHost();
		private Dictionary<string, List<ActivityDefinition>> _expectedActivitySets = 
			new Dictionary<string, List<ActivityDefinition>>();
		private ILocalStorageService _localStorage;

		[OneTimeSetUp]
		public void Setup()
		{
			SetUpActivityService();
			ServiceSetup();
		}

		[OneTimeTearDown]
		public void CleanupApplicationData()
		{
			CleanupActivityService();
		}

		public void ServiceSetup()
		{
			_host.AddService(_activityDefinitionService);
			IJSRuntime jsRuntime = Substitute.For<IJSRuntime>();
			_host.AddService(jsRuntime);
			IFileReaderService fileReaderService = Substitute.For<IFileReaderService>();
			_host.AddService(fileReaderService);
			_localStorage = Substitute.For<ILocalStorageService>();
			_host.AddService(_localStorage);
		}

		[Test]
		public void ManageActivityDefinitions_InitializeEmptyLocalStorage_ShowsDefaultActivitySet()
		{
			// Arrange / Act
			_localStorage.ClearReceivedCalls();
			_localStorage.GetItemAsync<string>(Arg.Any<string>())
				.Returns(Task.FromResult(string.Empty));
			RenderedComponent<ManageActivityDefinitions> component = 
				_host.AddComponent<ManageActivityDefinitions>();

			// Assert
			// Verify activity set selector is initialized to default
			Assert.That(component.Instance.ActivitySet, Is.EqualTo(DefaultSetName),
				"ActivitySet initial value");
			// Verify activity grid has correct number of rows.
			HtmlAgilityPack.HtmlNode gridCountSpan = component.FindAll("span")
				.FirstOrDefault(s => s.Attributes
				.Any(a => a.Value.Equals("grid-itemscount-caption", 
				StringComparison.OrdinalIgnoreCase)));
			Assert.That(gridCountSpan?.InnerText,
				Is.EqualTo(_expectedActivitySets[DefaultSetName].Count.ToString()),
				"Reported number of activities in grid");
			// Verify activity set is persisted.
			Received.InOrder(async () =>
			{
				// Empty comes from original load.
				await _localStorage.SetItemAsync(ActivitySetKey, DefaultSetName);
			});
		}

		[Test]
		public void ManageActivityDefinitions_InitializeFromLocalStorage_ShowsStoredActivitySet()
		{
			// Arrange / Act
			string activitySetName = _expectedActivitySets.Keys.Skip(1).First();
			_localStorage.GetItemAsync<string>(Arg.Any<string>())
				.Returns(Task.FromResult(activitySetName));
			RenderedComponent<ManageActivityDefinitions> component =
				_host.AddComponent<ManageActivityDefinitions>();

			// Assert
			// Verify activity set selector is initialized to default
			Assert.That(component.Instance.ActivitySet, Is.EqualTo(activitySetName),
				"ActivitySet initial value");
			// Verify activity grid has correct number of rows.
			HtmlAgilityPack.HtmlNode gridCountSpan = component.FindAll("span")
				.FirstOrDefault(s => s.Attributes
				.Any(a => a.Value.Equals("grid-itemscount-caption",
				StringComparison.OrdinalIgnoreCase)));
			Assert.That(gridCountSpan?.InnerText,
				Is.EqualTo(_expectedActivitySets[activitySetName].Count.ToString()),
				"Reported number of activities in grid");
		}

		[Test]
		public void ManageActivityDefinitions_SetActivitySet_UpdatesGrid()
		{
			// Arrange
			_localStorage.ClearReceivedCalls();
			RenderedComponent<ManageActivityDefinitions> component =
				_host.AddComponent<ManageActivityDefinitions>();

			// Act - change set
			string activitySetName = _expectedActivitySets.Keys.Skip(1).First();
			HtmlAgilityPack.HtmlNode setSelector = component.Find("select");
			setSelector.Change(activitySetName);

			// Assert
			// Verify activity set selector is updated
			Assert.That(component.Instance.ActivitySet, Is.EqualTo(activitySetName),
				"ActivitySet selected value");
			// Verify activity grid has correct number of rows.
			HtmlAgilityPack.HtmlNode gridCountSpan = component.FindAll("span")
				.FirstOrDefault(s => s.Attributes
				.Any(a => a.Value.Equals("grid-itemscount-caption",
				StringComparison.OrdinalIgnoreCase)));
			Assert.That(gridCountSpan?.InnerText,
				Is.EqualTo(_expectedActivitySets[activitySetName].Count.ToString()),
				"Reported number of activities in grid");

			// Verify activity set is persisted.
			Received.InOrder(async () =>
			{
				await _localStorage.SetItemAsync(ActivitySetKey, activitySetName);
			});
		}
	}
}
