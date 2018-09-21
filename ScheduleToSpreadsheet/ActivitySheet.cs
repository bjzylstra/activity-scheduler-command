using System.Collections.Generic;
using Camp;
using OfficeOpenXml;

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
            activityWorksheet.SetValue(row, column++, "Block");
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
                    activityWorksheet.Column(column).Width = 5;
                    activityWorksheet.SetValue(row, column, activityBlock.TimeSlot);
                    column++;
                    foreach (var camper in activityBlock.AssignedCampers)
                    {
                        activityWorksheet.Column(column).Width = 30;
                        activityWorksheet.SetValue(row, column, camper.ToString());
                        column++;
                    }
                    row++;
                }
            }
        }
    }
}
