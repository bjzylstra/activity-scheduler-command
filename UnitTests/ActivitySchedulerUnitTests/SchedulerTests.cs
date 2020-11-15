using Camp;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ActivitySchedulerUnitTests
{
    [TestFixture]
    public class SchedulerTests
    {
        private ILogger _logger;

        [SetUp]
        public void SetupLogger()
        {
            _logger = NSubstitute.Substitute.For<ILogger>();
        }
 
        [Test]
        public void ScheduleActivities_NoCampers_SucceedsNoResults()
        {
            // Arrange
            var camperRequestsList = new List<CamperRequests>
            {
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(0, unhappyCampers.Count, "Unhappy campers");
        }

        [Test]
        public void ScheduleActivities_CamperWithNoRequests_SucceedsNoResults()
        {
            // Arrange
            var camperRequestsList = new List<CamperRequests>
            {
                new CamperRequests
                {
                    Camper = new Camper{ FirstName = "Bub", LastName = "Slug"},
                    ActivityRequests = new List<ActivityRequest>()
                }
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(0, unhappyCampers.Count, "Unhappy campers");
        }

        [Test]
        public void ScheduleActivities_FirstCamperWithRequests_Succeeds()
        {
            // Arrange
            int numberOfActivities = 4;
            int capacity = 1;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity);
            int numberOfActivitiesTaken = 3;
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null)
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(0, unhappyCampers.Count, "Unhappy campers");
            Assert.AreEqual(numberOfActivitiesTaken, activityDefinitions
                .Count(ad => ad.ScheduledBlocks.Count == 1), "Blocks scheduled");
            AssertCamperRequestsFilled(camperRequestsList);
        }

        [Test]
        public void ScheduleActivities_AddedToExistingScheduledBlocks_Succeeds()
        {
            // Arrange
            int numberOfActivities = 4;
            int capacity = 2;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity);
            int numberOfActivitiesTaken = 3;
            // Camper 1 takes 1-3, camper 2 takes 2-4
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null),
                BuildCamper("Second", activityDefinitions.Skip(1).Take(numberOfActivitiesTaken), null)
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(0, unhappyCampers.Count, "Unhappy campers");
            Assert.AreEqual(4, activityDefinitions
                .Count(ad => ad.ScheduledBlocks.Count == 1), "Blocks scheduled");
            AssertCamperRequestsFilled(camperRequestsList);
        }

        [Test]
        public void ScheduleActivities_ActivityBlockFull_SucceedsBySchedulingSecondBlock()
        {
            // Arrange - campers want same activities and one does not have room.
            int numberOfActivities = 3;
            int capacity = 2;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity);
            int overSubscribedIndex = 1;
            activityDefinitions[overSubscribedIndex].MaximumCapacity = 1;

            int numberOfActivitiesTaken = 3;
            // Camper 1 takes 1-3, camper 2 takes 1-3 but no room in 2
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null),
                BuildCamper("Second", activityDefinitions.Take(numberOfActivitiesTaken), null)
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(0, unhappyCampers.Count, "Unhappy campers");
            for (int i = 0; i < numberOfActivities; i++)
            {
                // 2 of the over subscribed and 1 of the rest.
                Assert.AreEqual((i == overSubscribedIndex) ? 2 : 1, 
                    activityDefinitions[i].ScheduledBlocks.Count,
                    $"Number of scheduled blocks for {activityDefinitions[i].Name}");
            }
            AssertCamperRequestsFilled(camperRequestsList);
        }

        [Test]
        public void ScheduleActivities_ActivityBlockFullNoMoreCanBeScheduledNoAlternate_Fails()
        {
            // Arrange - campers want same activities and one does not have room.
            int numberOfActivities = 3;
            int capacity = 2;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity, true);
            int overSubscribedIndex = 1;
            activityDefinitions[overSubscribedIndex].MaximumCapacity = 1;

            int numberOfActivitiesTaken = 3;
            // Camper 1 takes 1-3, camper 2 takes 1-3 but no room in 2
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null),
                BuildCamper("Second", activityDefinitions.Take(numberOfActivitiesTaken), null)
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(1, unhappyCampers.Count, "Unhappy campers");
        }

        [Test]
        public void ScheduleActivities_ActivityBlockFullNoMoreCanBeScheduledHasAlternate_SucceedsUsingAlternate()
        {
            // Arrange - campers want same activities and one does not have room.
            int numberOfActivities = 4;
            int capacity = 2;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity, true);
            int overSubscribedIndex = 2;
            activityDefinitions[overSubscribedIndex].MaximumCapacity = 1;

            int numberOfActivitiesTaken = 3;
            // Camper 1 takes 1-3, camper 2 takes 1-3 but no room in 2
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null),
                // Supply an alternate of the final activity
                BuildCamper("Second", activityDefinitions.Take(numberOfActivitiesTaken), activityDefinitions[3])
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(0, unhappyCampers.Count, "Unhappy campers");
            Assert.AreEqual(4, activityDefinitions
                .Count(ad => ad.ScheduledBlocks.Count == 1), "Blocks scheduled");
            // Check that the alternate was scheduled.
            AssertCamperInActivity(camperRequestsList[1].Camper, camperRequestsList[1].AlternateActivity);
        }

        [Test]
        public void ScheduleActivities_Rank1ActivityBlockFullNoMoreCanBeScheduledHasAlternate_Fails()
        {
            // Arrange - campers want same activities and one does not have room.
            int numberOfActivities = 4;
            int capacity = 2;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity, true);
            int overSubscribedIndex = 0;
            activityDefinitions[overSubscribedIndex].MaximumCapacity = 1;

            int numberOfActivitiesTaken = 3;
            // Camper 1 takes 1-3, camper 2 takes 1-3 but no room in 2
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null),
                // Supply an alternate of the final activity
                BuildCamper("Second", activityDefinitions.Take(numberOfActivitiesTaken), activityDefinitions[3])
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(1, unhappyCampers.Count, "Unhappy campers");
            // Should not have scheduled the alternate
            Assert.AreEqual(3, activityDefinitions
                .Count(ad => ad.ScheduledBlocks.Count == 1), "Blocks scheduled");
            Assert.AreEqual(0, activityDefinitions[3].ScheduledBlocks.Count, "Alternate blocks scheduled");
        }

        [Test]
        public void ScheduleActivities_Rank2ActivityBlockFullNoMoreCanBeScheduledHasAlternate_Fails()
        {
            // Arrange - campers want same activities and one does not have room.
            int numberOfActivities = 4;
            int capacity = 2;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity, true);
            int overSubscribedIndex = 0;
            activityDefinitions[overSubscribedIndex].MaximumCapacity = 1;

            int numberOfActivitiesTaken = 3;
            // Camper 1 takes 1-3, camper 2 takes 1-3 but no room in 2
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null),
                // Supply an alternate of the final activity
                BuildCamper("Second", activityDefinitions.Take(numberOfActivitiesTaken), activityDefinitions[3])
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(1, unhappyCampers.Count, "Unhappy campers");
            // Should not have scheduled the alternate
            Assert.AreEqual(3, activityDefinitions
                .Count(ad => ad.ScheduledBlocks.Count == 1), "Blocks scheduled");
            Assert.AreEqual(0, activityDefinitions[3].ScheduledBlocks.Count, "Alternate blocks scheduled");
        }

        [Test]
        public void ScheduleActivities_2ActivityBlockFullHasAlternate_Fails()
        {
            // Arrange - campers want same activities and 2 do have room.
            int numberOfActivities = 4;
            int capacity = 2;
            List<ActivityDefinition> activityDefinitions = BuildActivityList(numberOfActivities, capacity, true);
            activityDefinitions[1].MaximumCapacity = 1;
            activityDefinitions[2].MaximumCapacity = 1;

            int numberOfActivitiesTaken = 3;
            // Camper 1 takes 1-3, camper 2 takes 1-3 but no room in 2
            var camperRequestsList = new List<CamperRequests>
            {
                BuildCamper("First", activityDefinitions.Take(numberOfActivitiesTaken), null),
                // Supply an alternate of the final activity
                BuildCamper("Second", activityDefinitions.Take(numberOfActivitiesTaken), activityDefinitions[3])
            };

            // Act
            var unhappyCampers = Scheduler.ScheduleActivities(camperRequestsList, false, _logger);

            // Assert
            Assert.AreEqual(1, unhappyCampers.Count, "Unhappy campers");
        }

        /// <summary>
        /// Assert the camper request was filled (all requests allocated)
        /// </summary>
        /// <param name="camperRequests">List of camper requests to test</param>
        private void AssertCamperRequestsFilled(List<CamperRequests> camperRequests)
        {
            foreach (var camperRequest in camperRequests)
            {
                // Check that requested activity was assigned.
                foreach (var activityRequest in camperRequest.ActivityRequests)
                {
                    AssertCamperInActivity(camperRequest.Camper, activityRequest.Activity);
                }
                // Check that the camper is not double booked.
                bool[] isScheduled = new bool[ActivityBlock.MaximumTimeSlots];
                foreach (var scheduledBlock in camperRequest.Camper.ScheduledBlocks)
                {
                    Assert.False(isScheduled[scheduledBlock.TimeSlot], 
                        $"Camper '{camperRequest.Camper.FirstName}' " +
                        $"is over subscribed in block '{scheduledBlock.TimeSlot}'");
                    isScheduled[scheduledBlock.TimeSlot] = true;
                }
            }
        }

        /// <summary>
        /// Assert that a camper has been scheduled into an activity.
        /// </summary>
        /// <param name="camper">Camper</param>
        /// <param name="activityDefinition">Activity that camper requested</param>
        private void AssertCamperInActivity(Camper camper, ActivityDefinition activityDefinition)
        {
            // Check each block of the activity before calling not there.
            int uninitialized = -1;
            int timeSlot = uninitialized;
            foreach (IActivityBlock activityBlock in activityDefinition.ScheduledBlocks)
            {
                if (activityBlock.AssignedCampers.Contains(camper))
                {
                    Assert.AreEqual(uninitialized, timeSlot, 
                        $"Camper {camper.FirstName} in 2 time slots for activity " +
                        $"{activityDefinition.Name}: {timeSlot} and {activityBlock.TimeSlot}");
                    timeSlot = activityBlock.TimeSlot;
                }
            }

            Assert.AreNotEqual(uninitialized, timeSlot, 
                $"Camper {camper.FirstName} not found in activity {activityDefinition.Name}");
        }

        /// <summary>
        /// Create a camper with a set of requests.
        /// </summary>
        /// <param name="name">Text to create a camper name from</param>
        /// <param name="activityRequests">Set of activity requests</param>
        /// <param name="alternateActivity">An alternate activity</param>
        /// <returns></returns>
        private CamperRequests BuildCamper(string name, 
            IEnumerable<ActivityDefinition> activityRequests, 
            ActivityDefinition alternateActivity)
        {
            int rank = 1;
            var theCamper = new Camper { FirstName = $"{name}", LastName = $"{name}" };
            var camperRequests = new CamperRequests
            {
                Camper = theCamper,
                ActivityRequests = new List<ActivityRequest>(activityRequests
                .Select(ar => new ActivityRequest { Rank = rank++, Activity = ar })),
                AlternateActivity = alternateActivity
            };
            return camperRequests;
        }

        /// <summary>
        /// Build a list of activities with specified capacity.
        /// </summary>
        /// <param name="numberOfActivities">Number of activities in the list</param>
        /// <param name="capacity">Maximum capacity of each activity</param>
        /// <param name="only1block">Allow activity in only 1 block</param>
        /// <returns>List of activities</returns>
        private List<ActivityDefinition> BuildActivityList(int numberOfActivities, int capacity, bool only1block = false)
        {
            List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>();
            for (int i = 0; i < numberOfActivities; i++)
            {
                if (only1block)
                {
                    List<int> usedBlocks = new List<int>();
                    for (int blockNumber = 0; blockNumber < ActivityBlock.MaximumTimeSlots; blockNumber++)
                    {
                        if (blockNumber != i) usedBlocks.Add(blockNumber);
                    }
                    activityDefinitions.Add(new ActivityDefinition(usedBlocks.ToArray())
                    {
                        Name = $"Activity {i + 1}",
                        OptimalCapacity = capacity,
                        MaximumCapacity = capacity
                    });
                }
                else
                {
                    activityDefinitions.Add(new ActivityDefinition()
                    {
                        Name = $"Activity {i + 1}",
                        OptimalCapacity = capacity,
                        MaximumCapacity = capacity
                    });
                }
            }
            return activityDefinitions;
        }
    }
}
