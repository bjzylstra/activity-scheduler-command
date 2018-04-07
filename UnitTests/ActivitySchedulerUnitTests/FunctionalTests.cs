using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace ActivitySchedulerUnitTests
{
    [TestCategory("Functional")]
    [TestClass]
    public class FunctionalTests
    {
        private const String CommandLineFormat = @"dotnet ..\..\..\..\..\Application\bin\Debug\netcoreapp2.0\ActivityScheduler.dll -r {0} -d {1}";

        [TestMethod]
        public void FunctionalTest_CommandLineIncomplete_ErrorMessage()
        {
            // Act
            var command = String.Format(@"dotnet ..\..\..\..\..\Application\bin\Debug\netcoreapp2.0\ActivityScheduler.dll");
            String output;
            String errors;
            int exitCode = ExecuteConsoleApplication(command, out output, out errors);

            // Assert - some kind of error about arguments
            AssertStringContains(errors, "Required option");
            // Ookii print usage to console is not getting along with NET Core.
            // Works directly from command line but fails in launched as a process.
            // Accept any negative exit code.
            Assert.IsTrue(exitCode < 0, "Exit code {0} should be less than 0", exitCode);
        }

        [TestMethod]
        public void FunctionalTest_ActivitiesNotFound_ErrorMessage()
        {
            // Act
            var command = String.Format(CommandLineFormat,
                 "\"..\\..\\..\\Skills Test Data.csv\"",
                 "NoData.xml");
            String output;
            String errors;
            int exitCode = ExecuteConsoleApplication(command, out output, out errors);

            // Assert
            Assert.AreEqual(-2, exitCode, "exitcode");
            AssertStringContains(errors, "Could not open Activity Definitions file");
        }

        [TestMethod]
        public void FunctionalTest_CamperRequestsNotFound_ErrorMessage()
        {
            // Act
            var command = String.Format(CommandLineFormat,
                "NoData.csv",
                "..\\..\\..\\Activities.xml");
            String output;
            String errors;
            int exitCode = ExecuteConsoleApplication(command, out output, out errors);

            // Assert
            Assert.AreEqual(-2, exitCode, "exitcode");
            AssertStringContains(errors, "Could not open Camper CSV file");
        }

        [TestMethod]
        public void FunctionalTest_ValidInput_GenerateOutput()
        {
            // Act
            var command = String.Format(CommandLineFormat,
                "\"..\\..\\..\\Skills Test Data.csv\"",
                "..\\..\\..\\Activities.xml");
            String output;
            String errors;
            int exitCode = ExecuteConsoleApplication(command, out output, out errors);

            // Assert
            Assert.AreEqual(0, exitCode, "exitcode");
            Assert.AreEqual(String.Empty, errors, "Errors");
            AssertStringContains(output, "Found 98 campers");
            AssertStringContains(output, "Found 9 activity definitions");
        }

        [TestMethod]
        public void FunctionalTest_RequestUnknownActivity_ErrorMessage()
        {
            // Act
            var command = String.Format(CommandLineFormat,
                "\"..\\..\\..\\Skills Test Data.csv\"",
                "..\\..\\..\\IncompleteActivities.xml");
            String output;
            String errors;
            int exitCode = ExecuteConsoleApplication(command, out output, out errors);

            // Assert
            Assert.AreEqual(-2, exitCode, "exitcode");
            AssertStringContains(errors, "Camper 'A, 1' requested unknown activity: 'Splash (beach activities)'");
        }

        /// <summary>
        /// Assert that a string has a specified substring
        /// </summary>
        /// <param name="containingString">String to test</param>
        /// <param name="expectedContent">Content to expect</param>
        private static void AssertStringContains(String containingString, String expectedContent)
        {
            Assert.IsTrue(containingString.Contains(expectedContent), "'{0}' did not contain '{1}'",
                containingString, expectedContent);
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
