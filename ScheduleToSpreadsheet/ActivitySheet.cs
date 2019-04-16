using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Camp;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using OfficeOpenXml.Style;
using OfficeOpenXml.VBA;

namespace ScheduleToSpreadsheet
{
    internal class ActivitySheet
    {
        private List<ActivityDefinition> _activitySchedule;
		private ExcelWorksheet _worksheet;

		/// <summary>
		/// Create the activity sheet definition from an activity schedule
		/// </summary>
		/// <param name="activitySchedule">Activity Definitions with schedule information</param>
		/// <param name="excelWorkbook">Work book</param>
		public ActivitySheet(List<ActivityDefinition> activitySchedule, ExcelWorkbook excelWorkbook)
        {
            _activitySchedule = activitySchedule;
			_worksheet = excelWorkbook.Worksheets.Add("Activities");
			if (_worksheet.Workbook.VbaProject == null)
			{
				_worksheet.Workbook.CreateVBAProject();
			}
		}

		/// <summary>
		/// Add and populate the activity sheet in the work book
		/// </summary>
		internal void BuildWorksheet()
        {
            int row = _worksheet.Cells.Start.Row;
            int column = _worksheet.Cells.Start.Column;

            // Add the headers
            _worksheet.SetValue(row, column++, "Activity");
            _worksheet.Column(column).Width = 5;
            _worksheet.SetValue(row, column++, "Block");
            _worksheet.Column(column).Width = 5;
            _worksheet.SetValue(row, column++, "# Campers");
            row++;
            _worksheet.View.FreezePanes(row, column);

            int topActivityRow;
            int bottomActivityRow;
            int countColumn = _worksheet.Cells.Start.Column + 2;
            foreach (var activity in _activitySchedule)
            {
                topActivityRow = row;
                bottomActivityRow = row;
                column = _worksheet.Cells.Start.Column;
                _worksheet.Column(column).Width = 20;
                _worksheet.SetValue(row, column, activity.Name);
                foreach (var activityBlock in activity.ScheduledBlocks)
                {
                    column = _worksheet.Cells.Start.Column + 1;
                    _worksheet.SetValue(row, column, activityBlock.TimeSlot);
                    column++;

                    // Get the count from the number of non-blank camper cells
                    ExcelRange activityCamperRange = _worksheet
                        .Cells[row, countColumn + 1, row, countColumn + 40];
                    _worksheet.Cells[row, column].Formula = $"COUNTA({activityCamperRange.Address})";
                    column++;

                    for (int camperIndex = 1; camperIndex <= activityBlock.AssignedCampers.Count; camperIndex++)
                    {
                        var camper = activityBlock.AssignedCampers[camperIndex - 1];
                        _worksheet.Column(column).Width = 30;
                        _worksheet.SetValue(row, column, camper.ToString());
                        column++;
                    }
                    bottomActivityRow = row;
                    row++;
                }

                AddConditionalColorToCount(_worksheet, topActivityRow, bottomActivityRow, countColumn, activity);

                AddConditionalColorToCampers(_worksheet, topActivityRow, bottomActivityRow, countColumn, activity);

            }
        }

		/// <summary>
		/// Add macros contained in embedded resources to the work sheet.
		/// </summary>
		internal void AddMacros()
		{
			ExcelVBAModule module = _worksheet.CodeModule;

			Assembly assembly = typeof(ActivitySheet).Assembly;
			List<string> resourceNames = new List<string>(assembly.GetManifestResourceNames());
			foreach (var resourceName in resourceNames.Where(r 
				=> r.StartsWith("ScheduleToSpreadsheet.Macros.Activities")))
			{
				Stream macroStream = typeof(ActivitySheet).Assembly.GetManifestResourceStream(
					resourceName);
				using (var reader = new StreamReader(macroStream))
				{
					module.Code = reader.ReadToEnd();
				}
			}

		}

