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
using static XRL.World.Parts.UD_FleshGolems_VengeanceAssistant;
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
    public class UD_FleshGolems_DeathDetails : IScribedPart
    {
        public static Dictionary<string, List<string>> CategorizedEvidenceOfDeath = new()
        {
            {   // Heat damage w/ NoBurn (only steam)
                "cooked", new()
                {
                    "burn-scars",
                    "charred =subject.uD_xTag:TextFragments:Skin=",
                }
            },
            {   // Heat damage w/o NoBurn
                "immolated", new()
                {
                    "charred =subject.uD_xTag:TextFragments:Skin=",
                    "scorch-marks",
                }
            },
            {   // Plasma damage
                "plasma-burned to death", new()
                {
                    "scorch-marks",
                    "an {{auroral|auroral}} after-glow",
                }
            },
            {   // Cold damage
                "frozen to death", new()
                {
                    "frost-bitten extremities",
                    "an icy countenance",
                }
            },
            {   // Electric damage
                "electrocuted", new()
                {
                    "electrical burns",
                    "lightning-shaped scars",
                }
            },
            {   // Thirst
                "thirst", new()
                {
                    "sunken features",
                    "signs of dessication",
                }
            },
            {   // Poison damage
                "died of poison", new()
                {
                    "sunken features",
                    "a palid complexion",
                    "blackened extremities",
                }
            },
            {   // Bleeding damage
                "bled to death", new()
                {
                    "hollow features",
                    "a palid complexion",
                }
            },
            {   // Metabolic damage (hulk honey)
                "failed", new()
                {
                    "bent limbs",
                    "exaggerated features",
                }
            },
            {   // Asphyxiation damage (osseous ash)
                "died of asphyxiation", new()
                {
                    "bulging features",
                }
            },
            {   // Psionic damage
                "psychically extinguished", new()
                {
                    "a far-away stare",
                    "a haunted affect",
                }
            },
            {   // Drain damage (syphon vim)
                "drained to extinction", new()
                {
                    "hollow features",
                    "sunken features",
                    "a palid complexion",
                }
            },
            {   // Thorns damage
                "pricked to death", new()
                {
                    "puncture scars",
                    "holes where they shouldn't be",
                }
            },
            {   // Bite damage (any bite)
                "bitten to death", new()
                {
                    "bite-marks",
                    "teeth-marks",
                    "pieces missing",
                }
            },
            {   // Killed
                "killed", new()
                {
                    "battle scars",
                    "signs of a lost fight",
                }
            },
        };
        public static List<string> GenericEvidenceOfDeath = new()
        {
            "mangled limbs",
            "open wounds",
            "visible decay",
        };

        private bool _Init;

        public bool Init
        {
            get => _Init;
            private set => _Init = value;
        }

        public static List<int> KillerEventIDs => new()
        {
            ReplaceInContextEvent.ID,
            OnDeathRemovalEvent.ID,
        };
        private GameObject _Killer;
        public GameObject Killer
        {
            get => _Killer;
            protected set => UpdateKiller(value);
        }
        public bool KillerIsCached;

        public GameObject Weapon;

        public DeathMemory DeathMemory;

        public KillerDetails KillerDetails;

        public DeathDescription DeathDescription;

        public bool Accidental;

        public bool Environmental => DeathDescription?.Method == "";

        public bool DeathQuestionsAreRude => DeathMemory != null && DeathMemory.GetIsRudeToAsk();

        public List<string> EvidenceOfDeath;

        public UD_FleshGolems_DeathDetails()
        {
            Init = false;

            Killer = null;
            KillerIsCached = false;
            Weapon = null;

            DeathMemory = default;
            KillerDetails = null;
            DeathDescription = null;

            Accidental = false;

            EvidenceOfDeath = null;
        }

        public bool Initialize(IDeathEvent DeathEvent)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(ParentObject?.DebugName ?? NULL),
                    Debug.Arg(nameof(Init), Init),
                    Debug.Arg(nameof(DeathEvent.Killer), DeathEvent?.Killer?.DebugName ?? NULL),
                    Debug.Arg(nameof(DeathEvent.Weapon), DeathEvent?.Weapon?.DebugName ?? NULL),
                    Debug.Arg(nameof(DeathEvent.Accidental), DeathEvent?.Accidental),
                });

            if (Init)
                return true;

            if (ParentObject is not GameObject corpse
                || DeathEvent == null)
                return false;

            Init = true;

            Killer = DeathEvent.Killer;
            Weapon = DeathEvent.Weapon;

            DeathMemory = DeathMemory.Make(corpse, DeathEvent, out KillerDetails, out DeathDescription);

            Accidental = DeathEvent.Accidental;

            return true;
        }

        public bool Initialize(
            GameObject Killer,
            GameObject Weapon,
            GameObject Projectile,
            DeathDescription DeathDescription,
            bool Accidental,
            bool KillerIsCached)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(ParentObject?.DebugName ?? NULL),
                    Debug.Arg(nameof(Init), Init),
                    Debug.Arg(nameof(Killer), Killer?.DebugName ?? NULL),
                    Debug.Arg(nameof(Weapon), Weapon?.DebugName ?? NULL),
                    Debug.Arg(nameof(Projectile), Projectile?.DebugName ?? NULL),
                    Debug.Arg(nameof(DeathDescription), DeathDescription != null),
                    Debug.Arg(nameof(Accidental), Accidental),
                });

            if (Init)
                return true;

            if (ParentObject is not GameObject corpse
                || DeathDescription == null)
                return false;

            Init = true;

            this.Killer = Killer;
            this.KillerIsCached = KillerIsCached;
            this.Weapon = Weapon ?? Projectile;

            if (Killer != null)
                KillerDetails = new(ParentObject, Killer);

            this.DeathDescription = DeathDescription;

            DeathMemory = DeathMemory.Make(corpse, Killer, Weapon ?? Projectile, KillerDetails, DeathDescription);

            this.Accidental = Accidental;

            return true;
        }

        public string Killed(string Adverb = null)
        {
            if (Adverb.IsNullOrEmpty()
                && Accidental)
                Adverb = "accidentally";

            return DeathDescription?.GetKilled(Adverb);
        }

        public string WereKilled(string Adverb = null, bool FirstPerson = false)
        {
            string output = DeathDescription?.GetWas();

            if (!FirstPerson)
                output = DeathDescription?.GetWere()
                    ?.StartReplace()
                    ?.AddObject(ParentObject, "subject")
                    ?.ToString();

            return output + DeathDescription?.GetKilled(Adverb);
        }

        public string KnownKiller()
        {
            if (KillerDetails == null)
            {
                MetricsManager.LogModWarning(
                    mod: ThisMod,
                    Message: Name + " attempted to output generic killer for non-existant " + nameof(KillerDetails));
                return null;
            }
            if (!DeathMemory.RemembersKiller())
            {
                MetricsManager.LogModWarning(
                    mod: ThisMod, 
                    Message: Name + " attempted to output generic killer when " + nameof(DeathMemory) + " doesn't recall one");
                return null;
            }
            return KillerDetails?[DeathMemory];
        }

        public string KillerFeature()
        {
            if (DeathMemory.RemembersKillerFeature())
                return KillerDetails?.NotableFeature;

            return "someone with an enigmatic form";
        }

        public string KillerCreature()
        {
            if (DeathMemory.RemembersKillerCreature())
                return KillerDetails?.CreatureType;

            return "a mysterious entity";
        }

        public string KillerName()
        {
            if (DeathMemory.RemembersKillerName())
                return KillerDetails?.DisplayName;

            return "someone";
        }

        public string Method(bool WithIndefiniteArticle = false)
        {
            if (DeathMemory.RemembersMethod()
                && DeathDescription.GetMethod() is string method
                && !method.IsNullOrEmpty())
            {
                if (WithIndefiniteArticle)
                {
                    return DeathDescription.GetAMethod(null, DeathDescription.ForceNoMethodArticle);
                }
                return method;
            }

            return "strange method";
        }

        public string KilledWithMethod(string Adverb = null)
        {
            if (Adverb.IsNullOrEmpty()
                && Accidental)
                Adverb = "accidentally";

            return DeathDescription?.KilledWithMethod(Adverb);
        }

        public string WithMethod()
            => DeathDescription?.WithMethod();

        public GameObject UpdateKiller(GameObject Killer)
        {
            foreach (int eventID in KillerEventIDs)
                _Killer?.UnregisterEvent(this, eventID);

            _Killer = Killer;
            if (DeathDescription != null)
            {
                DeathDescription.SetKiller(Killer);
                if (KillerDetails != null)
                {
                    KillerDetails?.Update(Killer);
                }
                KillerDetails ??= new(ParentObject, Killer);
            }

            foreach (int eventID in KillerEventIDs)
                _Killer?.RegisterEvent(this, eventID);
            return Killer;
        }

        public GameObject UpdateWeapon(GameObject Weapon, bool OverrideNonEmptyMethod = true)
        {
            this.Weapon = Weapon;
            if (DeathDescription != null
                && OverrideNonEmptyMethod)
            {
                DeathDescription.SetMethod(Weapon);
            }
            return Weapon;
        }

        public List<string> GetEvidenceOfDeath()
        {
            if (EvidenceOfDeath == null)
            {
                EvidenceOfDeath = new();

                List<string> categorizedEvidenceList = null;
                if (DeathDescription?.Category is string category
                    && CategorizedEvidenceOfDeath.ContainsKey(category))
                    categorizedEvidenceList = new(CategorizedEvidenceOfDeath[category]);

                List<string> genericEvidenceList = new(GenericEvidenceOfDeath);

                if (!categorizedEvidenceList.IsNullOrEmpty())
                {
                    string categorizedEvidence = categorizedEvidenceList.GetRandomElementCosmetic();
                    categorizedEvidenceList.Remove(categorizedEvidence);
                    EvidenceOfDeath.Add(categorizedEvidence);
                    if (!categorizedEvidenceList.IsNullOrEmpty())
                    {
                        genericEvidenceList.AddRange(categorizedEvidenceList);
                    }
                }
                if (!genericEvidenceList.IsNullOrEmpty())
                {
                    string genericEvidence = null;
                    if (EvidenceOfDeath.Count < 1)
                    {
                        genericEvidence = genericEvidenceList.GetRandomElementCosmetic();
                        genericEvidenceList.Remove(genericEvidence);
                        EvidenceOfDeath.Add(genericEvidence);
                    }
                    if (!genericEvidenceList.IsNullOrEmpty()
                        && Stat.RollCached("1d3") == 1)
                    {
                        genericEvidence = genericEvidenceList.GetRandomElementCosmetic();
                        genericEvidenceList.Remove(genericEvidence);
                        EvidenceOfDeath.Add(genericEvidence);
                    }
                }
            }
            return EvidenceOfDeath;
        }

        public override bool AllowStaticRegistration()
            => true;
        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == ReplaceInContextEvent.ID
            || ID == GetExtraPhysicalFeaturesEvent.ID
            || ID == GetDebugInternalsEvent.ID
            ;
        public override bool HandleEvent(ReplaceInContextEvent E)
        {
            if (E.Object == Killer)
            {
                Killer = E.Replacement;
            }
            else
            if (E.Replacement == ParentObject)
            {
                DeathMemory = DeathMemory.CopyMemories(E.Replacement, DeathMemory);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(OnDeathRemovalEvent E)
        {
            if (E.Dying == Killer
                && E.Dying.GetDropInventory() is IInventory dropInventory
                && dropInventory.GetInventoryZone() is Zone deathZone
                && deathZone.Built
                && dropInventory.GetInventoryCell() is Cell dropCell
                && dropCell.FindObject(GO => IsCorpseOfThisEntityOrDying(ParentObject, GO, E)) is GameObject corpse)
            {
                UpdateKiller(corpse);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetExtraPhysicalFeaturesEvent E)
        {
            if (E.Object == ParentObject)
            {
                foreach (string evidence in GetEvidenceOfDeath() ?? new())
                {
                    E.Features.Add(evidence);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, Init.YehNah(), nameof(Init));
            E.AddEntry(this, nameof(Killer), Killer?.DebugName ?? NULL);
            E.AddEntry(this, nameof(Weapon), Weapon?.DebugName ?? NULL);
            E.AddEntry(this, nameof(DeathMemory), DeathMemory.DebugInternalsString(ParentObject));
            E.AddEntry(this, nameof(KillerDetails), KillerDetails?.DebugInternalsString() ?? NULL);
            E.AddEntry(this, nameof(DeathDescription), DeathDescription?.DebugInternalsString() ?? NULL);
            E.AddEntry(this, Accidental.YehNah(), nameof(Accidental));
            E.AddEntry(this, Environmental.YehNah(), nameof(Environmental));
            string thirdPersonReason = DeathDescription?.ThirdPersonReason(
                Capitalize: true,
                Accidental: Accidental,
                DoReplacer: true)?.Strip();
            thirdPersonReason ??= "=subject.Subjective= simply died"
                .StartReplace()
                .AddObject(ParentObject, "subject")
                .ToString();
            thirdPersonReason += ".";
            E.AddEntry(this, nameof(DeathDescription.ThirdPersonReason), thirdPersonReason);
            E.AddEntry(this, nameof(EvidenceOfDeath), EvidenceOfDeath?.Aggregate("", (a, n) => a + "," + n)?[1..]);
            return base.HandleEvent(E);
        }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.Write(_Init);
            Writer.WriteGameObject(_Killer);
            base.Write(Basis, Writer);
        }
        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            _Init = Reader.ReadBoolean();
            _Killer = Reader.ReadGameObject();
            base.Read(Basis, Reader);
        }
    }
}
