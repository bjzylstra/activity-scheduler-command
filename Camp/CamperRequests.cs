using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Camp
{
	/// <summary>
	/// Represents the activity and cabin placement requests for a camper.
	/// Campers are sortable by their request lists - hardest to place go first.
	/// </summary>
	public class CamperRequests : IComparable<CamperRequests>
	{
		public sealed class CamperRequestsMap : ClassMap<CamperRequests>
		{
			private Dictionary<String, ActivityDefinition> _activityDefinitionByName;
			private Camper _lastReadCamper;
			private Camper _lastCabinMate;

			/// <summary>
			/// Construct the mapper for camper requests in the CSV file
			/// </summary>
			/// <param name="activityDefinitions">List of activity definitions</param>
			public CamperRequestsMap(List<ActivityDefinition> activityDefinitions)
			{
				_activityDefinitionByName = activityDefinitions.ToDictionary(
					ad => ad.Name, ad => ad);

				int index = 0;
				int camperIndex = index;
				Map(m => m.Camper).ConvertUsing(row => {
					_lastReadCamper = new Camper
					{
						LastName = row.GetField(camperIndex),
						FirstName = row.GetField(camperIndex + 1)
					};
					return _lastReadCamper;
				});
				index += 2;
				int mateIndex = index;
				Map(m => m.CabinMate).ConvertUsing(row =>
				{
					string cabinMateLastName = row.GetField(mateIndex);
					_lastCabinMate = String.IsNullOrEmpty(cabinMateLastName)
						? null
						: new Camper { LastName = cabinMateLastName };
					return _lastCabinMate;
				});
				index++;
				int activityIndex = index;
				Map(m => m.ActivityRequests).ConvertUsing(row => {
					return new List<ActivityRequest> {
						new ActivityRequest{Rank = 1, Activity = GetActivityForName(row.GetField(activityIndex))},
						new ActivityRequest{Rank = 2, Activity = GetActivityForName(row.GetField(activityIndex+1))},
						new ActivityRequest{Rank = 3, Activity = GetActivityForName(row.GetField(activityIndex+2))},
						new ActivityRequest{Rank = 4, Activity = GetActivityForName(row.GetField(activityIndex+3))}
					};
				});
				index += 4;
				int alternateIndex = index;
				Map(m => m.AlternateActivity).ConvertUsing(row =>
				{
					return GetActivityForName(row.GetField(alternateIndex));
				});
			}

			/// <summary>
			/// Look up the activity definition by name and handle empty names.
			/// </summary>
			/// <param name="activityName">Activity Name</param>
			/// <returns>Activity Definition for the name</returns>
			private ActivityDefinition GetActivityForName(String activityName)
			{
				if (String.IsNullOrWhiteSpace(activityName)) return null;

				if (_activityDefinitionByName.ContainsKey(activityName))
				{
					return _activityDefinitionByName[activityName];
				}
				String message = String.Format("Camper '{0}' requested unknown activity: '{1}'",
					_lastReadCamper, activityName);
				throw new KeyNotFoundException(message);
			}
		}


		public Camper Camper { get; set; }
		/// <summary>
		/// From the data, this appears to be only a last name. We may need better data
		/// before trying to use this?
		/// </summary>
		public Camper CabinMate { get; set; }
		private List<ActivityRequest> _activityRequests;
		public List<ActivityRequest> ActivityRequests
		{
			get { return _activityRequests; }
			set
			{
				// Leave it sorted.
				_activityRequests = (value != null)
					? _activityRequests = new List<ActivityRequest>(value)
					: _activityRequests = new List<ActivityRequest>();
				// Sort including name so that campers with the same requests get the
				// same ordering.
				_activityRequests.Sort();
			}
		}

		public ActivityDefinition AlternateActivity { get; set; }

		/// <summary>
		/// True if the alternate activity has been scheduled
		/// </summary>
		public bool ScheduledAlternateActivity
		{
			get
			{
				return Camper.ScheduledBlocks.Any(sb => sb.ActivityDefinition == AlternateActivity);
			}
		}

		/// <summary>
		/// Getter for the list of activities that have not been scheduled on this request.
		/// Adjusts for an activity having been replaced by the alternate.
		/// </summary>
		public List<ActivityRequest> UnscheduledActivities
		{
			get
			{
				List<ActivityRequest> unscheduledActivities = ActivityRequests
					.Where(ar => !Camper.ScheduledBlocks.Any(sb => sb.ActivityDefinition == ar.Activity))
					.ToList();
				if (ScheduledAlternateActivity)
				{
					// If the alternate got placed, it should have replaced the last request.
					unscheduledActivities.Remove(unscheduledActivities.Last());
				}
				return unscheduledActivities;

			}
		}

		/// <summary>
		/// Read the CamperRequests from a CSV file. The activities must be found
		/// in the activity list to be valid.
		/// </summary>
		/// <param name="csvFilePath"></param>
		/// <param name="activityDefinitions">List of valid activity definitions</param>
		/// <param name="logger">Logger</param>
		/// <returns></returns>
		public static List<CamperRequests> ReadCamperRequests(String csvFilePath, 
			List<ActivityDefinition> activityDefinitions, ILogger logger)
		{
			try
			{
				using (var inFileStream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read))
				{
					return ReadCamperRequests(inFileStream, activityDefinitions);
				}
			}
			catch (FileNotFoundException e)
			{
				logger.LogError($"Could not open Camper CSV file {e.FileName}");
			}
			catch (CsvHelperException e)
			{
				KeyNotFoundException keyNotFoundException = e.InnerException as KeyNotFoundException;
				if (keyNotFoundException != null)
				{
					logger.LogError($"Error parsing input file {csvFilePath}: {keyNotFoundException.Message}");
				}
				else
				{
					logger.LogError($"Exception parsing input file {csvFilePath}: {e.Message}");
				}
			}
			catch (Exception e)
			{
				logger.LogError($"Exception parsing input file {csvFilePath}: {e.Message}");
			}

			return null;
		}

		/// <summary>
		/// Read the camper requests from a CSV stream. The activities must be found
		/// in the activity list to be valid.
		/// </summary>
		/// <param name="csvStream">Camper requests on a stream</param>
		/// <param name="activityDefinitions">List of valid activity definitions</param>
		/// <returns></returns>
		public static List<CamperRequests> ReadCamperRequests(Stream csvStream, List<ActivityDefinition> activityDefinitions)
		{
			var streamReader = new StreamReader(csvStream);
			var csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				HeaderValidated = null
			});
			csvReader.Configuration.RegisterClassMap(new CamperRequestsMap(activityDefinitions));
			var camperRequestsEnumerator = csvReader.GetRecords<CamperRequests>();
			List<CamperRequests> camperRequestsList = new List<CamperRequests>(camperRequestsEnumerator);
			return camperRequestsList;
		}

		/// <summary>
		/// Compare camper requests by the difficulty to satisfy.
		/// More difficult requests go first.
		/// </summary>
		/// <param name="other">Other camper request</param>
		/// <returns>0 if equal, gt 0 if this before other, lt 0 if this after other</returns>
		public int CompareTo(CamperRequests other)
		{
			int compareValue = 0;
			for (int i = 0; i < Math.Min(_activityRequests.Count, other._activityRequests.Count); i++)
			{
				// Does not include name in the activity definition compare so equivalent by
				// complexity activities are not ranked by name (can keep checking other activities)
				compareValue = _activityRequests[i].CompareTo(other._activityRequests[i]);
				if (compareValue != 0) return compareValue;
			}
			// Lists match up to the min. If one list is longer, it is harder to satisfy
			// thus reverse the sort order so that longer goes first.
			compareValue = _activityRequests.Count.CompareTo(other._activityRequests.Count) * -1;
			if (compareValue != 0) return compareValue;

			// Check the alternate. No alternate is harder to resolve than with an alternate
			compareValue = (AlternateActivity == null)
				? other.AlternateActivity == null ? 0 : 1
				: other.AlternateActivity == null ? -1 : ActivityDefinition.CompareIncludingName(AlternateActivity, other.AlternateActivity);

			return compareValue;
		}

		/// <summary>
		/// From the list of camper requests
		/// </summary>
		/// <param name="camperRequestsList">List of camper requests</param>
		/// <returns>Collection of camper groups. Each group contains the campers
		/// that have common camper mate requests</returns>
		public static List<HashSet<Camper>> GenerateCamperMateGroups(List<CamperRequests> camperRequestsList)
		{
			List<HashSet<Camper>> camperMateGroups = new List<HashSet<Camper>>();
			foreach (CamperRequests camperRequest in camperRequestsList?.Where(cr => cr.CabinMate != null))
			{
				HashSet<Camper> camperGroup = camperMateGroups.FirstOrDefault(grp => grp.Contains(camperRequest.Camper));
				HashSet<Camper> mateGroup = camperMateGroups.FirstOrDefault(grp => grp.Contains(camperRequest.CabinMate));
				if (camperGroup == null)
				{
					if (mateGroup == null)
					{
						// Neither is in a group. Make a new group for them
						// and add the mate
						mateGroup = new HashSet<Camper>(new Camper.CamperEqualityCompare());
						camperMateGroups.Add(mateGroup);
						mateGroup.Add(camperRequest.CabinMate);
					}
					// Camperis not in a group so add to the mate group
					mateGroup.Add(camperRequest.Camper);
				}
				else
				{
					// Camper is in a group
					if (mateGroup == null)
					{
						// Mate is not in a group so add the mate
						camperGroup.Add(camperRequest.CabinMate);
					}
					else if (!ReferenceEquals(camperGroup, mateGroup))
					{
						// If the groups are not the same group, combine them
						// and remove one from the list.
						camperGroup.UnionWith(mateGroup);
						camperMateGroups.Remove(mateGroup);
					}
				}
			}
			return camperMateGroups;
		}

		/// <summary>
		/// From the list of camper requests, generate the activity preferences
		/// </summary>
		/// <param name="camperRequestsList">List of camper requests</param>
		/// <returns>Dictionary of camper to ordered list of activities</returns>
		public static Dictionary<Camper, List<ActivityDefinition>> GenerateCamperActivityPreferences(
			List<CamperRequests> camperRequestsList)
		{
			return camperRequestsList.ToDictionary(cr => cr.Camper, cr => 
				cr.ActivityRequests.OrderBy(ar => ar.Rank)
					.Select(ar => ar.Activity).Append(cr.AlternateActivity)
					.Where(a => a != null).ToList(),
					new Camper.CamperEqualityCompare());
		}
	}
}
