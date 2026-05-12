using System.Collections.Generic;
using MusicAlbums.Comps;
using MusicAlbums.Doers;
using RimWorld;
using UnityEngine;
using Verse;

namespace MusicAlbums.UI
{
    // BookUIUtility only accepts Book, so I can't reuse it here. I've copied the three-panel layout from ITab_Book with the rect math left identical so the two tabs look the same.
    public class ITab_MusicAlbum : ITab
    {
        private Vector2 descScroll;

        private const float TopPadding = 20f;
        private const float InitialHeight = 350f;
        private const float TitleHeight = 30f;
        private const float InitialWidth = 610f;

        public ITab_MusicAlbum()
        {
            size = new Vector2(Mathf.Min(InitialWidth, Verse.UI.screenWidth), InitialHeight);
            labelKey = "TabAlbumContents";
        }

        public override bool IsVisible => base.SelThing is MusicAlbum;

        protected override void FillTab()
        {
            if (base.SelThing is not MusicAlbum album) return;

            Rect outer = new Rect(0f, TopPadding, size.x, size.y - TopPadding).ContractedBy(10f);

            Rect titleRect = outer;
            titleRect.y = 10f;
            titleRect.height = TitleHeight;

            Rect infoRect = outer;
            infoRect.xMax = outer.center.x - 17f;
            infoRect.y = titleRect.yMax + 17f;
            infoRect.yMax = outer.yMax;

            Rect descRect = outer;
            descRect.x = infoRect.xMax + 20f;
            descRect.xMax = outer.xMax + 10f;
            descRect.y = titleRect.yMax + 26f;
            descRect.yMax = outer.yMax;

            DrawTitle(titleRect, album);
            DrawInfoPanel(infoRect, album);
            DrawDescPanel(descRect, album, ref descScroll);
        }

        private static void DrawTitle(Rect rect, MusicAlbum album)
        {
            Rect iconRect = rect;
            iconRect.width = iconRect.height;
            GUI.DrawTexture(iconRect, album.def.uiIcon);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            Rect labelRect = rect;
            labelRect.x += rect.height + 10f;
            labelRect.width -= rect.height + 10f;
            Widgets.LabelFit(labelRect, album.LabelCap);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawInfoPanel(Rect rect, MusicAlbum album)
        {
            float y = rect.y;
            DrawBenefits(rect, ref y, album);
            Rect hyperlinkRect = rect;
            hyperlinkRect.y = y;
            hyperlinkRect.yMax = rect.yMax;
            DrawHyperlinks(hyperlinkRect, ref y, album);
        }

        private static void DrawBenefits(Rect rect, ref float y, MusicAlbum album)
        {
            bool hasAny = false;
            foreach (AlbumOutcomeDoer doer in album.AlbumComp.Doers)
            {
                if (!string.IsNullOrEmpty(doer.GetBenefitsString()))
                {
                    hasAny = true;
                    break;
                }
            }
            if (!hasAny) return;

            DrawSubheader(rect, ref y, "Benefits".Translate());
            y += 10f;
            foreach (AlbumOutcomeDoer doer in album.AlbumComp.Doers)
            {
                string benefit = doer.GetBenefitsString();
                if (!string.IsNullOrEmpty(benefit))
                    Widgets.Label(rect, ref y, benefit);
            }
            y += 10f;
        }

        private static void DrawSubheader(Rect rect, ref float y, string title)
        {
            Rect groupRect = new Rect
            {
                x = rect.x,
                y = y,
                xMax = rect.xMax,
                yMax = rect.yMax
            };
            GUI.BeginGroup(groupRect);
            float curY = 0f;
            Widgets.ListSeparator(ref curY, groupRect.width, title);
            y += curY;
            GUI.EndGroup();
        }

        private static void DrawHyperlinks(Rect rect, ref float y, MusicAlbum album)
        {
            Color normalOptionColor = Widgets.NormalOptionColor;
            float offset = 0f;

            // Hyperlinks render bottom-up so I iterate in reverse.
            List<Dialog_InfoCard.Hyperlink> links = new List<Dialog_InfoCard.Hyperlink>(album.GetHyperlinks());
            for (int i = links.Count - 1; i >= 0; i--)
            {
                Dialog_InfoCard.Hyperlink item = links[i];
                float height = Text.CalcHeight(item.Label, rect.width);
                Rect linkRect = rect;
                linkRect.y = rect.yMax - height - offset - 4f;
                linkRect.height = height;
                TaggedString label = "ViewHyperlink".Translate(item.Label);
                Widgets.HyperlinkWithIcon(linkRect, item, label, 2f, 6f, normalOptionColor);
                offset += height;
                y += height;
            }
        }

        private static void DrawDescPanel(Rect rect, MusicAlbum album, ref Vector2 scroll)
        {
            Widgets.LabelScrollable(rect, album.FlavorUI, ref scroll);
        }
    }
}
