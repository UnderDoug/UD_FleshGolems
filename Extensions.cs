using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using XRL.CharacterBuilds.Qud;
using XRL.Rules;
using XRL.World;
using XRL.World.Parts;

using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;

namespace UD_FleshGolems
{
    public static class Extensions
    {
        public static bool IsPlayerBlueprint(this string Blueprint)
        {
            return Blueprint == Startup.PlayerBlueprint;
        }

        public static string ThisManyTimes(this string @string, int Times = 1)
        {
            if (Times < 1)
            {
                return null;
            }
            string output = "";

            for (int i = 0; i < Times; i++)
            {
                output += @string;
            }

            return output;
        }
        public static string ThisManyTimes(this char @char, int Times = 1)
        {
            return @char.ToString().ThisManyTimes(Times);
        }

        public static void TryAdd<T>(this ICollection<T> Collection, T Item)
        {
            if (!Collection.Contains(Item))
            {
                Collection.Add(Item);
            }
        }

        public static T GetRandomElementCosmeticExcluding<T>(this IEnumerable<T> Enumerable, Predicate<T> Exclude)
            where T : class
        {
            List<T> filteredList = new(Enumerable);
            filteredList.RemoveAll(m => Exclude != null && Exclude(m));
            return filteredList.GetRandomElementCosmetic();
        }

        public static Commerce BlurValue(this Commerce Commerce, int Margin)
        {
            int adjustment = Stat.RandomCosmetic(-Margin, Margin);
            Commerce.Value += adjustment;
            return Commerce;
        }
        public static Commerce BlurValue(this Commerce Commerce, double MarginPercent)
        {
            int margin = (int)(MarginPercent * 100.0);
            double adjustmentFactor = 1.0 + (Stat.RandomCosmetic(-margin, margin) / 100.0);
            Commerce.Value *= adjustmentFactor;
            return Commerce;
        }

        public static IEnumerable<string> GetPartNames(this GameObject Object)
        {
            foreach (IPart part in Object.PartsList)
            {
                yield return part.Name;
            }
        }

        public static bool OverlapsWith<T>(this IEnumerable<T> Enumerable1, IEnumerable<T> Enumerable2)
        {
            foreach (T item1 in Enumerable1)
            {
                foreach (T item2 in Enumerable2)
                {
                    if (item1.Equals(item2))
                    {
                        return true;
                    }    
                }
            }
            return false;
        }

        public static bool ContainsAll<T>(this ICollection<T> Collection1, ICollection<T> Collection2)
        {
            int matches = 0;
            int targetMatches = Collection2.Count;
            if (targetMatches > Collection1.Count)
            {
                return false;
            }
            foreach (T item2 in Collection2)
            {
                foreach (T item1 in Collection1)
                {
                    if (item1.Equals(item2))
                    {
                        matches++;
                        if (targetMatches == matches)
                        {
                            break;
                        }
                    }    
                }
            }
            return targetMatches >= matches;
        }

        public static GameObject SetWontSell(this GameObject Item, bool WontSell)
        {
            Item.SetIntProperty(nameof(WontSell), WontSell ? 1 : 0, RemoveIfZero: true);
            return Item;
        }

        public static int DamageTo1HP(this GameObject Creature)
        {
            if (Creature == null || Creature.GetStat("Hitpoints") is not Statistic hitpoints)
            {
                return 0;
            }
            return hitpoints.Value - 1;
        }

        public static T OverrideWithDeepCopyOrRequirePart<T>(this GameObject GameObject, T PartToCopy, Func<GameObject, GameObject> MapInv = null)
            where T : IPart, new()
        {
            if (PartToCopy == null)
            {
                return GameObject?.RequirePart<T>();
            }
            if (GameObject != null && GameObject.TryGetPart(out T existingPart))
            {
                GameObject?.RemovePart(existingPart);
            }
            T returnPart = null;
            try
            {
                returnPart = GameObject?.AddPart(PartToCopy.DeepCopy(GameObject, MapInv) as T);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(
                    nameof(OverrideWithDeepCopyOrRequirePart) + " -> " + 
                    nameof(GameObject.AddPart) + "(" + 
                    nameof(PartToCopy.DeepCopy) + ")", 
                    x: x, 
                    category: "game_mod_exception");
                returnPart = PartToCopy;
            }
            return returnPart;
        }

