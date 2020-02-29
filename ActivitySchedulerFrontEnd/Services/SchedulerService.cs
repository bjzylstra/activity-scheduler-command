using Camp;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ActivitySchedulerFrontEnd.Services
{
	public class SchedulerService : ISchedulerService
	{
		private readonly ILogger<SchedulerService> _logger;

		public SchedulerService(ILogger<SchedulerService> logger)
		{
			_logger = logger;
		}

		public List<CamperRequests> ScheduleActivities(List<CamperRequests> camperRequests, List<ActivityDefinition> activityDefinitions)
		{
			return Scheduler.ScheduleActivities(camperRequests, activityDefinitions, _logger);
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
	}
}
