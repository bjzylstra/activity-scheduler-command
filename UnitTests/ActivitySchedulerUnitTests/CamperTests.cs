using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActivityScheduler;
using Moq;

namespace ActivitySchedulerUnitTests
{
    [TestClass]
    public class CamperTests
    {
        [TestMethod]
        public void TryAssignBlock_BlockRejectsAdd_Fails()
        {
            // Arrange - need a camper with all slots open and and block that rejects
            Camper camper = new Camper();
            var mockBlock = new Mock<IActivityBlock>();
            mockBlock.Setup(b => b.TimeSlot).Returns(2);
            mockBlock.Setup(b => b.TryAddCamper(It.IsAny<Camper>())).Returns(false);

            // Act
            var didAssign = camper.TryAssignBlock(mockBlock.Object);

            // Assert
            Assert.IsFalse(didAssign, "Succeeded in assigning block");
            Assert.AreEqual(0, camper.ScheduledBlocks.Count, "Number of scheduled blocks");
        }

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
            Camper camper = new Camper(new int[] { 1 });
            var mockBlock = new Mock<IActivityBlock>();
            mockBlock.Setup(b => b.TimeSlot).Returns(1);
            mockBlock.Setup(b => b.TryAddCamper(It.IsAny<Camper>())).Returns(true);

            // Act
            var didAssign = camper.TryAssignBlock(mockBlock.Object);

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
            var mockBlock = new Mock<IActivityBlock>();
            mockBlock.Setup(b => b.TimeSlot).Returns(2);
            mockBlock.Setup(b => b.TryAddCamper(It.IsAny<Camper>())).Returns(true);

            // Act
            var didAssign = camper.TryAssignBlock(mockBlock.Object);

            // Assert
            Assert.IsTrue(didAssign, "Succeeded in assigning block");
            Assert.AreEqual(1, camper.ScheduledBlocks.Count, "Number of scheduled blocks");
            Assert.IsFalse(camper.IsAvailableInTimeSlot(2), "Slot with activity is available");
        }
    }
}
