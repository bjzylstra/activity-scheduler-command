using Camp;
using GridMvc.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ActivitySchedulerFrontEnd.Services
{
	public class SchedulerService : ISchedulerService
	{
		private readonly ILogger<SchedulerService> _logger;
		private List<ActivityDefinition> _scheduledActivities;

		public SchedulerService(ILogger<SchedulerService> logger)
		{
			_logger = logger;
		}

		public List<CamperRequests> ScheduleActivities(List<CamperRequests> camperRequests, List<ActivityDefinition> activityDefinitions)
		{
			List<CamperRequests> unscheduledCampers = Scheduler.ScheduleActivities(camperRequests, activityDefinitions, _logger);
			_scheduledActivities = activityDefinitions;
			return unscheduledCampers;
		}

		public ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId, Action<IGridColumnCollection<IActivityBlock>> columns, QueryDictionary<StringValues> query)
		{
			var server = new GridServer<IActivityBlock>(
				_scheduledActivities == null 
				? new List<ActivityBlock>()
				: _scheduledActivities.SelectMany(ad => ad.ScheduledBlocks),
				new QueryCollection(query), true,
			"activityScheduleGrid", columns);

			return server.ItemsToDisplay;
		}

		public ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId, QueryDictionary<StringValues> query)
		{
			var server = new GridServer<IActivityBlock>(
				_scheduledActivities == null
				? new List<ActivityBlock>()
				: _scheduledActivities.SelectMany(ad => ad.ScheduledBlocks),
				new QueryCollection(query), true,
			"activityScheduleGrid", null).AutoGenerateColumns();

			return server.ItemsToDisplay;
		}

		public Action<IGridColumnCollection<IActivityBlock>> GetActivityScheduleColumns()
		{
			return c =>
			{
				Func<IActivityBlock, int, string> CssForCount = (IActivityBlock block, int index) =>
				{
					return index > block.ActivityDefinition.OptimalCapacity
									? index > block.ActivityDefinition.MaximumCapacity
									? "red" : "yellow" : "";
				};
				c.Add(ab => ab.ActivityDefinition.Name).Titled(nameof(ActivityDefinition.Name)).SetWidth(20);
				c.Add(ab => ab.TimeSlot).RenderValueAs(ab => $"{ab.TimeSlot+1}").Titled("Block").SetWidth(5);
				c.Add(ab => ab.AssignedCampers.Count).Titled("#").SetWidth(5)
				.SetCellCssClassesContraint(ab => CssForCount(ab, ab.AssignedCampers.Count));
				// Columns have to be statically rendered so indexed loop does not work.
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(0)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 1));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(1)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 2));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(2)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 3));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(3)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 4));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(4)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 5));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(5)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 6));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(6)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 7));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(7)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 8));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(8)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 9));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(9)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 10));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(10)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 11));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(11)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 12));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(12)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 13));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(13)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 14));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(14)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 15));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(15)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 16));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(16)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 17));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(17)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 18));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(18)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 19));
				c.Add().RenderValueAs(ab => $"{ab.GetCamperName(19)}").SetWidth(20)
				.SetCellCssClassesContraint(ab => CssForCount(ab, 20));
			};
		}
	}

	public interface ISchedulerService
	{
		/// <summary>
		/// Generate a schedule for the camper requests
		/// </summary>
		/// <param name="camperRequests">Camper requests - updated by scheduling</param>
		/// <param name="activityDefinitions">Activity definitions - updated by scheduling</param>
		/// <returns>List of unsuccessful camper requests</returns>
		List<CamperRequests> ScheduleActivities(List<CamperRequests> camperRequests,
			List<ActivityDefinition> activityDefinitions);

		ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId,
			Action<IGridColumnCollection<IActivityBlock>> columns,
			QueryDictionary<StringValues> query);
		ItemsDTO<IActivityBlock> GetActivityBlocksGridRows(string scheduleId,
			QueryDictionary<StringValues> query);

		Action<IGridColumnCollection<IActivityBlock>> GetActivityScheduleColumns();
	}
}
