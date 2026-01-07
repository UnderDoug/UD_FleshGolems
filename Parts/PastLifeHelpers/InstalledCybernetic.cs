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
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;

namespace UD_FleshGolems.Parts.PastLifeHelpers
{
    [Serializable]
    public class InstalledCybernetic : IComposite
    {
        public GameObject Cybernetic;
        public string ImplantedLimbType;

        protected InstalledCybernetic()
        {
            Cybernetic = null;
            ImplantedLimbType = null;
        }
        public InstalledCybernetic(GameObject Cybernetic, string ImplantedPart)
            : this()
        {
            this.Cybernetic = Cybernetic;
            ImplantedLimbType = ImplantedPart;
        }
        public InstalledCybernetic(GameObject Cybernetic, BodyPart ImplantedPart)
            : this(Cybernetic, ImplantedPart.Type) { }

        public InstalledCybernetic(GameObject Cybernetic, Body ImplantedBody)
            : this(Cybernetic, ImplantedBody.FindCybernetics(Cybernetic)) { }

        public InstalledCybernetic(GameObject Cybernetic)
            : this(Cybernetic, Cybernetic?.Implantee?.Body) { }

        public InstalledCybernetic(KeyValuePair<GameObject, string> SourcePair)
            : this(SourcePair.Key, SourcePair.Value) { }

        public void Deconstruct(out GameObject Cybernetic, out string ImplantedLimbType)
        {
            Cybernetic = this.Cybernetic;
            ImplantedLimbType = this.ImplantedLimbType;
        }

        public override string ToString()
            => ImplantedLimbType + ": " + (Cybernetic?.DebugName ?? NULL);

        public KeyValuePair<GameObject, string> GetKeyValuePair()
            => new(Cybernetic, ImplantedLimbType);
        public static InstalledCybernetic NewFromKeyValuePair(KeyValuePair<GameObject, string> SourcePair)
            => new(SourcePair);
    }


}
