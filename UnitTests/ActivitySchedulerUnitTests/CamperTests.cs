using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActivityScheduler;

namespace ActivitySchedulerUnitTests
{
    [TestClass]
    public class CamperTests
    {
        [TestMethod]
        public void TryAssignBlock_NullBlock_Fails()
        {
            // Arrange - need a camper
            Camper camper = new Camper();

            // Act
            var didAssign = camper.TryAssignBlock(null);

            // Assert
            Assert.IsFalse(didAssign, "Succeeded in assigning block");
            Assert.AreEqual(0, camper.ScheduledBlocks.Count, "Number of scheduled blocks");
        }

        [TestMethod]
        public void TryAssignBlock_TimeSlotUnavailable_Fails()
        {
            // Arrange - need a camper with only 1 block available and 
            // block for an occupied slot
            Camper camper = new Camper(new int[] { 0, 1, 3 });
            ActivityBlock activityBlock = new ActivityBlock { TimeSlot = 1 };

            // Act
            var didAssign = camper.TryAssignBlock(activityBlock);

            // Assert
            Assert.IsFalse(didAssign, "Succeeded in assigning block");
            Assert.AreEqual(0, camper.ScheduledBlocks.Count, "Number of scheduled blocks");
        }

        [TestMethod]
        public void TryAssignBlock_TimeSlotAvailable_Succeeds()
        {
            // Arrange - need a camper with only 1 block available and 
            // block for an occupied slot
            Camper camper = new Camper(new int[] { 0, 1, 3 });
            ActivityBlock activityBlock = new ActivityBlock
            {
                TimeSlot = 2,
                ActivityDefinition = new ActivityDefinition { MaximumCapacity = 1 }
            };

            // Act
            var didAssign = camper.TryAssignBlock(activityBlock);

            // Assert
            Assert.IsTrue(didAssign, "Succeeded in assigning block");
            Assert.AreEqual(1, camper.ScheduledBlocks.Count, "Number of scheduled blocks");
            Assert.IsFalse(camper.IsAvailableInTimeSlot(2), "Slot with activity is available");
        }
    }
}
