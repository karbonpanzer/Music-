using MusicAlbums.Doers;
using RimWorld;
using Verse;

namespace MusicAlbums.Comps
{
    // Extending CompProperties_Readable directly gets me the doers list, developmentalStageFilter, and mentalBreakIntensity for free.
    public class CompProperties_MusicAlbum : CompProperties_Readable
    {
        public RulePackDef nameMaker;
        public RulePackDef descriptionMaker;
        public FloatRange ageYearsRange = new FloatRange(5f, 50f);
        public float pickWeight = 1f;
        public GraphicData openGraphic;
        public GraphicData verticalGraphic;

        public CompProperties_MusicAlbum()
        {
            compClass = typeof(CompMusicAlbum);
        }
    }
}
