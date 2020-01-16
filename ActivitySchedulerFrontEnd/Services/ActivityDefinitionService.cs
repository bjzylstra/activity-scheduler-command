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
		private List<ActivityDefinition> _activityDefinitions = new List<ActivityDefinition>
		{
			new ActivityDefinition{ Name = "Water fun", MinimumCapacity=10, MaximumCapacity=20 },
			new ActivityDefinition{ Name = "Field games", MinimumCapacity=10, MaximumCapacity=40, OptimalCapacity=16 }
		};
		private List<string> _activitySetNames = new List<string> { "preload", "default" };

		public ActivityDefinitionService()
		{
			try
			{
				var activityDefinitions = ActivityDefinition.ReadActivityDefinitions("DefaultActivities.xml");
				if (activityDefinitions != null)
				{
					_activityDefinitions = activityDefinitions;
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

		public ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(Action<IGridColumnCollection<ActivityDefinition>> columns, QueryDictionary<StringValues> query)
		{
			var server = new GridServer<ActivityDefinition>(_activityDefinitions, new QueryCollection(query),
				true, "activityDefinitionsGrid", columns)
				.Sortable()
				.Filterable()
				.WithMultipleFilters();

			// return items to displays
			return server.ItemsToDisplay;
		}

		public ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(QueryDictionary<StringValues> query)
		{
			var server = new GridServer<ActivityDefinition>(_activityDefinitions, new QueryCollection(query),
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
			return _activitySetNames;
		}
	}

	public interface IActivityDefinitionService : ICrudDataService<ActivityDefinition>
	{
		ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(Action<IGridColumnCollection<ActivityDefinition>> columns, QueryDictionary<StringValues> query);
		ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(QueryDictionary<StringValues> query);
		IEnumerable<string> GetActivitySetNames();
	}
}
