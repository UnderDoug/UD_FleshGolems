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
            private readonly GameObjectBlueprint Entity;

            [SerializeField]
            private readonly Corpse PrimaryCorpse;

            [SerializeField]
            private List<Corpse> Corpses;

            [SerializeField]
            private List<CorpseProduct> CorpseCorpseProducts;

            private CorpseSheet()
            {
                Entity = null;
                PrimaryCorpse = null;
                Corpses = new();
                CorpseCorpseProducts = new();
            }

            public CorpseSheet(GameObjectBlueprint Entity)
                : this()
            {
                this.Entity = Entity;
                PrimaryCorpse = Entity.GetCorpseBlueprintWeightPair();
                if (GetInheritedCorpses(PrimaryCorpse) is List<Corpse> inheritedCorpses
                    && !inheritedCorpses.IsNullOrEmpty())
                {
                    Corpses.AddRange(inheritedCorpses);
                }
            }
            public CorpseSheet(GameObjectBlueprint Entity, List<Corpse> Corpses)
                : this(Entity)
            {
                foreach (Corpse corpse in Corpses)
                {
                    AddUniqueCorpse(this.Corpses, corpse);
                    AddInheritedCorpses(GetInheritedCorpses(corpse));
                }
            }
            public CorpseSheet(GameObjectBlueprint Entity, List<Corpse> Corpses, List<CorpseProduct> CorpseCorpseProducts)
                : this(Entity, Corpses)
            {
                this.CorpseCorpseProducts = CorpseCorpseProducts;
            }
            public CorpseSheet(string Entity)
                : this(Entity.GetGameObjectBlueprint())
            {
            }
            public CorpseSheet(string Entity, List<string> Corpses)
                : this(Entity.GetGameObjectBlueprint(), Corpses.ConvertAll(s => new Corpse(s, 0)))
            {
            }
            public CorpseSheet(string Entity, List<string> Corpses, List<string> CorpseCorpseProducts)
                : this(Entity.GetGameObjectBlueprint(), Corpses.ConvertAll(s => new Corpse(s, 0)), CorpseCorpseProducts.ConvertAll(s => new CorpseProduct(s)))
            {
            }

            public GameObjectBlueprint GetEntity()
            {
                return Entity;
            }

            public Corpse GetPrimaryCorpseBlueprintWeightPair()
            {
                return PrimaryCorpse;
            }
            public string GetPrimaryCorpse()
            {
                return PrimaryCorpse.Blueprint;
            }
            public GameObjectBlueprint GetPrimaryCorpseBlueprint()
            {
                return PrimaryCorpse?.GetGameObjectBlueprint();
            }
            public bool HasPrimary()
            {
                return PrimaryCorpse?.GetGameObjectBlueprint() is var primaryCorpseBlueprint
                    && primaryCorpseBlueprint.IsCorpse();
            }

            private bool CorpseBluprintPairHasSameBlueprint(Corpse Old, Corpse New)
            {
                return Old.Blueprint == New.Blueprint;
            }
            private bool NewCorpseBluprintPairHasHigherWeight(Corpse Old, Corpse New)
            {
                return Old.Blueprint == New.Blueprint;
            }
            private bool AddUniqueCorpse(List<Corpse> Collection, Corpse Item)
            {
                return Collection.AddUnique(
                        Item: Item,
                        Comparer: CorpseBluprintPairHasSameBlueprint,
                        OnBasisOldNew: NewCorpseBluprintPairHasHigherWeight);
            }
            private bool AddUniqueCorpseBlueprintWeightPair(List<Corpse> Collection, string Blueprint, int Weight)
            {
                return AddUniqueCorpse(Collection, new(Blueprint, Weight));
            }
            private bool AddUniqueCorpseBlueprintWeightPair(List<Corpse> Collection, GameObjectBlueprint Blueprint, int Weight)
            {
                return AddUniqueCorpseBlueprintWeightPair(Collection, Blueprint.Name, Weight);
            }
            private List<Corpse> GetInheritedCorpses(Corpse CorpseBlueprintWeightPair)
            {
                List<Corpse> inheritedCorpses = new();
                GameObjectBlueprint inheritedCorpse = CorpseBlueprintWeightPair?.GetGameObjectBlueprint()?.Inherits?.GetGameObjectBlueprint();
                while (inheritedCorpse != null
                    && inheritedCorpse.IsCorpse()
                    && AddUniqueCorpse(inheritedCorpses, new(inheritedCorpse.Name, CorpseBlueprintWeightPair.Weight)))
                {
                    inheritedCorpse = inheritedCorpse.Inherits?.GetGameObjectBlueprint();
                }
                return inheritedCorpses;
            }
            private bool AddInheritedCorpses(List<Corpse> InheritedCorpseBlueprintWeightPairs)
            {
                bool any = false;
                if (!InheritedCorpseBlueprintWeightPairs.IsNullOrEmpty())
                {
                    Corpses ??= new();
                    foreach (Corpse inheritedCorpseBlueprintWeightPair in InheritedCorpseBlueprintWeightPairs)
                    {
                        any = AddUniqueCorpse(Corpses, inheritedCorpseBlueprintWeightPair) || any;
                    }
                }
                return any;
            }

            public List<Corpse> GetCorpses(bool ExcludeProducts, Predicate<GameObjectBlueprint> Filter = null)
            {
                List<Corpse> baseList = new()
                {
                    PrimaryCorpse,
                };
                Corpses ??= new();
                CorpseCorpseProducts ??= new();
                baseList.AddRange(Corpses);
                if (!ExcludeProducts)
                {
                    baseList.AddRange(CorpseCorpseProducts.ConvertAll(cp => new Corpse(cp.Blueprint, 0)));
                }
                if (Filter != null)
                {
                    List<Corpse> filteredList = new();
                    foreach (Corpse corpseBlueprintWeightPair in baseList)
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
            public List<Corpse> GetCorpses(Predicate<GameObjectBlueprint> Filter = null)
            {
                return GetCorpses(false, Filter);
            }

            public CorpseSheet AddCorpse(Corpse Corpse)
            {
                Corpses ??= new();
                Corpses.AddUnique(Corpse);
                if (GetInheritedCorpses(Corpse) is List<Corpse> inheritedCorpses
                    && !inheritedCorpses.IsNullOrEmpty())
                {
                    Corpses.AddRange(inheritedCorpses);
                }
                return this;
            }
            public CorpseSheet AddCorpse(string Corpse)
            {
                Corpses ??= new();
                Corpse corpseBlueprintWeightPair = new(Corpse, 0);
                AddUniqueCorpse(Corpses, corpseBlueprintWeightPair);
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
                return GetCorpses(!CheckAll).Any(c => c.Blueprint == Corpse);
            }
            public bool EntityHasCorpse(Corpse Corpse, bool CheckAll = true)
            {
                return EntityHasCorpse(Corpse.Blueprint, CheckAll);
            }
            public bool EntityHasCorpseWithWeight(Corpse Corpse, bool CheckAll = true)
            {
                return GetCorpses(!CheckAll).Any(c => c.Blueprint == Corpse.Blueprint && c.Weight == Corpse.Weight);
            }
        }
    }
}
