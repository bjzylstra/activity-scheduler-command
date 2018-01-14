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
        /// <param name="activityDefinitions">List of activity definitions</param>
        /// <returns></returns>
        public static List<ActivityBlock> ScheduleActivities(
            List<CamperRequests> camperRequestList, 
            List<ActivityDefinition> activityDefinitions)
        {
            var scheduledActivities = new List<ActivityBlock>();
            var activityDefinitionByName = activityDefinitions.ToDictionary(ad => ad.Name, ad => ad);

            foreach (var camperRequest in camperRequestList)
            {
                // Schedule the requested activities for a camper.
                foreach (var activityRequest in camperRequest.ActivityRequests)
                {
                    // Get the activity definition.

                    // Check if there is an existing block with room.
                    // If not create a block in the next time slot. 
                    // If too many blocks use the alternate instead (if the alternate is full then oversubscribed).
                    // Assign the camper to the block.
                }
            }

            return scheduledActivities;
        }
    }
}
