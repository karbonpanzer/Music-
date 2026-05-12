using System.Collections.Generic;
using MusicAlbums.Comps;
using MusicAlbums.Doers;
using MusicAlbums.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MusicAlbums.Buildings
{
    public class ITab_ContentsCDs : ITab_ContentsBase
    {
        private static readonly CachedTexture DropTex = new CachedTexture("UI/Buttons/Drop");

        public Building_CDRack Rack => base.SelThing as Building_CDRack;

        public override IList<Thing> container
        {
            get
            {
                ThingOwner held = Rack?.GetDirectlyHeldThings();
                if (held == null) return null;
                List<Thing> result = new List<Thing>(held.Count);
                foreach (Thing t in held)
                    result.Add(t);
                return result;
            }
        }

        public override bool IsVisible => base.SelThing is Building_CDRack && base.IsVisible;

        public override bool VisibleInBlueprintMode => false;

        public ITab_ContentsCDs()
        {
            labelKey = "TabCDRackContents";
            containedItemsKey = "TabCDRackContents";
        }

        protected override void DoItemsLists(Rect inRect, ref float curY)
        {
            ListContainedAlbums(inRect, container, ref curY);
        }

        private void ListContainedAlbums(Rect inRect, IList<Thing> albums, ref float curY)
        {
            GUI.BeginGroup(inRect);
            float num = curY;
            Widgets.ListSeparator(ref curY, inRect.width, containedItemsKey.Translate());
            Rect rect = new Rect(0f, num, inRect.width, curY - num - 3f);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegionByKey(rect, "ContainedCDsDesc");
            }

            bool flag = false;
            for (int i = 0; i < albums.Count; i++)
            {
                if (albums[i] is MusicAlbum album)
                {
                    flag = true;
                    DoRow(album, inRect.width, i, ref curY);
                }
            }
            if (!flag)
                Widgets.NoneLabel(ref curY, inRect.width);

            GUI.EndGroup();
        }

        private void DoRow(MusicAlbum album, float width, int i, ref float curY)
        {
            Rect rect = new Rect(0f, curY, width, 28f);
            Widgets.InfoCardButton(0f, curY, album);

            if (Mouse.IsOver(rect))
                Widgets.DrawHighlightSelected(rect);
            else if (i % 2 == 1)
                Widgets.DrawLightHighlight(rect);

            Rect dropBtn = new Rect(rect.width - 24f, curY, 24f, 24f);
            if (Widgets.ButtonImage(dropBtn, DropTex.Texture))
            {
                List<IntVec3> walkable = new List<IntVec3>();
                foreach (IntVec3 cell in Rack.OccupiedRect().AdjacentCells)
                    if (cell.Walkable(Rack.Map))
                        walkable.Add(cell);
                if (!walkable.TryRandomElement(out var result))
                    result = Rack.Position;

                Rack.GetDirectlyHeldThings().TryDrop(album, result, Rack.Map, ThingPlaceMode.Near, 1, out var dropped);
                if (dropped.TryGetComp(out CompForbiddable comp))
                    comp.Forbidden = true;
            }
            else if (Widgets.ButtonInvisible(rect))
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(album);
                InspectPaneUtility.OpenTab(typeof(ITab_MusicAlbum));
            }

            TooltipHandler.TipRegionByKey(dropBtn, "EjectCDTooltip");
            Widgets.ThingIcon(new Rect(24f, curY, 28f, 28f), album);

            Rect labelRect = new Rect(60f, curY, width - 36f, rect.height);
            labelRect.xMax = dropBtn.xMin;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, album.LabelCap.Truncate(labelRect.width));
            Text.Anchor = TextAnchor.UpperLeft;

            if (Mouse.IsOver(rect))
            {
                TargetHighlighter.Highlight(album, arrow: true, colonistBar: false);
                TooltipHandler.TipRegion(rect, album.DescriptionDetailed);
            }

            curY += 28f;
        }
    }
}
