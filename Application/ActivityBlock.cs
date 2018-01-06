using System.Collections.Generic;

namespace ActivityScheduler
{
    /// <summary>
    /// An instance of an activity definition assigned to specific time block
    /// </summary>
    public class ActivityBlock
    {
        public ActivityDefinition ActivityDefinition { get; set; }
        /// <summary>
        /// What block of the day.
        /// </summary>
        public int BlockNumber { get; set; }

        private List<Camper> _assignedCampers = new List<Camper>();
        /// <summary>
        /// Campers that are assigned to the block.
        /// </summary>
        public List<Camper> AssignedCampers { get { return _assignedCampers; } }
    }
}
