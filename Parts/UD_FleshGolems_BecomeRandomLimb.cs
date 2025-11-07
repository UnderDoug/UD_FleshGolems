using System;
using System.Collections.Generic;
using System.Text;

using Qud.API;
using XRL.World.Anatomy;
using static XRL.World.Parts.UD_FleshGolems_HasCyberneticsButcherableCybernetic;

using UD_FleshGolems;
using static UD_FleshGolems.Const;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_BecomeRandomLimb : IScribedPart
    {
        public static Dictionary<string, string> LimbToSlotTranslations => new()
        {
            { "Arm", "Arm" },
            { "Leg", "Feet" },
            { "Foot", "Feet" },
            { "Hand", "Hands" },
            { "Head", "Head" },
            { "Face", "Face" },
        };
        public static Dictionary<string, List<string>> SlotToLimbTranslations => new()
        {
            { "Arm", new() { "Arm" } },
            { "Feet", new() { "Leg", "Foot" } },
            { "Hands", new() { "Hand" } },
            { "Head", new() { "Head" } },
            { "Face", new() { "Face" } },
        };

        public string TypesOneOf;

        public string BasesOneOf;

        public string BlueprintsOneOf;

        public string TagsOneOf;

        public string CyberneticsBlueprints;

        public string CyberneticsTable;

        public int? TableCount;

        public string CyberneticsForLimb;

        public int? ForLimbCount;

        public string CyberneticsForLimbAroundCost;

        public int? ForLimbAroundCostCount;

        public int CyberneticsCount;

        public bool UseImplantedAdjectiveIfImplanted;

        private bool MarkedForOblivion;

        public UD_FleshGolems_BecomeRandomLimb()
        {
            TypesOneOf = null;
            BasesOneOf = null;
            BlueprintsOneOf = null;
            TagsOneOf = null;

            CyberneticsBlueprints = null;

            CyberneticsTable = null;
            TableCount = null;

            CyberneticsForLimb = null;
            ForLimbCount = null;

            CyberneticsForLimbAroundCost = null;
            ForLimbAroundCostCount = null;

            CyberneticsCount = 0;

            UseImplantedAdjectiveIfImplanted = true;

            MarkedForOblivion = false;
        }

        public override bool AllowStaticRegistration() => true;

        public static string ProcessSpec(string Spec, List<string> Wildcards = null)
        {
            string processed = null;
            if (!Spec.IsNullOrEmpty() && (Wildcards.IsNullOrEmpty() || !Wildcards.Contains(Spec)))
            {
                processed = Spec.CachedCommaExpansion().GetRandomElement();
                if (!Wildcards.IsNullOrEmpty() && Wildcards.Contains(processed))
                {
                    processed = null;
                }
            }
            return processed;
        }

        public static int GetCyberneticsCount(int? PossibleCount = null, int BaseCount = 1) => Math.Max(1, PossibleCount ?? BaseCount);

        public static bool CyberneticsSlotsContainsSeverableLimbType(string CyberneticsSlots) => 
               CyberneticsSlots.Contains("Arm")
            || CyberneticsSlots.Contains("Feet")
            || CyberneticsSlots.Contains("Hands")
            || CyberneticsSlots.Contains("Head")
            || CyberneticsSlots.Contains("Face");

        public override bool WantEvent(int ID, int Cascade) => base.WantEvent(ID, Cascade)
            || ID == BeforeObjectCreatedEvent.ID;

        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            if (E.Object == ParentObject)
            {
                string blueprint = ProcessSpec(BlueprintsOneOf, Wildcards);
                string @base = ProcessSpec(BasesOneOf, Wildcards);
                string tag = ProcessSpec(TagsOneOf, Wildcards);
                string type = ProcessSpec(TypesOneOf, Wildcards);

                if (!CyberneticsForLimb.IsNullOrEmpty() 
                    && Anatomies.GetBodyPartType(CyberneticsForLimb) != null)
                {
                    type = CyberneticsForLimb;
                }
                type ??= ParentObject.GetPropertyOrTag("UD_FleshGolems LimbType");

                SanitizeLimbType(type, out type, Wildcards);

                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_BecomeRandomLimb) + "." + nameof(BeforeObjectCreatedEvent) + ", " +
                    nameof(blueprint) + ": " + blueprint + ", " +
                    nameof(@base) + ": " + @base + ", " +
                    nameof(tag) + ": " + tag + ", " +
                    nameof(type) + ": " + type);

                string tableSlotType = null;
                string tableBlueprint = null;
                if (!CyberneticsTable.IsNullOrEmpty())
                {
                    UnityEngine.Debug.Log("    " + nameof(CyberneticsTable) + ": " + (CyberneticsTable ?? "null"));
                    for (int i = 0; i < 5; i++)
                    {
                        PopulationResult cyberneticsPopulationResult = PopulationManager.RollOneFrom(CyberneticsTable);
                        if (cyberneticsPopulationResult != null
                            && GameObjectFactory.Factory.GetBlueprintIfExists(cyberneticsPopulationResult.Blueprint) is GameObjectBlueprint cyberneticsBlueprint
                            && cyberneticsBlueprint.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Slots), out string tableCyberneticSlots)
                            && CyberneticsSlotsContainsSeverableLimbType(tableCyberneticSlots))
                        {
                            UnityEngine.Debug.Log(
                                "    " + "    [" + i + "] " + nameof(cyberneticsPopulationResult) + ": " + (cyberneticsPopulationResult.Blueprint ?? "null") + ", " +
                                nameof(tableCyberneticSlots) + ": " + tableCyberneticSlots);
                            if (tableCyberneticSlots.Contains(','))
                            {
                                foreach (string slotType in tableCyberneticSlots.Split(',').ShuffleInPlace())
                                {
                                    if (SlotToLimbTranslations.ContainsKey(slotType))
                                    {
                                        tableSlotType = SlotToLimbTranslations[slotType].GetRandomElement();
                                        tableBlueprint = cyberneticsBlueprint.Name;
                                        break;
                                    }
                                }
                            }
                            else
                            if (SlotToLimbTranslations.ContainsKey(tableCyberneticSlots))
                            {
                                tableSlotType = SlotToLimbTranslations[tableCyberneticSlots].GetRandomElement();
                                tableBlueprint = cyberneticsBlueprint.Name;
                                break;
                            }
                        }
                    }
                    bool good = !tableSlotType.IsNullOrEmpty();
                    UnityEngine.Debug.Log("    " + "    [" + (good ? TICK : CROSS) + "] " + (good ? "Success" : "Fail") + "!");
                }
                if (!tableSlotType.IsNullOrEmpty())
                {
                    type = tableSlotType;
                }

                if (BodyPart.MakeSeveredBodyPart(blueprint, type, tag, @base) is GameObject severedLimbObject)
                {
                    bool hasCybernetics = false;
                    if (!CyberneticsBlueprints.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsList(severedLimbObject, CyberneticsBlueprints)
                            || hasCybernetics;
                    }
                    if (!CyberneticsTable.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsTable(severedLimbObject, CyberneticsTable, GetCyberneticsCount(TableCount, CyberneticsCount))
                            || hasCybernetics;
                    }
                    if (!CyberneticsForLimb.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsForLimb(severedLimbObject, CyberneticsForLimb, GetCyberneticsCount(ForLimbCount, CyberneticsCount))
                            || hasCybernetics;
                    }
                    if (!CyberneticsForLimbAroundCost.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsForLimbAroundCost(severedLimbObject, CyberneticsForLimbAroundCost, GetCyberneticsCount(ForLimbAroundCostCount, CyberneticsCount))
                            || hasCybernetics;
                    }
                    if (CyberneticsBlueprints.IsNullOrEmpty()
                        && CyberneticsTable.IsNullOrEmpty()
                        && CyberneticsForLimb.IsNullOrEmpty()
                        && CyberneticsForLimbAroundCost.IsNullOrEmpty()
                        && CyberneticsCount > 0)
                    {
                        hasCybernetics = EmbedButcherableCyberneticsForLimb(severedLimbObject, type, CyberneticsCount)
                            || hasCybernetics;
                    }
                    if (hasCybernetics && UseImplantedAdjectiveIfImplanted)
                    {
                        var cyberneticsHasRandomImplants = new CyberneticsHasRandomImplants();
                        severedLimbObject.RequirePart<DisplayNameAdjectives>().AddAdjective(cyberneticsHasRandomImplants.Adjective);
                    }
                    E.ReplacementObject = severedLimbObject;
                }
                else
                {
                    MarkedForOblivion = true;
                }
            }
            return base.HandleEvent(E);
        }
    }
}
