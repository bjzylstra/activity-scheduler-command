using Camp;
using NUnit.Framework;

namespace ActivitySchedulerUnitTests
{
    [TestFixture]
    public class ActivityBlockTests
    {
        [Test]
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
            Assert.That(didAdd, Is.True, "TryAddCamper succeeded");
            Assert.That(activityBlock.AssignedCampers.Count, Is.EqualTo(1),
                "Number of assigned campers");
        }

        [Test]
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
            Assert.That(didAdd, Is.False, "TryAddCamper succeeded");
            Assert.That(activityBlock.AssignedCampers.Count, Is.EqualTo(1),
                "Number of assigned campers");
            Assert.That(activityBlock.AssignedCampers[0].FirstName, Is.EqualTo("First"),
                "Name of assigned camper");
        }
    }
}
