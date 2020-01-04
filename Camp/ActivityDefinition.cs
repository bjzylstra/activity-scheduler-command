using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Camp
{
    /// <summary>
    /// Defines an activity and its capacity
    /// Expect these to be read from an XML file.
    /// </summary>
    public class ActivityDefinition : IComparable<ActivityDefinition>
    {

        public String Name { get; set; }
        public int MinimumCapacity { get; set; }
        public int MaximumCapacity { get; set; }
        public int OptimalCapacity { get; set; }

        private List<IActivityBlock> _scheduledBlocks = new List<IActivityBlock>();
        /// <summary>
        /// Return a copy of the scheduled blocks for reading only.
        /// </summary>
        [XmlIgnore]
        public List<IActivityBlock> ScheduledBlocks { get { return new List<IActivityBlock>(_scheduledBlocks); } }

        private Boolean[] _isAvailableBlocks;

        /// <summary>
        /// Read the activity schedule from a CSV file.
        /// </summary>
        /// <param name="inputFilePath">Path to the activity schedule file</param>
        /// <returns>List of activity definitions describing the schedule</returns>
        public static List<ActivityDefinition> ReadScheduleFromCsvFile(String inputFilePath)
        {
            List<ActivityDefinition> activityDefinitions = new List<ActivityDefinition>();
            try
            {
                var inFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
                var streamReader = new StreamReader(inFileStream);
                var csvReader = new CsvReader(streamReader, new Configuration
                {
                    HasHeaderRecord = true,
                    HeaderValidated = null
                });

                // Skip over the header line
                csvReader.Read();

                ActivityDefinition currentActivityDefinition = null;
                Dictionary<string, Camper> campersByName = new Dictionary<string, Camper>();

                while (csvReader.Read())
                {
                    string activityName = csvReader.GetField(0);
                    if (!string.IsNullOrWhiteSpace(activityName))
                    {
                        currentActivityDefinition = new ActivityDefinition { Name = activityName };
                        activityDefinitions.Add(currentActivityDefinition);
                    }
                    if (currentActivityDefinition == null)
                    {
                        throw new Exception("Malformed file: no activity at start");
                    }
                    int timeSlot = csvReader.GetField<int>(1);
                    currentActivityDefinition._scheduledBlocks.Add(new ActivityBlock
                    {
                        ActivityDefinition = currentActivityDefinition,
                        TimeSlot = timeSlot
                    });
                    // Next is the collection of camper names.
                    int camperIndex = 0;
                    object camperFullNameObject;
                    while (csvReader.TryGetField(typeof(string), 2 + camperIndex, out camperFullNameObject))
                    {
                        // Find or create the camper
                        string camperFullName = (string)camperFullNameObject;
                        Camper camper;
                        if (!campersByName.TryGetValue(camperFullName, out camper))
                        {
                            string[] camperNameParts = camperFullName.Split(',');
                            camper = new Camper
                            {
                                FirstName = camperNameParts[1].Trim('"'),
                                LastName = camperNameParts[0].Trim('"')
                            };
                            campersByName.Add(camperFullName, camper);
                        }
                        // Add camper to activity
                        currentActivityDefinition.ScheduledBlocks[timeSlot].AssignedCampers.Add(camper);

                        // Add activity to camper
                        camper.ScheduledBlocks.Add(currentActivityDefinition.ScheduledBlocks[timeSlot]);

                        camperIndex++;
                    }
                }
                return activityDefinitions;
            }
            catch (FileNotFoundException e)
            {
                Console.Error.WriteLine("Could not open Activity Schedule CSV file {0}", e.FileName);
            }
            catch (CsvHelperException e)
            {
                KeyNotFoundException keyNotFoundException = e.InnerException as KeyNotFoundException;
                if (keyNotFoundException != null)
                {
                    Console.Error.WriteLine("Error parsing input file {0}: {1}", inputFilePath,
                        keyNotFoundException.Message);
                }
                else
                {
                    Console.Error.WriteLine("Exception parsing input file {0}: {1}", inputFilePath,
                        e.Message);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception parsing input file {0}: {1}", inputFilePath,
                    e.Message);
            }
            return null;
        }

        /// <summary>
        /// Write the activity schedules to a CSV file.
        /// </summary>
        /// <param name="activityDefinitions">List of activity defintions</param>
        /// <param name="outputFilePath">Path to the CSV file</param>
        public static void WriteScheduleToCsvFile(List<ActivityDefinition> activityDefinitions, String outputFilePath)
        {
            try
            {
                using (var outTextWriter = new StreamWriter(outputFilePath))
                {
                    using (var csvWriter = new CsvHelper.CsvWriter(outTextWriter))
                    {
                        // Write the header
                        csvWriter.WriteField("Activity");
                        csvWriter.WriteField("Block");
                        csvWriter.NextRecord();

                        // Write the activities
                        foreach (var activity in activityDefinitions)
                        {
                            for (int i = 0; i < ActivityBlock.MaximumTimeSlots; i++)
                            {
                                // Name only on the first row.
                                csvWriter.WriteField((i == 0) ? activity.Name : " ");

                                // Block number.
                                csvWriter.WriteField(i);

                                // Find activity block in that time slot.
                                IActivityBlock activityBlock = activity.ScheduledBlocks
                                    .FirstOrDefault(sb => sb.TimeSlot == i);
                                if (activityBlock != null)
                                {
                                    // Campers in the block
                                    foreach (var camper in activityBlock.AssignedCampers)
                                    {
                                        csvWriter.WriteField($"\"{camper}\"");
                                    }
                                }
                                csvWriter.NextRecord();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception writing output file {0}: {1}", outputFilePath,
                    e.Message);
            }
        }

        /// <summary>
        /// Read the activity definition XML file to generate a list of activity definitions
        /// </summary>
        /// <param name="xmlPath">Path to the activity definition XML file</param>
        /// <returns>List of activity defintions found in the file. Returns null if unsuccessful</returns>
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
                Console.Error.WriteLine("Could not open Activity Definitions file {0}", e.FileName);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception parsing input file {0}: {1}", xmlPath,
                        e.Message);
            }
            return null;
        }

        /// <summary>
        /// Read the activity definition XML from a string to generate a list of activity definitions
        /// </summary>
        /// <param name="buffer">String containing the activity definition XML file</param>
        /// <returns>List of activity definitions found. Returns null if unsuccessful</returns>
        public static List<ActivityDefinition> ReadActivityDefinitionsFromString(String contents)
        {
            try
            {
                StringReader reader = new StringReader(contents);
                XmlSerializer serializer = new XmlSerializer(typeof(List<ActivityDefinition>));
                return (List<ActivityDefinition>)serializer.Deserialize(reader);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception parsing ActivityDefinitions: {0}", 
                        e.Message);
            }
            return null;
        }

        /// <summary>
        /// Write the activity definition list as XML to a string.
        /// </summary>
        /// <param name="activityDefinitions">List of activity definitions</param>
        /// <returns>XML representation of the activity definitions</returns>
        public static string WriteActivityDefinitionsToString(List<ActivityDefinition> activityDefinitions)
        {
            try
            {
                var memoryStream = new MemoryStream();
                XmlSerializer serializer = new XmlSerializer(typeof(List<ActivityDefinition>));
                serializer.Serialize(memoryStream, activityDefinitions);
                memoryStream.Position = 0;
                using (StreamReader reader = new StreamReader(memoryStream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception parsing ActivityDefinitions: {0}",
                        e.Message);
            }
            return null;
        }

        /// <summary>
        /// Default constructor required for serialization.
        /// </summary>
        public ActivityDefinition()
        {
            _isAvailableBlocks = new Boolean[ActivityBlock.MaximumTimeSlots];
            for (int i = 0; i < ActivityBlock.MaximumTimeSlots; i++)
            {
                _isAvailableBlocks[i] = true;
            }
        }

        /// <summary>
        /// Constructor for testing that pre-allocates slots
        /// </summary>
        /// <param name="usedSlots">Slot numbers to pre-allocate</param>
        public ActivityDefinition(int[] usedSlots) : this()
        {
            foreach (var usedSlot in usedSlots)
            {
                if (usedSlot >= 0 && usedSlot < ActivityBlock.MaximumTimeSlots)
                {
                    _isAvailableBlocks[usedSlot] = false;
                }
            }
        }

        /// <summary>
        /// Prebuild the blocks
        /// </summary>
        public void PreloadBlocks()
        {
            for (int i = 0; i < ActivityBlock.MaximumTimeSlots; i++)
            {
                TryCreateBlock(i);
            }
        }

        /// <summary>
        /// Try to assign the camper to a new activity block for the activity.
        /// </summary>
        /// <param name="camper">Camper to assign</param>
        /// <param name="activityDefinition">Activity to assign to</param>
        /// <returns>true if the camper was assigned</returns>
        public bool TryAssignCamperToNewActivityBlock(Camper camper)
        {
            // Go through the campers unused slots and try to create to create
            // an activity block.
            Boolean didAssignCamper = false;
            for (int slotNumber = 0; slotNumber < ActivityBlock.MaximumTimeSlots && !didAssignCamper; slotNumber++)
            {
                if (camper.IsAvailableInTimeSlot(slotNumber))
                {
                    var newActivityBlock = TryCreateBlock(slotNumber);
                    didAssignCamper = camper.TryAssignBlock(newActivityBlock);
                }
            }

            return didAssignCamper;
        }

        /// <summary>
        /// Try to assign a camper to existing blocks of the activity.
        /// </summary>
        /// <param name="camper">Camper to assign</param>
        /// <param name="limitByOptimal">Do not exceed optimal capacity</param>
        /// <returns>true if the camper was assigned</returns>
        public bool TryAssignCamperToExistingActivityBlock(Camper camper, Boolean limitByOptimal)
        {
            bool didAssign = false;

            int capacity = (limitByOptimal) ? OptimalCapacity : MaximumCapacity;
            var firstFitBlock = _scheduledBlocks.Find(
                b => camper.IsAvailableInTimeSlot(b.TimeSlot)
                && b.AssignedCampers.Count < capacity);

            if (firstFitBlock != null)
            {
                // If this assign fails something is wrong because it was checked in the find.
                didAssign = camper.TryAssignBlock(firstFitBlock);
            }

            return didAssign;
        }

        /// <summary>
        /// Try to create a block in the specified slot.
        /// </summary>
        /// <param name="slotNumber">Time slot to create the block in</param>
        /// <returns>Activity block if it could be created. Otherwise null.</returns>
        private IActivityBlock TryCreateBlock(int slotNumber)
        {
            IActivityBlock createdBlock = null;
            if (_isAvailableBlocks[slotNumber])
            {
                createdBlock = new ActivityBlock()
                {
                    TimeSlot = slotNumber,
                    ActivityDefinition = this
                };
                _scheduledBlocks.Add(createdBlock);
                _isAvailableBlocks[slotNumber] = false;
            }
            return createdBlock;
        }

        /// <summary>
        /// Default sort is by capacities. For difficulty of placement only.
        /// </summary>
        /// <param name="otherActivityDefinition"></param>
        /// <returns>0 if equal, gt 0 if this before other, lt 0 if this after other</returns>
        public int CompareTo(ActivityDefinition otherActivityDefinition)
        {
            int compareValue = 0;
//            compareValue = OptimalCapacity.CompareTo(otherActivityDefinition.OptimalCapacity);
            if (compareValue != 0) return compareValue;
            compareValue = MaximumCapacity.CompareTo(otherActivityDefinition.MaximumCapacity);
            return compareValue;
        }

        /// <summary>
        /// Sort by capacities but also name if capacities match to get consistent list.
        /// </summary>
        /// <param name="x">An activity definition</param>
        /// <param name="y">Another activity definition</param>
        /// <returns>0 if equal, gt 0 if this before other, lt 0 if this after other</returns>
        public static int CompareIncludingName(ActivityDefinition x, ActivityDefinition y)
        {
            int compareValue = x.CompareTo(y);
            if (compareValue != 0) return compareValue;
            compareValue = x.Name.CompareTo(y.Name);
            return compareValue;
        }

    }
}
