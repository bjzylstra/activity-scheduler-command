using System.Collections.Generic;

namespace Camp
{
    /// <summary>
    /// Public methods on an interface to support mocking.
    /// </summary>
    public interface IActivityBlock
    {
        ActivityDefinition ActivityDefinition { get; set; }

        int TimeSlot { get; set; }

        List<Camper> AssignedCampers { get; }

        string GetCamperName(int camperIndex);

        bool TryAddCamper(Camper camper);

        void AddCamper(Camper camper);

        void RemoveCamper(Camper camper);
    }

    /// <summary>
    /// An instance of an activity definition assigned to specific time block
    /// </summary>
    public class ActivityBlock : IActivityBlock
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
        /// Normalized function to get the camper name that handles index out of bounds.
        /// Required for the grid display
        /// </summary>
        /// <param name="camperIndex">Index in AssignedCampers</param>
        /// <returns>Formatted name of camper at camperIndex or empty string if out of bounds</returns>
        public string GetCamperName(int camperIndex)
        {
            return camperIndex < _assignedCampers.Count 
                ? _assignedCampers[camperIndex].FullName 
                : string.Empty;
        }

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

        /// <summary>
        /// Add the camper without concern for limits
        /// </summary>
        /// <param name="camper">Camper to add</param>
        public void AddCamper(Camper camper)
        {
            _assignedCampers.Add(camper);
        }

        /// <summary>
        /// Remove the camper from the activity block
        /// </summary>
        /// <param name="camper">Camper to remove</param>
        public void RemoveCamper(Camper camper)
        {
            _assignedCampers.Remove(camper);
        }
    }
}
