using System;
using System.Collections.Generic;
using System.Text;

using UD_FleshGolems;
using static UD_FleshGolems.Const;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_RaggedNaturalWeapon : IModification
    {
        public enum TaxonomyAdjective : int
        {
            None = 0,
            Ragged = 1,
            Jagged = 2,
            Fettid = 3,
            Decayed = 4,
        }

        public const string TAXONOMY_PROPTAG = "UD_FleshGolems Ragged Taxonomy";

        public const string RAGGED_ADJECTIVE = "ragged";
        public const string JAGGED_ADJECTIVE = "jagged";
        public const string FETTID_ADJECTIVE = "fettid";
        public const string DECAYED_ADJECTIVE = "decayed";

        public const string RAGGED_SHADER = "UD_FleshGolems_ragged";
        public const string JAGGED_SHADER = "UD_FleshGolems_jagged";
        public const string FETTID_SHADER = "UD_FleshGolems_fettid";
        public const string DECAYED_SHADER = "UD_FleshGolems_decayed";

        public TaxonomyAdjective Taxonomy;

        public string Adjective => GetAdjective();

        [SerializeField]
        private bool DisplayNameAdjusted;

        [SerializeField]
        private GameObject _Wielder;

        public GameObject Wielder
        {
            get => _Wielder;
            set
            {
                Description = null;
                _Wielder = value;
                if (value != null)
                {
                    ProcessDescriptionElements(Wielder);
                }
            }
        }

        [SerializeField]
        private string Description;

        public UD_FleshGolems_RaggedNaturalWeapon()
        {
            Taxonomy = TaxonomyAdjective.None;
            DisplayNameAdjusted = false;
            Wielder = null;
            Description = null;
        }

        public string GetAdjective()
        {
            if (Taxonomy == TaxonomyAdjective.None)
            {
                Taxonomy = DetermineTaxonomyAdjective(Wielder);
            }
            string adjective = Taxonomy switch
            {
                TaxonomyAdjective.Jagged  => JAGGED_ADJECTIVE,
                TaxonomyAdjective.Fettid  => FETTID_ADJECTIVE,
                TaxonomyAdjective.Decayed => DECAYED_ADJECTIVE,
                                        _ => RAGGED_ADJECTIVE,
            };
            string colorShader = Taxonomy switch
            {
                TaxonomyAdjective.Jagged  => JAGGED_SHADER,
                TaxonomyAdjective.Fettid  => FETTID_SHADER,
                TaxonomyAdjective.Decayed => DECAYED_SHADER,
                                        _ => RAGGED_SHADER,
            };
            return "{{" + colorShader + "|" + adjective + "}}";
        }

        public static TaxonomyAdjective DetermineTaxonomyAdjective(GameObject Wielder)
        {
            TaxonomyAdjective output = TaxonomyAdjective.None;
            if (Wielder == null)
            {
                return output;
            }
            if (Wielder.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                && pastLife.TryGetBlueprint(out GameObjectBlueprint pastLifeBlueprint))
            {
                if (pastLifeBlueprint.InheritsFrom("Robot"))
                {
                    output = TaxonomyAdjective.Jagged;
                }
                else
                if (pastLifeBlueprint.InheritsFromAny("Plant", "BasePlant", "MutatedPlant", "BaseSlynth"))
                {
                    output = TaxonomyAdjective.Fettid;
                }
                else
                if (pastLifeBlueprint.InheritsFromAny("Fungus", "ActiveFungus", "MutatedFungus"))
                {
                    output = TaxonomyAdjective.Decayed;
                }
                else
                {
                    output = TaxonomyAdjective.Ragged;
                }
            }
            if (output == TaxonomyAdjective.Ragged && Wielder?.GetBlueprint() is var wielderBlueprint)
            {
                string taxonomyPropTag = wielderBlueprint.GetPropertyOrTag(TAXONOMY_PROPTAG);
                if (wielderBlueprint.InheritsFrom("Robot Corpse")
                    || taxonomyPropTag.EqualsNoCase(TaxonomyAdjective.Jagged.ToString()))
                {
                    output = TaxonomyAdjective.Jagged;
                }
                else
                if (wielderBlueprint.InheritsFrom("UD_FleshGolems Plant Corpse")
                    || taxonomyPropTag.EqualsNoCase(TaxonomyAdjective.Fettid.ToString()))
                {
                    output = TaxonomyAdjective.Fettid;
                }
                else
                if (wielderBlueprint.InheritsFrom("UD_FleshGolems Fungus Corpse")
                    || taxonomyPropTag.EqualsNoCase(TaxonomyAdjective.Decayed.ToString()))
                {
                    output = TaxonomyAdjective.Decayed;
                }
                else
                {
                    output = TaxonomyAdjective.Ragged;
                }
            }
            return output;
        }

        public override void Configure()
        {
            WorksOnSelf = true;
        }

        public override int GetModificationSlotUsage() => 0;

        public override bool ModificationApplicable(GameObject Object) => 
            Object.HasPart<Physics>()
            && Object.IsNatural();

        public override void ApplyModification(GameObject obj)
        {
            if (obj == ParentObject
                && !obj.HasProperName
                && !DisplayNameAdjusted
                && obj.Render is Render render
                && render.DisplayName.Contains("ragged "))
            {
                render.DisplayName = render.DisplayName.Replace("ragged ", "");
                DisplayNameAdjusted = true;
            }
            base.ApplyModification(obj);
        }

        public void ClearDescription()
        {
            Description = null;
        }

        public void ProcessDescriptionElements(GameObject Wielder = null)
        {
            if (ParentObject?.GetPart<Description>() is Description descriptionPart
                && Wielder != null)
            {
                Taxonomy = DetermineTaxonomyAdjective(Wielder);

                Description = descriptionPart._Short
                    .StartReplace()
                    .AddObject(Wielder)
                    .ToString(); ;

                descriptionPart._Short = Description;
            }
        }

        public override bool AllowStaticRegistration() => true;

        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || ID == GetDisplayNameEvent.ID
            || ID == GetShortDescriptionEvent.ID
            || ID == GetDebugInternalsEvent.ID
            ;

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (!E.Object.HasProperName
                && (E.Understood()
                    || E.AsIfKnown))
            {
                if (!DisplayNameAdjusted
                    && E.Object.Render is Render render
                    && render.DisplayName.Contains("ragged "))
                {
                    render.DisplayName = render.DisplayName.Replace("ragged ", "");
                    DisplayNameAdjusted = true;
                }
                E.AddAdjective(Adjective);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (Description.IsNullOrEmpty() && Wielder != null)
                ProcessDescriptionElements(Wielder);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Taxonomy), Taxonomy.ToString());
            E.AddEntry(this, nameof(DisplayNameAdjusted), DisplayNameAdjusted);
            E.AddEntry(this, nameof(Wielder), Wielder?.DebugName ?? NULL);
            return base.HandleEvent(E);
        }
    }
}
