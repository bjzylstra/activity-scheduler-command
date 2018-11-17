dotnet publish\ActivityScheduler.dll -r CamperRequests.csv -d Activities.xml -a ActivitySchedule.csv -c CamperSchedule.csv 1> ScheduleCampers.log 2>&1
@ECHO OFF
IF NOT %ERRORLEVEL% EQU 0 (
	ECHO Not all the campers could be placed in their requested activities.
	ECHO See the details in the ScheduleCampers.log file displayed next ...
	pause
	type ScheduleCampers.log
	pause
	exit %ERRORLEVEL%
)
dotnet publish\ScheduleToSpreadsheet.dll -a ActivitySchedule.csv -d Activities.xml -s Schedule.xlsx 1> BuildSpreadsheet.log 2>&1
IF %ERRORLEVEL% EQU 0 (
	ECHO Success!
	ECHO Open the Schedule.xlsx file in Excel to the generated schedule.
	ECHO Details of the scheduling can be found in ScheduleCampers.log
) ELSE (
	ECHO Could not generate the spreadsheet from the schedule csv file.
	ECHO See the details in the BuildSpreadsheet.log file displayed next ...
	pause
	type BuildSpreadsheet.log
	pause
	exit %ERRORLEVEL%
)
