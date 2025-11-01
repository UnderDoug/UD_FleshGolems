using System;
using System.Collections.Generic;
using System.Text;

using XRL.Language;
using XRL.World.Parts.Mutation;
using XRL.World.Quests.GolemQuest;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_CoprseReanimationHelper : IScribedPart
    {
        public bool IsALIVE;

        public bool AlwaysAnimate;

        public UD_FleshGolems_CoprseReanimationHelper()
        {
            IsALIVE = false;
            AlwaysAnimate = false;
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public static bool MakeItALIVE(GameObject Corpse)
        {
            if (Corpse is GameObject frankenCorpse)
            {
                frankenCorpse.SetIntProperty("NoAnimatedNamePrefix", 1);
                frankenCorpse.SetIntProperty("Bleeds", 1);
                // frankenCorpse.Physics.Solid = true;

                UD_FleshGolem_ReanimatedCorpse reanimatedCorpse = frankenCorpse.RequirePart<UD_FleshGolem_ReanimatedCorpse>();

                if (frankenCorpse.TryGetPart(out ConversationScript convo)
                    && convo.ConversationID == "NewlySentientBeings")
                {
                    frankenCorpse.RemovePart(convo);
                    frankenCorpse.AddPart(new ConversationScript("UD_FleshGolems NewlyReanimatedBeings"));
                }

                Description frankenDescription = frankenCorpse.RequirePart<Description>();
                if (frankenDescription != null)
                {
                    frankenDescription._Short =
                        "Viscera and muck are brought squirming back into a horrific facsimile of life. " +
                        "Wet slurps and gurgles escape =subject.possessive= every movement and twist the " +
                        "gut of anyone unfortunate enough to hear it.";
                }
                string sourceBlueprintName = frankenCorpse.GetStringProperty("SourceBlueprint")
                    ?? frankenCorpse.Blueprint.Replace(" Corpse", "")
                    ?? "Trash Monk";

                Mutations frankenMutations = frankenCorpse.RequirePart<Mutations>();

                if (GameObjectFactory.Factory.GetBlueprintIfExists(sourceBlueprintName) is GameObjectBlueprint sourceBlueprint)
                {
                    if (sourceBlueprint.DisplayName() is string sourceBlueprintDisplayName)
                    {
                        if (frankenDescription != null
                            && sourceBlueprint.TryGetPartParameter(nameof(Description), nameof(Description.Short), out string sourceDescription))
                        {
                            frankenDescription._Short += "\n\n" +
                                "In life, this mess was " + Grammar.A(sourceBlueprintDisplayName) + ":\n" +
                                sourceDescription;
                        }
                        if (frankenCorpse.GetStringProperty("CreatureName") is string sourceCreatureName)
                        {
                            frankenCorpse.Render.DisplayName = "corpse of " + sourceCreatureName;
                        }
                        else
                        {
                            frankenCorpse.Render.DisplayName = sourceBlueprintDisplayName + " corpse";
                        }
                    }

                    string BleedLiquid = null;
                    if (sourceBlueprint.Tags.ContainsKey(nameof(BleedLiquid)))
                    {
                        BleedLiquid = sourceBlueprint.Tags[nameof(BleedLiquid)];
                    }
                    if (BleedLiquid.IsNullOrEmpty())
                    {
                        BleedLiquid = "blood-1000";
                    }
                    frankenCorpse.SetStringProperty("BleedLiquid", BleedLiquid);

                    if (frankenCorpse.GetStringProperty("KillerID") is string killerID
                        && GameObject.FindByID(killerID) is GameObject killer)
                    {
                        frankenCorpse.SetStringProperty("KillerName", killer.GetReferenceDisplayName(Short: true));
                    }

                    Skills frankenSkills = frankenCorpse.RequirePart<Skills>();
                    if (frankenSkills != null)
                    {
                        foreach (GamePartBlueprint sourceSkill in sourceBlueprint.Skills.Values)
                        {
                            frankenSkills.AddSkill(sourceSkill.Name);
                        }
                    }
                    foreach ((string statName, Statistic sourceStat) in sourceBlueprint.Stats)
                    {
                        frankenCorpse.Statistics[statName] = sourceStat;
                    }

                    if (sourceBlueprint.TryGetPartParameter(nameof(Physics), nameof(Physics.Weight), out int sourceWeight))
                    {
                        frankenCorpse.Physics.Weight = sourceWeight;
                    }

                    if (sourceBlueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string sourceAnatomy))
                    {
                        if (frankenCorpse.Body == null)
                        {
                            frankenCorpse.AddPart(new Body()).Anatomy = sourceAnatomy;
                        }
                        else
                        {
                            frankenCorpse.Body.Rebuild(sourceAnatomy);
                        }
                    }

                    if (GolemBodySelection.GetBodyBlueprintFor(sourceBlueprint) is GameObjectBlueprint golemBodyBlueprint)
                    {
                        if (golemBodyBlueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string golemAnatomy))
                        {
                            if (frankenCorpse.Body == null)
                            {
                                frankenCorpse.AddPart(new Body()).Anatomy = golemAnatomy;
                            }
                            else
                            {
                                frankenCorpse.Body.Rebuild(golemAnatomy);
                            }
                        }
                        if (golemBodyBlueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.Tile), out string golemTile))
                        {
                            frankenCorpse.Render.Tile = golemTile;
                            UnityEngine.Debug.Log(nameof(golemTile) + ": " + golemTile);
                        }
                        foreach ((string statName, Statistic sourceStat) in golemBodyBlueprint.Stats)
                        {
                            frankenCorpse.Statistics[statName] = new(sourceStat);
                        }
                    }
                    if (frankenMutations != null)
                    {
                        if (!sourceBlueprint.Mutations.IsNullOrEmpty())
                        {
                            foreach (GamePartBlueprint sourceMutation in sourceBlueprint.Mutations.Values)
                            {
                                frankenMutations.AddMutation(sourceMutation.Name);
                            }
                        }
                        bool giveRegen = false;
                        if (giveRegen
                            && MutationFactory.GetMutationEntryByName("Regeneration").Class is string regenerationMutationClass)
                        {
                            if (frankenMutations.GetMutation(regenerationMutationClass) is not BaseMutation regenerationMutation)
                            {
                                frankenMutations.AddMutation(regenerationMutationClass, Level: 10);
                                regenerationMutation = frankenMutations.GetMutation(regenerationMutationClass);
                            }
                            regenerationMutation.CapOverride = 10;

                            if (regenerationMutation.Level < 10)
                            {
                                regenerationMutation.ChangeLevel(10);
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == AnimateEvent.ID
                || ID == AfterObjectCreatedEvent.ID;
        }
        public override bool HandleEvent(AnimateEvent E)
        {
            if (!IsALIVE
                && ParentObject == E.Object)
            {
                IsALIVE = MakeItALIVE(E.Object);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterObjectCreatedEvent E)
        {
            if (ParentObject == E.Object)
            {
                if (AlwaysAnimate && !ParentObject.HasPart<AnimatedObject>())
                {
                    AnimateObject.Animate(ParentObject);
                }
            }
            return base.HandleEvent(E);
        }
    }
}
