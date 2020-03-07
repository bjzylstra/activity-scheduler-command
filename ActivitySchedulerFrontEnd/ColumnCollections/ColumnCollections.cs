using ActivitySchedulerFrontEnd.Pages;
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

        public static Action<IGridColumnCollection<IActivityBlock>> ActivityScheduleColumns(object context)
        {
            return c =>
            {
                Func<IActivityBlock, int, string> CssForCount = (IActivityBlock block, int index) =>
                {
                    string style = "";
                    if (index > block.ActivityDefinition.MaximumCapacity)
                    {
                        style = "capacity-over";
                    }
                    else if (index > block.ActivityDefinition.OptimalCapacity)
                    {
                        style = "capacity-warning";
                    }
                    else if (index < block.ActivityDefinition.MinimumCapacity)
                    {
                        style = "capacity-under";
                    }
                    return style;
                };

                c.Add(ab => ab.ActivityDefinition.Name).Titled(nameof(ActivityDefinition.Name)).SetWidth(15);

                c.Add(ab => ab.TimeSlot).RenderValueAs(ab => $"{ab.TimeSlot+1}").Titled("Block").SetWidth(5);

                c.Add(ab => ab.AssignedCampers.Count).Titled("#").SetWidth(3)
                .SetCellCssClassesContraint(ab => CssForCount(ab, ab.AssignedCampers.Count));

                c.Add().SetWidth(20).Titled("Campers")
                .RenderComponentAs<ActivityCampers>(context)
                .Css("activity-camper-set");
            };
        }
    }
}
