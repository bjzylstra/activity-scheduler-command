using OfficeOpenXml;
using OfficeOpenXml.VBA;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ScheduleToSpreadsheet
{
	internal class WorkSheet
	{
		protected ExcelWorksheet _worksheet;
		protected ExcelVBAModule _macroModule;

		/// <summary>
		/// Construct a worksheet that can get macros loaded from embedded resources
		/// </summary>
		/// <param name="worksheetName">Name of the worksheet</param>
		/// <param name="excelWorkbook">Excel workbook to add worksheets to</param>
		protected WorkSheet(string worksheetName, ExcelWorkbook excelWorkbook)
		{
			_worksheet = excelWorkbook.Worksheets.Add(worksheetName);
			if (_worksheet.Workbook.VbaProject == null)
			{
				_worksheet.Workbook.CreateVBAProject();
			}
			_macroModule = excelWorkbook.VbaProject.Modules.AddModule($"{worksheetName}Commands");
		}

		/// <summary>
		/// Add VB code contained in embedded resources to the work sheet.
		/// </summary>
		internal void AddVisualBasicCode()
		{
			StringBuilder codeBuilder = new StringBuilder();

			Assembly assembly = typeof(ActivitySheet).Assembly;
			List<string> resourceNames = new List<string>(assembly.GetManifestResourceNames());
			foreach (var resourceName in resourceNames.Where(r
				=> r.StartsWith($"ScheduleToSpreadsheet.Macros.{_worksheet.Name}")))
			{
				Stream macroStream = typeof(ActivitySheet).Assembly.GetManifestResourceStream(
					resourceName);
				using (var reader = new StreamReader(macroStream))
				{
					codeBuilder.Append(reader.ReadToEnd());
					codeBuilder.AppendLine();
				}
			}

			_worksheet.CodeModule.Code = codeBuilder.ToString();
		}

		/// <summary>
		/// Add macros contained in embedded resources to the command module.
		/// </summary>
		internal void AddCommands()
		{
			StringBuilder codeBuilder = new StringBuilder();

			Assembly assembly = typeof(ActivitySheet).Assembly;
			List<string> resourceNames = new List<string>(assembly.GetManifestResourceNames());
			foreach (var resourceName in resourceNames.Where(r
				=> r.StartsWith($"ScheduleToSpreadsheet.Macros.{_worksheet.Name}Commands")))
			{
				Stream macroStream = typeof(ActivitySheet).Assembly.GetManifestResourceStream(
					resourceName);
				using (var reader = new StreamReader(macroStream))
				{
					codeBuilder.Append(reader.ReadToEnd());
					codeBuilder.AppendLine();
				}
			}

			_macroModule.Code = codeBuilder.ToString();
		}
	}
}
