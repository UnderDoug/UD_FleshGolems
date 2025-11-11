using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.Wish;
using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.Language;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;

using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;
using XRL.World.Parts;
using XRL.UI;

namespace UD_FleshGolems
{
    [HasWishCommand]
    [HasVariableReplacer]
    public static class Utils
    {
        public static ModInfo ThisMod => ModManager.GetMod(MOD_ID);

        public static string GetPlayerBlueprint()
        {
            if (!EmbarkBuilderConfiguration.activeModules.IsNullOrEmpty())
            {
                foreach (AbstractEmbarkBuilderModule activeModule in EmbarkBuilderConfiguration.activeModules)
                {
                    if (activeModule.type == nameof(QudSpecificCharacterInitModule))
                    {
                        QudSpecificCharacterInitModule characterInit = activeModule as QudSpecificCharacterInitModule;
                        string blueprint = characterInit?.builder?.GetModule<QudGenotypeModule>()?.data?.Entry?.BodyObject
                            ?? characterInit?.builder?.GetModule<QudSubtypeModule>()?.data?.Entry?.BodyObject
                            ?? "Humanoid";
                        return characterInit.builder.info.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, blueprint);
                    }
                }
            }
            return null;
        }

        [VariableReplacer]
        public static string ud_nbsp(DelegateContext Context)
        {
            string nbsp = "\xFF";
            string output = nbsp;
            if (!Context.Parameters.IsNullOrEmpty() && int.TryParse(Context.Parameters[0], out int count))
            {
                for (int i = 1; i < count; i++)
                {
                    output += nbsp;
                }
            }
            return output;
        }

        [VariableReplacer]
        public static string ud_weird(DelegateContext Context)
        {
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty())
            {
                if (Context.Parameters.Count > 1)
                {
                    output = "{{" + Context.Parameters[0] + "|";
                    for (int i = 1; i < Context.Parameters.Count; i++)
                    {
                        if (i > 1)
                        {
                            output += " ";
                        }
                        output += TextFilters.Weird(Context.Parameters[i]);
                    }
                    output += "}}";
                }
                else
                {
                    return TextFilters.Weird(Context.Parameters[0]);
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTag(DelegateContext Context)
        {
            if (!Context.Parameters.IsNullOrEmpty())
            {
                UnityEngine.Debug.Log(nameof(UD_xTag) + "." + nameof(Context.Target) + ": " + (Context?.Target?.DebugName ?? NULL));
                UnityEngine.Debug.Log(nameof(UD_xTag) + "." + nameof(Context.Value) + ": " + (Context?.Value?.ToString() ?? NULL));
                foreach (string parameter in Context.Parameters)
                {
                    UnityEngine.Debug.Log("    " + parameter);
                }
            }
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 1
                && Context.Target is GameObject target
                && target.GetxTag(Context.Parameters[0], Context.Parameters[1]) is string xtag
                && xtag.CachedCommaExpansion().GetRandomElementCosmetic() is string tagValue)
            {
                output = tagValue;
                if (Context.Capitalize)
                {
                    output = tagValue.Capitalize();
                }
            }
            return output;
        }

        public static string AppendTick(string String, bool WithSpaceAfter = true)
        {
            return String + "[" + TICK + "]" + (WithSpaceAfter ? " " : "");
        }
        public static string AppendCross(string String, bool WithSpaceAfter = true)
        {
            return String + "[" + CROSS + "]" + (WithSpaceAfter ? " " : "");
        }
        public static string AppendYehNah(string String, bool Yeh, bool WithSpaceAfter = true)
        {
            return String + "[" + (Yeh ? TICK : CROSS) + "]" + (WithSpaceAfter ? " " : "");
        }

        public static int CapDamageTo1HPRemaining(GameObject Creature, int DamageAmount)
        {
            if (Creature == null || Creature.GetStat("Hitpoints") is not Statistic hitpoints)
            {
                return 0;
            }
            return Math.Min(hitpoints.Value - 1, DamageAmount);
        }

        public static GameObjectBlueprint GetGameObjectBlueprint(string Blueprint)
        {
            return GameObjectFactory.Factory.GetBlueprint(Blueprint);
        }

        /* 
         * 
         * Wishes!
         * 
         */
        [WishCommand( Command = "UD_FleshGolems test kit" )]
        public static bool ReanimationTestKit_WishHandler()
        {
            The.Player.ReceiveObject("Neuro Animator");
            The.Player.ReceiveObject("Antimatter Cell");
            Examiner.IDAll();
            if (Popup.AskNumber("How many corpses do you want?") is int corpseCount)
            {
                for (int i = 0; i < corpseCount; i++)
                {

                }
            }
            return true;
        }
    }
}