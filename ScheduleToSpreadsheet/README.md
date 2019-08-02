# schedule-to-spreadsheet

Command line application to convert a schedule CSV into an Excel file.

Usage: dotnet ScheduleToSpreadsheet.dll -a <String> -s <String> [-d <String>] [-Help]

  -a, --ActivityScheduleCsvPath    Required. Path to the CSV file with the activity schedules

  -s, --ScheduleExcelPath          Required. Path to the output Excel spreadsheet file

  -d, --ActivityDefinitionsPath    Path to the XML file with the activity definitions

  --help                           Display this help screen.

  --version                        Display version information.


# Coverage (combined with ActivityScheduler for now)
cd UnitTests\ActivitySchedulerUnitTests
dotnet build /t:coverage
Report is in file://UnitTests/ActivitySchedulerUnitTests/coverage/index.html

# TODO
- Add button to the activity sheet to repack the camper list
- Add button to the activity sheet to update schedule on the camper sheet
- Add button to the camper sheet to update schedule on the activity sheet
- Block numbers off by one between sheets. Make both 1-4 on display
- 