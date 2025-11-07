using System;
using System.Collections.Generic;
using System.Text;

using Qud.API;

using UD_FleshGolems;
using static UD_FleshGolems.Const;

using XRL.World.Anatomy;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_HasCyberneticsButcherableCybernetic : IScribedPart
    {
        public static List<string> Wildcards => new() { "*", "any", "Any", "ANY" };

        public string Cybernetics;

        public string Table;

        public int? TableCount;

        public string ForLimb;

        public int? ForLimbCount;

        public string ForLimbAroundCost;

        public int? ForLimbAroundCostCount;

        public int Count;

        public bool UseImplantedAdjectiveIfImplanted;

        public UD_FleshGolems_HasCyberneticsButcherableCybernetic()
        {
            Cybernetics = null;

            Table = null;
            TableCount = null;

            ForLimb = null;
            ForLimbCount = null;

            ForLimbAroundCost = null;
            ForLimbAroundCostCount = null;

            Count = 1;

            UseImplantedAdjectiveIfImplanted = true;
        }

        public UD_FleshGolems_HasCyberneticsButcherableCybernetic(string ForLimbAroundCostOrJustLimb)
            : this()
        {
            if (ForLimbAroundCostOrJustLimb.Contains(','))
            {
                ForLimbAroundCost = ForLimbAroundCostOrJustLimb;
                ForLimbAroundCostCount = Count;
            }
            else
            {
                ForLimb = ForLimbAroundCostOrJustLimb;
                ForLimbCount = Count;
            }
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public static bool IsBlueprintForLimb(GameObjectBlueprint b, string Limb) =>
            b.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Slots), out string slots)
            && (slots.Contains(Limb) || Wildcards.Contains(Limb));
        public static bool IsBlueprintForLimbAroundCost(GameObjectBlueprint b, string Limb, int Cost) =>
            IsBlueprintForLimb(b, Limb)
            && b.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Cost), out int cost)
            && Math.Abs(cost - Cost) < 3;

        public static void SanitizeLimbType(string ForLimb, out string SafeLimb, List<string> Wildcards = null)
        {
            SafeLimb = ForLimb;
            if (ForLimb.IsNullOrEmpty() || Anatomies.GetBodyPartType(ForLimb) == null || (!Wildcards.IsNullOrEmpty() && Wildcards.Contains(ForLimb)))
            {
                SafeLimb = Anatomies.BodyPartTypeList.GetRandomElement().Type;
            }
        }

        public static bool TryGetButcherableCyberneticPart(GameObject Corpse, out CyberneticsButcherableCybernetic ButcherableCyberneticPart)
        {
            ButcherableCyberneticPart = null;
            if (Corpse == null)
            {
                return false;
            }
            return Corpse.TryRequirePart(out  ButcherableCyberneticPart);
        }

        public bool EmbedButcherableCybernetics()
        {
            bool any = false;

            any = EmbedButcherableCyberneticsList() || any;
            any = EmbedButcherableCyberneticsTable() || any;
            any = EmbedButcherableCyberneticsForLimb() || any;
            any = EmbedButcherableCyberneticsForLimbAroundCost() || any;

            if (any && UseImplantedAdjectiveIfImplanted)
            {
                var cyberneticsHasRandomImplants = new CyberneticsHasRandomImplants();
                ParentObject.RequirePart<DisplayNameAdjectives>().AddAdjective(cyberneticsHasRandomImplants.Adjective);
            }

            return any;
        }

        // List Only
        public static bool EmbedButcherableCyberneticsList(GameObject Corpse, string Cybernetics)
        {
            bool any = false;
            if (TryGetButcherableCyberneticPart(Corpse, out CyberneticsButcherableCybernetic butcherableCybernetic))
            {
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + "." + nameof(EmbedButcherableCyberneticsList) + ", " +
                    nameof(Corpse) + ": " + (Corpse?.DebugName ?? Const.NULL) + ", " +
                    nameof(Cybernetics) + ": \"" + Cybernetics + "\"");

                butcherableCybernetic.Cybernetics ??= new();

                if (!Cybernetics.IsNullOrEmpty())
                {
                    foreach (string cyberneticBlueprint in Cybernetics.CachedCommaExpansion())
                    {
                        UnityEngine.Debug.Log("    " + nameof(cyberneticBlueprint) + ": " + cyberneticBlueprint);
                        if (GameObject.Create(cyberneticBlueprint) is GameObject cyberneticObject)
                        {
                            butcherableCybernetic.Cybernetics.Add(cyberneticObject);
                            any = true;
                        }
                    }
                }
                UnityEngine.Debug.Log("    [" + (any ? TICK : CROSS) + "] " + (any ? "Success" : "Fail") + "!");
            }
            return any;
        }
        public bool EmbedButcherableCyberneticsList()
        {
            return EmbedButcherableCyberneticsList(ParentObject, Cybernetics);
        }

        // Table Only
        public static bool EmbedButcherableCyberneticsTable(GameObject Corpse, string Table, int Count = 1)
        {
            Count = Math.Max(1, Count);
            bool any = false;
            if (TryGetButcherableCyberneticPart(Corpse, out CyberneticsButcherableCybernetic butcherableCybernetic))
            {
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + "." + nameof(EmbedButcherableCyberneticsTable) + ", " +
                    nameof(Corpse) + ": " + (Corpse?.DebugName ?? Const.NULL) + ", " +
                    nameof(Table) + ": \"" + Table + "\"" + ", " +
                    nameof(Count) + ": " + Count);

                butcherableCybernetic.Cybernetics ??= new();

                if (Table.IsNullOrEmpty())
                {
                    int maxAtempts = 5;
                    for (int i = 0; i < Count; i++)
                    {
                        for (int j = 0; j < maxAtempts; j++)
                        {
                            if (PopulationManager.CreateOneFrom(Table) is GameObject cybernticObject)
                            {
                                if (!cybernticObject.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItem))
                                {
                                    MetricsManager.LogModError(
                                        mod: Utils.ThisMod,
                                        Message: nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + " " +
                                            "pulled non-cybernetic " + (cybernticObject?.DebugName ?? Const.NULL) + " from table " + Table);
                                    cybernticObject.Obliterate();
                                }
                                else
                                {
                                    butcherableCybernetic.Cybernetics.Add(cybernticObject);
                                    any = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                UnityEngine.Debug.Log("    [" + (any ? TICK : CROSS) + "] " + (any ? "Success" : "Fail") + "!");
            }
            return any;
        }
        public bool EmbedButcherableCyberneticsTable()
        {
            return EmbedButcherableCyberneticsTable(ParentObject, Table, TableCount ?? Count);
        }

        // ForLimb Only
        public static bool EmbedButcherableCyberneticsForLimb(GameObject Corpse, string ForLimb, int Count = 1)
        {
            Count = Math.Max(1, Count);
            bool any = false;
            if (TryGetButcherableCyberneticPart(Corpse, out CyberneticsButcherableCybernetic butcherableCybernetic))
            {
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + "." + nameof(EmbedButcherableCyberneticsForLimb) + ", " +
                    nameof(Corpse) + ": " + (Corpse?.DebugName ?? Const.NULL) + ", " +
                    nameof(ForLimb) + ": \"" + ForLimb + "\"" + ", " +
                    nameof(Count) + ": " + Count);

                butcherableCybernetic.Cybernetics ??= new();

                if (ForLimb.IsNullOrEmpty())
                {
                    SanitizeLimbType(ForLimb, out ForLimb, Wildcards);
                    for (int i = 0; i < Count; i++)
                    {
                        if (GameObject.Create(EncountersAPI.GetAnItemBlueprint(b => IsBlueprintForLimb(b, ForLimb))) is GameObject cybernticObject)
                        {
                            if (!cybernticObject.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItem))
                            {
                                MetricsManager.LogModError(
                                    mod: Utils.ThisMod,
                                    Message: nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + " " +
                                        "pulled non-cybernetic " + (cybernticObject?.DebugName ?? Const.NULL) + " " +
                                        nameof(ForLimb) + " " + ForLimb);
                                cybernticObject.Obliterate();
                            }
                            else
                            {
                                butcherableCybernetic.Cybernetics.Add(cybernticObject);
                                any = true;
                                break;
                            }
                        }
                    }
                }
                UnityEngine.Debug.Log("    [" + (any ? TICK : CROSS) + "] " + (any ? "Success" : "Fail") + "!");
            }
            return any;
        }
        public bool EmbedButcherableCyberneticsForLimb()
        {
            return EmbedButcherableCyberneticsForLimb(ParentObject, ForLimb, ForLimbCount ?? Count);
        }

        // ForLimbAroundCost Only
        public static bool EmbedButcherableCyberneticsForLimbAroundCost(GameObject Corpse, string ForLimbAroundCost, int Count = 1)
        {
            Count = Math.Max(1, Count);
            bool any = false;
            if (TryGetButcherableCyberneticPart(Corpse, out CyberneticsButcherableCybernetic butcherableCybernetic)
                && ForLimbAroundCost.Contains(','))
            {
                string[] forLimbAroundCost = ForLimbAroundCost.Split(',');
                string forLimb = forLimbAroundCost[0];
                bool parsed = int.TryParse(forLimbAroundCost[1], out int aroundCost);

                SanitizeLimbType(forLimb, out forLimb, Wildcards);

                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + "." + nameof(EmbedButcherableCyberneticsForLimb) + ", " +
                    nameof(Corpse) + ": " + (Corpse?.DebugName ?? Const.NULL) + ", " +
                    nameof(forLimb) + ": \"" + forLimb + "\"" + ", " +
                    nameof(aroundCost) + ": \"" + aroundCost + "\"" + ", " +
                    nameof(Count) + ": " + Count);

                butcherableCybernetic.Cybernetics ??= new();

                if (forLimb.IsNullOrEmpty() && parsed)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        if (EncountersAPI.GetAnItemBlueprint(b => IsBlueprintForLimbAroundCost(b, forLimb, aroundCost)) is string cyberneticBlueprint
                            && GameObject.Create(cyberneticBlueprint) is GameObject cybernticObject)
                        {
                            if (!cybernticObject.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItem))
                            {
                                MetricsManager.LogModError(
                                    mod: Utils.ThisMod,
                                    Message: nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + " " +
                                        "pulled non-cybernetic " + (cybernticObject?.DebugName ?? Const.NULL) + " " +
                                        nameof(forLimb) + " " + forLimb + " " + nameof(aroundCost) + " " + aroundCost);
                                cybernticObject.Obliterate();
                            }
                            else
                            {
                                butcherableCybernetic.Cybernetics.Add(cybernticObject);
                                any = true;
                                break;
                            }
                        }
                    }
                }
                UnityEngine.Debug.Log("    [" + (any ? TICK : CROSS) + "] " + (any ? "Success" : "Fail") + "!");
            }
            return any;
        }
        public bool EmbedButcherableCyberneticsForLimbAroundCost()
        {
            return EmbedButcherableCyberneticsForLimbAroundCost(ParentObject, ForLimbAroundCost, ForLimbAroundCostCount ?? Count);
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == AfterObjectCreatedEvent.ID;
        }
        public override bool HandleEvent(AfterObjectCreatedEvent E)
        {
            if (ParentObject == E.Object
                && Cybernetics.IsNullOrEmpty()
                && EmbedButcherableCybernetics())
            {
                ParentObject.RemovePart<Food>();
            }
            return base.HandleEvent(E);
        }
    }
}
