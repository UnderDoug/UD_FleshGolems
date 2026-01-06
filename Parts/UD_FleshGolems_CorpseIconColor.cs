using System;
using System.Collections.Generic;
using System.Text;

using UD_FleshGolems;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_CorpseIconColor : IIconColorPart
    {
        public const string SET_COLORS_PROPTAG = "UD_FleshGolems CorpseIconColor";

        public UD_FleshGolems_CorpseIconColor()
        {
            SetTileColor("&r");
        }
        public UD_FleshGolems_CorpseIconColor(string TileColor, string DetailColor, bool DoDetail = true)
        {
            SetTileColor(TileColor);
            if (DoDetail)
            {
                SetDetailColor(DetailColor);
            }
        }
        public UD_FleshGolems_CorpseIconColor(GameObjectBlueprint Blueprint, bool DoDetail = true)
            : this(
                  TileColor: Blueprint?.GetPartParameter<string>(nameof(Parts.Render), nameof(Parts.Render.TileColor)),
                  DetailColor: Blueprint?.GetPartParameter<string>(nameof(Parts.Render), nameof(Parts.Render.DetailColor)),
                  DoDetail: DoDetail)
        {
        }
        public UD_FleshGolems_CorpseIconColor(string Blueprint)
            : this(GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint)) { }

        public UD_FleshGolems_CorpseIconColor(GameObject Corpse, bool DoDetail = true)
            : this(Corpse?.Render?.TileColor, Corpse?.Render?.DetailColor, DoDetail)
        {
            Dictionary<string, string> setColors = null;
            if (setColors.IsNullOrEmpty()
                && Corpse.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                && pastLife.GetBlueprint()?.GetPropertyOrTag(SET_COLORS_PROPTAG) is string setColorsPropTagByPastLifeBlueprint)
            {
                setColors = setColorsPropTagByPastLifeBlueprint.CachedDictionaryExpansion();
            }
            if (setColors.IsNullOrEmpty()
                && Corpse.GetPropertyOrTag(SET_COLORS_PROPTAG) is string setColorsPropTagByCorpseObject)
            {
                setColors = setColorsPropTagByCorpseObject.CachedDictionaryExpansion();
            }
            if (!setColors.IsNullOrEmpty())
            {
                if (setColors.ContainsKey("TileColor"))
                {
                    SetTileColor("&" + setColors["TileColor"][^1]);
                }
                if (DoDetail && setColors.ContainsKey("DetailColor"))
                {
                    SetDetailColor(setColors["DetailColor"][^1].ToString());
                }
            }
        }

        public UD_FleshGolems_CorpseIconColor SetTileColor(string TileColor)
        {
            if (!TileColor.IsNullOrEmpty())
            {
                TextForeground = TileColor;
                TextForegroundPriority = 92;
                TileForeground = TileColor;
                TileForegroundPriority = 92;
            }
            return this;
        }
        public UD_FleshGolems_CorpseIconColor SetDetailColor(string DetailColor)
        {
            if (!DetailColor.IsNullOrEmpty())
            {
                TileDetail = DetailColor;
                TileDetailPriority = 91;
            }
            return this;
        }

        public UD_FleshGolems_CorpseIconColor SetTileColorFromBlueprint(GameObjectBlueprint Blueprint)
        {
            if (Blueprint != null
                && Blueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.TileColor), out string tileColor))
            {
                SetTileColor(tileColor);
            }
            return this;
        }
        public UD_FleshGolems_CorpseIconColor SetTileColorFromBlueprint(string Blueprint)
            => SetTileColorFromBlueprint(GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint));

        public UD_FleshGolems_CorpseIconColor SetDetailColorFromBlueprint(GameObjectBlueprint Blueprint)
        {
            if (Blueprint != null
                && Blueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.DetailColor), out string detailColor))
            {
                SetDetailColor(detailColor);
            }
            return this;
        }
        public UD_FleshGolems_CorpseIconColor SetDetailColorFromBlueprint(string Blueprint)
            => SetDetailColorFromBlueprint(GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint));

        public UD_FleshGolems_CorpseIconColor SetColorsFromBlueprint(GameObjectBlueprint Blueprint)
            => SetTileColorFromBlueprint(Blueprint).SetDetailColorFromBlueprint(Blueprint);

        public UD_FleshGolems_CorpseIconColor SetColorsFromBlueprint(string Blueprint)
            => SetTileColorFromBlueprint(Blueprint).SetDetailColorFromBlueprint(Blueprint);
    }
}
