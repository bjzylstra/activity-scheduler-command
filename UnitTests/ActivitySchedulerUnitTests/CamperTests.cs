using NUnit.Framework;
using NSubstitute;
using Camp;

namespace ActivitySchedulerUnitTests
{
    [TestFixture]
    public class CamperTests
    {
        [Test]
        public void TryAssignBlock_BlockRejectsAdd_Fails()
        {
            // Arrange - need a camper with all slots open and and block that rejects
            Camper camper = new Camper();
            var mockBlock = Substitute.For<IActivityBlock>();
            mockBlock.TimeSlot.Returns(2);
            mockBlock.TryAddCamper(Arg.Any<Camper>()).Returns(false);

            // Act
            var didAssign = camper.TryAssignBlock(mockBlock);

            // Assert
            Assert.That(didAssign, Is.False, "Succeeded in assigning block");
            Assert.That(camper.ScheduledBlocks.Count, Is.EqualTo(0), "Number of scheduled blocks");
        }

        [Test]
        public void TryAssignBlock_NullBlock_Fails()
        {
            // Arrange - need a camper
            Camper camper = new Camper();

            // Act
            var didAssign = camper.TryAssignBlock(null);

            // Assert
            Assert.That(didAssign, Is.False, "Succeeded in assigning block");
            Assert.That(camper.ScheduledBlocks.Count, Is.EqualTo(0), "Number of scheduled blocks");
        }

        [Test]
        public void TryAssignBlock_TimeSlotUnavailable_Fails()
        {
            // Arrange - need a camper with only 1 block available and 
            // block for an occupied slot
            Camper camper = new Camper(new int[] { 1 });
            var mockBlock = Substitute.For<IActivityBlock>();
            mockBlock.TimeSlot.Returns(1);
            mockBlock.TryAddCamper(Arg.Any<Camper>()).Returns(true);

            // Act
            var didAssign = camper.TryAssignBlock(mockBlock);

            // Assert
            Assert.That(didAssign, Is.False, "Succeeded in assigning block");
            Assert.That(camper.ScheduledBlocks.Count, Is.EqualTo(0), "Number of scheduled blocks");
        }

        [Test]
        public void TryAssignBlock_TimeSlotAvailable_Succeeds()
        {
            // Arrange - need a camper with only 1 block available and 
            // block for an occupied slot
            Camper camper = new Camper(new int[] { 0, 1, 3 });
            var mockBlock = Substitute.For<IActivityBlock>();
            mockBlock.TimeSlot.Returns(2);
            mockBlock.TryAddCamper(Arg.Any<Camper>()).Returns(true);

            // Act
            var didAssign = camper.TryAssignBlock(mockBlock);

            // Assert
            Assert.That(didAssign, Is.True, "Succeeded in assigning block");
            Assert.That(camper.ScheduledBlocks.Count, Is.EqualTo(1), "Number of scheduled blocks");
            Assert.That(camper.IsAvailableInTimeSlot(2), Is.False, "Slot with activity is available");
        }
    }
}
