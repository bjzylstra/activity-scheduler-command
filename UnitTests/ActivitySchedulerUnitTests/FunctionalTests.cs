using NUnit.Framework;
using OfficeOpenXml;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ActivitySchedulerUnitTests
{
	[TestFixture]
	public class FunctionalTests
	{
		private const String ScheduleCommandFormat = @"dotnet ..\..\..\..\..\Application\bin\Debug\netcoreapp2.0\ActivityScheduler.dll " +
			"-r {0} -d {1}";
		private const String ScheduleCommandMaximumFormatWithOutput = @"dotnet ..\..\..\..\..\Application\bin\Debug\netcoreapp2.0\ActivityScheduler.dll " +
			"-r {0} -d {1} -a {2} -c {3}";
		private const String ScheduleCommandOptimalFormatWithOutput = @"dotnet ..\..\..\..\..\Application\bin\Debug\netcoreapp2.0\ActivityScheduler.dll " +
			"-r {0} -d {1} -a {2} -c {3} -o";
		private const String SpreadsheetCommandFormat = @"dotnet ..\..\..\..\..\ScheduleToSpreadsheet\bin\Debug\netcoreapp2.0\ScheduleToSpreadsheet.dll " +
			"-a {0} -s {1}";
		private const String SpreadsheetCommandFormatWithDefinitions = @"dotnet ..\..\..\..\..\ScheduleToSpreadsheet\bin\Debug\netcoreapp2.0\ScheduleToSpreadsheet.dll " +
			"-a {0} -s {1} -d {2}";

		[Test]
		public void ActivityScheduler_CommandLineIncomplete_ErrorMessage()
		{
			// Act
			var command = String.Format(@"dotnet ..\..\..\..\..\Application\bin\Debug\netcoreapp2.0\ActivityScheduler.dll");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert - some kind of error about arguments
			Assert.That(errors, Contains.Substring("Required option"), "Errors");
			Assert.That(exitCode, Is.EqualTo(-1), "Application exit code");
		}

		[Test]
		public void ActivityScheduler_ActivitiesNotFound_ErrorMessage()
		{
			// Act
			var command = String.Format(ScheduleCommandFormat,
				 "\"..\\..\\..\\Skills Test Data.csv\"",
				 "NoData.xml");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(errors, Contains.Substring("Could not open Activity Definitions file"), "Errors");
			Assert.That(exitCode, Is.EqualTo(-2), "exitcode");
		}

		[Test]
		public void ActivityScheduler_CamperRequestsNotFound_ErrorMessage()
		{
			// Act
			var command = String.Format(ScheduleCommandFormat,
				"NoData.csv",
				"..\\..\\..\\Activities.xml");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(errors, Contains.Substring("Could not open Camper CSV file"), "Errors");
			Assert.That(exitCode, Is.EqualTo(-2), "exitcode");
		}

		[Test]
		public void ActivityScheduler_UseMaximumsOnly_GenerateOutput()
		{
			// Act
			var command = String.Format(ScheduleCommandMaximumFormatWithOutput,
				"CamperRequests.csv",
				"Activities.xml",
				"activitySchedule.csv",
				"camperSchedule.csv");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(exitCode, Is.EqualTo(0), "exitcode");
			Assert.That(errors, Is.Empty, "Errors");
			Assert.That(output, Contains.Substring("Found 98 campers"), "Output");
			Assert.That(output, Contains.Substring("Found 9 activity definitions"), "Output");
		}

		[Test]
		public void ActivityScheduler_UseOptimum_GenerateOutput()
		{
			// Act
			var command = String.Format(ScheduleCommandOptimalFormatWithOutput,
				"CamperRequests.csv",
				"Activities.xml",
				"activitySchedule.csv",
				"camperSchedule.csv");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(exitCode, Is.EqualTo(0), "exitcode");
			Assert.That(errors, Is.Empty, "Errors");
			Assert.That(output, Contains.Substring("Found 98 campers"), "Output");
			Assert.That(output, Contains.Substring("Found 9 activity definitions"), "Output");
		}

		[Test]
		public void ActivityScheduler_NoActivity4_GenerateOutput()
		{
			// Act
			var command = String.Format(ScheduleCommandMaximumFormatWithOutput,
				@"..\..\..\NoActivity4.csv",
				"Activities.xml",
				"activitySchedule.csv",
				"camperSchedule.csv");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(exitCode, Is.EqualTo(0), "exitcode");
			Assert.That(errors, Is.Empty, "Errors");
			Assert.That(output, Contains.Substring("Found 98 campers"), "Output");
			Assert.That(output, Contains.Substring("Found 9 activity definitions"), "Output");
		}

		[Test]
		public void ActivityScheduler_RequestUnknownActivity_ErrorMessage()
		{
			// Act
			var command = String.Format(ScheduleCommandFormat,
				"\"..\\..\\..\\Skills Test Data.csv\"",
				"..\\..\\..\\IncompleteActivities.xml");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(exitCode, Is.EqualTo(-2), "exitcode");
			Assert.That(errors, Contains.Substring("Camper 'A, 1' requested unknown activity: 'Splash (beach activities)'"), 
				"Errors");
		}

		[Test]
		public void ScheduleToSpreadsheet_CommandLineIncomplete_ErrorMessage()
		{
			// Act
			var command = String.Format(@"dotnet ..\..\..\..\..\ScheduleToSpreadsheet\bin\Debug\netcoreapp2.0\ScheduleToSpreadsheet.dll");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert - some kind of error about arguments
			Assert.That(errors, Contains.Substring("Required option"), "Errors");
			Assert.That(exitCode, Is.EqualTo(-1), "Application exit code");
		}

		[Test]
		public void ScheduleToSpreadsheet_ActivitiesNotFound_ErrorMessage()
		{
			// Act
			var command = String.Format(SpreadsheetCommandFormatWithDefinitions,
				 "\"..\\..\\..\\ActivitySchedule.csv\"",
				 "Schedule.xlsx",
				 "NoData.xml");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(errors, Contains.Substring("Could not open Activity Definitions file"), "Errors");
			Assert.That(exitCode, Is.EqualTo(-2), "exitcode");
		}

		[Test]
		public void ScheduleToSpreadsheet_ActivityScheduleNotFound_ErrorMessage()
		{
			// Act
			var command = String.Format(SpreadsheetCommandFormat,
				 "NoFound.csv",
				 "Schedule.xlsx");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(errors, Contains.Substring("Could not open Activity Schedule CSV file"), "Errors");
			Assert.That(exitCode, Is.EqualTo(-2), "exitcode");
		}

		[Test]
		public void ScheduleToSpreadsheet_OutputFileCouldNotBeCreated_ErrorMessage()
		{
			// Act
			var command = String.Format(SpreadsheetCommandFormat,
				 "\"..\\..\\..\\ActivitySchedule.csv\"",
				 "NoDirectory\\Schedule.xlsx");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(errors, Contains.Substring("Could not create spreadsheet file"), "Errors");
			Assert.That(exitCode, Is.EqualTo(-2), "exitcode");
		}

		[Test]
		public void ScheduleToSpreadsheet_NoActivitDefinitions_Success()
		{
			// Act
			FileInfo scheduleFile = new FileInfo("Schedule.xlsx");
			var command = String.Format(SpreadsheetCommandFormat,
				 "\"..\\..\\..\\ActivitySchedule.csv\"",
				 scheduleFile.FullName);
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(exitCode, Is.EqualTo(0), "exitcode");
			Assert.That(errors, Is.Empty, "Errors");

			AssertValidScheduleWorkbook(scheduleFile);
		}

		[Test]
		public void ScheduleToSpreadsheet_WithActivityDefinitions_Success()
		{
			// Act
			FileInfo scheduleFile = new FileInfo("Schedule.xlsx");
			var command = String.Format(SpreadsheetCommandFormatWithDefinitions,
				 "\"..\\..\\..\\ActivitySchedule.csv\"",
				 scheduleFile.FullName, "Activities.xml");
			String output;
			String errors;
			int exitCode = ExecuteConsoleApplication(command, out output, out errors);

			// Assert
			Assert.That(exitCode, Is.EqualTo(0), "exitcode");
			Assert.That(errors, Is.Empty, "Errors");

			AssertValidScheduleWorkbook(scheduleFile);
		}

		/// <summary>
		/// Verify that the workbook file exists and is a valid schedule workbook
		/// </summary>
		/// <param name="workbookFile">FileInfo for the workbook file</param>
		private static void AssertValidScheduleWorkbook(FileInfo workbookFile)
		{
			// Open the schedule file
			Assert.That(workbookFile.Exists, "Schedule file exists");
			using (var excelApplication = new ExcelPackage(workbookFile))
			{
				ExcelWorkbook workbook = excelApplication.Workbook;
				Assert.That(workbook.Worksheets.Select(ws => ws.Name),
					Is.EquivalentTo(new[] { "Activities", "Campers" }),
					"Work sheet names");
			}
		}

		/// <summary>
		/// Execute a console application and return the exit code,
		/// console output and error output
		/// </summary>
		/// <param name="command">Command line to execute</param>
		/// <param name="output">Console output</param>
		/// <param name="errors">Error output</param>
		/// <returns>Application exit code</returns>
		private static int ExecuteConsoleApplication(string command,
			out String output, out String errors)
		{
			var procStartInfo =
				new ProcessStartInfo("cmd", "/c " + command);

			// The following commands are needed to redirect the standard output and error.
			// This means that it will be redirected to the Process.StandardOutput StreamReader.
			procStartInfo.RedirectStandardOutput = true;
			procStartInfo.RedirectStandardError = true;
			procStartInfo.UseShellExecute = false;
			// Do not create the black window.
			procStartInfo.CreateNoWindow = true;
			// Now we create a process, assign its ProcessStartInfo and start it
			Process proc = new Process();
			proc.StartInfo = procStartInfo;
			proc.Start();
			// Get the output into a string
			output = proc.StandardOutput.ReadToEnd();
			errors = proc.StandardError.ReadToEnd();
			return proc.ExitCode;
		}
	}
}
