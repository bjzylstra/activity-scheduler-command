using System;

namespace ActivityScheduler
{
    /// <summary>
    /// Represents a camper. 
    /// </summary>
    public class Camper
    {
        public String LastName { get; set; }
        public String FirstName { get; set; }

        public override string ToString()
        {
            return String.Format("{0}, {1}", LastName, FirstName);
        }
    }
}
