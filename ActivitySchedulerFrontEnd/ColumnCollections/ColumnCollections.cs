using Camp;
using GridShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActivitySchedulerFrontEnd.ColumnCollections
{
	public class ColumnCollections
	{
        public static Action<IGridColumnCollection<ActivityDefinition>> ActivityDefinitionColumns = c =>
        {
            c.Add(ad => ad.Name).Titled("Name").SetWidth(20);
        };
    }
}
