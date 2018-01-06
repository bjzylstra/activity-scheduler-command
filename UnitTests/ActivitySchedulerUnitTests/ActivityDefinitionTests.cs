using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActivityScheduler;
using System;
using System.Collections.Generic;

namespace ActivitySchedulerUnitTests
{
    [TestClass]
    public class ActivityDefinitionTests
    {
        private const String BadContentFileLocation = @"..\..\..\Bogus.csv";
        private const String GoodFileLocation = @"..\..\..\Activities.xml";
        private const String NonExistentFileLocation = @"NoSuchDirectory\NoSuchFile.xml";

        [TestMethod]
        public void ReadActivityDefinitions_fileNotFound_returnsNull()
        {
            // Act
            var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(NonExistentFileLocation);

            // Assert
            Assert.IsNull(activityDefinitions, "Return from ReadActivityDefinitions");
        }

        [TestMethod]
        public void ReadActivityDefinitions_invalidInput_returnsNull()
        {
            // Act
            var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(NonExistentFileLocation);

            // Assert
            Assert.IsNull(activityDefinitions, "Return from ReadActivityDefinitions");
        }

        [TestMethod]
        public void ReadActivityDefinitions_validInput_loadsList()
        {
            // Act
            var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(GoodFileLocation);

            // Assert
            Assert.IsNotNull(activityDefinitions, "Return from ReadActivityDefinitions");
            List<ActivityDefinition> expectedActivityDefinitions = new List<ActivityDefinition>{
                new ActivityDefinition { Name = "Archery", MaximumCapacity = 16, OptimalCapacity = 12 },
                new ActivityDefinition { Name = "Canoeing", MaximumCapacity = 12, OptimalCapacity = 12 },
                new ActivityDefinition { Name = "Crafts", MaximumCapacity = 12, OptimalCapacity = 12 },
                new ActivityDefinition { Name = "Creation Exploration", MaximumCapacity = int.MaxValue, OptimalCapacity = 12 },
                new ActivityDefinition { Name = "Extreme Water Sports", MaximumCapacity = 14, OptimalCapacity = 14 },
                new ActivityDefinition { Name = "Horsemanship", MaximumCapacity = 14, OptimalCapacity = 14 },
                new ActivityDefinition { Name = "Splash", MaximumCapacity = int.MaxValue, OptimalCapacity = 14 },
                new ActivityDefinition { Name = "Team Sports", MaximumCapacity = int.MaxValue, OptimalCapacity = 14, MinimumCapacity = 8 },
                new ActivityDefinition { Name = "Wall Climbing", MaximumCapacity = 12, OptimalCapacity = 10 }
            };
            AssertListsEqual(expectedActivityDefinitions, activityDefinitions);
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