        public static string SetSpecies(this GameObject GameObject, string Species)
        {
            GameObject.SetStringProperty(nameof(Species), Species);
            return Species;
        }
        public static string SetGenotype(this GameObject GameObject, string Genotype)
        {
            GameObject.SetStringProperty(nameof(Genotype), Genotype);
            return Genotype;
        }
        public static string SetSubtype(this GameObject GameObject, string Subtype)
        {
            GameObject.SetStringProperty(nameof(Subtype), Subtype);
            return Subtype;
        }

        public static GameObjectBlueprint GetGameObjectBlueprint(this string Blueprint)
        {
            return Utils.GetGameObjectBlueprint(Blueprint);
        }

        public static string GetCorpseBlueprint(this GameObjectBlueprint Blueprint)
        {
            if (Blueprint.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBlueprint))
            {
                return corpseBlueprint;
            }
            return null;
        }
        public static bool TryGetCorpseBlueprint(this GameObjectBlueprint Blueprint, out string CorpseBlueprint)
        {
            return (CorpseBlueprint = Blueprint.GetCorpseBlueprint()) != null
                && GameObjectFactory.Factory.HasBlueprint(CorpseBlueprint);
        }

        public static int? GetCorpseChance(this GameObjectBlueprint Blueprint)
        {
            if (Blueprint.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseChance), out int corpseChance))
            {
                return corpseChance;
            }
            return null;
        }
        public static bool TryGetCorpseChance(this GameObjectBlueprint Blueprint, out int CorpseChance)
        {
            CorpseChance = 0;
            int? corpseChance = Blueprint.GetCorpseChance();
            if (corpseChance != null)
            {
                CorpseChance = (int)corpseChance;
                return true;
            }
            return false;
        }
        public static bool TryGetCorpseBlueprintAndChance(this GameObjectBlueprint Blueprint, out string CorpseBlueprint, out int CorpseChance)
        {
            CorpseChance = 0;
            return Blueprint.TryGetCorpseBlueprint(out CorpseBlueprint)
                && Blueprint.TryGetCorpseChance(out CorpseChance);
        }


        public static bool IsCorpse(this GameObjectBlueprint Blueprint, Predicate<GameObjectBlueprint> Filter)
        {
            return Blueprint != null
                && Blueprint.InheritsFrom("Corpse")
                && (Filter == null || Filter(Blueprint));
        }

        public static bool IsCorpse(this GameObjectBlueprint Blueprint)
        {
            return Blueprint != null
                && Blueprint.IsCorpse(null);
        }

        public static bool IsCorpse(this string Blueprint, Predicate<GameObjectBlueprint> Filter = null)
        {
            return Blueprint != null
                && Blueprint.GetGameObjectBlueprint().IsCorpse(Filter);
        }

        public static bool IsCorpse(this GameObject Corpse, Predicate<GameObjectBlueprint> Filter = null)
        {
            return Corpse != null
                && !Corpse.HasPart<AnimatedObject>()
                && Corpse.HasPart<UD_FleshGolems_CorpseReanimationHelper>()
                && Corpse.GetBlueprint().IsCorpse(Filter);
        }

        public static bool InheritsFrom(this string Blueprint, string BaseBlueprint)
        {
            return Utils.ThisBlueprintInheritsFromThatOne(Blueprint, BaseBlueprint);
        }

        public static bool IsBaseBlueprint(this string Blueprint)
        {
            return Utils.IsBaseGameObjectBlueprint(Blueprint);
        }

        public static string GenerateBulletList(
            this List<string> Items,
            string Label = null,
            string Bullet = "\u0007",
            string BulletColor = "K")
        {
            Label = Label.IsNullOrEmpty() ? "" : Label + "\n";
            string output = "";
            foreach (string item in Items)
            {
                if (!output.IsNullOrEmpty())
                {
                    output += "\n";
                }
                output += "{{" + BulletColor + "|" + Bullet + "}} " + item;
            }
            return Label + output;
        }
    }
}
