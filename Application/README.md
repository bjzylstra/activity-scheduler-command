# activity-scheduler-command

Command line application to read the camper signups CSV file and generate schedule CSVs for each activity.

Usage: ActivityScheduler.dll -InputCSV <String> [-Help]

    -InputCSV <String>
        Path to a CSV file describing the camper activity requests Alias: -i.

    -Help [<Boolean>]
        Displays this help message. Alias: -?.

Execute with dotnet

# Coverage
cd UnitTests\ActivitySchedulerUnitTests
dotnet build /t:coverage
Report is in file://UnitTests/ActivitySchedulerUnitTests/coverage/index.html

# TODO
- Schedule 1 pass by optimal and pick up the unplaced using maximum
- Do not use alternate for choice 1 & 2 (requires storing choice rank during sort)
- Output to log file
- Bat file to bundle up to run from double click