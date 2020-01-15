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
			new ActivityDefinition{ Name = "Water fun", MinimumCapacity=10, MaximumCapacity=20, OptimalCapacity=15 },
			new ActivityDefinition{ Name = "Field games", MinimumCapacity=10, MaximumCapacity=40, OptimalCapacity=16 }
		};

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
				true, "activityDefinitionsGrid", columns);

			// return items to displays
			return server.ItemsToDisplay;
		}

		public ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(QueryDictionary<StringValues> query)
		{
			var server = new GridServer<ActivityDefinition>(_activityDefinitions, new QueryCollection(query),
				true, "activityDefinitionGrid", null).AutoGenerateColumns();

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
	}

	public interface IActivityDefinitionService : ICrudDataService<ActivityDefinition>
	{
		ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(Action<IGridColumnCollection<ActivityDefinition>> columns, QueryDictionary<StringValues> query);
		ItemsDTO<ActivityDefinition> GetActivityDefinitionsGridRows(QueryDictionary<StringValues> query);

	}
}
