using ActivityScheduler;
using NUnit.Framework;
using System.Collections.Generic;

namespace ActivitySchedulerUnitTests
{
    [TestFixture]
    public class SchedulerTests
    {
        [Test]
        public void ScheduleActivities_NoCampers_SucceedsNoResults()
        {
            var succeeded = Scheduler.ScheduleActivities(new List<CamperRequests>());
            Assert.IsTrue(succeeded, "Succeeded");
        }
    }
}