		/// <summary>
		/// Add coloring based on the activity block subscription level (count) to the campers in the block
		/// </summary>
		/// <param name="_worksheet">Worksheet with activity block details</param>
		/// <param name="topActivityRow">Row number for the first (top) block of the activity</param>
		/// <param name="bottomActivityRow">Row number for the last (bottom) block of the activity</param>
		/// <param name="countColumn">Column number for the count fields</param>
		/// <param name="activity">Activity definition</param>
		private static void AddConditionalColorToCampers(ExcelWorksheet _worksheet, int topActivityRow, int bottomActivityRow, int countColumn, ActivityDefinition activity)
        {
            int maximumCapacity = Math.Min(activity.MaximumCapacity, 100);
            int optimalCapacity = Math.Min(activity.OptimalCapacity, maximumCapacity);
            ExcelRange activityCampersAboveMaximum = _worksheet
                .Cells[topActivityRow, countColumn + maximumCapacity + 1, bottomActivityRow, countColumn + maximumCapacity + 20];
            IExcelConditionalFormattingContainsText campersMoreThanMaximum =
               activityCampersAboveMaximum.ConditionalFormatting.AddContainsText();
            campersMoreThanMaximum.Style.Fill.PatternType = ExcelFillStyle.Solid;
            campersMoreThanMaximum.Style.Fill.BackgroundColor.Color = Color.Red;

            if (optimalCapacity < maximumCapacity)
            {
                ExcelRange activityCampersAboveOptimal = _worksheet
                    .Cells[topActivityRow, countColumn + optimalCapacity + 1, bottomActivityRow, countColumn + maximumCapacity];
                IExcelConditionalFormattingContainsText campersMoreThanOptimal =
                   activityCampersAboveOptimal.ConditionalFormatting.AddContainsText();
                campersMoreThanOptimal.Style.Fill.PatternType = ExcelFillStyle.Solid;
                campersMoreThanOptimal.Style.Fill.BackgroundColor.Color = Color.Yellow;
            }

            if (activity.MinimumCapacity > 0)
            {
                ExcelRange activityCampersBelowMinimum = _worksheet
                    .Cells[topActivityRow, countColumn + 1, bottomActivityRow, countColumn + activity.MinimumCapacity];
                IExcelConditionalFormattingNotContainsText emptyBelowMinimum =
                   activityCampersBelowMinimum.ConditionalFormatting.AddNotContainsText();
                emptyBelowMinimum.Style.Fill.PatternType = ExcelFillStyle.Solid;
                emptyBelowMinimum.Style.Fill.BackgroundColor.Color = Color.CornflowerBlue;

            }
        }

        /// <summary>
        /// Color the counts for each block of an activity
        /// </summary>
        /// <param name="_worksheet">Worksheet with activity block details</param>
        /// <param name="topActivityRow">Row number for the first (top) block of the activity</param>
        /// <param name="bottomActivityRow">Row number for the last (bottom) block of the activity</param>
        /// <param name="countColumn">Column number for the count fields</param>
        /// <param name="activity">Activity definition</param>
        private static void AddConditionalColorToCount(ExcelWorksheet _worksheet, int topActivityRow, int bottomActivityRow, int countColumn, ActivityDefinition activity)
        {
            ExcelRange activityCounts = _worksheet
                .Cells[topActivityRow, countColumn, bottomActivityRow, countColumn];
            IExcelConditionalFormattingGreaterThan countMoreThanMaximum =
                activityCounts.ConditionalFormatting.AddGreaterThan();
            countMoreThanMaximum.Formula = $"{activity.MaximumCapacity}";
            countMoreThanMaximum.Style.Fill.PatternType = ExcelFillStyle.Solid;
            countMoreThanMaximum.Style.Fill.BackgroundColor.Color = Color.Red;
            IExcelConditionalFormattingGreaterThan countMoreThanOptimal =
                activityCounts.ConditionalFormatting.AddGreaterThan();
            countMoreThanOptimal.Formula = $"{activity.OptimalCapacity}";
            countMoreThanOptimal.Style.Fill.PatternType = ExcelFillStyle.Solid;
            countMoreThanOptimal.Style.Fill.BackgroundColor.Color = Color.Yellow;
            IExcelConditionalFormattingLessThan countLessThanMinimum =
                activityCounts.ConditionalFormatting.AddLessThan();
            countLessThanMinimum.Formula = $"{activity.MinimumCapacity}";
            countLessThanMinimum.Style.Fill.PatternType = ExcelFillStyle.Solid;
            countLessThanMinimum.Style.Fill.BackgroundColor.Color = Color.CornflowerBlue;
        }
    }
}
