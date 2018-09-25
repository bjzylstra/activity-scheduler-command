using System.Collections.Generic;
using System.Drawing;
using Camp;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ScheduleToSpreadsheet
{
    internal class ActivitySheet
    {
        private List<ActivityDefinition> _activitySchedule;

        /// <summary>
        /// Create the activity sheet definition from an activity schedule
        /// </summary>
        /// <param name="activitySchedule">Activity Definitions with schedule information</param>
        public ActivitySheet(List<ActivityDefinition> activitySchedule)
        {
            _activitySchedule = activitySchedule;
        }

        internal void AddToWorkbook(ExcelWorkbook excelWorkbook)
        {
            ExcelWorksheet activityWorksheet = excelWorkbook.Worksheets.Add("Activities");

            int row = activityWorksheet.Cells.Start.Row;
            int column = activityWorksheet.Cells.Start.Column;

            // Add the headers
            activityWorksheet.SetValue(row, column++, "Activity");
            activityWorksheet.Column(column).Width = 5;
            activityWorksheet.SetValue(row, column++, "Block");
            activityWorksheet.Column(column).Width = 5;
            activityWorksheet.SetValue(row, column++, "# Campers");
            row++;
            activityWorksheet.View.FreezePanes(row, column);

            foreach (var activity in _activitySchedule)
            {
                column = activityWorksheet.Cells.Start.Column;
                activityWorksheet.Column(column).Width = 20;
                activityWorksheet.SetValue(row, column, activity.Name);
                foreach (var activityBlock in activity.ScheduledBlocks)
                {
                    column = activityWorksheet.Cells.Start.Column + 1;
                    activityWorksheet.SetValue(row, column, activityBlock.TimeSlot);
                    column++;

                    bool showLimits = activity.MaximumCapacity > 0;
                    bool aboveMinimum = activityBlock.AssignedCampers.Count >= activity.MinimumCapacity;
                    int numberOfCampers = activityBlock.AssignedCampers.Count;
                    activityWorksheet.SetValue(row, column, numberOfCampers);
                    if (showLimits)
                    {
                        activityWorksheet.Cells[row, column].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        activityWorksheet.Cells[row, column].Style.Fill.BackgroundColor
                            .SetColor(numberOfCampers > activity.MaximumCapacity || !aboveMinimum
                                ? Color.OrangeRed
                                : (numberOfCampers > activity.OptimalCapacity) ? Color.Yellow
                                : Color.LawnGreen);
                    }
                    column++;

                    for (int camperIndex = 1; camperIndex <= activityBlock.AssignedCampers.Count; camperIndex++)
                    {
                        var camper = activityBlock.AssignedCampers[camperIndex-1];
                        activityWorksheet.Column(column).Width = 30;
                        activityWorksheet.SetValue(row, column, camper.ToString());
                        if (showLimits)
                        {
                            activityWorksheet.Cells[row, column].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            activityWorksheet.Cells[row, column].Style.Fill.BackgroundColor
                                .SetColor(camperIndex > activity.MaximumCapacity || !aboveMinimum
                                    ? Color.OrangeRed
                                    : (camperIndex > activity.OptimalCapacity) ? Color.Yellow
                                    : Color.LawnGreen);
                        }
                        column++;
                    }
                    row++;
                }
            }
        }
    }
}
