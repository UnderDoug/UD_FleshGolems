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
        [Serializable]
        public partial class CorpseSheet : IComposite
        {
            [SerializeField]
            private readonly CorpseBlueprint Corpse;

            [SerializeField]
            private List<CorpseEntityPair> Pairs;

            private CorpseSheet()
            {
                Corpse = null;
                Pairs = new();
            }

            public CorpseSheet(CorpseBlueprint Corpse)
                : this()
            {
                this.Corpse = Corpse;
            }

            public CorpseBlueprint GetCorpse()
            {
                return Corpse;
            }
            public GameObjectBlueprint GetCorpseBlueprint()
            {
                return Corpse.GetGameObjectBlueprint();
            }
            public Dictionary<EntityBlueprint, int> GetWeightedList()
            {
                Dictionary<EntityBlueprint, int> weightedList= new();
                foreach (CorpseEntityPair entityWeighted in Pairs)
                {
                    AccumulateEntityWeight(ref weightedList, Corpse, entityWeighted);
                }
                return weightedList;
            }
            private static void AccumulateEntityWeight(
                ref Dictionary<EntityBlueprint, int> WeightedList,
                CorpseBlueprint Key,
                CorpseEntityPair Pair)
            {
                WeightedList ??= new();
                if (Pair[Key] is EntityWeight entityWeight)
                {
                    if (WeightedList.ContainsKey(entityWeight.Entity))
                    {
                        WeightedList[entityWeight.Entity] += entityWeight.Weight;
                    }
                    else
                    {
                        WeightedList.Add(entityWeight.Entity, entityWeight.Weight);
                    }
                }
            }

            private bool CorpseBluprintPairHasSameBlueprint(CorpseEntityPair Old, CorpseEntityPair New)
            {
                return Old.Corpse == New.Corpse;
            }
            private bool NewCorpseBluprintPairHasHigherWeight(CorpseEntityPair Old, CorpseEntityPair New)
            {
                return Old.Corpse == New.Corpse;
            }
            private bool AddUniqueCorpse(List<CorpseEntityPair> Collection, CorpseEntityPair Item)
            {
                return Collection.AddUnique(
                        Item: Item,
                        Comparer: CorpseBluprintPairHasSameBlueprint,
                        OnBasisOldNew: NewCorpseBluprintPairHasHigherWeight);
            }
            private bool AddUniqueCorpseBlueprintWeightPair(List<CorpseEntityPair> Collection, string Blueprint, int Weight)
            {
                return AddUniqueCorpse(Collection, new(Blueprint, Weight));
            }
            private bool AddUniqueCorpseBlueprintWeightPair(List<CorpseEntityPair> Collection, GameObjectBlueprint Blueprint, int Weight)
            {
                return AddUniqueCorpseBlueprintWeightPair(Collection, Blueprint.Name, Weight);
            }
            private List<CorpseEntityPair> GetInheritedCorpses(CorpseEntityPair CorpseBlueprintWeightPair)
            {
                List<CorpseEntityPair> inheritedCorpses = new();
                GameObjectBlueprint inheritedCorpse = CorpseBlueprintWeightPair?.GetGameObjectBlueprint()?.Inherits?.GetGameObjectBlueprint();
                while (inheritedCorpse != null
                    && inheritedCorpse.IsCorpse()
                    && AddUniqueCorpse(inheritedCorpses, new(inheritedCorpse.Name, CorpseBlueprintWeightPair.Weight)))
                {
                    inheritedCorpse = inheritedCorpse.Inherits?.GetGameObjectBlueprint();
                }
                return inheritedCorpses;
            }
            private bool AddInheritedCorpses(List<CorpseEntityPair> InheritedCorpseBlueprintWeightPairs)
            {
                bool any = false;
                if (!InheritedCorpseBlueprintWeightPairs.IsNullOrEmpty())
                {
                    Pairs ??= new();
                    foreach (CorpseEntityPair inheritedCorpseBlueprintWeightPair in InheritedCorpseBlueprintWeightPairs)
                    {
                        any = AddUniqueCorpse(Pairs, inheritedCorpseBlueprintWeightPair) || any;
                    }
                }
                return any;
            }

            public List<CorpseEntityPair> GetCorpses(bool ExcludeProducts, Predicate<GameObjectBlueprint> Filter = null)
            {
                List<CorpseEntityPair> baseList = new()
                {
                    PrimaryCorpse,
                };
                Pairs ??= new();
                CorpseCorpseProducts ??= new();
                baseList.AddRange(Pairs);
                if (!ExcludeProducts)
                {
                    baseList.AddRange(CorpseCorpseProducts.ConvertAll(cp => new CorpseEntityPair(cp.Blueprint, 0)));
                }
                if (Filter != null)
                {
                    List<CorpseEntityPair> filteredList = new();
                    foreach (CorpseEntityPair corpseBlueprintWeightPair in baseList)
                    {
                        if (Filter(corpseBlueprintWeightPair.GetGameObjectBlueprint()))
                        {
                            filteredList.Add(corpseBlueprintWeightPair);
                        }
                    }
                    return filteredList;
                }
                return baseList;
            }
            public List<CorpseEntityPair> GetCorpses(Predicate<GameObjectBlueprint> Filter = null)
            {
                return GetCorpses(false, Filter);
            }

            public CorpseSheet AddCorpse(CorpseEntityPair Corpse)
            {
                Pairs ??= new();
                Pairs.AddUnique(Corpse);
                if (GetInheritedCorpses(Corpse) is List<CorpseEntityPair> inheritedCorpses
                    && !inheritedCorpses.IsNullOrEmpty())
                {
                    Pairs.AddRange(inheritedCorpses);
                }
                return this;
            }
            public CorpseSheet AddCorpse(string Corpse)
            {
                Pairs ??= new();
                CorpseEntityPair corpseBlueprintWeightPair = new(Corpse, 0);
                AddUniqueCorpse(Pairs, corpseBlueprintWeightPair);
                AddInheritedCorpses(GetInheritedCorpses(corpseBlueprintWeightPair));
                return this;
            }

            public List<CorpseProduct> GetCorpseProducts(Predicate<GameObjectBlueprint> Filter = null)
            {
                CorpseCorpseProducts ??= new();
                if (Filter != null)
                {
                    List<CorpseProduct> filteredList = new();
                    foreach (CorpseProduct corpseProduct in CorpseCorpseProducts)
                    {
                        if (Filter(corpseProduct.GetGameObjectBlueprint()))
                        {
                            filteredList.Add(corpseProduct);
                        }
                    }
                    return filteredList;
                }
                return CorpseCorpseProducts;
            }
            public bool EntityHasCorpse(string Corpse, bool CheckAll = true)
            {
                return GetCorpses(!CheckAll).Any(c => c.Corpse == Corpse);
            }
            public bool EntityHasCorpse(CorpseEntityPair Corpse, bool CheckAll = true)
            {
                return EntityHasCorpse(Corpse.Corpse, CheckAll);
            }
            public bool EntityHasCorpseWithWeight(CorpseEntityPair Corpse, bool CheckAll = true)
            {
                return GetCorpses(!CheckAll).Any(c => c.Corpse == Corpse.Corpse && c.Weight == Corpse.Weight);
            }
        }
    }
}
