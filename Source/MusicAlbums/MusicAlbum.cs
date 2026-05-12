using System.Collections.Generic;
using System.Text;
using MusicAlbums.Comps;
using MusicAlbums.Doers;
using MusicAlbums.Utility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace MusicAlbums
{
    // I'm keeping this close to Book so it feels native to the codebase. IsOpen becomes IsPlaying,mentalBreakChance is dropped because that's an Anomaly hook I don't need, and the subject/topic symbol system is skipped because albums don't have skill topics the way novels do.
    public class MusicAlbum : ThingWithComps
    {
        private string title;
        private bool isPlaying;
        private bool descCanBeInvalidated;
        private float joyFactor = 1f;
        private string descriptionFlavor;
        private string description;

        private Graphic cachedPlayingGraphic;
        private Graphic cachedVerticalGraphic;
        private CompMusicAlbum cachedComp;

        public CompMusicAlbum AlbumComp =>
            cachedComp ?? (cachedComp = GetComp<CompMusicAlbum>());

        private Graphic PlayingGraphic =>
            cachedPlayingGraphic ??
            (cachedPlayingGraphic = AlbumComp.Props.openGraphic?.Graphic);

        public Graphic VerticalGraphic =>
            cachedVerticalGraphic ??
            (cachedVerticalGraphic = AlbumComp.Props.verticalGraphic?.Graphic);

        public float JoyFactor => joyFactor;
        public string Title => title;

        public bool IsPlaying
        {
            get => isPlaying;
            set => isPlaying = value;
        }

        public override string LabelNoCount =>
            title + GenLabel.LabelExtras(this, includeHp: true, includeQuality: true);

        public override string LabelNoParenthesis => title;

        public string FlavorUI => descriptionFlavor;

        public override string DescriptionFlavor => DescriptionDetailed;

        public override string DescriptionDetailed
        {
            get
            {
                EnsureDescriptionUpToDate();
                return description;
            }
        }

        // If there's no CompQuality I generate immediately. Otherwise I wait for PostQualitySet so the quality is known before the grammar runs, generating before quality is set would give everything Normal-quality descriptions regardless of what it actually rolls.
        public override void PostPostMake()
        {
            base.PostPostMake();
            if (!this.HasComp<CompQuality>())
                GenerateAlbum();
        }

        public override void PostQualitySet()
        {
            base.PostQualitySet();
            GenerateAlbum();
        }

        public override bool CanStackWith(Thing other) => false;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (isPlaying && PlayingGraphic != null)
            {
                Rot4 rot = (base.ParentHolder is Pawn_CarryTracker carrier)
                    ? carrier.pawn.Rotation
                    : base.Rotation;
                PlayingGraphic.Draw(drawLoc, flip ? rot.Opposite : rot, this);
            }
            else
            {
                base.DrawAt(drawLoc, flip);
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            FloatMenuOption option = new FloatMenuOption(
                "AssignListenNow".Translate(Label),
                () => PawnListenNow(selPawn));

            if (!AlbumUtility.CanListenToAlbum(this, selPawn, out string reason))
            {
                option.Label = string.Format("{0}: {1}", "AssignCannotListenNow".Translate(Label), reason);
                option.Disabled = true;
            }

            Pawn reserver = selPawn.Map.reservationManager.FirstRespectedReserver(this, selPawn)
                         ?? selPawn.Map.physicalInteractionReservationManager.FirstReserverOf(this);
            if (reserver != null)
                option.Label += string.Format(" ({0})", "ReservedBy".Translate(reserver.LabelShort, reserver));

            option.iconThing = this;
            yield return option;

            foreach (FloatMenuOption baseOption in base.GetFloatMenuOptions(selPawn))
            {
                baseOption.iconThing = this;
                yield return baseOption;
            }
        }

        public void PawnListenNow(Pawn pawn)
        {
            pawn.jobs.TryTakeOrderedJob(
                JobMaker.MakeJob(MusicAlbumsDefOf.ListenToAlbum, this),
                JobTag.Misc);
        }

        // I'm reusing ReadingSpeed as the engagement stat rather than defining a new one. ListeningSpeed would just be a copy of ReadingSpeed with a different name.
        public void OnAlbumListenTick(Pawn pawn, int delta, float roomBonusFactor)
        {
            float factor = pawn.GetStatValue(StatDefOf.ReadingSpeed) * roomBonusFactor * delta;
            foreach (AlbumOutcomeDoer doer in AlbumComp.Doers)
                doer.OnReadingTick(pawn, factor);
        }

        public bool ProvidesOutcome(Pawn listener)
        {
            foreach (AlbumOutcomeDoer doer in AlbumComp.Doers)
                if (doer.DoesProvideOutcome(listener)) return true;
            return false;
        }

        public void SetJoyFactor(float factor) => joyFactor = factor;

        public IEnumerable<Dialog_InfoCard.Hyperlink> GetHyperlinks()
        {
            foreach (AlbumOutcomeDoer doer in AlbumComp.Doers)
                foreach (Dialog_InfoCard.Hyperlink link in doer.GetHyperlinks())
                    yield return link;
        }

        public virtual void GenerateAlbum(Pawn artist = null, long? fixedDate = null)
        {
            GrammarRequest common = default;

            long absTicks = fixedDate ?? (GenTicks.TicksAbs
                - (long)(AlbumComp.Props.ageYearsRange.RandomInRange * 3600000f));
            common.Rules.Add(new Rule_String("date", GenDate.DateFullStringAt(absTicks, Vector2.zero)));
            common.Rules.Add(new Rule_String("date_season", GenDate.DateMonthYearStringAt(absTicks, Vector2.zero)));

            if (this.HasComp<CompQuality>())
                common.Constants.Add("quality", ((int)GetComp<CompQuality>().Quality).ToString());

            TaleData_Pawn pawnData = (artist == null)
                ? TaleData_Pawn.GenerateRandom(humanLike: true)
                : TaleData_Pawn.GenerateFrom(artist);
            foreach (Rule rule in pawnData.GetRules("ANYPAWN", common.Constants))
                common.Rules.Add(rule);

            AppendDoerRules(artist, ref common);

            GrammarRequest titleReq = common;
            titleReq.Includes.Add(AlbumComp.Props.nameMaker);
            title = GenText.CapitalizeAsTitle(
                GrammarResolver.Resolve("title", titleReq)).StripTags();

            GrammarRequest descReq = common;
            descReq.Includes.Add(AlbumComp.Props.descriptionMaker);
            descReq.Includes.Add(RulePackDefOf.TalelessImages);
            descReq.Includes.Add(RulePackDefOf.ArtDescriptionRoot_Taleless);
            descReq.Includes.Add(RulePackDefOf.ArtDescriptionUtility_Global);
            descriptionFlavor = GrammarResolver.Resolve("desc", descReq).StripTags();

            description = GenerateFullDescription();
        }

        private void AppendDoerRules(Pawn artist, ref GrammarRequest common)
        {
            foreach (AlbumOutcomeDoer doer in AlbumComp.Doers)
            {
                doer.Reset();
                doer.OnAlbumGenerated(artist);

                IEnumerable<Rule_String> extraRules = doer.GetTopicRuleStrings();
                if (extraRules == null) continue;
                foreach (Rule_String rule in extraRules)
                    common.Rules.Add(rule);
            }
        }

        private void EnsureDescriptionUpToDate()
        {
            if (descCanBeInvalidated)
                description = GenerateFullDescription();
        }

        private string GenerateFullDescription()
        {
            StringBuilder sb = new StringBuilder();
            descCanBeInvalidated = false;

            sb.AppendLine(title.Colorize(ColoredText.TipSectionTitleColor)
                + GenLabel.LabelExtras(this, includeHp: false, includeQuality: true) + "\n");
            sb.AppendLine(descriptionFlavor + "\n");

            foreach (AlbumOutcomeDoer doer in AlbumComp.Doers)
            {
                string benefit = doer.GetBenefitsString();
                if (!string.IsNullOrEmpty(benefit))
                {
                    if (doer.BenefitDetailsCanChange())
                        descCanBeInvalidated = true;
                    sb.AppendLine(benefit);
                }
            }

            return sb.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref descriptionFlavor, "descriptionFlavor");
            Scribe_Values.Look(ref joyFactor, "joyFactor", 0f);
            Scribe_Values.Look(ref isPlaying, "isPlaying", defaultValue: false);
            Scribe_Values.Look(ref descCanBeInvalidated, "descCanBeInvalidated", defaultValue: false);
            Scribe_Values.Look(ref description, "description");
        }
    }
}
