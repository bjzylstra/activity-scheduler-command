using Camp;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ScheduleToSpreadsheet
{
    internal class CamperSheet : WorkSheet
    {
        private List<Camper> _camperSchedule;

		/// <summary>
		/// Construct the camper sheet generator from the activity block definitions
		/// </summary>
		/// <param name="activitySchedule">List of activity definitions with schedule information</param>
		/// <param name="excelWorkbook">Excel workbook to add worksheets to</param>
		public CamperSheet(List<ActivityDefinition> activitySchedule, ExcelWorkbook excelWorkbook)
			: base("Campers", excelWorkbook)
        {
            _camperSchedule = activitySchedule.SelectMany(activity => 
                activity.ScheduledBlocks
                .SelectMany(block => block.AssignedCampers)
                ).Distinct().ToList();
            Comparer<Camper> camperComparer = Comparer<Camper>
                .Create((c1, c2) => c1.FullName.CompareTo(c2.FullName));
            _camperSchedule.Sort(camperComparer);
		}

		/// <summary>
		/// Populate the camper sheet in the work book
		/// </summary>
		internal void BuildWorksheet()
        {
            int row = _worksheet.Cells.Start.Row;
            int column = _worksheet.Cells.Start.Column;
			// Find the expected number of scheduled blocks by polling all campers
			int maxBlockNumber = _camperSchedule.Aggregate(0, (maxBlocks, c) 
				=> Math.Max(maxBlocks, c.ScheduledBlocks.Count(sb => sb != null)));

            // Add the headers
            _worksheet.SetValue(row, column, "Camper");
            _worksheet.Column(column).Width = 30;
            column++;
            _worksheet.View.FreezePanes(row+1, column);
            for (int blockNumber = 0; blockNumber < maxBlockNumber; blockNumber++)
            {
                _worksheet.Column(column).Width = 20;
                _worksheet.SetValue(row, column, $"Block {blockNumber+1}");
                column++;
            }

            row++;
            foreach (var camper in _camperSchedule)
            {
                column = _worksheet.Cells.Start.Column;
                _worksheet.SetValue(row, column, camper.FullName);
				int numberOfScheduledBlocks = camper.ScheduledBlocks.Count(b => b != null);
				if (numberOfScheduledBlocks < maxBlockNumber)
				{
					_worksheet.Cells[row, column].Style.Fill.PatternType = ExcelFillStyle.Solid;
					_worksheet.Cells[row, column].Style.Fill.BackgroundColor
						.SetColor(Color.Red);
				}
				column++;
                for (int blockNumber = 0; blockNumber < maxBlockNumber; blockNumber++)
                {
                    var activityBlock = camper.ScheduledBlocks.FirstOrDefault(block => block.TimeSlot == blockNumber);
                    if (activityBlock != null)
                    {
                        _worksheet.SetValue(row, column, activityBlock.ActivityDefinition.Name);
                    }
                    _worksheet.Cells[row, column].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    _worksheet.Cells[row, column].Style.Fill.BackgroundColor
                        .SetColor(activityBlock == null ? Color.Red : Color.LawnGreen);
                    column++;
                }
                row++;
            }
        }
	}
}
