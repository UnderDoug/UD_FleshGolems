using System;
using System.Collections.Generic;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public partial class CorpseEntityPair 
                : IComposite
                , IEquatable<CorpseEntityPair>
                , IEquatable<BlueprintWeight>
                , IEquatable<KeyValuePair<BlueprintWrapper, CorpseEntityPair.PairRelationship>>
                , IComparable<CorpseEntityPair>
                , IComparable<CorpseEntityPair.PairRelationship>
                , IComparable<BlueprintWeight>
                , IComparable<KeyValuePair<BlueprintWrapper, CorpseEntityPair.PairRelationship>>
            {
                public enum PairRelationship : int
                {
                    None = int.MaxValue,
                    PrimaryCorpse = 0,
                    InheritedCorpse = 1,
                    CorpseProduct = 2,
                    CorpseCountsAs = 3,
                }

                public BlueprintWeight this[BlueprintWrapper Reference]
                {
                    get
                    {
                        if (Corpse == Reference)
                        {
                            return (EntityWeight)this;
                        }
                        else
                        if (Entity == Reference)
                        {
                            return (CorpseWeight)this;
                        }
                        return null;
                    }
                    private set
                    {
                        if (value.Blueprint != (BlueprintWrapper)null)
                        {
                            if (Corpse == Reference)
                            {
                                Entity = (EntityBlueprint)value.Blueprint;
                            }
                            else
                            if (Entity == Reference)
                            {
                                Corpse = (CorpseBlueprint)value.Blueprint;
                            }
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

                public GameObjectBlueprint GetCorpseGameObjectBlueprint()
                {
                    return Corpse.GetGameObjectBlueprint();
                }
                public GameObjectBlueprint GetEntityGameObjectBlueprint()
                {
                    return Entity.GetGameObjectBlueprint();
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
                    return other != null
                        && Corpse.Equals(other.Corpse)
                        && Entity.Equals(other.Entity)
                        && Weight.Equals(other.Weight)
                        && Relationship.Equals(other.Relationship);
                }
                public bool Equals(BlueprintWeight other)
                {
                    return other != null
                        && Corpse.Equals(other.Blueprint)
                        && Weight.Equals(other.Weight);
                }
                public bool Equals(KeyValuePair<BlueprintWrapper, PairRelationship> other)
                {
                    return other is KeyValuePair<BlueprintWrapper, PairRelationship> otherKVP
                        && Corpse.Equals(otherKVP.Key)
                        && Relationship.Equals(other.Value);
                }

                public static bool operator ==(CorpseEntityPair Operand1, BlueprintWeight Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(CorpseEntityPair Operand1, BlueprintWeight Operand2) => !(Operand1 == Operand2);

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
                public int CompareTo(BlueprintWeight other)
                {
                    if (other.Blueprint != (BlueprintWrapper)null)
                    {
                        return 1;
                    }
                    if (!Corpse.Equals(other.Blueprint))
                    {
                        return 0;
                    }
                    return Weight.CompareTo(other.Weight);
                }
                public int CompareTo(KeyValuePair<BlueprintWrapper, PairRelationship> other)
                {
                    if (other.Key != (BlueprintWrapper)null)
                    {
                        return 1;
                    }
                    if (!Corpse.Equals(other.Key))
                    {
                        return 0;
                    }
                    return Relationship.CompareTo(other.Value);
                }

                public static bool operator >(CorpseEntityPair Operand1, BlueprintWeight Operand2) => Operand1.Weight > Operand2.Weight;
                public static bool operator <(CorpseEntityPair Operand1, BlueprintWeight Operand2) => Operand1.Weight < Operand2.Weight;
                public static bool operator >=(CorpseEntityPair Operand1, BlueprintWeight Operand2) => Operand1.Weight >= Operand2.Weight;
                public static bool operator <=(CorpseEntityPair Operand1, BlueprintWeight Operand2) => Operand1.Weight <= Operand2.Weight;
            }
        }
    }
}
