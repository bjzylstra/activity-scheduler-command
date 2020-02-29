using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Camp
{
    /// <summary>
    /// Represents a camper. 
    /// </summary>
    public class Camper
    {
        public String LastName { get; set; }
        public String FirstName { get; set; }

        private Boolean[] _isAvailableBlocks;
        private List<IActivityBlock> _scheduledBlocks = new List<IActivityBlock>();
        public List<IActivityBlock> ScheduledBlocks { get { return _scheduledBlocks; } }

        /// <summary>
        /// Write the camper schedules to a CSV file.
        /// </summary>
        /// <param name="camperList">List of campers</param>
        /// <param name="outputFilePath">Path to the CSV file</param>
        public static void WriteScheduleToCsvFile(IEnumerable<Camper> camperList, string outputFilePath)
        {
            try
            {
                List<Camper> campers = camperList.ToList();
                campers.Sort((c1, c2) =>
                {
                    int compareValue = c1.LastName.CompareTo(c2.LastName);
                    if (compareValue == 0)
                    {
                        compareValue = c1.FirstName.CompareTo(c2.FirstName);
                    }
                    return compareValue;
                });
                using (var outTextWriter = new StreamWriter(outputFilePath))
                {
                    using (var csvWriter = new CsvHelper.CsvWriter(outTextWriter, 
                        CultureInfo.InvariantCulture))
                    {
                        // Write the header
                        csvWriter.WriteField("Camper");
                        for (int i = 0; i < ActivityBlock.MaximumTimeSlots; i++)
                        {
                            csvWriter.WriteField($"Block {i}");
                        }
                        csvWriter.NextRecord();

                        // Write the activities
                        foreach (var camper in campers)
                        {
                            csvWriter.WriteField($"\"{camper}\"");

                            for (int i = 0; i < ActivityBlock.MaximumTimeSlots; i++)
                            {
                                // Find activity block in that time slot.
                                IActivityBlock activityBlock = camper.ScheduledBlocks
                                    .FirstOrDefault(sb => sb.TimeSlot == i);
                                csvWriter.WriteField(activityBlock != null ? activityBlock.ActivityDefinition.Name : "Free");
                            }
                            csvWriter.NextRecord();
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
        /// Default constructor. Set up the available blocks.
        /// </summary>
        public Camper()
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
        public Camper(int[] usedSlots) : this()
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
        /// Is the camper available in the slot
        /// </summary>
        /// <param name="slotNumber">Slot to check</param>
        /// <returns>true if the camper is available in the slot</returns>
        public Boolean IsAvailableInTimeSlot(int slotNumber)
        {
            return _isAvailableBlocks[slotNumber];
        }

        /// <summary>
        /// Try to assign a block to a camper
        /// </summary>
        /// <param name="block">Block to assign</param>
        /// <returns>true if the block was assigned</returns>
        public Boolean TryAssignBlock(IActivityBlock block)
        {
            Boolean mayAssign = block != null
                && _isAvailableBlocks[block.TimeSlot];
            if (mayAssign)
            {
                // Try to add to the block
                mayAssign = block.TryAddCamper(this);
            }
            if (mayAssign)
            {
                // Added to the block - now add the block to the camper.
                _scheduledBlocks.Add(block);
                _isAvailableBlocks[block.TimeSlot] = false;
            }

            return mayAssign;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}", LastName, FirstName);
        }
    }
}
