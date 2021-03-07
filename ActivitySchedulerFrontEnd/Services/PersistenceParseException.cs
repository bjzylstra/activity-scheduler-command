using System;

namespace ActivitySchedulerFrontEnd.Services
{
	/// <summary>
	/// Exception for failing to read the schedule persistence
	/// </summary>
	public class PersistenceParseException : Exception
	{
		public string PersistenceName { get; private set; }

		public PersistenceParseException(string persistenceName, string description)
			: base(description)
		{
			PersistenceName = persistenceName;
		}

		public PersistenceParseException(string persistenceName, string description, 
			Exception innerException)
			: base(description, innerException)
		{
			PersistenceName = persistenceName;
		}
	}
}
