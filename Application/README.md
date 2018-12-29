# activity-scheduler-command

Command line application to read the camper signups CSV file and generate schedule CSVs for each activity.

Usage: dotnet ActivityScheduler.dll -r <String> -d <String> [-a <String>] [-c <String>]

  -r, --CamperRequestsPath         Required. Path to a CSV file describing the camper activity requests

  -d, --ActivityDefinitionsPath    Required. Path to the XML file with the activity definitions

  -a, --ActivityScheduleCsvPath    Path to where to write the CSV file with the activity schedules

  -c, --CamperScheduleCsvPath      Path to where to write the CSV file with the camper schedules

  --help                           Display this help screen.

  --version                        Display version information.


# Coverage
cd UnitTests\ActivitySchedulerUnitTests
dotnet build /t:coverage
Report is in file://UnitTests/ActivitySchedulerUnitTests/coverage/index.html

# Generating a release folder in the solution folder
cd Application
dotnet msbuild /t:BuildRelease

# TODO
- Schedule 1 pass by optimal and pick up the unplaced using maximum
- Output to log file
