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
                Map(m => m.LastName).Index(index++);
                Map(m => m.FirstName).Index(index++);
                Map(m => m.CabinMate).Index(index++);
                Map(m => m.Activity1).Index(index++);
                Map(m => m.Activity2).Index(index++);
                Map(m => m.Activity3).Index(index++);
                Map(m => m.Activity4).Index(index++);
                Map(m => m.AlternateActivity).Index(index++);
            }
        }

        public static int NumberOfActivities = 4;
        public String LastName { get; set; }
        public String FirstName { get; set; }
        public String CabinMate { get; set; }
        public String Activity1 { get; set; }
        public String Activity2 { get; set; }
        public String Activity3 { get; set; }
        public String Activity4 { get; set; }
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
