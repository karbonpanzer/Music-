using System.Collections.Generic;
using MusicAlbums.Comps;
using MusicAlbums.Doers;
using RimWorld;
using UnityEngine;
using Verse;

namespace MusicAlbums.Buildings
{
    public class Building_CDRack : Building, IThingHolderEvents<MusicAlbum>, IHaulEnroute, IStorageGroupMember, IHaulDestination, IStoreSettingsParent, IHaulSource, IThingHolder, ISearchableContents
    {
        private ThingOwner<MusicAlbum> innerContainer;
        private StorageSettings settings;
        private StorageGroup storageGroup;

        private Graphic bookendGraphicEastInt;
        private Graphic bookendGraphicNorthInt;

        private static readonly Vector3 DrawOffset       = new Vector3(0f, 0.018292684f, 0f);
        private static readonly Vector3 DrawOffsetEnd    = new Vector3(0f, 0.07317074f,  0f);
        private const float AlbumWidthEastWest   = 0.16f;
        private const float AlbumWidthNorthSouth  = 0.155f;

        private static readonly Vector3[] RotOffsets = new Vector3[4]
        {
            new Vector3(-0.081f, 0f,  0f),
            new Vector3(-0.082f, 0f,  0.02f),
            new Vector3( 0.08f,  0f,  0.08f),
            new Vector3( 0.082f, 0f, -0.15f),
        };

        private Graphic BookendGraphicEast => bookendGraphicEastInt ?? (bookendGraphicEastInt = def.building.bookendGraphicEast?.GraphicColoredFor(this));
        private Graphic BookendGraphicNorth => bookendGraphicNorthInt ?? (bookendGraphicNorthInt = def.building.bookendGraphicNorth?.GraphicColoredFor(this));

        public int MaxAlbums => def.building.maxItemsInCell * def.size.Area;
        public IReadOnlyList<MusicAlbum> HeldAlbums => innerContainer.InnerListForReading;

        public ThingOwner SearchableContents => innerContainer;
        public bool StorageTabVisible => true;
        public bool HaulSourceEnabled => true;
        public bool HaulDestinationEnabled => true;

        StorageGroup IStorageGroupMember.Group { get => storageGroup; set => storageGroup = value; }
        bool IStorageGroupMember.DrawConnectionOverlay => Spawned;
        Map IStorageGroupMember.Map => MapHeld;
        string IStorageGroupMember.StorageGroupTag => def.building.storageGroupTag;
        StorageSettings IStorageGroupMember.StoreSettings => GetStoreSettings();
        StorageSettings IStorageGroupMember.ParentStoreSettings => GetParentStoreSettings();
        StorageSettings IStorageGroupMember.ThingStoreSettings => settings;
        bool IStorageGroupMember.DrawStorageTab => true;
        bool IStorageGroupMember.ShowRenameButton => Faction == Faction.OfPlayer;

        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public StorageSettings GetStoreSettings() => storageGroup?.GetStoreSettings() ?? settings;
        public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

        public bool Accepts(Thing t)
        {
            if (HeldAlbums.Count >= MaxAlbums)
            {
                if (t is not MusicAlbum album) return false;
                if (!innerContainer.InnerListForReading.Contains(album)) return false;
            }
            return GetStoreSettings().AllowedToAccept(t) && innerContainer.CanAcceptAnyOf(t);
        }

        public int SpaceRemainingFor(ThingDef _) => MaxAlbums - HeldAlbums.Count;

        public void Notify_SettingsChanged()
        {
            if (Spawned) MapHeld.listerHaulables.Notify_HaulSourceChanged(this);
        }

        public void Notify_ItemAdded(MusicAlbum item)
        {
            this.GetRoom()?.Notify_ContainedThingSpawnedOrDespawned(this);
            MapHeld.listerHaulables.Notify_AddedThing(item);
        }

        public void Notify_ItemRemoved(MusicAlbum item)
        {
            this.GetRoom()?.Notify_ContainedThingSpawnedOrDespawned(this);
        }

