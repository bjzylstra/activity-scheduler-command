using Camp;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ScheduleToSpreadsheet
{
    internal class CamperSheet
    {
        private List<Camper> _camperSchedule;

        /// <summary>
        /// Construct the camper sheet generator from the activity block definitions
        /// </summary>
        /// <param name="activitySchedule">List of activity definitions with schedule information</param>
        public CamperSheet(List<ActivityDefinition> activitySchedule)
        {
            _camperSchedule = activitySchedule.SelectMany(activity => 
                activity.ScheduledBlocks
                .SelectMany(block => block.AssignedCampers)
                ).Distinct().ToList();
            Comparer<Camper> camperComparer = Comparer<Camper>
                .Create((c1, c2) => c1.ToString().CompareTo(c2.ToString()));
            _camperSchedule.Sort(camperComparer);
        }

        /// <summary>
        /// Add and populate the camper sheet in the work book
        /// </summary>
        /// <param name="excelWorkbook">Work book</param>
        internal void AddToWorkbook(ExcelWorkbook excelWorkbook)
        {
            ExcelWorksheet camperWorksheet = excelWorkbook.Worksheets.Add("Campers");

            int row = camperWorksheet.Cells.Start.Row;
            int column = camperWorksheet.Cells.Start.Column;
			// Find the expected number of scheduled blocks by polling all campers
			int maxBlockNumber = _camperSchedule.Aggregate(0, (maxBlocks, c) 
				=> Math.Max(maxBlocks, c.ScheduledBlocks.Count(sb => sb != null)));

            // Add the headers
            camperWorksheet.SetValue(row, column, "Camper");
            camperWorksheet.Column(column).Width = 30;
            column++;
            camperWorksheet.View.FreezePanes(row+1, column);
            for (int blockNumber = 0; blockNumber < maxBlockNumber; blockNumber++)
            {
                camperWorksheet.Column(column).Width = 20;
                camperWorksheet.SetValue(row, column, $"Block {blockNumber+1}");
                column++;
            }

            row++;
            foreach (var camper in _camperSchedule)
            {
                column = camperWorksheet.Cells.Start.Column;
                camperWorksheet.SetValue(row, column, camper.ToString());
				int numberOfScheduledBlocks = camper.ScheduledBlocks.Count(b => b != null);
				if (numberOfScheduledBlocks < maxBlockNumber)
				{
					camperWorksheet.Cells[row, column].Style.Fill.PatternType = ExcelFillStyle.Solid;
					camperWorksheet.Cells[row, column].Style.Fill.BackgroundColor
						.SetColor(Color.Red);
				}
				column++;
                for (int blockNumber = 0; blockNumber < maxBlockNumber; blockNumber++)
                {
                    var activityBlock = camper.ScheduledBlocks.FirstOrDefault(block => block.TimeSlot == blockNumber);
                    if (activityBlock != null)
                    {
                        camperWorksheet.SetValue(row, column, activityBlock.ActivityDefinition.Name);
                    }
                    camperWorksheet.Cells[row, column].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    camperWorksheet.Cells[row, column].Style.Fill.BackgroundColor
                        .SetColor(activityBlock == null ? Color.Red : Color.LawnGreen);
                    column++;
                }
                row++;
            }
        }
    }
}
