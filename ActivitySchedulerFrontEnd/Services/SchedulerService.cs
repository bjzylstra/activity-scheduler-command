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

	}
}
