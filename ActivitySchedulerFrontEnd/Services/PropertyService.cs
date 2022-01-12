using GridMvc.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActivitySchedulerFrontEnd.Services
{
	public class PropertyService : IPropertyService
	{
		/// <summary>
		/// A property to display
		/// </summary>
		public class Property
		{
			/// <summary>
			/// Name of the property
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// Value of the property
			/// </summary>
			public object Value { get; set; }

			/// <summary>
			/// Initializer Constructor
			/// </summary>
			/// <param name="name">Property name</param>
			/// <param name="value">Property value</param>
			public Property(string name, object value)
			{
				Name = name;
				Value = value;
			}
		}

		private readonly Dictionary<string, List<Property>> _propertiesById = 
			new Dictionary<string, List<Property>>
			{
				{
					"TryIt", new List<Property>
					{
						new Property("Name", "Property"),
						new Property("Type", "Stringy")
					} 
				}
			};

		public ItemsDTO<Property> GetPropertyRows(string propertySetId, 
			Action<IGridColumnCollection<Property>> columns, 
			QueryDictionary<StringValues> query)
		{
			var emptyPropertySet = new List<Property>();
			var server = new GridServer<Property>(
				_propertiesById.GetValueOrDefault(propertySetId, emptyPropertySet),
				new QueryCollection(query), true, "propertyGrid", columns);

			return server.ItemsToDisplay;
		}
	}

	public interface IPropertyService
	{
		/// <summary>
		/// Get grid rows for a property set
		/// </summary>
		/// <param name="propertySetId">Id of the property set</param>
		/// <param name="columns">Property column details</param>
		/// <param name="query">Query</param>
		/// <returns>Property Items</returns>
		ItemsDTO<PropertyService.Property> GetPropertyRows(string propertySetId,
			Action<IGridColumnCollection<PropertyService.Property>> columns,
			QueryDictionary<StringValues> query);
	}
}
