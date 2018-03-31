using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActivityScheduler
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
                Map(m => m.CabinMate).Index(index++);
                int activityIndex = index;
                Map(m => m.ActivityRequests).ConvertUsing(row => {
                    return new List<ActivityDefinition> {
                        GetActivityForName(row.GetField(activityIndex)),
                        GetActivityForName(row.GetField(activityIndex+1)),
                        GetActivityForName(row.GetField(activityIndex+2)),
                        GetActivityForName(row.GetField(activityIndex+3))
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
        public String CabinMate { get; set; }
        private List<ActivityDefinition> _activityDefinitions;
        public List<ActivityDefinition> ActivityRequests
        {
            get { return _activityDefinitions; }
            set
            {
                // Leave it sorted.
                _activityDefinitions = (value != null)
                    ? _activityDefinitions = new List<ActivityDefinition>(value)
                    : _activityDefinitions = new List<ActivityDefinition>();
                // Sort including name so that campers with the same requests get the
                // same ordering.
                _activityDefinitions.Sort(ActivityDefinition.CompareIncludingName);
            }
        }
        public ActivityDefinition AlternateActivity { get; set; }

        /// <summary>
        /// Read the CamperRequests from a CSV file. The activities must be found
        /// in the activity list to be valid.
        /// </summary>
        /// <param name="csvFilePath"></param>
        /// <param name="activityDefinitions">List of valid activity definitions</param>
        /// <returns></returns>
        public static List<CamperRequests> ReadCamperRequests(String csvFilePath, 
            List<ActivityDefinition> activityDefinitions)
        {
            try
            {
                var inFileStream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read);
                var streamReader = new StreamReader(inFileStream);
                var csvReader = new CsvReader(streamReader, new Configuration
                {
                    HasHeaderRecord = true,
                    HeaderValidated = null
                });
                csvReader.Configuration.RegisterClassMap(new CamperRequestsMap(activityDefinitions));
                var camperRequestsEnumerator = csvReader.GetRecords<CamperRequests>();
                List<CamperRequests> camperRequestsList = new List<CamperRequests>(camperRequestsEnumerator);
                return camperRequestsList;
            }
            catch (FileNotFoundException e)
            {
                Console.Error.WriteLine("Could not open Camper CSV file {0}", e.FileName);
            }
            catch (CsvHelperException e)
            {
                KeyNotFoundException keyNotFoundException = e.InnerException as KeyNotFoundException;
                if (keyNotFoundException != null)
                {
                    Console.Error.WriteLine("Error parsing input file {0}: {1}", csvFilePath,
                        keyNotFoundException.Message);
                }
                else
                {
                    Console.Error.WriteLine("Exception parsing input file {0}: {1}", csvFilePath,
                        e.Message);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception parsing input file {0}: {1}", csvFilePath,
                    e.Message);
            }

            return null;
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
            for (int i = 0; i < Math.Min(_activityDefinitions.Count, other._activityDefinitions.Count); i++)
            {
                // Does not include name in the activity definition compare so equivalent by
                // complexity activities are not ranked by name (can keep checking other activities)
                compareValue = _activityDefinitions[i].CompareTo(other._activityDefinitions[i]);
                if (compareValue != 0) return compareValue;
            }
            // Lists match up to the min. If one list is longer, it is harder to satisfy
            // thus reverse the sort order so that longer goes first.
            compareValue = _activityDefinitions.Count.CompareTo(other._activityDefinitions.Count) * -1;
            if (compareValue != 0) return compareValue;

            // Check the alternate. No alternate is harder to resolve than with an alternate
            compareValue = (AlternateActivity == null)
                ? other.AlternateActivity == null ? 0 : 1
                : other.AlternateActivity == null ? -1 : ActivityDefinition.CompareIncludingName(AlternateActivity, other.AlternateActivity);

            return compareValue;
        }
    }
}
