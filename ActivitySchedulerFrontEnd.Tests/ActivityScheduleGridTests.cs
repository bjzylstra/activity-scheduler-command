using ActivitySchedulerFrontEnd.Pages;
using ActivitySchedulerFrontEnd.Services;
using Camp;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActivitySchedulerFrontEnd.Tests
{
	[TestFixture]
	public class ActivityScheduleGridTests : ActivitySchedulerTestsBase
	{
		private TestHost _host = new TestHost();
		private ISchedulerService _schedulerService;

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
			ILogger<SchedulerService> schedulerServiceLogger = Substitute.For<ILogger<SchedulerService>>();
			_schedulerService = new SchedulerService(schedulerServiceLogger);
			_host.AddService(_schedulerService);
			IJSRuntime jsRuntime = Substitute.For<IJSRuntime>();
			_host.AddService(jsRuntime);
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
				_schedulerService.ScheduleActivities(camperRequests, activityDefinitions);
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
		public void ActivityScheduleGrid_OverSubscribedSchedule_HasUnscheduledBlocks()
		{
			// Arrange - run schedule with successful data set
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			using (MemoryStream camperRequestStream = new MemoryStream(_overSubscribedCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.ScheduleActivities(camperRequests, activityDefinitions);
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
			Assert.That(addedActivityNames[0], Is.EqualTo(" Unscheduled"), 
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
				_schedulerService.ScheduleActivities(camperRequests, activityDefinitions);
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
			Assert.That(component.Instance.DragPayload, Is.Not.Null, "Drag payload");
			// The first cell should be archery block 0
			Assert.That(component.Instance.DragPayload.activityBlock.ActivityDefinition.Name,
				Is.EqualTo("Archery"), "Drag payload activity name");
			Assert.That(component.Instance.DragPayload.activityBlock.TimeSlot,
				Is.EqualTo(0), "Drag pay load time slot");
			// Camper should be set to first assigned camper.
			Assert.That(component.Instance.DragPayload.activityBlock.AssignedCampers[0].FirstName,
				Is.EqualTo(component.Instance.DragPayload.camper.FirstName), "Dragged campers first name");
			Assert.That(component.Instance.DragPayload.activityBlock.AssignedCampers[0].LastName,
				Is.EqualTo(component.Instance.DragPayload.camper.LastName), "Dragged campers last name");
		}

		[Test]
		public void ActivityScheduleGrid_DropCamperInDifferentTimeSlot_CamperIsNotMoved()
		{
			// Arrange - run schedule with successful data set and start a drag
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			List<CamperRequests> camperRequests;
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.ScheduleActivities(camperRequests, activityDefinitions);
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
			Camper camper = component.Instance.DragPayload.camper;
			List<string> initialCamperActivities = camper.ScheduledBlocks
				.Select(block => block.ActivityDefinition.Name).ToList();

			// Act - Drop on block 1 (source was block 0) for next activity
			HtmlNode dropTarget = activityBlockCampers.First(n =>
				n.GetAttributeValue("id", "")
				.Equals($"{activityDefinitions[1].Name}-1"));
			dropTarget.TriggerEventAsync("ondrop", new DragEventArgs());

			// Assert - Grid pay load is reset.
			Assert.That(component.Instance.DragPayload.activityBlock, Is.EqualTo(null), 
				"Drag payload activity block");
			Assert.That(component.Instance.DragPayload.camper, Is.EqualTo(null),
				"Drag payload camper");

			// Verify camper activities have not changed
			List<string> finalCamperActivities = camper.ScheduledBlocks
				.Select(block => block.ActivityDefinition.Name).ToList();
			Assert.That(finalCamperActivities, Is.EquivalentTo(initialCamperActivities),
				"Final camper activity set");
		}

		[Test]
		public void ActivityScheduleGrid_DropCamperInSameTimeSlot_CamperIsMoved()
		{
			// Arrange - run schedule with successful data set and start a drag
			List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>(
				_activityDefinitionService.GetActivityDefinition(DefaultSetName));
			List<CamperRequests> camperRequests;
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				_schedulerService.ScheduleActivities(camperRequests, activityDefinitions);
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
			Camper camper = component.Instance.DragPayload.camper;
			List<string> expectedCamperActivities = camper.ScheduledBlocks
				.Select(block => block.ActivityDefinition.Name).ToList();

			// Act - Drop on the block 0 for the next activity
			HtmlNode dropTarget = activityBlockCampers.First(n =>
				n.GetAttributeValue("id", "")
				.Equals($"{activityDefinitions[1].Name}-0"));
			dropTarget.TriggerEventAsync("ondrop", new DragEventArgs());
			expectedCamperActivities[0] = activityDefinitions[1].Name;

			// Assert - Grid pay load is reset.
			Assert.That(component.Instance.DragPayload.activityBlock, Is.EqualTo(null),
				"Drag payload activity block");
			Assert.That(component.Instance.DragPayload.camper, Is.EqualTo(null),
				"Drag payload camper");

			// Verify camper activities have not changed
			List<string> finalCamperActivities = camper.ScheduledBlocks
				.Select(block => block.ActivityDefinition.Name).ToList();
			Assert.That(finalCamperActivities, Is.EquivalentTo(expectedCamperActivities),
				"Final camper activity set");
		}

	}
}
