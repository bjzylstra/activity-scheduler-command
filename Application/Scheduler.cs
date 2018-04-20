﻿using System;
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
        /// <returns>List of camper requests that did not get placed</returns>
        public static List<CamperRequests> ScheduleActivities(List<CamperRequests> camperRequestList)
        {
            List<CamperRequests> unsuccessfulCamperRequests = new List<CamperRequests>();
            foreach (var camperRequest in camperRequestList)
            {
                var camper = camperRequest.Camper;

                // First schedule any activities that already have blocks allocated.
                List<ActivityRequest> newBlockActivities = new List<ActivityRequest>();
                foreach (var activityRequest in camperRequest.ActivityRequests)
                {
                    if (activityRequest.Activity.TryAssignCamperToExistingActivityBlock(camper, false))
                    {
                        Console.Out.WriteLine($"Placed camper '{camper}' in existing activity block in slot " +
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
                        Console.Out.WriteLine($"Placed camper '{camper}' in new activity block in slot " +
                            $"'{activityRequest.Activity.ScheduledBlocks.Where(sb => sb.AssignedCampers.Contains(camper)).Select(sb => sb.TimeSlot).First()}' " +
                            $"for '{activityRequest}'");
                    }
                    else
                    {
                        if (activityRequest.Rank > 2)
                        {
                            noFitActivities.Add(activityRequest);
                            Console.Out.WriteLine($"Trying alternate for camper '{camper}' {activityRequest}" +
                                $"camper has activities in blocks: " +
                                $"'{String.Join(',', camper.ScheduledBlocks.Select(b => b.TimeSlot).ToArray())}'");
                        }
                        else
                        {
                            Console.Error.WriteLine($"Failed to place camper '{camper}' in {activityRequest}" +
                                $"camper has activities in blocks: " +
                                $"'{String.Join(',', camper.ScheduledBlocks.Select(b => b.TimeSlot).ToArray())}'");
                            unsuccessfulCamperRequests.Add(camperRequest);
                            continue;
                        }
                    }
                }

                // If all activities were placed - continue with next camper
                if (noFitActivities.Count == 0) continue;

                // If there are more than no fit activity, fail.
                if (noFitActivities.Count > 1)
                {
                    Console.Error.WriteLine($"Failed to place camper '{camper}' in " +
                        $"'{String.Join(',', noFitActivities.Select(a => a.ToString()).ToArray())}' " +
                        $"camper has activities in blocks: " +
                        $"'{String.Join(',', camper.ScheduledBlocks.Select(b => b.TimeSlot).ToArray())}'");
                    unsuccessfulCamperRequests.Add(camperRequest);
                    continue;
                }

                // Try the alternate.
                if (camperRequest.AlternateActivity != null)
                {
                    if (camperRequest.AlternateActivity.TryAssignCamperToExistingActivityBlock(camper, false))
                    {
                        Console.Out.WriteLine($"Used alternate to place camper '{camper}' in existing activity block in slot " +
                            $"'{camperRequest.AlternateActivity.ScheduledBlocks.Where(sb => sb.AssignedCampers.Contains(camper)).Select(sb => sb.TimeSlot).First()}' " +
                            $"for '{camperRequest.AlternateActivity.Name}'");
                        continue;
                    }
                    if (camperRequest.AlternateActivity.TryAssignCamperToNewActivityBlock(camper))
                    {
                        Console.Out.WriteLine($"Placed camper '{camper}' in new activity block in slot " +
                            $"'{camperRequest.AlternateActivity.ScheduledBlocks.Where(sb => sb.AssignedCampers.Contains(camper)).Select(sb => sb.TimeSlot).First()}' " +
                            $"for '{camperRequest.AlternateActivity.Name}'");
                        continue;
                    }
                }

                // Alternate did not fit. FAIL
                Console.Error.WriteLine($"Failed to place camper '{camper}' " +
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
