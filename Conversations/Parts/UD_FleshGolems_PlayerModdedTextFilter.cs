using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Text;
using XRL.World.AI;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Events;
using UD_FleshGolems.Parts.VengeanceHelpers;

using static UD_FleshGolems.Const;
using static UD_FleshGolems.Options;
using static UD_FleshGolems.Utils;
using UD_FleshGolems.Attributes;
using System.Reflection;
using UD_FleshGolems.ModdedText;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_PlayerModdedTextFilter : IConversationPart
    {
        public static List<string> BaseGameFilters => new()
        {
            nameof(TextFilters.Angry),
            nameof(TextFilters.Corvid),
            nameof(TextFilters.WaterBird),
            nameof(TextFilters.Fish),
            nameof(TextFilters.Frog),
            nameof(TextFilters.Leet),
            nameof(TextFilters.Lallated),
            nameof(TextFilters.Weird),
            nameof(TextFilters.CrypticMachine),
        };
        public static Dictionary<string, string> BaseGameBlueprintFilterMappings => new()
        {
            { "BaseCannibal", nameof(TextFilters.Angry) },
            { "White Esh", nameof(TextFilters.WaterBird) },
            { "BaseBird", nameof(TextFilters.Corvid) },
            { "BaseFish", nameof(TextFilters.Fish) },
            { "BaseFrog", nameof(TextFilters.Frog) },
            { "Svardym", nameof(TextFilters.Frog) },
            { "BaseRobot", nameof(TextFilters.Leet) },
        };

        public MethodInfo FilterMethod;
        public bool BaseOnly;

        public UD_FleshGolems_PlayerModdedTextFilter()
        {
            FilterMethod = null;
            BaseOnly = false;
        }

        private static bool TryFindBaseGameFilter(string Key, out string FilterName)
        {
            FilterName = null;
            if (!Key.IsNullOrEmpty()
                && !BaseGameBlueprintFilterMappings.IsNullOrEmpty()
                && BaseGameBlueprintFilterMappings.Keys.ToList() is List<string> filterNames
                && filterNames.Find(s => s.EqualsNoCase(Key)) is string filterName
                && BaseGameBlueprintFilterMappings.TryGetValue(filterName, out string baseFilterName))
            {
                FilterName = baseFilterName;
                return true;
            }
            return false;
        }
        private static bool TryFindFilter(string Key, out MethodInfo FilterMethod)
        {
            FilterMethod = null;
            return !Key.IsNullOrEmpty()
                && !ModdedTextFilters.TextFilterEntries.IsNullOrEmpty()
                && ModdedTextFilters.TextFilterEntries.Keys.ToList() is List<string> filterNames
                && filterNames.Find(s => s.EqualsNoCase(Key)) is string filterName
                && ModdedTextFilters.TextFilterEntries.TryGetValue(filterName, out FilterMethod);
        }
        public static string FilterText(
            GameObject Player,
            string Text,
            MethodInfo FilterMethodOverride,
            out MethodInfo FilterMethod,
            bool BaseOnlyOverride,
            out bool BaseOnly)
        {
            FilterMethod = FilterMethodOverride;
            BaseOnly = BaseOnlyOverride;

            if (Player == null
                || Text.IsNullOrEmpty())
                return Text;

            if (!BaseOnly)
            {
                if (FilterMethod != null)
                    return FilterMethod.Invoke(null, new object[] { Text }) as string;

                if (TryFindFilter(Player.Blueprint, out FilterMethod))
                    return FilterMethod.Invoke(null, new object[] { Text }) as string;

                if (TryFindFilter(Player.GetSubtype(), out FilterMethod))
                    return FilterMethod.Invoke(null, new object[] { Text }) as string;

                if (TryFindFilter(Player.GetGenotype(), out FilterMethod))
                    return FilterMethod.Invoke(null, new object[] { Text }) as string;

                if (TryFindFilter(Player.GetSpecies(), out FilterMethod))
                    return FilterMethod.Invoke(null, new object[] { Text }) as string;

                if (TryFindFilter(Player.GetBlueprint().GetBase(), out FilterMethod))
                    return FilterMethod.Invoke(null, new object[] { Text }) as string;

                if (TryFindFilter(Player.GetBlueprint().GetBase(), out FilterMethod))
                    return FilterMethod.Invoke(null, new object[] { Text }) as string;
            }
            if (TryFindBaseGameFilter(Player.GetBlueprint().GetBase(), out string baseGameFilterName))
            {
                BaseOnly = true;
                return TextFilters.Filter(Text, baseGameFilterName);
            }

            return Text;
        }
        public string FilterText(string Text)
            => FilterText(
                Player: The.Player,
                Text: Text, 
                FilterMethodOverride: FilterMethod,
                FilterMethod: out FilterMethod,
                BaseOnlyOverride: BaseOnly,
                BaseOnly: out BaseOnly);

        public override void Awake()
        {
            base.Awake();
        }
        
        public override bool WantEvent(int ID, int Propagation)
            => base.WantEvent(ID, Propagation)
            || ID == EnterElementEvent.ID
            ;
        
        public override bool HandleEvent(EnterElementEvent E)
        {
            List<Choice> choices = ParentElement
                    ?.Elements
                    ?.Where(e => e is Choice)
                    ?.Select(e => e as Choice)
                    ?.ToList()
                ?? new();
            foreach (Choice choice in choices)
            {
                choice.Text = FilterText(choice?.Text);
                if (choice.Texts is List<ConversationText> choiceTexts)
                    foreach (ConversationText choiceText in choiceTexts)
                        choiceText.Text = FilterText(choiceText?.Text);
            }
            return base.HandleEvent(E);
        }
    }
}
