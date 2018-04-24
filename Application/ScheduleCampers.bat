dotnet publish\ActivityScheduler.dll -r CamperRequests.csv -d Activities.xml -a ActivitySchedule.csv -c CamperSchedule.csv 1> ScheduleCampers.log 2>&1
@ECHO OFF
IF %ERRORLEVEL% EQU 0 (
	ECHO Success!
	ECHO Load ActivitySchedule.csv and CamperSchedule.csv into Excel to see the generated schedule.
	ECHO Details of the scheduling can be found in ScheduleCampers.log
) ELSE (
	ECHO Not all the campers could be placed in their requested activities.
	ECHO See the details in the ScheduleCampers.log file displayed next ...
	pause
	type ScheduleCampers.log
)
pause