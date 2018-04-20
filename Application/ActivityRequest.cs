using System;

namespace ActivityScheduler
{
    /// <summary>
    /// A camper activity request composed of an activity and the 
    /// camper's rank of the activity
    /// </summary>
    public class ActivityRequest : IComparable<ActivityRequest>
    {
        public int Rank { get; set; }
        public ActivityDefinition Activity { get; set; }

        /// <summary>
        /// Compare this request to another for sorting.
        /// - Sort by rank
        /// </summary>
        /// <param name="other"></param>
        /// <returns>0 if equal, gt 0 if this before other, lt 0 if this after other</returns>
        public int CompareTo(ActivityRequest other)
        {
            return Rank.CompareTo(other.Rank);
        }

        public override string ToString()
        {
            return $"Rank {Rank} activity '{Activity.Name}'";
        }
    }
}
