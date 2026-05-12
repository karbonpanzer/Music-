using System.Collections.Generic;
using MusicAlbums.Comps;
using MusicAlbums.Doers;
using MusicAlbums.Utility;
using RimWorld;
using Verse;
using Verse.AI;

namespace MusicAlbums.Jobs
{
    // Kept close to JobDriver_Reading. Cuts are: no isLearningDesire path, no Intellectual skill gain. Reset is called in the finish action rather than generation so the buff can fire again on the next listen session.
    public class JobDriver_ListeningToAlbum : JobDriver
    {
        public const TargetIndex AlbumIndex = TargetIndex.A;
        public const TargetIndex SurfaceIndex = TargetIndex.B;

        private const int ManualListenTicks = 4000;
        private const int ChairSearchRadius = 32;
        private const int UrgentJobCheckIntervalTicks = 600; // 10 seconds at 60 ticks/second, same interval vanilla reading uses

        private bool hasInInventory;
        private bool carrying;

        public MusicAlbum Album => job.GetTarget(AlbumIndex).Thing as MusicAlbum;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Album, job, 1, 1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.count = 1;
            hasInInventory = pawn.inventory != null && pawn.inventory.Contains(Album);
            carrying = pawn?.carryTracker.CarriedThing == Album;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            SetFinalizerJob(delegate (JobCondition condition)
            {
                if (!pawn.IsCarryingThing(Album)) return null;
                if (condition != JobCondition.Succeeded)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out _);
                    return null;
                }
                return HaulAIUtility.HaulToStorageJob(pawn, Album, forced: false);
            });

            foreach (Toil t in PrepareToListenToAlbum())
                yield return t;

            int duration = job.playerForced ? ManualListenTicks : job.def.joyDuration;
            yield return ListenToAlbum(duration);
        }

        private IEnumerable<Toil> PrepareToListenToAlbum()
        {
            if (!carrying)
            {
                if (hasInInventory)
                {
                    yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, AlbumIndex);
                }
                else
                {
                    yield return Toils_Goto
                        .GotoCell(Album.PositionHeld, PathEndMode.ClosestTouch)
                        .FailOnDestroyedOrNull(AlbumIndex)
                        .FailOnSomeonePhysicallyInteracting(AlbumIndex);

                    yield return Toils_Haul.StartCarryThing(
                        AlbumIndex,
                        putRemainderInQueue: false,
                        subtractNumTakenFromJobCount: false,
                        failIfStackCountLessThanJobCount: false,
                        reserve: true,
                        canTakeFromInventory: true);
                }

                yield return CarryToListeningSpot().FailOnDestroyedOrNull(AlbumIndex);
                yield return FindAdjacentSurface();
            }
        }

        private Toil ListenToAlbum(int duration)
        {
            Toil toil = Toils_General.Wait(duration);
            toil.debugName = "ListeningToAlbum";
            toil.FailOnDestroyedNullOrForbidden(AlbumIndex);
            toil.handlingFacing = true;

            toil.initAction = delegate
            {
                Album.IsPlaying = true;
                pawn.pather.StopDead();
                job.showCarryingInspectLine = false;
            };

            toil.tickIntervalAction = delegate (int delta)
            {
                if (job.GetTarget(SurfaceIndex).IsValid)
                    pawn.rotationTracker.FaceCell(job.GetTarget(SurfaceIndex).Cell);
                else if (Album.Spawned)
                    pawn.rotationTracker.FaceCell(Album.Position);
                else if (pawn.Rotation == Rot4.North)
                    pawn.Rotation = new Rot4(Rand.Range(1, 4));

                float roomBonus = AlbumUtility.GetListeningBonus(Album);
                Album.OnAlbumListenTick(pawn, delta, roomBonus);

                if (pawn.CurJob != null && pawn.needs?.joy != null)
                {
                    JoyTickFullJoyAction fullJoyAction = job.playerForced
                        ? JoyTickFullJoyAction.None
                        : JoyTickFullJoyAction.GoToNextToil;

                    JoyUtility.JoyTickCheckEnd(
                        pawn, delta, fullJoyAction, Album.JoyFactor * roomBonus);
                }

                pawn.GainComfortFromCellIfPossible(delta);

                if (pawn.IsHashIntervalTick(UrgentJobCheckIntervalTicks, delta))
                    pawn.jobs.CheckForJobOverride(9.1f);
            };

            toil.AddEndCondition(() =>
                AlbumUtility.CanListenToAlbum(Album, pawn, out _)
                    ? JobCondition.Ongoing
                    : JobCondition.InterruptForced);

            toil.AddFinishAction(delegate
            {
                Album.IsPlaying = false;

                // TaleRecorder skipped for now - I would need a proper TaleData_ListenedToAlbum class

                JoyUtility.TryGainRecRoomThought(pawn);

                foreach (AlbumOutcomeDoer doer in Album.AlbumComp.Doers)
                {
                    // Inspiration rolls need the pawn in scope so I handle it here rather than inside the doer itself.
                    if (doer is AlbumOutcomeDoer_Inspiration inspirationDoer)
                    {
                        if (pawn.mindState?.inspirationHandler != null
                            && !pawn.mindState.inspirationHandler.Inspired
                            && Rand.Chance(inspirationDoer.Chance))
                        {
                            InspirationDef def = pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                            if (def != null)
                                pawn.mindState.inspirationHandler.TryStartInspiration(def,
                                    "AlbumInspirationReason".Translate(Album.Title));
                        }
                    }

                    doer.Reset();
                }
            });

            return toil;
        }

        // Spot-finding lifted from JobDriver_Reading - sitting to listen works the same way.
        private Toil CarryToListeningSpot()
        {
            Toil toil = ToilMaker.MakeToil("CarryToListeningSpot");
            toil.initAction = delegate
            {
                if (!TryGetClosestChairFreeSittingSpot(skipInteractionCells: true, out IntVec3 cell)
                 && !TryGetClosestChairFreeSittingSpot(skipInteractionCells: false, out cell))
                {
                    cell = RCellFinder.SpotToChewStandingNear(
                        pawn, Album,
                        c => !c.Fogged(pawn.Map) && pawn.CanReserveSittableOrSpot(c));
                }

                if (!cell.IsValid)
                    pawn.pather.StartPath(pawn.Position, PathEndMode.OnCell);
                else
                {
                    pawn.ReserveSittableOrSpot(cell, pawn.CurJob);
                    pawn.Map.pawnDestinationReservationManager.Reserve(pawn, pawn.CurJob, cell);
                    pawn.pather.StartPath(cell, PathEndMode.OnCell);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        private bool TryGetClosestChairFreeSittingSpot(bool skipInteractionCells, out IntVec3 cell)
        {
            Thing thing = GenClosest.ClosestThingReachable(
                pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                ChairSearchRadius,
                t => ValidateChair(t, pawn, skipInteractionCells)
                  && t.Position.GetDangerFor(pawn, t.Map) == Danger.None);

            if (thing != null)
                return TryFindFreeSittingSpotOnThing(thing, pawn, skipInteractionCells, out cell);

            cell = IntVec3.Invalid;
            return false;
        }

        private Toil FindAdjacentSurface()
        {
            Toil toil = ToilMaker.MakeToil("FindAdjacentSurface");
            toil.initAction = delegate
            {
                Building firstThing = pawn.Position.GetFirstThing<Building>(pawn.Map);
                if (firstThing?.def?.building != null && firstThing.def.building.isSittable)
                {
                    if (!TryFaceClosestSurface(pawn.Position, pawn.Map))
                    {
                        job.SetTarget(SurfaceIndex, pawn.Position + firstThing.Rotation.FacingCell);
                        pawn.jobs.curDriver.rotateToFace = SurfaceIndex;
                    }
                }
                else
                {
                    TryFaceClosestSurface(pawn.Position, pawn.Map);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool TryFaceClosestSurface(IntVec3 pos, Map map)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 adj = pos + new Rot4(i).FacingCell;
                if (adj.GetSurfaceType(map) == SurfaceType.Eat)
                {
                    job.SetTarget(SurfaceIndex, adj);
                    pawn.jobs.curDriver.rotateToFace = SurfaceIndex;
                    return true;
                }
            }
            for (int j = 0; j < 4; j++)
            {
                IntVec3 adj = pos + new Rot4(j).FacingCell;
                if (adj.GetSurfaceType(map) == SurfaceType.Item)
                {
                    job.SetTarget(SurfaceIndex, adj);
                    pawn.jobs.curDriver.rotateToFace = SurfaceIndex;
                    return true;
                }
            }
            return false;
        }

        private static bool ValidateChair(Thing t, Pawn pawn, bool skipInteractionCells)
        {
            if (t.def.building == null || !t.def.building.isSittable) return false;
            if (!TryFindFreeSittingSpotOnThing(t, pawn, skipInteractionCells, out _)) return false;
            if (t.Fogged()) return false;
            if (t.IsForbidden(pawn)) return false;
            if (!pawn.CanReserve(t)) return false;
            if (!t.IsSociallyProper(pawn)) return false;
            if (t.IsBurning()) return false;
            if (t.HostileTo(pawn)) return false;
            return true;
        }

        private static bool TryFindFreeSittingSpotOnThing(Thing t, Pawn pawn,
            bool skipInteractionCells, out IntVec3 cell)
        {
            foreach (IntVec3 c in t.OccupiedRect())
            {
                if ((!skipInteractionCells || !c.IsBuildingInteractionCell(pawn.Map))
                 && !c.Fogged(pawn.Map)
                 && pawn.CanReserveSittableOrSpot(c))
                {
                    cell = c;
                    return true;
                }
            }
            cell = default;
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref carrying, "carrying", false);
            Scribe_Values.Look(ref hasInInventory, "hasInInventory", false);
        }
    }
}
