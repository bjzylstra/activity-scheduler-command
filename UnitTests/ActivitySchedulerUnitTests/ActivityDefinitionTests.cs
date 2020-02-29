using NUnit.Framework;
using System;
using System.Collections.Generic;
using Camp;
using Microsoft.Extensions.Logging;

namespace ActivitySchedulerUnitTests
{
    [TestFixture]
    public class ActivityDefinitionTests
    {
        private const String BadContentFileLocation = @"..\..\..\Bogus.csv";
        private const String GoodDefinitionFileLocation = @"..\..\..\Activities.xml";
        private const String GoodScheduleCsvFileLocation = @"..\..\..\ActivitySchedule.csv";
        private const String NonExistentDefinitionFileLocation = @"NoSuchDirectory\NoSuchFile.xml";
        public static List<ActivityDefinition> DefaultActivityDefinitions = new List<ActivityDefinition>{
            new ActivityDefinition { Name = "Archery", MaximumCapacity = 16, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Canoeing", MaximumCapacity = 12, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Crafts", MaximumCapacity = 12, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Creation Exploration", MaximumCapacity = int.MaxValue, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Extreme Water Sports", MaximumCapacity = 14, OptimalCapacity = 14 },
            new ActivityDefinition { Name = "Horsemanship", MaximumCapacity = 14, OptimalCapacity = 14 },
            new ActivityDefinition { Name = "Splash (beach activities)", MaximumCapacity = int.MaxValue, OptimalCapacity = 14 },
            new ActivityDefinition { Name = "Team Sports", MaximumCapacity = int.MaxValue, OptimalCapacity = 14, MinimumCapacity = 8 },
            new ActivityDefinition { Name = "Wall Climbing", MaximumCapacity = 12, OptimalCapacity = 10 }
        };

        public static List<ActivityDefinition> IncompleteActivityDefinitions = new List<ActivityDefinition>{
            new ActivityDefinition { Name = "Archery", MaximumCapacity = 16, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Canoeing", MaximumCapacity = 12, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Crafts", MaximumCapacity = 12, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Creation Exploration", MaximumCapacity = int.MaxValue, OptimalCapacity = 12 },
            new ActivityDefinition { Name = "Extreme Water Sports", MaximumCapacity = 14, OptimalCapacity = 14 },
            new ActivityDefinition { Name = "Horsemanship", MaximumCapacity = 14, OptimalCapacity = 14 },
            new ActivityDefinition { Name = "Team Sports", MaximumCapacity = int.MaxValue, OptimalCapacity = 14, MinimumCapacity = 8 },
            new ActivityDefinition { Name = "Wall Climbing", MaximumCapacity = 12, OptimalCapacity = 10 }
        };

        private ILogger _logger;

        [SetUp]
        public void SetupLogger()
        {
            _logger = NSubstitute.Substitute.For<ILogger>();
        }

        [Test]
        public void ReadActivityDefinitions_fileNotFound_returnsNull()
        {
            // Act
            var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(NonExistentDefinitionFileLocation, _logger);

            // Assert
            Assert.IsNull(activityDefinitions, "Return from ReadActivityDefinitions");
        }

        [Test]
        public void ReadActivityDefinitions_invalidInput_returnsNull()
        {
            // Act
            var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(NonExistentDefinitionFileLocation, _logger);

            // Assert
            Assert.IsNull(activityDefinitions, "Return from ReadActivityDefinitions");
        }

        [Test]
        public void ReadActivityDefinitions_validInput_loadsList()
        {
            // Act
            var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(GoodDefinitionFileLocation, _logger);

            // Assert
            Assert.IsNotNull(activityDefinitions, "Return from ReadActivityDefinitions");
            AssertListsEqual(DefaultActivityDefinitions, activityDefinitions);
        }

        [Test]
        public void TryAssignCamperToNewActivityBlock_CamperIntersectActivityAvailable_Success()
        {
            // Arrange - setup a camper with slot 0 used and an activity with slot 1 used.
            Camper camper = new Camper(new int[]{ 0 });
            ActivityDefinition activityDefinition = new ActivityDefinition(new int[] { 1 }) { OptimalCapacity = 1, MaximumCapacity = 1 };

            // Act
            var didAssign = activityDefinition.TryAssignCamperToNewActivityBlock(camper);

            // Assert - should succeed and new block created in slot 2
            Assert.IsTrue(didAssign, "Assign to new block succeeded");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks.Count, "Number of activity blocks");
            Assert.AreEqual(2, activityDefinition.ScheduledBlocks[0].TimeSlot, "Time slot of the scheduled block");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks[0].AssignedCampers.Count,
                "Number of assigned campers");
        }

