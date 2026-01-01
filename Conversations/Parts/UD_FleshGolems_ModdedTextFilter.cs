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
    public class UD_FleshGolems_ModdedTextFilter : IConversationPart
    {
        public string Filters;

        public List<string> FiltersList => Filters?.CachedCommaExpansion() ?? new();

        public UD_FleshGolems_ModdedTextFilter()
        {
            Priority = 1;
            Filters = null;
        }

        public override void Awake()
        {
            base.Awake();
        }
        
        public override bool WantEvent(int ID, int Propagation)
            => base.WantEvent(ID, Propagation)
            || ID == PrepareTextLateEvent.ID
            ;
        
        public override bool HandleEvent(PrepareTextLateEvent E)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(PrepareTextLateEvent)),
                    Debug.Arg(nameof(Filters), Filters ?? "empty"),
                });
            if (!FiltersList.IsNullOrEmpty()
                && E.Text is string text
                && !text.IsNullOrEmpty()
                && ModdedTextFilters.TextFilterEntries
                    ?.Aggregate(
                        seed: new List<MethodInfo>(),
                        func: delegate (List<MethodInfo> a, KeyValuePair<string, MethodInfo> n)
                        {
                            if (FiltersList.Contains(n.Key))
                                a.Add(n.Value);
                            return a;
                        }) is List<MethodInfo> moddedTextFilterMethods)
            {
                foreach (MethodInfo moddedTextFilterMethod in moddedTextFilterMethods)
                    E.Text = moddedTextFilterMethod.Invoke(null, new object[] { E.Text }) as string;
            }
            return base.HandleEvent(E);
        }
    }
}
