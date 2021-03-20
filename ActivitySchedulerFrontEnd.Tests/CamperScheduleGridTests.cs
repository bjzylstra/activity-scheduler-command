using ActivitySchedulerFrontEnd.Pages;
using ActivitySchedulerFrontEnd.Services;
using Blazored.LocalStorage;
using Camp;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class CamperScheduleGridTests : ActivitySchedulerTestsBase
	{
		private TestHost _host = new TestHost();
		private ILocalStorageService _localStorage;

		[OneTimeSetUp]
		public void PreloadActivityService()
		{
			SetUpApplicationServices();
			ServiceSetup();
		}

		[OneTimeTearDown]
		public void CleanupApplicationData()
		{
			CleanupApplicationServices();
		}

		private void ServiceSetup()
		{
			_host.AddService(_schedulerService);
			IJSRuntime jsRuntime = Substitute.For<IJSRuntime>();
			_host.AddService(jsRuntime);
			_localStorage = Substitute.For<ILocalStorageService>();
			_host.AddService(_localStorage);
		}

		[TestCase(1)]
		public void CamperScheduleGrid_ValidSchedule_AllCamperBlocksPopulated(int numberOfActivitiesToVerify)
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}

			// Act - load the grid component
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();

			// Assert
			List<HtmlNode> nameCells = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("FullName"))).ToList();
			int numberOfCampers = 98;
			Assert.That(nameCells, Has.Count.EqualTo(numberOfCampers),
				"Number of camper rows");

			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			foreach (var activity in schedule.Take(numberOfActivitiesToVerify))
			{
				for (int timeSlot = 0; timeSlot < ActivityBlock.MaximumTimeSlots; timeSlot++)
				{
					IActivityBlock activityBlock = activity.ScheduledBlocks.First(
						ab => ab.TimeSlot == timeSlot);
					foreach (var camper in activityBlock.AssignedCampers)
					{
						string camperSlotId = $"{camper.FullName}-{timeSlot}";
						List<HtmlNode> camperSlotCells = component.FindAll("select")
							.Where(node => node.Attributes.AttributesWithName("id")
							.Any(a => a.Value.Equals(camperSlotId))).ToList();
						Assert.That(camperSlotCells, Has.Count.EqualTo(1),
							$"Number of camper slots for Id = {camperSlotId}");
						List<HtmlAttribute> valueAttributes = camperSlotCells[0].Attributes
							.AttributesWithName("value").ToList();
						Assert.That(valueAttributes, Has.Count.EqualTo(1),
							$"Number of value attributes on selector for ID={camperSlotId}");
						Assert.That(valueAttributes[0].Value,
							Is.EqualTo(activity.Name), "Selected activity");
					}
				}
			}
		}

		[TestCase(2)]
		public void CamperScheduleGrid_OverSubscribedSchedule_HasUnscheduledBlocks(int numberOfActivitiesToVerify)
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_overSubscribedCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}

			// Act - load the grid component
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();

			// Assert
			List<HtmlNode> nameCells = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("FullName"))).ToList();
			int numberOfCampers = 104;
			Assert.That(nameCells, Has.Count.EqualTo(numberOfCampers),
				"Number of camper rows");

			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			ActivityDefinition unscheduledActivity = schedule.First(ad => 
				ad.Name.Equals(SchedulerService.UnscheduledActivityName));
			foreach (var activity in schedule.Take(numberOfActivitiesToVerify).Append(unscheduledActivity))
			{
				for (int timeSlot = 0; timeSlot < ActivityBlock.MaximumTimeSlots; timeSlot++)
				{
					IActivityBlock activityBlock = activity.ScheduledBlocks.First(
						ab => ab.TimeSlot == timeSlot);
					foreach (var camper in activityBlock.AssignedCampers)
					{
						string camperSlotId = $"{camper.FullName}-{timeSlot}";
						List<HtmlNode> camperSlotCells = component.FindAll("select")
							.Where(node => node.Attributes.AttributesWithName("id")
							.Any(a => a.Value.Equals(camperSlotId))).ToList();
						Assert.That(camperSlotCells, Has.Count.EqualTo(1),
							$"Number of camper slots for Id = {camperSlotId}");
						List<HtmlAttribute> valueAttributes = camperSlotCells[0].Attributes
							.AttributesWithName("value").ToList();
						Assert.That(valueAttributes, Has.Count.EqualTo(1),
							$"Number of value attributes on selector for ID={camperSlotId}");
						Assert.That(valueAttributes[0].Value,
							Is.EqualTo(activity.Name), "Selected activity");
					}
				}
			}
		}

		[TestCase(0,0)]
		public void CamperScheduleGrid_ChangeCamperActivity_ScheduleIsUpdated(int camperIndex, int timeSlot)
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}

			// Act - load the grid component and update a camper
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();
			List<string> fullNames = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("FullName")))
				.Select(node => node.InnerText).ToList();
			string camperSlotId = $"{fullNames[camperIndex]}-{timeSlot}";
			List<HtmlNode> camperSlotCells = component.FindAll("select")
				.Where(node => node.Attributes.AttributesWithName("id")
				.Any(a => a.Value.Equals(camperSlotId))).ToList();
			List<HtmlAttribute> valueAttributes = camperSlotCells[0].Attributes
				.AttributesWithName("value").ToList();
			string originalActivityName = valueAttributes[0].Value;
			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			string updatedActivityName = schedule[0].Name == originalActivityName
				? schedule[1].Name : schedule[0].Name;
			camperSlotCells[0].Change(updatedActivityName);

			// Assert
			// Reload the schedule and verify the camper has changed the activity.
			List<ActivityDefinition> updatedSchedule = _schedulerService.GetSchedule(scheduleId);
			Assert.That(updatedSchedule.First(ad => ad.Name.Equals(originalActivityName))
				.ScheduledBlocks[timeSlot].AssignedCampers.Select(c => c.FullName),
				Has.None.EqualTo(fullNames[camperIndex]), "Source activity camper list");
			Assert.That(updatedSchedule.First(ad => ad.Name.Equals(updatedActivityName))
				.ScheduledBlocks[timeSlot].AssignedCampers.Select(c => c.FullName),
				Has.One.EqualTo(fullNames[camperIndex]), "Target activity camper list");
		}

		[TestCase(0, 0)]
		public async Task CamperScheduleGrid_ChangeCamperActivity_SelecedCamperStillSelected(int camperIndex, int timeSlot)
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}
			// Select a camper
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();
			List<string> fullNames = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("FullName")))
				.Select(node => node.InnerText).ToList();
			HtmlNode nameCell = component.FindAll("button")
				.First(node => node.InnerText.Equals(fullNames[camperIndex]));
			await nameCell.ClickAsync();

			// Act - load the grid component and update a camper
			string camperSlotId = $"{fullNames[camperIndex]}-{timeSlot}";
			List<HtmlNode> camperSlotCells = component.FindAll("select")
				.Where(node => node.Attributes.AttributesWithName("id")
				.Any(a => a.Value.Equals(camperSlotId))).ToList();
			List<HtmlAttribute> valueAttributes = camperSlotCells[0].Attributes
				.AttributesWithName("value").ToList();
			string originalActivityName = valueAttributes[0].Value;
			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			string updatedActivityName = schedule[0].Name == originalActivityName
				? schedule[1].Name : schedule[0].Name;
			camperSlotCells[0].Change(updatedActivityName);

			// Assert - camper is still selected
			List<HtmlNode> selectedCamperRows = component.FindAll("tr")
				.Where(node => node.Attributes.AttributesWithName("class")
				.Any(a => a.Value.Contains("selected-camper"))).ToList();
			Assert.That(selectedCamperRows, Has.Count.EqualTo(1), "Selected camper rows");
			Assert.That(selectedCamperRows[0].Elements("td").First(n =>
				n.Attributes.Any(a => a.Name.Equals("data-name") && a.Value.Equals("FullName"))).InnerText,
				Is.EqualTo(fullNames[camperIndex]), "Camper row name");
		}

		[Test]
		public async Task CamperScheduleGrid_SelectCamper_CamperRowHilite()
		{
			// Arrange - run schedule with successful data set and load grid
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();

			// Act - select a camper
			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			Camper selectedCamper = schedule.First().ScheduledBlocks.First()
				.AssignedCampers.First();
			HtmlNode nameCell = component.FindAll("button")
				.First(node => node.InnerText.Equals(selectedCamper.FullName));
			await nameCell.ClickAsync();

			// Assert - load the row and verify it has the selected-camper class
			List<HtmlNode> selectedCamperRows = component.FindAll("tr")
				.Where(node => node.Attributes.AttributesWithName("class")
				.Any(a => a.Value.Contains("selected-camper"))).ToList();
			Assert.That(selectedCamperRows, Has.Count.EqualTo(1), "Selected camper rows");
			Assert.That(selectedCamperRows[0].Elements("td").First(n =>
				n.Attributes.Any(a => a.Name.Equals("data-name") && a.Value.Equals("FullName"))).InnerText,
				Is.EqualTo(selectedCamper.FullName), "Camper row name");
		}

		[Test]
		public async Task CamperScheduleGrid_SelectCamper_CamperGroupRowsHilite()
		{
			// Arrange - run schedule with successful data set and load grid
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();

			// Act - select a camper
			// Find a camper in a camper group
			List<HashSet<Camper>> camperGroups = _schedulerService.GetCamperGroupsForScheduleId(scheduleId);
			HashSet<Camper> selectedCamperGroup = camperGroups.First();
			// Pick a camper with a full name in the group
			Camper selectedCamper = selectedCamperGroup.First(c => !string.IsNullOrEmpty(c.FirstName));
			HtmlNode nameCell = component.FindAll("button")
				.First(node => node.InnerText.Equals(selectedCamper.FullName));
			await nameCell.ClickAsync();

			// Assert - load the row and verify it has the selected-group class
			List<HtmlNode> selectedCamperGroupRows = component.FindAll("tr")
				.Where(node => node.Attributes.AttributesWithName("class")
				.Any(a => a.Value.Contains("selected-group"))).ToList();
			// The selected camper does not get selected-group, only his peers
			Assert.That(selectedCamperGroupRows, Has.Count.EqualTo(selectedCamperGroup.Count-1), 
				"Selected camper group rows");
			// Check that each peer has a row in the set. Need to strip it down to the last names 
			// because the group has incomplete names
			List<string> selectedCamperGroupLastNames = selectedCamperGroupRows.Select(g => 
				g.Elements("td").First(n =>	n.Attributes.Any(a => 
					a.Name.Equals("data-name") && a.Value.Equals("FullName")))
				.InnerText.Split(',')[0]).ToList();
			foreach (Camper camper in selectedCamperGroup.Where(c => c != selectedCamper))
			{
				Assert.That(selectedCamperGroupLastNames,
					Has.One.EqualTo(camper.LastName), "Camper row name");
			}
		}

		[Test]
		public async Task CamperScheduleGrid_SelectCamper_CamperActivitiesHilite()
		{
			// Arrange - run schedule with successful data set and load grid
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();

			// Act - select a camper
			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			Camper selectedCamper = schedule.First().ScheduledBlocks.First()
				.AssignedCampers.First();
			HtmlNode nameCell = component.FindAll("button")
				.First(node => node.InnerText.Equals(selectedCamper.FullName));
			await nameCell.ClickAsync();

			// Assert - load all the activity cells
			List<HtmlNode> activityCells = component.FindAll("select")
				.Where(node => node.Id.Contains('-')).ToList();
			// Only Activity cells with an activity matching the selected camper in that slot
			// should have the selected-camper-activity
			List<HtmlNode> selectedActivityCells = activityCells.Where(node => 
				node.Attributes.AttributesWithName("class")
					.Any(a => a.Value.Contains("selected-camper-activity"))).ToList();
			Dictionary<int, string> selectedCamperActivities = selectedCamper.ScheduledBlocks
				.ToDictionary(b => b.TimeSlot, b => b.ActivityDefinition.Name);
			foreach (HtmlNode selectedActivityCell in selectedActivityCells)
			{
				int slotId = int.Parse(selectedActivityCell.Id.Split('-')[1]);
				Assert.That(selectedActivityCell.Attributes["value"].Value,
					Is.EqualTo(selectedCamperActivities[slotId]),
					$"{selectedActivityCell.Id}");
			}

			List<HtmlNode> unselectedActivityCells = activityCells.Where(node =>
				node.Attributes.AttributesWithName("class")
					.All(a => !a.Value.Contains("selected-camper-activity"))).ToList();
			foreach (HtmlNode unselectedActivityCell in unselectedActivityCells)
			{
				int slotId = int.Parse(unselectedActivityCell.Id.Split('-')[1]);
				Assert.That(unselectedActivityCell.Attributes["value"].Value,
					Is.Not.EqualTo(selectedCamperActivities[slotId]),
					$"{unselectedActivityCell.Id}");
			}

		}

		[Test]
		public void CamperScheduleGrid_ValidSchedule_ActivityDropDownShowsPreferences()
		{
			// Arrange - run schedule with successful data set and load grid
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(new ValueTask<string>(Task.FromResult(scheduleId)));
			}

			// Act - just render it.
			RenderedComponent<CamperScheduleGrid> component =
				_host.AddComponent<CamperScheduleGrid>();

			// Assert - Grab an activity cell for a camper and verify its
			// option text is annotated to match the preferences.
			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			Dictionary<Camper, List<ActivityDefinition>> preferences = _schedulerService.GetCamperPreferencesForScheduleId(scheduleId);
			// Do a bunch of campers
			foreach (Camper camper in schedule.First().ScheduledBlocks.First().AssignedCampers)
			{
				int expectedStars = preferences[camper].Count;
				foreach (var preference in preferences[camper])
				{
					foreach (HtmlNode camperActivityCell in component.FindAll("select")
						.Where(node => node.Id.Contains($"{camper.FullName}")))
					{
						// Find the text node for the prefered activity
						HtmlNode textNode = camperActivityCell.Descendants("#text")
							.First(n => n.InnerText.Contains(preference.Name));
						// Verify it has the expected number of stars
						string preferenceAnnotation = new string('*', expectedStars) + ' ';
						Assert.That(textNode.InnerText.Trim(),
							Contains.Substring(preferenceAnnotation),
							$"Preference {expectedStars}");
					}
					expectedStars--;
				}
			}
		}

	}
}
