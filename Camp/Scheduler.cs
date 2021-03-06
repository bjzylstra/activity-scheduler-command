﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Camp
{
    /// <summary>
    /// Functional class to generate the schedules.
    /// </summary>
    public static class Scheduler
    {
        /// <summary>
        /// Generate a schedule for the camper requests
        /// </summary>
        /// <param name="camperRequests">Camper requests - updated by scheduling</param>
        /// <param name="activityDefinitions">Activity definitions - updated by scheduling</param>
        /// <param name="logger">Logger</param>
        /// <returns>List of unsuccessful camper requests</returns>
        public static List<CamperRequests> ScheduleActivities(List<CamperRequests> camperRequests,
            List<ActivityDefinition> activityDefinitions, ILogger logger)
        {
            // Sort the campers by difficulty to resolve activity list.
            // Most difficult go first.
            camperRequests.Sort();

            // Preload the activity blocks
            foreach (var activity in activityDefinitions)
            {
                activity.PreloadBlocks();
            }

            List<CamperRequests> unsuccessfulCamperRequests = Scheduler.ScheduleActivities(camperRequests,
                true, logger);
            if (unsuccessfulCamperRequests.Any())
            {
                //Console.Out.WriteLine($"Attempting to resolve {unsuccessfulCamperRequests.Count} " +
                //	$"unsuccessful camper requests using the activity maximum limits");
                unsuccessfulCamperRequests = Scheduler.ScheduleActivities(unsuccessfulCamperRequests,
                    false, logger);
            }
            foreach (var activity in activityDefinitions)
            {
                foreach (var activityBlock in activity.ScheduledBlocks)
                {
                    logger.LogDebug($"Scheduled '{activity.Name}' " +
                        $"in block {activityBlock.TimeSlot} " +
                        $"with {activityBlock.AssignedCampers.Count} campers");
                }
            }
            return unsuccessfulCamperRequests;
        }

        /// <summary>
        /// Schedule a set of camper requests into a list of ActivityBlocks
        /// </summary>
        /// <param name="camperRequestList">List of camper requests</param>
        /// <param name="useOptimalAsLimit">Use the activity optimal as the maximum</param>
        /// <param name="logger">Logger</param>
        /// <returns>List of camper requests that did not get placed</returns>
        public static List<CamperRequests> ScheduleActivities(List<CamperRequests> camperRequestList,
            bool useOptimalAsLimit, ILogger logger)
        {
            List<CamperRequests> unsuccessfulCamperRequests = new List<CamperRequests>();
            foreach (var camperRequest in camperRequestList)
            {
                var camper = camperRequest.Camper;

                // First schedule any activities that already have blocks allocated.
                List<ActivityRequest> newBlockActivities = new List<ActivityRequest>();
                foreach (var activityRequest in camperRequest.UnscheduledActivities)
                {
                    // Handle missing activity requests by logging and skipping.
                    if (activityRequest?.Activity == null)
                    {
                        logger.LogDebug($"Camper '{camper}' has no activity in rank '{activityRequest?.Rank}'. Skipping to next activity.");
                        continue;
                    }
                    if (activityRequest.Activity.TryAssignCamperToExistingActivityBlock(camper, useOptimalAsLimit))
                    {
                        logger.LogDebug($"Placed camper '{camper}' in existing activity block in slot " +
                            $"'{activityRequest.Activity.ScheduledBlocks.Where(sb => sb.AssignedCampers.Contains(camper)).Select(sb => sb.TimeSlot).First()}' " +
                            $"for '{activityRequest}'");
                    }
                    else
                    {
                        // No block exists, need to create one after all activities with
                        // existing blocks are done.
                        newBlockActivities.Add(activityRequest);
                    }
                }

                // Try to create new blocks and allocate the camper.
                List<ActivityRequest> noFitActivities = new List<ActivityRequest>();
                foreach (var activityRequest in newBlockActivities)
                {
                    if (activityRequest.Activity.TryAssignCamperToNewActivityBlock(camper))
                    {
                        logger.LogDebug($"Placed camper '{camper}' in new activity block in slot " +
                            $"'{activityRequest.Activity.ScheduledBlocks.Where(sb => sb.AssignedCampers.Contains(camper)).Select(sb => sb.TimeSlot).First()}' " +
                            $"for '{activityRequest}'");
                    }
                    else
                    {
                        if (activityRequest.Rank > 2)
                        {
                            noFitActivities.Add(activityRequest);
                            logger.LogDebug($"Trying alternate for camper '{camper}' {activityRequest}" +
                                $"camper has activities in blocks: " +
                                $"'{String.Join(',', camper.ScheduledBlocks.Select(b => b.TimeSlot).ToArray())}'");
                        }
                        else
                        {
                            logger.LogDebug($"Failed to place camper '{camper}' in {activityRequest}" +
                                $"camper has activities in blocks: " +
                                $"'{String.Join(',', camper.ScheduledBlocks.Select(b => b.TimeSlot).ToArray())}'");
                            unsuccessfulCamperRequests.Add(camperRequest);
                            continue;
                        }
                    }
                }

                // If all activities were placed - continue with next camper
                if (noFitActivities.Count == 0) continue;

                // If fitting the alternate won't complete the schedule, fail.
                if (camperRequest.ScheduledAlternateActivity || noFitActivities.Count > 1)
                {
                    logger.LogWarning($"Failed to place camper '{camper}' in " +
                        $"'{String.Join(',', noFitActivities.Select(a => a.ToString()).ToArray())}' " +
                        $"camper has activities in blocks: " +
                        $"'{String.Join(',', camper.ScheduledBlocks.Select(b => b.TimeSlot).ToArray())}'");
                    unsuccessfulCamperRequests.Add(camperRequest);
                    continue;
                }

                // Try the alternate.
                if (camperRequest.AlternateActivity != null)
                {
                    if (camperRequest.AlternateActivity.TryAssignCamperToExistingActivityBlock(camper, useOptimalAsLimit))
                    {
                        logger.LogDebug($"Used alternate to place camper '{camper}' in existing activity block in slot " +
                            $"'{camperRequest.AlternateActivity.ScheduledBlocks.Where(sb => sb.AssignedCampers.Contains(camper)).Select(sb => sb.TimeSlot).First()}' " +
                            $"for '{camperRequest.AlternateActivity.Name}'");
                        continue;
                    }
                    if (camperRequest.AlternateActivity.TryAssignCamperToNewActivityBlock(camper))
                    {
                        logger.LogDebug($"Placed camper '{camper}' in new activity block in slot " +
                            $"'{camperRequest.AlternateActivity.ScheduledBlocks.Where(sb => sb.AssignedCampers.Contains(camper)).Select(sb => sb.TimeSlot).First()}' " +
                            $"for '{camperRequest.AlternateActivity.Name}'");
                        continue;
                    }
                }

                // Alternate did not fit. FAIL
                logger.LogWarning($"Failed to place camper '{camper}' " +
                    $"in alternate '{camperRequest.AlternateActivity?.Name}' " +
                    $"after trying '{String.Join(',', noFitActivities.Select(a => a.ToString()).ToArray())}' " +
                    $"camper has activities in blocks: " +
                    $"'{String.Join(',', camper.ScheduledBlocks.Select(b => b.TimeSlot).ToArray())}'");
                unsuccessfulCamperRequests.Add(camperRequest);
            }
            return unsuccessfulCamperRequests;
        }

    }

}
