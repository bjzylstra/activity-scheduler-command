using NUnit.Framework;
using System;
using Camp;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text;

namespace ActivitySchedulerUnitTests
{
    [TestFixture]
    public class CamperRequestsTests
    {
        private const String BadContentFileLocation = @"..\..\..\Bogus.csv";
        private const String GoodFileLocation = @"..\..\..\Skills Test Data.csv";
        private const String NonExistentFileLocation = @"NoSuchDirectory\NoSuchFile.csv";

        private ILogger _logger;

        [SetUp]
        public void SetupLogger()
        {
            _logger = NSubstitute.Substitute.For<ILogger>();
        }
 
        [Test]
        public void ReadCamperRequests_fileNotFound_returnsNull()
        {
			// Act
			List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(NonExistentFileLocation,
                ActivityDefinitionTests.DefaultActivityDefinitions, _logger);

            // Assert
            Assert.That(camperRequests, Is.Null, "Return from ReadCamperRequests");
        }

        [Test]
        public void ReadCamperRequests_invalidRequestFile_returnsNull()
        {
			// Act
			List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(BadContentFileLocation,
                ActivityDefinitionTests.DefaultActivityDefinitions, _logger);

            // Assert
            Assert.That(camperRequests, Is.Null, "Return from ReadCamperRequests");
        }

        [Test]
        public void ReadCamperRequests_validInput_loadsList()
        {
			// Act
			List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(GoodFileLocation,
                ActivityDefinitionTests.DefaultActivityDefinitions, _logger);

            // Assert
            Assert.That(camperRequests, Is.Not.Null, "Return from ReadCamperRequests");
            Assert.That(camperRequests.Count, Is.EqualTo(98), "Number of camper requests");
        }

        [Test]
        public void ReadCamperRequests_UnknownActivity_returnsNull()
        {
			// Act
			List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(GoodFileLocation,
                ActivityDefinitionTests.IncompleteActivityDefinitions, _logger);

            // Assert
            Assert.That(camperRequests, Is.Null, "Return from ReadCamperRequests");
        }

        [Test]
		public void GenerateCamperMateGroups_ValidInput_GeneratesExpectedList()
		{
			// Arrange load the known good camper list
			List<CamperRequests> camperRequests = CamperRequests.ReadCamperRequests(GoodFileLocation,
                ActivityDefinitionTests.DefaultActivityDefinitions, _logger);

			// Act - Generate the camper groups
			List<HashSet<Camper>> camperMateGroups = CamperRequests.GenerateCamperMateGroups(camperRequests);

            // Assert
            Assert.That(camperMateGroups, Is.Not.Null, "camperMateGroups");
            List<HashSet<Camper>> expectedCamperMateGroups = new List<HashSet<Camper>>
            {
                {
                    new HashSet<Camper>{
                        new Camper { LastName = "A"},
                        new Camper { LastName = "S"},
                        new Camper { LastName = "E"},
                        new Camper { LastName = "O"},
                        new Camper { LastName = "X"},
                        new Camper { LastName = "AC"},
                        new Camper { LastName = "AM"},
                        new Camper { LastName = "AS"},
                        new Camper { LastName = "AX"},
                        new Camper { LastName = "BC"},
                        new Camper { LastName = "BF"},
                        new Camper { LastName = "BK"},
                        new Camper { LastName = "BN"},
                        new Camper { LastName = "CA"},
                        new Camper { LastName = "CH"},
                        new Camper { LastName = "CJ"},
                        new Camper { LastName = "CK"},
                        new Camper { LastName = "CR"},
                        new Camper { LastName = "D"},
                        new Camper { LastName = "R"},
                        new Camper { LastName = "AN"},
                        new Camper { LastName = "BG"},
                        new Camper { LastName = "CB"},
                        new Camper { LastName = "CL"}
                    }
                },
                {
                    new HashSet<Camper>{
                        new Camper { LastName = "B"},
                        new Camper { LastName = "U"},
                        new Camper { LastName = "V"},
                        new Camper { LastName = "Z"},
                        new Camper { LastName = "AA"},
                        new Camper { LastName = "AK"},
                        new Camper { LastName = "AT"},
                        new Camper { LastName = "BM"},
                        new Camper { LastName = "CG"},
                        new Camper { LastName = "CI"}
                    }
                },
                {
                    new HashSet<Camper>{
                        new Camper { LastName = "C"},
                        new Camper { LastName = "Y"},
                        new Camper { LastName = "T"},
                        new Camper { LastName = "AE"},
                        new Camper { LastName = "AH"},
                        new Camper { LastName = "AI"},
                        new Camper { LastName = "AR"},
                        new Camper { LastName = "AZ"},
                        new Camper { LastName = "BL"},
                        new Camper { LastName = "BR"},
                        new Camper { LastName = "CF"}
                    }
                },
                {
                    new HashSet<Camper>{
                        new Camper { LastName = "G"},
                        new Camper { LastName = "F"},
                        new Camper { LastName = "N"},
                        new Camper { LastName = "BD"},
                        new Camper { LastName = "BS"},
                        new Camper { LastName = "CC"},
                        new Camper { LastName = "CS"},
                        new Camper { LastName = "I"}
                    }
                },
                {
                    new HashSet<Camper>{
                        new Camper { LastName = "K"},
                        new Camper { LastName = "H"},
                        new Camper { LastName = "P"},
                        new Camper { LastName = "BE"},
                        new Camper { LastName = "BU"}
                    }
                },
                {
                    new HashSet<Camper>{
                        new Camper { LastName = "AB"},
                        new Camper { LastName = "W"},
                        new Camper { LastName = "AO"},
                        new Camper { LastName = "AP"}
                    }
                }
            };
            Assert.That(camperMateGroups.Count, Is.EqualTo(expectedCamperMateGroups.Count),
                "Number of camper mate groups");
			foreach (var expectedCamperMateGroup in expectedCamperMateGroups)
			{
                bool foundMatch = false;
				foreach (var actualCamperMateGroup in camperMateGroups)
				{
                    foundMatch = actualCamperMateGroup.SetEquals(expectedCamperMateGroup);
                    if (foundMatch) break;
				}
                StringBuilder expectedCamperStringBuilder = new StringBuilder();
                foreach(Camper expectedCamper in expectedCamperMateGroup)
				{
                    if (expectedCamperStringBuilder.Length > 0)
					{
                        expectedCamperStringBuilder.Append(',');
					}
                    expectedCamperStringBuilder.Append(expectedCamper.LastName);
				}
                Assert.That(foundMatch, $"found match for {expectedCamperStringBuilder}");
			}
		}
    }
}
