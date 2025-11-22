using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

using HarmonyLib;

using XRL.World;

using UD_FleshGolems.Logging;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;
using SerializeField = UnityEngine.SerializeField;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseEntityPair : IComposite
    {
        public enum PairRelationship : int
        {
            None = int.MaxValue,
            PrimaryCorpse = 0,
            InheritedCorpse = 1,
            CorpseProduct = 2,
            CorpseCountsAs = 3,
        }

        public CorpseBlueprint Corpse;
        public EntityBlueprint Entity;
        public int Weight;
        public Relationship Relationship { get; private set; }

        private CorpseEntityPair()
        {
            Corpse = null;
            Entity = null;
            Weight = 0;
            Relationship = Relationship.None;
        }

        public CorpseEntityPair(
            CorpseBlueprint Corpse,
            EntityBlueprint Entity,
            int Weight,
            Relationship Relationship)
            : this()
        {
            this.Corpse = Corpse;
            this.Entity = Entity;
            this.Weight = Weight;
            this.Relationship = Relationship;
        }

        public CorpseEntityPair(
            CorpseWeight CorpseWeight,
            EntityBlueprint Entity,
            Relationship Relationship)
            : this(
                  Corpse: CorpseWeight.GetBlueprint(),
                  Entity: Entity,
                  Weight: CorpseWeight.Weight,
                  Relationship: Relationship) { }

        public CorpseEntityPair(
            CorpseBlueprint Corpse,
            EntityWeight EntityWeight,
            Relationship Relationship)
            : this(
                  Corpse: Corpse,
                  Entity: EntityWeight.GetBlueprint(),
                  Weight: EntityWeight.Weight,
                  Relationship: Relationship) { }

        public BlueprintWeight this[[AllowNull] BlueprintBox Reference]
        {
            get
            {
                if ((CorpseBlueprint)Reference == Corpse)
                {
                    return GetEntityWeight();
                }
                else
                if ((EntityBlueprint)Reference == Entity)
                {
                    return GetCorpseWeight();
                }
                return null;
            }
            private set
            {
                if (value.Blueprint is not null)
                {
                    if ((CorpseBlueprint)Reference == Corpse)
                    {
                        Entity = value.Blueprint as EntityBlueprint;
                    }
                    else
                    if ((EntityBlueprint)Reference == Entity)
                    {
                        Corpse = value.Blueprint as CorpseBlueprint;
                    }
                    Weight = value.Weight;
                }
            }
        }

        public GameObjectBlueprint GetCorpseGameObjectBlueprint()
            => Corpse.GetGameObjectBlueprint();

        public GameObjectBlueprint GetEntityGameObjectBlueprint()
            => Entity.GetGameObjectBlueprint();

        public CorpseWeight GetCorpseWeight()
            => Corpse is not null
            ? new CorpseWeight(Corpse, Weight)
            : null;

        public EntityWeight GetEntityWeight()
            => Entity is not null
            ? new EntityWeight(Entity, Weight)
            : null;

        public override string ToString()
            => Corpse + ":" + Entity + "::" + Weight + ";" + Relationship;

        public void Deconstruct(out CorpseBlueprint Corpse, out EntityBlueprint Entity, out int Weight, out Relationship Relationship)
        {
            Corpse = this.Corpse;
            Entity = this.Entity;
            Weight = this.Weight;
            Relationship = this.Relationship;
        }
    }
}
