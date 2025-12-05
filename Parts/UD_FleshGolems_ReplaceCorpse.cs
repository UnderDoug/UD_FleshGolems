using System;
using System.Collections.Generic;
using System.Text;

using XRL.Rules;
using XRL.World.ObjectBuilders;

using UD_FleshGolems;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using static UD_FleshGolems.Utils;

namespace XRL.World.Parts
{
    public class UD_FleshGolems_ReplaceCorpse : IScribedPart
    {
        public static UD_FleshGolems_NecromancySystem NecromancySystem => UD_FleshGolems_NecromancySystem.System;

        public string Level;
        public string Species;
        public string Faction;
        public string Tags;
        public string Population;

        public bool FallbackOnSpecFail;

        public bool ExcludeCorpsesFromOriginEntity;

        public UD_FleshGolems_ReplaceCorpse()
        {
            Level = null;
            Species = null;
            Faction = null;
            Tags = null;
            Population = null;
            FallbackOnSpecFail = false;
            ExcludeCorpsesFromOriginEntity = true;
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

                DieRoll specLevel = null;
                List<string> specSpecies = null;
                List<string> specFaction = null;
                List<string> specTags = null;
                string specPopulation = null;
                bool anySpec = false;
                if (!Level.IsNullOrEmpty())
                {
                    specLevel = new();
                    specLevel.Parse(Level, out bool isInvalidDie);
                    anySpec = true;
                    if (isInvalidDie)
                    {
                        specLevel = null;
                        anySpec = false;
                    }
                }
                if (!Species.IsNullOrEmpty())
                {
                    specSpecies = Species.CachedCommaExpansion();
                    anySpec = true;
                }
                if (!Faction.IsNullOrEmpty())
                {
                    specFaction = Faction.CachedCommaExpansion();
                    anySpec = true;
                }
                if (!Tags.IsNullOrEmpty())
                {
                    specTags = Tags.CachedCommaExpansion();
                    anySpec = true;
                }

                if (!Population.IsNullOrEmpty()
                    && Population.CachedCommaExpansion()?.GetRandomElement() is string rolledPopulation)
                {
                    specPopulation = rolledPopulation;
                    anySpec = true;
                }
                if (replacementCorpse == null
                    && !specPopulation.IsNullOrEmpty()
                    && PopulationManager.RollOneFrom(specPopulation).Blueprint.GetGameObjectBlueprint() is var populationEntityModel)
                {
                    bool corpseSheetHasAcceptableCorpse(CorpseSheet CorpseSheet)
                        => UD_FleshGolems_Reanimated.CorpseSheetHasAcceptableCorpse(CorpseSheet, populationEntityModel.Name);

                    if (UD_FleshGolems_Reanimated.TryGetRandomCorpseFromNecronomicon(populationEntityModel.Name, out GameObjectBlueprint corpseModel, corpseSheetHasAcceptableCorpse))
                    {
                        replacementCorpse = corpseModel.createUnmodified();
                    }
                }
                if (replacementCorpse == null
                    && anySpec)
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

                            int modelLevelRounded = levelDieRoll.AverageRounded();
                            if (modelLevelRounded < specLevel.Min() || modelLevelRounded > specLevel.Max())
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

                        if (ExcludeCorpsesFromOriginEntity && entityModel.IsCorpse())
                            return false;

                        return true;
                    }

                    GameObjectBlueprint entityToSpec = NecromancySystem
                        ?.GetEntityModelsWithCorpse(isAccordingToSpec)
                        ?.GetRandomElementCosmetic();

                    bool corpseSheetHasAcceptableCorpse(CorpseSheet CorpseSheet)
                        => UD_FleshGolems_Reanimated.CorpseSheetHasAcceptableCorpse(CorpseSheet, entityToSpec.Name);

                    if (entityToSpec != null
                        && UD_FleshGolems_Reanimated.TryGetRandomCorpseFromNecronomicon(entityToSpec.Name, out GameObjectBlueprint corpseModel, corpseSheetHasAcceptableCorpse))
                    {
                        replacementCorpse = corpseModel.createUnmodified();
                    }
                }
                if (replacementCorpse == null
                    && (!anySpec || FallbackOnSpecFail)
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
                else
                {
                    E.ReplacementObject = GameObject.CreateUnmodified("UD_FleshGolems ObliterateSelf Widget");
                    MetricsManager.LogModError(ThisMod, Name + " " + " failed to find appropriate replacement corpse for " + (ParentObject?.DebugName ?? "null object") + ".");
                }
            }
            return base.HandleEvent(E);
        }
    }
}
