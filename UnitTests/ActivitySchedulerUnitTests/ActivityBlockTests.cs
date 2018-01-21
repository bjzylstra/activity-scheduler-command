using ActivityScheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ActivitySchedulerUnitTests
{
    [TestClass]
    public class ActivityBlockTests
    {
        [TestMethod]
        public void TryAddCamper_ActivityHasRoom_Success()
        {
            // Arrange - Activity with room for 1 and a camper
            ActivityBlock activityBlock = new ActivityBlock
            {
                ActivityDefinition = new ActivityDefinition { MaximumCapacity = 1 }
            };
            Camper camper = new Camper();

            // Act
            var didAdd = activityBlock.TryAddCamper(camper);

            // Assert
            Assert.IsTrue(didAdd, "TryAddCamper succeeded");
            Assert.AreEqual(1, activityBlock.AssignedCampers.Count, "Number of assigned campers");
        }

        [TestMethod]
        public void TryAddCamper_ActivityHasNoRoom_Failure()
        {
            // Arrange - Activity with room for 1 and a camper
            ActivityBlock activityBlock = new ActivityBlock
            {
                ActivityDefinition = new ActivityDefinition { MaximumCapacity = 1 }
            };
            activityBlock.TryAddCamper(new Camper { FirstName = "First" });

            // Act
            var didAdd = activityBlock.TryAddCamper(new Camper { FirstName = "Second" });

            // Assert
            Assert.IsFalse(didAdd, "TryAddCamper succeeded");
            Assert.AreEqual(1, activityBlock.AssignedCampers.Count, "Number of assigned campers");
            Assert.AreEqual("First", activityBlock.AssignedCampers[0].FirstName, "Name of assigned camper");
        }
    }
}