        [Test]
        public void TryAssignCamperToNewActivityBlock_CamperIntersectActivityNotAvailable_Fail()
        {
            // Arrange - setup a camper with slot 0,2 used and an activity with slot 1,3 used.
            Camper camper = new Camper(new int[] { 0,2 });
            ActivityDefinition activityDefinition = new ActivityDefinition(new int[] { 1,3 });

            // Act
            var didAssign = activityDefinition.TryAssignCamperToNewActivityBlock(camper);

            // Assert - should succeed and new block created in slot 2
            Assert.IsFalse(didAssign, "Assign to new block succeeded");
            Assert.AreEqual(0, activityDefinition.ScheduledBlocks.Count, "Number of activity blocks");
        }

        [Test]
        public void TryAssignCamperToExistingActivityBlock_CamperAvailableBlockHasRoom_Success()
        {
            // Arrange - Add activity block to activity that can take 2 campers
            // And add a camper with only that slot available.
            ActivityDefinition activityDefinition = new ActivityDefinition { OptimalCapacity = 2, MaximumCapacity = 3 };
            // Ensure create of block in slot 2 by only having that block available.
            Camper firstCamper = new Camper(new int[] { 0, 1, 3 }) { FirstName = "First" };
            activityDefinition.TryAssignCamperToNewActivityBlock(firstCamper);
            Camper secondCamper = new Camper(new int[] { 0, 1, 3 }) { FirstName = "Second" };

            // Act
            var didAssign = activityDefinition.TryAssignCamperToExistingActivityBlock(secondCamper, true);

            // Assert - should succeed and both campers there
            Assert.IsTrue(didAssign, "Assign to existing block succeeded");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks.Count, "Number of activity blocks");
            Assert.AreEqual(2, activityDefinition.ScheduledBlocks[0].TimeSlot, "Time slot of the scheduled block");
            Assert.AreEqual(2, activityDefinition.ScheduledBlocks[0].AssignedCampers.Count,
                "Number of assigned campers");
        }

        [Test]
        public void TryAssignCamperToExistingActivityBlock_CamperNotAvailable_Fail()
        {
            // Arrange - Add activity block to activity that can take 2 campers
            // And add a camper with only that slot available.
            ActivityDefinition activityDefinition = new ActivityDefinition { OptimalCapacity = 2, MaximumCapacity = 3 };
            // Ensure create of block in slot 2 by only having that block available.
            Camper firstCamper = new Camper(new int[] { 0, 1, 3 }) { FirstName = "First" };
            activityDefinition.TryAssignCamperToNewActivityBlock(firstCamper);
            Camper secondCamper = new Camper(new int[] { 2 }) { FirstName = "Second" };

            // Act
            var didAssign = activityDefinition.TryAssignCamperToExistingActivityBlock(secondCamper, true);

            // Assert - should fail and first camper is there
            Assert.IsFalse(didAssign, "Assign to existing block succeeded");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks.Count, "Number of activity blocks");
            Assert.AreEqual(2, activityDefinition.ScheduledBlocks[0].TimeSlot, "Time slot of the scheduled block");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks[0].AssignedCampers.Count,
                "Number of assigned campers");
            Assert.AreEqual("First", activityDefinition.ScheduledBlocks[0].AssignedCampers[0].FirstName, "Name of the assigned camper");
        }

        [Test]
        public void TryAssignCamperToExistingActivityBlock_CamperAvailableBlockHasNoRoom_Fail()
        {
            // Arrange - Add activity block to activity that can take 1 campers
            // And add a camper with only that slot available.
            ActivityDefinition activityDefinition = new ActivityDefinition { OptimalCapacity = 1, MaximumCapacity = 3 };
            // Ensure create of block in slot 2 by only having that block available.
            Camper firstCamper = new Camper(new int[] { 0, 1, 3 }) { FirstName = "First" };
            activityDefinition.TryAssignCamperToNewActivityBlock(firstCamper);
            Camper secondCamper = new Camper(new int[] { 0, 1, 3 }) { FirstName = "Second" };

            // Act
            var didAssign = activityDefinition.TryAssignCamperToExistingActivityBlock(secondCamper, true);

            // Assert
            Assert.IsFalse(didAssign, "Assign to existing block succeeded");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks.Count, "Number of activity blocks");
            Assert.AreEqual(2, activityDefinition.ScheduledBlocks[0].TimeSlot, "Time slot of the scheduled block");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks[0].AssignedCampers.Count,
                "Number of assigned campers");
            Assert.AreEqual("First", activityDefinition.ScheduledBlocks[0].AssignedCampers[0].FirstName, "Name of the assigned camper");
        }

        [Test]
        public void TryAssignCamperToExistingActivityBlock_CamperAvailableBlockAtOptimalAllowToMax_Success()
        {
            // Arrange - Add activity block to activity that can take 1 campers
            // And add a camper with only that slot available.
            ActivityDefinition activityDefinition = new ActivityDefinition { OptimalCapacity = 1, MaximumCapacity = 2 };
            // Ensure create of block in slot 2 by only having that block available.
            Camper firstCamper = new Camper(new int[] { 0, 1, 3 }) { FirstName = "First" };
            activityDefinition.TryAssignCamperToNewActivityBlock(firstCamper);
            Camper secondCamper = new Camper(new int[] { 0, 1, 3 }) { FirstName = "Second" };

            // Act
            var didAssign = activityDefinition.TryAssignCamperToExistingActivityBlock(secondCamper, false);

            // Assert
            Assert.IsTrue(didAssign, "Assign to existing block succeeded");
            Assert.AreEqual(1, activityDefinition.ScheduledBlocks.Count, "Number of activity blocks");
            Assert.AreEqual(2, activityDefinition.ScheduledBlocks[0].TimeSlot, "Time slot of the scheduled block");
            Assert.AreEqual(2, activityDefinition.ScheduledBlocks[0].AssignedCampers.Count,
                "Number of assigned campers");
        }

        [Test]
        public void ReadScheduleFromCsvFile_MultipleActivitiesAndCampers_ScheduledLoaded()
        {
            // Act 
            var activityDefinitions = ActivityDefinition.ReadScheduleFromCsvFile(GoodScheduleCsvFileLocation, _logger);

            // Assert
            Assert.IsNotNull(activityDefinitions, "Activity Definitions");
            Assert.AreEqual(9, activityDefinitions.Count, "Number of activities");
            ActivityDefinition checkActivityDefinition = activityDefinitions[8];
            Assert.AreEqual(4, checkActivityDefinition.ScheduledBlocks.Count, 
                $"Number of blocks in {checkActivityDefinition.Name}");
            IActivityBlock checkActivityBlock = checkActivityDefinition.ScheduledBlocks[3];
            Assert.That(checkActivityBlock.AssignedCampers.Count, Is.GreaterThan(0),
                $"Number of campers in {checkActivityDefinition.Name}, block {checkActivityBlock.TimeSlot}");
        }

        [Test]
        public void ReadScheduleFromCsvFile_FileNotFound_NoScheduleLoaded()
        {
            // Act 
            var activityDefinitions = ActivityDefinition.ReadScheduleFromCsvFile(NonExistentDefinitionFileLocation, _logger);
            Assert.That(activityDefinitions, Is.Null, "Activity Definitions");
        }

        [Test]
        public void ReadScheduleFromCsvFile_BadCsvFile_NoScheduleLoaded()
        {
            // Act 
            var activityDefinitions = ActivityDefinition.ReadScheduleFromCsvFile(BadContentFileLocation, _logger);
            Assert.That(activityDefinitions, Is.Null, "Activity Definitions");
        }

        /// <summary>
        /// Assert that a list of activity definitions matches an expected list
        /// </summary>
        /// <param name="expected">Expected list of activity definitions</param>
        /// <param name="actual">Actual list of activity definitions</param>
        private void AssertListsEqual(List<ActivityDefinition> expected, List<ActivityDefinition> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count, "Number of activity definitions");
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].Name, actual[i].Name, "Name of element at " + i);
                Assert.AreEqual(expected[i].MinimumCapacity, actual[i].MinimumCapacity, "MinimumCapacity of element at " + i);
                Assert.AreEqual(expected[i].MaximumCapacity, actual[i].MaximumCapacity, "MaximumCapacity of element at " + i);
                Assert.AreEqual(expected[i].OptimalCapacity, actual[i].OptimalCapacity, "OptimalCapacity of element at " + i);
            }
        }
    }
}
