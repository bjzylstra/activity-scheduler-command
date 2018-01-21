using System.Collections.Generic;

namespace ActivityScheduler
{
    /// <summary>
    /// An instance of an activity definition assigned to specific time block
    /// </summary>
    public class ActivityBlock
    {
        public static int MaximumTimeSlots = 4;

        public ActivityDefinition ActivityDefinition { get; set; }
        /// <summary>
        /// 0 based index of the daily time slots.
        /// </summary>
        public int TimeSlot { get; set; }

        private List<Camper> _assignedCampers = new List<Camper>();
        /// <summary>
        /// Campers that are assigned to the block.
        /// </summary>
        public List<Camper> AssignedCampers { get { return _assignedCampers; } }

        /// <summary>
        /// Try to add a camper to an activity block. Checks the max capacity for the defn
        /// </summary>
        /// <param name="camper">Camper to add</param>
        /// <returns>true if the camper could be added</returns>
        public bool TryAddCamper(Camper camper)
        {
            bool didAdd = false;
            if (_assignedCampers.Count < ActivityDefinition.MaximumCapacity)
            {
                _assignedCampers.Add(camper);
                didAdd = true;
            }
            return didAdd;
        }
    }
}
