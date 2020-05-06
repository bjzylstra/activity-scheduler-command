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
			List<ActivityDefinition> schedule;
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				string scheduleId = "MySchedule";
				schedule = _schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
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

			foreach(var activity in schedule.Take(numberOfActivitiesToVerify))
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
			List<ActivityDefinition> schedule;
			using (MemoryStream camperRequestStream = new MemoryStream(_overSubscribedCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				string scheduleId = "MySchedule";
				schedule = _schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
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
			List<ActivityDefinition> schedule;
			string scheduleId = "MySchedule";
			using (MemoryStream camperRequestStream = new MemoryStream(_validCamperRequestsBuffer))
			{
				List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(
					camperRequestStream, activityDefinitions);
				schedule = _schedulerService.CreateSchedule(scheduleId, camperRequests, activityDefinitions);
				_localStorage.GetItemAsync<string>(Arg.Any<string>())
					.Returns(Task.FromResult(scheduleId));
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

	}
}
