using CsvHelper;
using CsvHelper.Configuration;
using Ookii.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;

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
            public CamperRequestsMap()
            {
                int index = 0;
                int camperIndex = index;
                Map(m => m.Camper).ConvertUsing(row => new Camper { LastName = row.GetField(camperIndex),
                    FirstName = row.GetField(camperIndex + 1) });
                index += 2;
                Map(m => m.CabinMate).Index(index++);
                int activityIndex = index;
                Map(m => m.ActivityRequests).ConvertUsing(row => new List<string> {
                    row.GetField(activityIndex),
                    row.GetField(activityIndex + 1),
                    row.GetField(activityIndex + 2),
                    row.GetField(activityIndex + 3)
                });
                index += 4;
                Map(m => m.AlternateActivity).Index(index++);
            }
        }

        public Camper Camper { get; set; }
        public String CabinMate { get; set; }
        public List<String> ActivityRequests { get; set; }
        public String AlternateActivity { get; set; }

        public static List<CamperRequests> ReadCamperRequests(String csvFilePath)
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
                csvReader.Configuration.RegisterClassMap(new CamperRequestsMap());
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
