using System;
using System.Collections.Generic;
using System.Text;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_RaggedNaturalWeapon : IModification
    {
        public static string Adjective => "{{UD_FleshGolems_ragged|ragged}}";

        [SerializeField]
        private string WielderID;

        private GameObject _Wielder;
        public GameObject Wielder
        {
            get => _Wielder ??= GameObject.FindByID(WielderID);
            set
            {
                WielderID = value?.ID;
                _Wielder = value;
            }
        }

        [SerializeField]
        private string Description;

        public UD_FleshGolems_RaggedNaturalWeapon()
        {
            Description = null;
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
            
            base.ApplyModification(obj);
        }

        public override bool AllowStaticRegistration() => true;

        public void ProcessDescriptionElements(GameObject Wielder = null)
        {
            if (ParentObject.TryGetPart(out Description descriptionPart)
                && Wielder != null || this.Wielder != null)
            {

                List<string> poeticFeatures = new()
                {
                    "viscera",
                    "muck",
                };
                if (Wielder?.GetxTag("TextFragments", "PoeticFeatures") is string poeticFeaturesXTag)
                {
                    poeticFeatures = new(poeticFeaturesXTag.Split(','));
                }
                string firstPoeticFeature = poeticFeatures.GetRandomElement() ?? "Viscera";
                poeticFeatures.Remove(firstPoeticFeature);
                string secondPoeticFeature = poeticFeatures.GetRandomElement() ?? "muck";
                poeticFeatures.Remove(secondPoeticFeature);

                string poeticVerb = Wielder?.GetxTag("TextFragments", "PoeticVerbs")?.Split(',')?.GetRandomElement() ?? "squirming";

                string poeticAdjective = Wielder?.GetxTag("TextFragments", "PoeticAdjectives")?.Split(',')?.GetRandomElement() ?? "wet";

                List<string> poeticNoises = new()
                    {
                        "gurgles",
                        "slurps",
                    };
                if (Wielder?.GetxTag("TextFragments", "PoeticnNoises") is string poeticNoisesXTag)
                {
                    poeticFeatures = new(poeticNoisesXTag.Split(','));
                }
                string firstPoeticNoise = poeticNoises.GetRandomElement();
                poeticNoises.Remove(firstPoeticNoise);
                string secondPoeticNoise = poeticNoises.GetRandomElement();
                poeticFeatures.Remove(secondPoeticNoise);

                Description = descriptionPart._Short
                    .Replace("*FirstFeature*", firstPoeticFeature.Capitalize())
                    .Replace("*secondFeature*", secondPoeticFeature)
                    .Replace("*verbing*", poeticVerb)
                    .Replace("*Adjective*", poeticAdjective.Capitalize())
                    .Replace("*firstNoise*", firstPoeticNoise)
                    .Replace("*secondNoise*", secondPoeticNoise);
                descriptionPart._Short = Description;
            }
        }

        public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade)
            || ID == GetDisplayNameEvent.ID
            || ID == GetShortDescriptionEvent.ID;

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if ((E.Understood() || E.AsIfKnown) && !E.Object.HasProperName)
            {
                E.AddAdjective(Adjective);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            return base.HandleEvent(E);
        }
    }
}
