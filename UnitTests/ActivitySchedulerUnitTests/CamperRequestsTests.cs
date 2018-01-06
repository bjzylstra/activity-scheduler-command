using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActivityScheduler;
using System;

namespace ActivitySchedulerUnitTests
{
    [TestClass]
    public class CamperRequestsTests
    {
        private const String BadContentFileLocation = @"..\..\..\Bogus.csv";
        private const String GoodFileLocation = @"..\..\..\Skills Test Data.csv";
        private const String NonExistentFileLocation = @"NoSuchDirectory\NoSuchFile.csv";

        [TestMethod]
        public void ReadCamperRequests_fileNotFound_returnsNull()
        {
            // Act
            var camperRequests = CamperRequests.ReadCamperRequests(NonExistentFileLocation);

            // Assert
            Assert.IsNull(camperRequests, "Return from ReadCamperRequests");
        }

        [TestMethod]
        public void ReadCamperRequests_invalidInput_returnsNull()
        {
            // Act
            var camperRequests = CamperRequests.ReadCamperRequests(BadContentFileLocation);

            // Assert
            Assert.IsNull(camperRequests, "Return from ReadCamperRequests");
        }

        [TestMethod]
        public void ReadCamperRequests_validInput_loadsList()
        {
            // Act
            var camperRequests = CamperRequests.ReadCamperRequests(GoodFileLocation);

            // Assert
            Assert.IsNotNull(camperRequests, "Return from ReadCamperRequests");
            Assert.AreEqual(98, camperRequests.Count, "Number of camper requests");
        }

    }
}