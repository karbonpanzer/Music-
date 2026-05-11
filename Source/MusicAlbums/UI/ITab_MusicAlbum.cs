using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MusicAlbums
{
    // I can't call BookUIUtility directly since its methods take a Book, so I'm replicating the same three-panel layout from ITab_Book here with the rect math copied verbatim so it looks identical.
    public class ITab_MusicAlbum : ITab
    {
        private Vector2 descScroll;

        private const float TopPadding = 20f;
        private const float InitialHeight = 350f;
        private const float TitleHeight = 30f;
        private const float InitialWidth = 610f;

        public ITab_MusicAlbum()
        {
            size = new Vector2(Mathf.Min(InitialWidth, UI.screenWidth), InitialHeight);
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
            DrawBookInfoPanel(infoRect, album);
            DrawBookDescPanel(descRect, album, ref descScroll);
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

        private static void DrawBookInfoPanel(Rect rect, MusicAlbum album)
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
            float num = 0f;
            foreach (Dialog_InfoCard.Hyperlink item in album.GetHyperlinks().Reverse())
            {
                float num2 = Text.CalcHeight(item.Label, rect.width);
                Rect linkRect = rect;
                linkRect.y = rect.yMax - num2 - num - 4f;
                linkRect.height = num2;
                TaggedString taggedString = "ViewHyperlink".Translate(item.Label);
                Widgets.HyperlinkWithIcon(linkRect, item, taggedString, 2f, 6f, normalOptionColor);
                num += num2;
                y += num2;
            }
        }

        private static void DrawBookDescPanel(Rect rect, MusicAlbum album, ref Vector2 scroll)
        {
            Widgets.LabelScrollable(rect, album.FlavorUI, ref scroll);
        }
    }
}
