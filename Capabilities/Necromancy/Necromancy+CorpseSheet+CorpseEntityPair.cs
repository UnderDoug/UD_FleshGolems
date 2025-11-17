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
using XRL;
using XRL.World.Parts;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {

            [Serializable]
            public class CorpseWeight
            {
                public CorpseBlueprint Corpse;
                public int Weight;
                public CorpseWeight(CorpseBlueprint Entity, int Weight)
                {
                    this.Corpse = Entity;
                    this.Weight = Weight;
                }

                public static implicit operator KeyValuePair<CorpseBlueprint, int>(CorpseWeight Operand) => new(Operand.Corpse, Operand.Weight);
                public static implicit operator CorpseWeight(KeyValuePair<CorpseBlueprint, int>  Operand) => new(Operand.Key, Operand.Value);
            }

            [Serializable]
            public class EntityWeight
            {
                public EntityBlueprint Entity;
                public int Weight;
                public EntityWeight(EntityBlueprint Entity, int Weight)
                {
                    this.Entity = Entity;
                    this.Weight = Weight;
                }
                public static implicit operator KeyValuePair<EntityBlueprint, int>(EntityWeight Operand) => new(Operand.Entity, Operand.Weight);
                public static implicit operator EntityWeight(KeyValuePair<EntityBlueprint, int> Operand) => new(Operand.Key, Operand.Value);
            }

            [Serializable]
            public class CorpseEntityPair 
                : IComposite
                , IEquatable<CorpseEntityPair>
                , IEquatable<CorpseWeight>
                , IEquatable<EntityWeight>
                , IEquatable<KeyValuePair<CorpseBlueprint, CorpseEntityPair.PairRelationship>>
                , IEquatable<KeyValuePair<EntityBlueprint, CorpseEntityPair.PairRelationship>>
                , IComparable<CorpseEntityPair>
                , IComparable<CorpseEntityPair.PairRelationship>
                , IComparable<CorpseWeight>
                , IComparable<EntityWeight>
                , IComparable<KeyValuePair<CorpseBlueprint, CorpseEntityPair.PairRelationship>>
                , IComparable<KeyValuePair<EntityBlueprint, CorpseEntityPair.PairRelationship>>
            {
                public enum PairRelationship : int
                {
                    None = 0,
                    PrimaryCorpse = 1,
                    InheritedCorpse = 2,
                    CorpseProduct = 3,
                    CorpseCountsAs = 4,
                }

                public EntityWeight this[CorpseBlueprint Reference]
                {
                    get
                    {
                        if (Corpse == Reference)
                        {
                            return (EntityWeight)this;
                        }
                        return null;
                    }
                    private set
                    {
                        if (value.Entity != (EntityBlueprint)null)
                        {
                            Entity = value.Entity;
                            Weight = value.Weight;
                        }
                    }
                }
                public CorpseWeight this[EntityBlueprint Reference]
                {
                    get
                    {
                        if (Entity == Reference)
                        {
                            return (CorpseWeight)this;
                        }
                        return null;
                    }
                    private set
                    {
                        if (value.Corpse != (CorpseBlueprint)null)
                        {
                            Corpse = value.Corpse;
                            Weight = value.Weight;
                        }
                    }
                }

                public CorpseBlueprint Corpse;
                public EntityBlueprint Entity;
                public int Weight;
                public PairRelationship Relationship { get; private set; }

                private CorpseEntityPair()
                {
                    Corpse = null;
                    Entity = null;
                    Weight = 0;
                    Relationship = PairRelationship.None;
                }

                public CorpseEntityPair(
                    CorpseBlueprint Corpse, 
                    EntityBlueprint Entity, 
                    int Weight, 
                    PairRelationship Relationship)
                    : this()
                {
                    this.Corpse = Corpse;
                    this.Entity = Entity;
                    this.Weight = Weight;
                    this.Relationship = Relationship;
                }

                public CorpseEntityPair(CorpseEntityPair CorpseEntityPair)
                    : this(
                          Corpse: CorpseEntityPair.Corpse,
                          Entity: CorpseEntityPair.Entity,
                          Weight: CorpseEntityPair.Weight,
                          Relationship: CorpseEntityPair.Relationship)
                {
                }

                public GameObjectBlueprint GetGameObjectBlueprint()
                {
                    return Corpse.GetGameObjectBlueprint();
                }

                public override string ToString()
                {
                    return Corpse + ":" + Entity + "::" + Weight + ";" + Relationship;
                }

                public void Deconstruct(out CorpseBlueprint Corpse) => Corpse = this.Corpse;
                public void Deconstruct(out EntityBlueprint Entity) => Entity = this.Entity;
                public void Deconstruct(out int Weight) => Weight = this.Weight;
                public void Deconstruct(out PairRelationship Relationship) => Relationship = this.Relationship;
                public void Deconstruct(out CorpseBlueprint Corpse, out EntityBlueprint Entity)
                {
                    Deconstruct(out Corpse);
                    Deconstruct(out Entity);
                }
                public void Deconstruct(out CorpseBlueprint Corpse, out EntityBlueprint Entity, out int Weight)
                {
                    Deconstruct(out Corpse);
                    Deconstruct(out Entity);
                    Deconstruct(out Weight);
                }
                public void Deconstruct(out CorpseBlueprint Corpse, out EntityBlueprint Entity, out int Weight, out PairRelationship Relationship)
                {
                    Deconstruct(out Corpse);
                    Deconstruct(out Entity);
                    Deconstruct(out Weight);
                    Deconstruct(out Relationship);
                }

                public static implicit operator CorpseWeight(CorpseEntityPair Operand) => new(Operand.Corpse, Operand.Weight);
                public static implicit operator EntityWeight(CorpseEntityPair Operand) => new(Operand.Entity, Operand.Weight);

                public static explicit operator string(CorpseEntityPair Operand) => Operand.ToString();
                public static explicit operator int(CorpseEntityPair Operand) => Operand.Weight;
                public static explicit operator GameObjectBlueprint(CorpseEntityPair Operand) => Operand.GetGameObjectBlueprint();

                // Equality
                public override bool Equals(object obj = null)
                {
                    if (obj == null)
                    {
                        return false;
                    }
                    return base.Equals(obj);
                }

                public override int GetHashCode() => 
                    Corpse.GetHashCode() 
                    ^ Entity.GetHashCode() 
                    ^ Weight.GetHashCode()
                    ^ Relationship.GetHashCode();

                public bool Equals(CorpseEntityPair other)
                {
                    return other.Equals(null)
                        && Corpse.Equals(other.Corpse)
                        && Entity.Equals(other.Entity)
                        && Weight.Equals(other.Weight)
                        && Relationship.Equals(other.Relationship);
                }

                public bool Equals(CorpseWeight other)
                {
                    return other != null
                        && Corpse.Equals(other.Corpse)
                        && Weight.Equals(other.Weight);
                }
                public bool Equals(EntityWeight other)
                {
                    return other != null
                        && Entity.Equals(other.Entity)
                        && Weight.Equals(other.Weight);
                }
                public bool Equals(KeyValuePair<CorpseBlueprint, PairRelationship> other)
                {
                    return other is KeyValuePair<CorpseBlueprint, PairRelationship> otherKVP
                        && Corpse.Equals(otherKVP.Key)
                        && Relationship.Equals(other.Value);
                }
                public bool Equals(KeyValuePair<EntityBlueprint, PairRelationship> other)
                {
                    return other is KeyValuePair<EntityBlueprint, PairRelationship> otherKVP
                        && Entity.Equals(otherKVP.Key)
                        && Relationship.Equals(other.Value);
                }

                public static bool operator ==(CorpseEntityPair Operand1, CorpseWeight Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(CorpseEntityPair Operand1, CorpseWeight Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseEntityPair Operand1, EntityWeight Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(CorpseEntityPair Operand1, EntityWeight Operand2) => !(Operand1 == Operand2);

                // Comparison
                public int CompareTo(CorpseEntityPair other)
                {
                    if (other is null)
                    {
                        return 1;
                    }
                    if (Entity != other.Entity || Corpse != other.Corpse)
                    {
                        return 0;
                    }
                    if (!Relationship.Equals(other.Relationship))
                    {
                        return -Relationship.CompareTo(other.Relationship);
                    }
                    return Weight.CompareTo(other.Weight);
                }
                public int CompareTo(PairRelationship other)
                {
                    return -Relationship.CompareTo(other);
                }
                public int CompareTo(CorpseWeight other)
                {
                    if (other.Corpse != (CorpseBlueprint)null)
                    {
                        return 1;
                    }
                    if (!Corpse.Equals(other.Corpse))
                    {
                        return 0;
                    }
                    return Weight.CompareTo(other.Weight);
                }
                public int CompareTo(EntityWeight other)
                {
                    if (other.Entity != (EntityBlueprint)null)
                    {
                        return 1;
                    }
                    if (!Entity.Equals(other.Entity))
                    {
                        return 0;
                    }
                    return Weight.CompareTo(other.Weight);
                }
                public int CompareTo(KeyValuePair<CorpseBlueprint, PairRelationship> other)
                {
                    if (other.Key != (CorpseBlueprint)null)
                    {
                        return 1;
                    }
                    if (!Corpse.Equals(other.Key))
                    {
                        return 0;
                    }
                    return Relationship.CompareTo(other.Value);
                }
                public int CompareTo(KeyValuePair<EntityBlueprint, PairRelationship> other)
                {
                    if (other.Key != (EntityBlueprint)null)
                    {
                        return 1;
                    }
                    if (!Entity.Equals(other.Key))
                    {
                        return 0;
                    }
                    return Relationship.CompareTo(other.Value);
                }

                public static bool operator >(CorpseEntityPair Operand1, CorpseEntityPair Operand2) => Operand1.Weight > Operand2.Weight;
                public static bool operator <(CorpseEntityPair Operand1, CorpseEntityPair Operand2) => Operand1.Weight < Operand2.Weight;
                public static bool operator >=(CorpseEntityPair Operand1, CorpseEntityPair Operand2) => Operand1.Weight >= Operand2.Weight;
                public static bool operator <=(CorpseEntityPair Operand1, CorpseEntityPair Operand2) => Operand1.Weight <= Operand2.Weight;
            }
        }
    }
}
