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
		private Dictionary<string, List<ActivityDefinition>> _activitySets = new Dictionary<string, List<ActivityDefinition>>
		{
			{
				"default", new List<ActivityDefinition>{
					new ActivityDefinition{ Name = "Water fun", MinimumCapacity=10, MaximumCapacity=20 },
					new ActivityDefinition{ Name = "Field games", MinimumCapacity=10, MaximumCapacity=40, OptimalCapacity=16 }
				}
			},
			{
				"preload", new List<ActivityDefinition>{
					new ActivityDefinition{ Name = "Water fun", MinimumCapacity=10, MaximumCapacity=20 },
					new ActivityDefinition{ Name = "Field games", MinimumCapacity=10, MaximumCapacity=40, OptimalCapacity=16 }
				}
			}
		};

		public ActivityDefinitionService()
		{
			try
			{
				DirectoryInfo dataDirectoryInfo = new DirectoryInfo(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
				string applicationName = Assembly.GetEntryAssembly().GetName().Name;
				DirectoryInfo applicationDirectoryInfo = dataDirectoryInfo.GetDirectories().FirstOrDefault(d =>
					d.Name.Equals(applicationName, StringComparison.OrdinalIgnoreCase));
				if (applicationDirectoryInfo == null)
				{
					dataDirectoryInfo.CreateSubdirectory(applicationName);
					applicationDirectoryInfo = dataDirectoryInfo.GetDirectories().FirstOrDefault(d =>
						d.Name.Equals(applicationName, StringComparison.OrdinalIgnoreCase));
					File.Copy("DefaultActivities.xml", applicationDirectoryInfo.FullName + "\\DefaultActivities.xml");
					applicationDirectoryInfo.Refresh();
				}

				string activityFileExtension = ".xml";
				foreach (var activityFile in applicationDirectoryInfo.EnumerateFiles()
					.Where(f => f.Extension.Equals(activityFileExtension, StringComparison.OrdinalIgnoreCase))
					)
				{
					var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(activityFile.FullName);
					if (activityDefinitions != null)
					{
						string activitySetName = activityFile.Name.Substring(0, 
							activityFile.Name.Length - activityFileExtension.Length);
						_activitySets.Add(activitySetName, activityDefinitions);
					}
				}
			}
			catch (Exception)
			{
				// TODO: nice log message here
			}
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