        public Building_CDRack()
        {
            innerContainer = new ThingOwner<MusicAlbum>(this, oneStackOnly: false);
        }

        public override void PostMake()
        {
            base.PostMake();
            settings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
                settings.CopyFrom(def.building.defaultStorageSettings);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (storageGroup != null && map != storageGroup.Map)
            {
                StorageSettings stored = storageGroup.GetStoreSettings();
                storageGroup.RemoveMember(this);
                storageGroup = null;
                settings.CopyFrom(stored);
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            storageGroup?.RemoveMember(this);
            storageGroup = null;
            if (mode != DestroyMode.WillReplace)
                innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
            base.DeSpawn(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            drawLoc -= Altitudes.AltIncVect * 2f;
            base.DrawAt(drawLoc, flip);

            Rot4 perpendicular = Rotation.Rotated(RotationDirection.Counterclockwise);
            float width = (Rotation == Rot4.North || Rotation == Rot4.South) ? AlbumWidthNorthSouth : AlbumWidthEastWest;
            Vector3 step      = perpendicular.FacingCell.ToVector3() * width;
            Vector3 start     = perpendicular.FacingCell.ToVector3() * ((float)(-MaxAlbums) * width * 0.5f);
            Vector3 rotOffset = RotOffsets[Rotation.AsInt];

            for (int i = 0; i < HeldAlbums.Count; i++)
            {
                MusicAlbum album = HeldAlbums[i];
                Rot4 facing = Rotation.Opposite;
                if (facing == Rot4.East || facing == Rot4.West) facing = facing.Opposite;
                album.VerticalGraphic?.Draw(drawLoc + start + rotOffset + DrawOffset + step * i, facing, this);
            }

            if (Rotation != Rot4.South)
            {
                if (Rotation != Rot4.North)
                    BookendGraphicEast?.Draw(drawLoc + DrawOffsetEnd, Rot4.North, this);
                else
                    BookendGraphicNorth?.Draw(drawLoc + DrawOffsetEnd, Rot4.North, this);
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (Find.Selector.SingleSelectedThing == this)
            {
                Room room = this.GetRoom();
                if (room != null && room.ProperRoom) room.DrawFieldEdges();
            }
            StorageGroupUtility.DrawSelectionOverlaysFor(this);
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (Spawned)
            {
                if (!string.IsNullOrEmpty(text)) text += "\n";
                if (storageGroup != null)
                {
                    text += string.Format("{0}: {1} ", "StorageGroupLabel".Translate(), storageGroup.RenamableLabel.CapitalizeFirst());
                    text += storageGroup.MemberCount <= 1
                        ? $"({"OneBuilding".Translate()})\n"
                        : $"({"NumBuildings".Translate(storageGroup.MemberCount).CapitalizeFirst()})\n";
                }
                text += string.Format("{0}: {1} / {2}", "CDRackStoredInspect".Translate(), HeldAlbums.Count, MaxAlbums);
            }
            return text;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos()) yield return g;
            foreach (Gizmo g in StorageSettingsClipboard.CopyPasteGizmosFor(GetStoreSettings())) yield return g;
            if (StorageTabVisible && MapHeld != null)
                foreach (Gizmo g in StorageGroupUtility.StorageGroupMemberGizmos(this)) yield return g;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption opt in HaulSourceUtility.GetFloatMenuOptions(this, selPawn)) yield return opt;
            foreach (MusicAlbum album in HeldAlbums)
                foreach (FloatMenuOption opt in album.GetFloatMenuOptions(selPawn)) yield return opt;
            foreach (FloatMenuOption opt in base.GetFloatMenuOptions(selPawn)) yield return opt;
        }

        public override void Notify_ColorChanged()
        {
            bookendGraphicEastInt = null;
            bookendGraphicNorthInt = null;
            base.Notify_ColorChanged();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Deep.Look(ref settings, "settings", this);
            Scribe_References.Look(ref storageGroup, "storageGroup");
        }
    }
}
