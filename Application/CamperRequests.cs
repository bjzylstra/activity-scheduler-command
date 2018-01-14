using CsvHelper;
using CsvHelper.Configuration;
using Ookii.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActivityScheduler
{
    /// <summary>
    /// Represents the activity and cabin placement requests
    /// for a camper. LastName/FirstName is the 'KEY' for the record.
    /// </summary>
    public class CamperRequests
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
        public List<ActivityDefinition> ActivityRequests { get; set; }
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
                using (LineWrappingTextWriter writer = LineWrappingTextWriter.ForConsoleError())
                {
                    writer.WriteLine("Could not open Camper CSV file {0}", e.FileName);
                    writer.WriteLine();
                }
            }
            catch (CsvHelperException e)
            {
                using (LineWrappingTextWriter writer = LineWrappingTextWriter.ForConsoleError())
                {
                    KeyNotFoundException keyNotFoundException = e.InnerException as KeyNotFoundException;
                    if (keyNotFoundException != null)
                    {
                        writer.WriteLine("Error parsing input file {0}: {1}", csvFilePath,
                            keyNotFoundException.Message);
                    }
                    else
                    {
                        writer.WriteLine("Exception parsing input file {0}: {1}", csvFilePath,
                            e.Message);
                    }
                    writer.WriteLine();
                }
            }
            catch (Exception e)
            {
                using (LineWrappingTextWriter writer = LineWrappingTextWriter.ForConsoleError())
                {
                    writer.WriteLine("Exception parsing input file {0}: {1}", csvFilePath,
                        e.Message);
                    writer.WriteLine();
                }
            }

            return null;
        }
    }
}
