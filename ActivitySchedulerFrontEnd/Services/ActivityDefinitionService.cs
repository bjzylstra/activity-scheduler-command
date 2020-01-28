using Camp;
using GridMvc.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
				// TODO: Troll the disk for activities and build up the dictionary
				var activityDefinitions = ActivityDefinition.ReadActivityDefinitions("DefaultActivities.xml");
				if (activityDefinitions != null)
				{
					_activitySets.Add("DefaultActivities", activityDefinitions);
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
