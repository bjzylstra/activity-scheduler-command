using Camp;
using GridShared;
using System;

namespace ActivitySchedulerFrontEnd.ColumnCollections
{
	public class ColumnCollections
	{
        public static Action<IGridColumnCollection<ActivityDefinition>> ActivityDefinitionColumns = c =>
        {
            c.Add(ad => ad.Name).Titled(nameof(ActivityDefinition.Name)).SetWidth(20);
            c.Add(ad => ad.MinimumCapacity).Titled(nameof(ActivityDefinition.MinimumCapacity)).SetWidth(5)
                .RenderValueAs(ad => ad.MinimumCapacity > 0 ? ad.MinimumCapacity.ToString() : "");
            c.Add(ad => ad.MaximumCapacity).Titled(nameof(ActivityDefinition.MaximumCapacity)).SetWidth(5)
                .RenderValueAs(ad => ad.MaximumCapacity < int.MaxValue ? ad.MaximumCapacity.ToString() : "No Limit");
            c.Add(ad => ad.OptimalCapacity).Titled(nameof(ActivityDefinition.OptimalCapacity)).SetWidth(5)
                .RenderValueAs(ad => ad.OptimalCapacity > 0 ? ad.OptimalCapacity.ToString() : "");
        };
    }
}
