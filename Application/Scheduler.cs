using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ActivityScheduler
{
    /// <summary>
    /// Functional class to generate the schedules.
    /// </summary>
    public static class Scheduler
    {
        /// <summary>
        /// Schedule a set of camper requests into a list of ActivityBlocks
        /// </summary>
        /// <param name="camperRequestList">List of camper requests</param>
        /// <returns>true if all the campers could be scheduled</returns>
        public static Boolean ScheduleActivities(List<CamperRequests> camperRequestList)
        {
            foreach (var camperRequest in camperRequestList)
            {
                var camper = camperRequest.Camper;

                // First schedule any activities that already have blocks allocated.
                List<ActivityDefinition> newBlockActivities = new List<ActivityDefinition>();
                foreach (var activityRequest in camperRequest.ActivityRequests)
                {
                    if (activityRequest.TryAssignCamperToExistingActivityBlock(camper, true))
                    {
                        // No block exists, need to create one after all activities with
                        // existing blocks are done.
                        newBlockActivities.Add(activityRequest);
                    }
                }

                // Try to create new blocks and allocate the camper.
                List<ActivityDefinition> noFitActivities = new List<ActivityDefinition>();
                foreach (var activityRequest in newBlockActivities)
                {
                    if (!activityRequest.TryAssignCamperToNewActivityBlock(camper))
                    {
                        noFitActivities.Add(activityRequest);
                    }
                }

                // If all activities were placed - continue with next camper
                if (noFitActivities.Count == 0) continue;

                // If there are more than no fit activity, fail.
                if (noFitActivities.Count > 1) return false;

                // Try the alternate.
                if (!camperRequest.AlternateActivity.TryAssignCamperToExistingActivityBlock(camper, true)
                    && !camperRequest.AlternateActivity.TryAssignCamperToNewActivityBlock(camper))
                {
                    // Alternate did not fit. FAIL
                    return false;
                }
            }
            return true;
        }
    }

}
