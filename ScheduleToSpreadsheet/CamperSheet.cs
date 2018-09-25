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

        internal void AddToWorkbook(ExcelWorkbook excelWorkbook)
        {
            ExcelWorksheet camperWorksheet = excelWorkbook.Worksheets.Add("Campers");

            int row = camperWorksheet.Cells.Start.Row;
            int column = camperWorksheet.Cells.Start.Column;
            int maxBlockNumber = 0;
            _camperSchedule.ForEach(camper => 
                camper.ScheduledBlocks.ForEach(block => 
                    maxBlockNumber = Math.Max(maxBlockNumber, block.TimeSlot)));

            // Add the headers
            camperWorksheet.SetValue(row, column, "Camper");
            camperWorksheet.Column(column).Width = 30;
            column++;
            camperWorksheet.View.FreezePanes(row+1, column);
            for (int blockNumber = 0; blockNumber < maxBlockNumber; blockNumber++)
            {
                camperWorksheet.Column(column).Width = 20;
                camperWorksheet.SetValue(row, column, $"Block {blockNumber}");
                column++;
            }

            row++;
            foreach (var camper in _camperSchedule)
            {
                column = camperWorksheet.Cells.Start.Column;
                camperWorksheet.SetValue(row, column, camper.ToString());
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
