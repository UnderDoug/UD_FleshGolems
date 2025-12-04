using System;
using System.Collections.Generic;
using System.Text;

using UD_FleshGolems;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;

using XRL.Rules;
using XRL.World.ObjectBuilders;

namespace XRL.World.Parts
{
    public class UD_FleshGolems_ReplaceCorpse : IScribedPart
    {
        public static UD_FleshGolems_NecromancySystem NecromancySystem => UD_FleshGolems_NecromancySystem.System;

        public string Spec;

        public Dictionary<string, string> SpecParameters => Spec?.CachedDictionaryExpansion();

        public bool FallbackOnSpecFail;

        public UD_FleshGolems_ReplaceCorpse()
        {
            Spec = null;
            FallbackOnSpecFail = false;
        }

        public override bool AllowStaticRegistration()
            => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(BeforeObjectCreatedEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            ; // || ID == BeforeObjectCreatedEvent.ID;

        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            if (ParentObject is GameObject corpseToReplace
                && corpseToReplace == E.Object)
            {
                GameObject replacementCorpse = null;

                if (!SpecParameters.IsNullOrEmpty())
                {
                    DieRoll specLevel = null;
                    List<string> specSpecies = null;
                    List<string> specFaction = null;
                    List<string> specTags = null;
                    string specPopulation = null;

                    if (SpecParameters.ContainsKey("Level"))
                    {
                        specLevel = new();
                        specLevel.Parse(SpecParameters["Level"], out bool isInvalidDie);
                        if (isInvalidDie)
                        {
                            specLevel = null;
                        }
                    }
                    if (SpecParameters.ContainsKey("Species"))
                    {
                        specSpecies = SpecParameters["Species"]?.CachedCommaExpansion();
                    }
                    if (SpecParameters.ContainsKey("Faction"))
                    {
                        specFaction = SpecParameters["Faction"]?.CachedCommaExpansion();
                    }
                    if (SpecParameters.ContainsKey("Tags"))
                    {
                        specTags = SpecParameters["Tags"]?.CachedCommaExpansion();
                    }
                    if (SpecParameters.ContainsKey("Population")
                        && SpecParameters["Population"]?.CachedCommaExpansion()?.GetRandomElement() is string rolledPopulation)
                    {
                        specPopulation = rolledPopulation;
                    }

                    if (specPopulation.IsNullOrEmpty()
                        && PopulationManager.RollOneFrom(specPopulation).Blueprint.GetGameObjectBlueprint() is var populationEntityModel)
                    {
                        bool corpseSheetHasAcceptableCorpse(CorpseSheet CorpseSheet)
                            => UD_FleshGolems_Reanimated.CorpseSheetHasAcceptableCorpse(CorpseSheet, populationEntityModel.Name);

                        if (UD_FleshGolems_Reanimated.TryGetRandomCorpseFromNecronomicon(populationEntityModel.Name, out GameObjectBlueprint corpseModel, corpseSheetHasAcceptableCorpse))
                        {
                            replacementCorpse = corpseModel.createUnmodified();
                        }
                    }
                    else
                    {
                        bool isAccordingToSpec(GameObjectBlueprint entityModel)
                        {
                            if (specLevel != null)
                            {
                                if (entityModel.GetStat("Level") is not Statistic levelStat)
                                    return false;

                                DieRoll levelDieRoll = new();

                                levelDieRoll.Parse(levelStat.ValueOrSValue, out bool isInvalidDie);
                                if (isInvalidDie)
                                    return false;

                                int specLevelRounded = specLevel.AverageRounded();
                                if (specLevelRounded < levelDieRoll.Min() || specLevelRounded > levelDieRoll.Max())
                                    return false;
                            }

                            if (!specSpecies.IsNullOrEmpty()
                                && (!entityModel.TryGetStringPropertyOrTag("Species", out string speciesPropTag)
                                    || !specSpecies.Contains(speciesPropTag)))
                                return false;
                                
                            if (!specFaction.IsNullOrEmpty()
                                && !specFaction.Contains(entityModel.GetPrimaryFaction()))
                                return false;

                            if (!specTags.IsNullOrEmpty()
                                && (entityModel.Tags.IsNullOrEmpty()
                                    || !entityModel.Tags.Keys.ContainsAll(specTags.ToArray())))
                                return false;

                            return true;
                        }

                        GameObjectBlueprint entityToSpec = NecromancySystem
                            ?.GetEntityModelsWithCorpse(isAccordingToSpec)
                            ?.GetRandomElementCosmetic();

                        bool corpseSheetHasAcceptableCorpse(CorpseSheet CorpseSheet)
                            => UD_FleshGolems_Reanimated.CorpseSheetHasAcceptableCorpse(CorpseSheet, entityToSpec.Name);

                        if (UD_FleshGolems_Reanimated.TryGetRandomCorpseFromNecronomicon(entityToSpec.Name, out GameObjectBlueprint corpseModel, corpseSheetHasAcceptableCorpse))
                        {
                            replacementCorpse = corpseModel.createUnmodified();
                        }
                    }
                }
                if (replacementCorpse == null
                    && (SpecParameters.IsNullOrEmpty() || FallbackOnSpecFail)
                    && UD_FleshGolems_NecromancySystem.IsReanimatableCorpse(corpseToReplace.GetBlueprint()))
                {
                    if (!corpseToReplace.TryGetPart(out UD_FleshGolems_PastLife pastLifePart) || !pastLifePart.Init)
                    {
                        pastLifePart = corpseToReplace.RequirePart<UD_FleshGolems_PastLife>().Initialize();
                    }
                    if (pastLifePart.Corpse?.CorpseBlueprint is string pastLifeCorpseBlueprint
                        && pastLifeCorpseBlueprint != corpseToReplace.Blueprint
                        && !pastLifeCorpseBlueprint.GetGameObjectBlueprint().HasPart(nameof(ReplaceObject)))
                    {
                        replacementCorpse = GameObjectFactory.Factory.CreateObject(pastLifeCorpseBlueprint);
                    }
                    else
                    if (pastLifePart.GetBlueprint().TryGetCorpseBlueprint(out pastLifeCorpseBlueprint)
                        && pastLifeCorpseBlueprint != corpseToReplace.Blueprint
                        && !pastLifeCorpseBlueprint.GetGameObjectBlueprint().HasPart(nameof(ReplaceObject)))
                    {
                        replacementCorpse = GameObjectFactory.Factory.CreateObject(pastLifeCorpseBlueprint);
                    }
                }
                if (replacementCorpse != null)
                {
                    bool doReplacement = true;
                    if (corpseToReplace.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper oldCorpseReanimationHelper)
                        && replacementCorpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper newCorpseReanimationHelper)
                        && oldCorpseReanimationHelper.AlwaysAnimate)
                    {
                        doReplacement = newCorpseReanimationHelper.Animate();
                    }
                    if (doReplacement)
                    {
                        E.ReplacementObject = replacementCorpse;
                    }
                }
            }
            return base.HandleEvent(E);
        }
    }
}
