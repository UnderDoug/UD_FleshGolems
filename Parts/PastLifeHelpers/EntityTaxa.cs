using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using XRL.World;
using XRL.Rules;
using XRL.Collections;
using XRL.World.Parts;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;

using UD_FleshGolems.Events;

using UD_FleshGolems.Logging;
using static UD_FleshGolems.Const;

namespace UD_FleshGolems.Parts.PastLifeHelpers
{
    [Serializable]
    public class EntityTaxa : StringMap<string>, IComposite
    {
        public string Blueprint
        {
            get => this[nameof(Blueprint)];
            set => this[nameof(Blueprint)] = value;
        }
        public string Subtype
        {
            get => this[nameof(Subtype)];
            set => this[nameof(Subtype)] = value;
        }
        public string Genotype
        {
            get => this[nameof(Genotype)];
            set => this[nameof(Genotype)] = value;
        }
        public string Species
        {
            get => this[nameof(Species)];
            set => this[nameof(Species)] = value;
        }

        public EntityTaxa()
        {
        }

        public EntityTaxa(string Blueprint, string Subtype, string Genotype, string Species)
            : this()
        {
            this.Blueprint = Blueprint;
            this.Subtype = Subtype;
            this.Genotype = Genotype;
            this.Species = Species;
        }

        public EntityTaxa(GameObject Entity)
            : this(Entity?.Blueprint, Entity?.GetSubtype(), Entity?.GetGenotype(), Entity?.GetSpecies())
        {
            if (Blueprint.GetGameObjectBlueprint().xTags is Dictionary<string, Dictionary<string, string>> entityXTags
                && entityXTags.TryGetValue(REANIMATED_TAXA_XTAG, out Dictionary<string, string> sourceTaxa))
            {
                foreach ((string label, string value) in sourceTaxa)
                {
                    this[label] ??= value;
                }
            }
        }

        public bool RestoreTaxa(GameObject Entity, bool RestoreNull = false)
        {
            bool any = false;
            foreach ((string label, string value) in this)
            {
                if (RestoreNull || value != null)
                {
                    Entity.SetStringProperty(label, value);
                    any = true;
                }
            }
            return any;
        }

        public IEnumerable<KeyValuePair<string, string>> GetCustomTaxa(Predicate<KeyValuePair<string, string>> Filter = null)
        {
            foreach (KeyValuePair<string, string> entry in this)
                if (Filter == null || Filter(entry)
                    && !entry.Key.EqualsAny(nameof(Blueprint), nameof(Subtype), nameof(Genotype), nameof(Species)))
                    yield return entry;
        }
    }
}
