using Ookii.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ActivityScheduler
{
    /// <summary>
    /// Defines an activity and its capacity
    /// Expect these to be read from an XML file.
    /// </summary>
    public class ActivityDefinition
    {
        public String Name { get; set; }
        public int MinimumCapacity { get; set; }
        public int MaximumCapacity { get; set; }
        public int OptimalCapacity { get; set; }

        public static List<ActivityDefinition> ReadActivityDefinitions(String xmlPath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ActivityDefinition>));
                using (FileStream fileStream = new FileStream(xmlPath, FileMode.Open))
                {
                    return (List<ActivityDefinition>)serializer.Deserialize(fileStream);
                }
            }
            catch (FileNotFoundException e)
            {
                using (LineWrappingTextWriter writer = LineWrappingTextWriter.ForConsoleError())
                {
                    writer.WriteLine("Could not open Activity Definitions file {0}", e.FileName);
                    writer.WriteLine();
                }
            }
            catch (Exception e)
            {
                using (LineWrappingTextWriter writer = LineWrappingTextWriter.ForConsoleError())
                {
                    writer.WriteLine("Exception parsing input file {0}: {1}", xmlPath,
                        e.Message);
                    writer.WriteLine();
                }
            }
            return null;
        }
    }
}
