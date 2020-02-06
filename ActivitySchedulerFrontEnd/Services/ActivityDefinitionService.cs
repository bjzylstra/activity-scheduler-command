using Camp;
using GridMvc.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace ActivitySchedulerFrontEnd.Services
{
	public class ActivityDefinitionService : IActivityDefinitionService
	{
		private Dictionary<string, List<ActivityDefinition>> _activitySets;
		private const string ActivityFileExtension = ".xml";

		/// <summary>
		/// Default constructor used by dependency injection
		/// </summary>
		public ActivityDefinitionService()
		{
			string applicationName = Assembly.GetEntryAssembly().GetName().Name;
			_activitySets = InitializeActivitySets(applicationName);
		}

		/// <summary>
		/// Construct with a fixed application name for testing
		/// </summary>
		/// <param name="folderName">Application name for local application data folder</param>
		public ActivityDefinitionService(string folderName)
		{
			_activitySets = InitializeActivitySets(folderName);
		}

		/// <summary>
		/// Generate the activity sets from the local application data folder.
		/// Creates and preloads with the embedded definitions if folder is not found.
		/// </summary>
		/// <param name="applicationName">Application name for local applications data folder</param>
		/// <returns>Dictionary of activity definitions by set name</returns>
		private Dictionary<string, List<ActivityDefinition>> InitializeActivitySets(string applicationName)
		{
			Dictionary<string, List<ActivityDefinition>> activitySets = new Dictionary<string, List<ActivityDefinition>>();
			try
			{
				DirectoryInfo dataDirectoryInfo = new DirectoryInfo(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
				DirectoryInfo applicationDirectoryInfo = dataDirectoryInfo.GetDirectories().FirstOrDefault(d =>
					d.Name.Equals(applicationName, StringComparison.OrdinalIgnoreCase));
				if (applicationDirectoryInfo == null)
				{
					// First time - load default from the embedded resource
					applicationDirectoryInfo = dataDirectoryInfo.CreateSubdirectory(applicationName);
					Assembly assembly = typeof(ActivityDefinitionService).Assembly;
					foreach (string activityResource in assembly.GetManifestResourceNames())
					{
						if (activityResource.EndsWith(ActivityFileExtension))
						{
							string[] resourcePathElements = activityResource.Split('.');
							string activitySet = resourcePathElements[resourcePathElements.Length - 2];
							using (FileStream writeStream = File.OpenWrite(
								$"{applicationDirectoryInfo.FullName}\\{activitySet}{ActivityFileExtension}"))
							{
								using (Stream activitiesStream = assembly.GetManifestResourceStream(activityResource))
								{
									byte[] buffer = new byte[activitiesStream.Length];
									int bytesRead = activitiesStream.Read(buffer, 0, buffer.Length);
									if (bytesRead > 0)
									{
										writeStream.Write(buffer, 0, bytesRead);
									}
								}
							}

						}
					}
					applicationDirectoryInfo.Refresh();
				}

				foreach (var activityFile in applicationDirectoryInfo.EnumerateFiles()
					.Where(f => f.Extension.Equals(ActivityFileExtension, StringComparison.OrdinalIgnoreCase))
					)
				{
					var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(activityFile.FullName);
					if (activityDefinitions != null)
					{
						string activitySetName = activityFile.Name.Substring(0,
							activityFile.Name.Length - ActivityFileExtension.Length);
						activitySets.Add(activitySetName, activityDefinitions);
					}
				}
			}
			catch (Exception e)
			{
				// TODO: nice log message here
			}
			return activitySets;
		}

		public Task Delete(params object[] keys)
		{
			throw new NotImplementedException();
		}

		public Task<ActivityDefinition> Get(params object[] keys)
		{
			throw new NotImplementedException();
		}

		public ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(string activitySet, 
			Action<IGridColumnCollection<ActivityDefinition>> columns, QueryDictionary<StringValues> query)
		{
			var server = new GridServer<ActivityDefinition>(_activitySets[activitySet], 
				new QueryCollection(query),
				true, "activityDefinitionsGrid", columns)
				.Sortable()
				.Filterable()
				.WithMultipleFilters();

			// return items to displays
			return server.ItemsToDisplay;
		}

		public ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(string activitySet,
			QueryDictionary<StringValues> query)
		{
			var server = new GridServer<ActivityDefinition>(_activitySets[activitySet], 
				new QueryCollection(query),
				true, "activityDefinitionGrid", null).AutoGenerateColumns()
				.Sortable()
				.Filterable()
				.WithMultipleFilters();

			// return items to displays
			return server.ItemsToDisplay;
		}

		public Task Insert(ActivityDefinition item)
		{
			throw new NotImplementedException();
		}

		public Task Update(ActivityDefinition item)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<string> GetActivitySetNames()
		{
			return _activitySets.Keys;
		}
	}

	public interface IActivityDefinitionService : ICrudDataService<ActivityDefinition>
	{
		ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(string activityDefinitionSet, 
			Action<IGridColumnCollection<ActivityDefinition>> columns, 
			QueryDictionary<StringValues> query);
		ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(string activityDefinitionSet, 
			QueryDictionary<StringValues> query);
		IEnumerable<string> GetActivitySetNames();
	}
}
