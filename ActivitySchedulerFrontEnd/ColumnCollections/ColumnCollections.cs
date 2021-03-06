﻿using ActivitySchedulerFrontEnd.Pages;
using Camp;
using GridShared;
using System;
using System.Linq;

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


                c.Add(ab => ab.ActivityDefinition.Name).Titled(nameof(ActivityDefinition.Name)).SetWidth(15)
                .Sortable(true).Filterable(true);

                int[] slotIds = { 0, 1, 2, 3 };
                SelectItem[] blockNumbers = slotIds.Select(b => new SelectItem($"{b}", $"Block {b+1}")).ToArray();
                c.Add(ab => ab.TimeSlot).RenderValueAs(ab => $"{ab.TimeSlot+1}").Titled("Block").SetWidth(5)
                .Sortable(true).Filterable(true).SetListFilter(blockNumbers);

                c.Add(ab => ab.AssignedCampers.Count).Titled("#").SetWidth(3)
                .SetCellCssClassesContraint(ab => CssForCount(ab, ab.AssignedCampers.Count))
                .Sortable(true).Filterable(true);

                c.Add().SetWidth(20).Titled("Campers")
                .RenderComponentAs<ActivityCampers>(context)
                .Css("activity-camper-set");
            };
        }

        public static Action<IGridColumnCollection<Camper>> CamperScheduleColumns(
            CamperScheduleGrid scheduleGrid,
            string scheduleId)
        {
            return c =>
            {
                c.Add(camper => camper.FullName).Titled("Camper Name").SetWidth(30)
                    .RenderComponentAs<ClickableCamperName>(scheduleGrid)
                    .Sortable(true);

                c.Add().Titled("Block 1").SetWidth(20)
                    .RenderComponentAs<CamperActivity>(new CamperActivity.Initializer
                    { ScheduleGrid = scheduleGrid, TimeSlot = 0 });
                c.Add().Titled("Block 2").SetWidth(20)
                    .RenderComponentAs<CamperActivity>(new CamperActivity.Initializer
                    { ScheduleGrid = scheduleGrid, TimeSlot = 1 });
                c.Add().Titled("Block 3").SetWidth(20)
                    .RenderComponentAs<CamperActivity>(new CamperActivity.Initializer
                    { ScheduleGrid = scheduleGrid, TimeSlot = 2 });
                c.Add().Titled("Block 4").SetWidth(20)
                    .RenderComponentAs<CamperActivity>(new CamperActivity.Initializer
                    { ScheduleGrid = scheduleGrid, TimeSlot = 3 });
            };
        }
    }
}
