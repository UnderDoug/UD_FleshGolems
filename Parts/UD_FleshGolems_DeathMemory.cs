using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;
using Qud.API;

using XRL;
using XRL.UI;
using XRL.Wish;
using XRL.Rules;
using XRL.Language;
using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;
using XRL.World.Effects;
using XRL.World.Capabilities;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;
using static XRL.World.Parts.UD_FleshGolems_DestinedForReanimation;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using UD_FleshGolems.Parts.VengeanceHelpers;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_DeathMemory : IScribedPart
    {
        public GameObject Killer;

        public GameObject Weapon;

        public DeathMemory DeathMemory;

        public KillerDetails? KillerDetails;

        public DeathDescription DeathDescription;

        public bool Accidental;

        public bool Environmental => DeathDescription?.Method == "";

        public UD_FleshGolems_DeathMemory()
        {
            Killer = null;
            Weapon = null;

            DeathMemory = default;
            KillerDetails = null;
            DeathDescription = null;

            Accidental = false;
        }

        public bool Initialize(IDeathEvent DeathEvent)
        {
            if (ParentObject is not GameObject corpse
                || DeathEvent == null)
                return false;

            Killer = DeathEvent.Killer;
            Weapon = DeathEvent.Weapon;

            DeathMemory = DeathMemory.Make(corpse, DeathEvent, out KillerDetails, out DeathDescription);

            Accidental = DeathEvent.Accidental;
            return true;
        }

        public bool Initialize(GameObject Killer, GameObject Weapon, GameObject Projectile, DeathDescription DeathDescription, bool Accidental)
        {
            if (DeathDescription == null)
                return false; 

            this.Killer = Killer;
            this.Weapon = Weapon ?? Projectile;

            if (Killer != null)
                KillerDetails = new(Killer);

            this.DeathDescription = DeathDescription;

            DeathMemory = DeathMemory.Make(ParentObject, Killer, Weapon ?? Projectile, KillerDetails, DeathDescription);

            this.Accidental = Accidental;

            return true;
        }

        public override bool AllowStaticRegistration()
            => true;

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == GetDebugInternalsEvent.ID
            ;

        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Killer), Killer?.DebugName ?? NULL);
            E.AddEntry(this, nameof(Weapon), Weapon?.DebugName ?? NULL);
            E.AddEntry(this, nameof(DeathMemory), DeathMemory.DebugInternals()?.Aggregate("", (a, n) => a + "\n" + n.Value + " " + n.Key));
            E.AddEntry(this, nameof(KillerDetails), KillerDetails?.DebugInternals()?.Aggregate("", (a, n) => a + "\n" + n.Key + ": " + n.Value) ?? NULL);
            E.AddEntry(this, nameof(DeathDescription), DeathDescription?.DebugInternals()?.Aggregate("", (a, n) => a + "\n" + n.Key + ": " + n.Value) ?? NULL);
            E.AddEntry(this, Accidental.YehNah(), nameof(Accidental));
            E.AddEntry(this, Environmental.YehNah(), nameof(Environmental));
            return base.HandleEvent(E);
        }
    }
}
