using ActivitySchedulerFrontEnd.Pages;
using ActivitySchedulerFrontEnd.Services;
using Blazored.LocalStorage;
using Camp;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class ActivityScheduleGridTests : ActivitySchedulerTestsBase
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

		[Test]
		public void ActivityScheduleGrid_ValidSchedule_OnlyActivityDefinitionBlocks()
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				string scheduleId = "MySchedule";
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
			}

			// Act - load the grid component
			RenderedComponent<ActivityScheduleGrid> component =
				_host.AddComponent<ActivityScheduleGrid>();

			// Assert
			List<HtmlNode> gridCells = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("ActivityDefinition.Name"))).ToList();
			Assert.That(gridCells, Has.Count.EqualTo(activityDefinitions.Count * 4),
				"Number of activity rows");
			List<string> cellActivityNames = gridCells.Select(c =>
				c.InnerText).Distinct().ToList();
			Assert.That(cellActivityNames, Is.EquivalentTo(activityDefinitions.Select(ad => ad.Name)),
				"Activity row names");
		}

		[Test]
		public void ActivityScheduleGrid_ValidSchedule_NoCapacityErrorStyles()
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				string scheduleId = "MySchedule";
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
			}

			// Act - load the grid component
			RenderedComponent<ActivityScheduleGrid> component =
				_host.AddComponent<ActivityScheduleGrid>();

			// Assert - none are marked over-subscribed or under-subscribed
			List<HtmlNode> countNodes = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("AssignedCampers.Count"))).ToList();
			List<string> countClass = countNodes
				.Select(node => node.Attributes["class"].Value)
				.ToList();
			Assert.That(countClass, Has.None.Contains("capacity-over"),
				"Activity block styles");
			Assert.That(countClass, Has.None.Contains("capacity-under"),
				"Activity block styles");
		}

		[Test]
		public void ActivityScheduleGrid_PrebuiltValidSchedule_NoCapacityErrorStyles()
		{
			// Arrange - run schedule with successful data set
			_localStorage.GetItemAsync<string>(Arg.Any<string>())
				.Returns(Task.FromResult(PrebuiltScheduleId));

			// Act - load the grid component
			RenderedComponent<ActivityScheduleGrid> component =
				_host.AddComponent<ActivityScheduleGrid>();

			// Assert - none are marked over-subscribed or under-subscribed
			List<HtmlNode> countNodes = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("AssignedCampers.Count"))).ToList();
			List<string> countClass = countNodes
				.Select(node => node.Attributes["class"].Value)
				.ToList();
			Assert.That(countClass, Has.None.Contains("capacity-over"),
				"Activity block styles");
			Assert.That(countClass, Has.None.Contains("capacity-under"),
				"Activity block styles");
		}

		[Test]
		public void ActivityScheduleGrid_OverSubscribedSchedule_HasUnscheduledBlocks()
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			using (MemoryStream camperRequestStream = new MemoryStream(_overSubscribedCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				string scheduleId = "MySchedule";
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
			}

			// Act - load the grid component
			RenderedComponent<ActivityScheduleGrid> component =
				_host.AddComponent<ActivityScheduleGrid>();

			// Assert - activity definition has a new activity for unscheduled
			List<string> originalActivityNames = _activityDefinitionService.GetActivityDefinition(DefaultSetName)
			.Select(ad => ad.Name).ToList();
			List<string> addedActivityNames = activityDefinitions.Select(ad => ad.Name)
				.Except(originalActivityNames).ToList();
			Assert.That(addedActivityNames, Has.Count.EqualTo(1),
				"Number of activity definitions added by scheduling");
			Assert.That(addedActivityNames[0], Is.EqualTo(SchedulerService.UnscheduledActivityName), 
				"Name of activity added by scheduling");

			// Check that all of the activities included the added 1 are on the grid
			List<HtmlNode> gridCells = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("data-name")
				.Any(a => a.Value.Equals("ActivityDefinition.Name"))).ToList();
			Assert.That(gridCells, Has.Count.EqualTo(activityDefinitions.Count * 4),
				"Number of activity rows");
			List<string> cellActivityNames = gridCells.Select(c =>
				c.InnerText).Distinct().ToList();
			Assert.That(cellActivityNames, Is.EquivalentTo(activityDefinitions.Select(ad => ad.Name)),
				"Activity row names");
		}

		[Test]
		public void ActivityScheduleGrid_StartDragOfCamper_PayloadUpdated()
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			List<CamperRequests> camperRequests;
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				string scheduleId = "MySchedule";
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
			}
			RenderedComponent<ActivityScheduleGrid> component =
				_host.AddComponent<ActivityScheduleGrid>();

			// Act - Start a drag on a camper activity cell
			List<HtmlNode> camperActivityCells = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("class")
				.Any(a => a.Value.Equals("activity-camper-cell"))).ToList();
			Assert.That(camperActivityCells, Has.Count.EqualTo(camperRequests.Count() * 4),
				"Number of camper activity cells");
			camperActivityCells[0].TriggerEventAsync("ondragstart", new DragEventArgs());

			// Assert - Grid pay load is set.
			// The first cell should be archery block 0
			Assert.That(component.Instance.DragPayload.ActivityName,
				Is.EqualTo("Archery"), "Drag payload activity name");
			Assert.That(component.Instance.DragPayload.TimeSlot,
				Is.EqualTo(0), "Drag pay load time slot");
			Assert.That(component.Instance.DragPayload.CamperName,
				Is.Not.Null, "Drag pay load camper name");
		}

		[Test]
		public void ActivityScheduleGrid_DropCamperInDifferentTimeSlot_CamperIsNotMoved()
		{
			// Arrange - run schedule with successful data set and start a drag
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			List<CamperRequests> camperRequests;
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
			}
			RenderedComponent<ActivityScheduleGrid> component =
				_host.AddComponent<ActivityScheduleGrid>();
			List<HtmlNode> camperActivityCells = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("class")
				.Any(a => a.Value.Equals("activity-camper-cell"))).ToList();
			Assert.That(camperActivityCells, Has.Count.EqualTo(camperRequests.Count() * 4),
				"Number of camper activity cells");
			// Gather up the activity block drop zones.
			List<HtmlNode> activityBlockCampers = component.FindAll("tr")
				.Where(node => node.Attributes.AttributesWithName("ondrop").Any())
				.ToList();
			Assert.That(activityBlockCampers, Has.Count.EqualTo(activityDefinitions.Count * 4),
				"Number of activity rows");
			// Start a drag on a camper in the first activity block
			HtmlNode sourceCamperActivity = camperActivityCells.First(
				node => node.ParentNode.GetAttributeValue("id", "") == 
				activityBlockCampers[0].GetAttributeValue("id", "NotDefined"));
			sourceCamperActivity.TriggerEventAsync("ondragstart", new DragEventArgs());
			string camperName = component.Instance.DragPayload.CamperName;

			// Act - Drop on block 1 (source was block 0) for next activity
			HtmlNode dropTarget = activityBlockCampers.First(n =>
				n.GetAttributeValue("id", "")
				.Equals($"{activityDefinitions[1].Name}-1"));
			dropTarget.TriggerEventAsync("ondrop", new DragEventArgs());

			// Assert - Grid pay load is reset.
			Assert.That(component.Instance.DragPayload.ActivityName, Is.EqualTo(null), 
				"Drag payload activity name");
			Assert.That(component.Instance.DragPayload.CamperName, Is.EqualTo(null),
				"Drag payload camper");
			Assert.That(component.Instance.DragPayload.TimeSlot, Is.EqualTo(0),
				"Drag payload block number");

			// Verify camper activities have not changed
			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			ActivityDefinition sourceActivity = schedule.Find(ad => ad.Name.Equals(activityDefinitions[0].Name));
			List<string> assignedCampersByName = sourceActivity.ScheduledBlocks[component.Instance.DragPayload.TimeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.One.EqualTo(camperName),
				"Assigned campers on drag source");
			ActivityDefinition targetActivity = schedule.Find(ad => ad.Name.Equals(activityDefinitions[1].Name));
			assignedCampersByName = targetActivity.ScheduledBlocks[component.Instance.DragPayload.TimeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.None.EqualTo(camperName),
				"Assigned campers on drop target");
		}

		[Test]
		public void ActivityScheduleGrid_DropCamperInSameTimeSlot_CamperIsMoved()
		{
			// Arrange - run schedule with successful data set and start a drag
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			List<CamperRequests> camperRequests;
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
			}
			RenderedComponent<ActivityScheduleGrid> component =
				_host.AddComponent<ActivityScheduleGrid>();
			List<HtmlNode> camperActivityCells = component.FindAll("td")
				.Where(node => node.Attributes.AttributesWithName("class")
				.Any(a => a.Value.Equals("activity-camper-cell"))).ToList();
			Assert.That(camperActivityCells, Has.Count.EqualTo(camperRequests.Count() * 4),
				"Number of camper activity cells");
			// Gather up the activity block drop zones.
			List<HtmlNode> activityBlockCampers = component.FindAll("tr")
				.Where(node => node.Attributes.AttributesWithName("ondrop").Any())
				.ToList();
			Assert.That(activityBlockCampers, Has.Count.EqualTo(activityDefinitions.Count * 4),
				"Number of activity rows");
			// Start a drag on a camper in the first activity block
			HtmlNode sourceCamperActivity = camperActivityCells.First(
				node => node.ParentNode.GetAttributeValue("id", "") ==
				activityBlockCampers[0].GetAttributeValue("id", "NotDefined"));
			sourceCamperActivity.TriggerEventAsync("ondragstart", new DragEventArgs());
			string camperName = component.Instance.DragPayload.CamperName;

			// Act - Drop on the block 0 for the next activity
			HtmlNode dropTarget = activityBlockCampers.First(n =>
				n.GetAttributeValue("id", "")
				.Equals($"{activityDefinitions[1].Name}-0"));
			dropTarget.TriggerEventAsync("ondrop", new DragEventArgs());

			// Assert - Grid pay load is reset.
			Assert.That(component.Instance.DragPayload.ActivityName, Is.EqualTo(null),
				"Drag payload activity");
			Assert.That(component.Instance.DragPayload.CamperName, Is.EqualTo(null),
				"Drag payload camper");
			Assert.That(component.Instance.DragPayload.TimeSlot, Is.EqualTo(0),
				"Drag payload time slot");

			// Verify camper activities have not changed
			List<ActivityDefinition> schedule = _schedulerService.GetSchedule(scheduleId);
			ActivityDefinition sourceActivity = schedule.Find(ad => ad.Name.Equals(activityDefinitions[0].Name));
			List<string> assignedCampersByName = sourceActivity.ScheduledBlocks[component.Instance.DragPayload.TimeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.None.EqualTo(camperName),
				"Assigned campers on drag source");
			ActivityDefinition targetActivity = schedule.Find(ad => ad.Name.Equals(activityDefinitions[1].Name));
			assignedCampersByName = targetActivity.ScheduledBlocks[component.Instance.DragPayload.TimeSlot]
				.AssignedCampers.Select(c => c.FullName).ToList();
			Assert.That(assignedCampersByName, Has.One.EqualTo(camperName),
				"Assigned campers on drop target");
		}

	}
}
