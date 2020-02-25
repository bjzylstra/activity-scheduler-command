using NUnit.Framework;
using System;
using Camp;

namespace ActivitySchedulerUnitTests
{
    [TestFixture]
    public class CamperRequestsTests
    {
        private const String BadContentFileLocation = @"..\..\..\Bogus.csv";
        private const String GoodFileLocation = @"..\..\..\Skills Test Data.csv";
        private const String NonExistentFileLocation = @"NoSuchDirectory\NoSuchFile.csv";

        [Test]
        public void ReadCamperRequests_fileNotFound_returnsNull()
        {
            // Act
            var camperRequests = CamperRequests.ReadCamperRequests(NonExistentFileLocation,
                ActivityDefinitionTests.DefaultActivityDefinitions);

            // Assert
            Assert.That(camperRequests, Is.Null, "Return from ReadCamperRequests");
        }

        [Test]
        public void ReadCamperRequests_invalidRequestFile_returnsNull()
        {
            // Act
            var camperRequests = CamperRequests.ReadCamperRequests(BadContentFileLocation,
                ActivityDefinitionTests.DefaultActivityDefinitions);

            // Assert
            Assert.That(camperRequests, Is.Null, "Return from ReadCamperRequests");
        }

        [Test]
        public void ReadCamperRequests_validInput_loadsList()
        {
            // Act
            var camperRequests = CamperRequests.ReadCamperRequests(GoodFileLocation,
                ActivityDefinitionTests.DefaultActivityDefinitions);

            // Assert
            Assert.That(camperRequests, Is.Not.Null, "Return from ReadCamperRequests");
            Assert.That(camperRequests.Count, Is.EqualTo(98), "Number of camper requests");
        }

        [Test]
        public void ReadCamperRequests_UnknownActivity_returnsNull()
        {
            // Act
            var camperRequests = CamperRequests.ReadCamperRequests(GoodFileLocation,
                ActivityDefinitionTests.IncompleteActivityDefinitions);

            // Assert
            Assert.That(camperRequests, Is.Null, "Return from ReadCamperRequests");
        }
    }
}
