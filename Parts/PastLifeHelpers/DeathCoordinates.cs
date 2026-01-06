using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;
using Qud.API;

using XRL.UI;
using XRL.Wish;
using XRL.Rules;
using XRL.Language;
using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using UD_FleshGolems.Capabilities;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using UD_FleshGolems.Capabilities.Necromancy;
using XRL.World.Effects;
using XRL.World.Capabilities;
using XRL;

namespace UD_FleshGolems.Parts.PastLifeHelpers
{
    [Serializable]
    public struct DeathCoordinates : IComposite
    {
        public string DeathZone;
        public int X;
        public int Y;

        public DeathCoordinates(string DeathZone, int X, int Y)
            : this()
        {
            this.DeathZone = DeathZone;
            this.X = X;
            this.Y = Y;
        }
        public DeathCoordinates(string DeathZone, Location2D DeathLocation)
            : this(DeathZone, DeathLocation.X, DeathLocation.Y) { }

        public readonly Location2D GetLocation()
            => new(X, Y);

        public readonly ZoneRequest GetZoneRequest()
            => new(DeathZone);

        public readonly Cell GetCell()
            => The.ZoneManager?.GetZone(DeathZone)?.GetCell(X, Y);

        public static explicit operator Cell(DeathCoordinates Source)
            => Source.GetCell();

        public override readonly string ToString()
            => DeathZone + "[" + X + "," + Y + "]";
    }
}
