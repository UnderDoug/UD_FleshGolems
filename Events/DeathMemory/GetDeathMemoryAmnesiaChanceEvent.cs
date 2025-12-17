using System;
using System.Collections.Generic;
using System.Text;

using UD_FleshGolems.Parts.VengeanceHelpers;

using XRL.World;
using XRL.World.Parts;

namespace UD_FleshGolems.Events
{
    [GameEvent(Cache = Cache.Pool)]
    public class GetDeathMemoryAmnesiaChanceEvent : IDeathMemoryEvent<GetDeathMemoryAmnesiaChanceEvent>
    {
        public KillerDetails? KillerDetails;
        public DeathDescription DeathDescription;

        public int BaseChance;
        public int Chance;

        public string Context;

        public GetDeathMemoryAmnesiaChanceEvent()
            : base()
        {
            KillerDetails = null;
            DeathDescription = null;
            BaseChance = 0;
            Chance = 0;
            Context = null;
        }

        public override void Reset()
        {
            base.Reset();
            BaseChance = 0;
            Chance = 0;
            KillerDetails = null;
            DeathDescription = null;
            Context = null;
        }

        public static GetDeathMemoryAmnesiaChanceEvent FromPool(
            GameObject Corpse,
            GameObject Killer,
            GameObject Weapon,
            KillerDetails? KillerDetails,
            DeathDescription DeathDescription,
            int BaseChance,
            string Context)
        {
            if (Corpse == null)
            {
                return FromPool();
            }
            GetDeathMemoryAmnesiaChanceEvent E = FromPool(Corpse, Killer, Weapon);
            E.KillerDetails = KillerDetails;
            E.DeathDescription = DeathDescription;
            E.BaseChance = BaseChance;
            E.Chance = BaseChance;
            E.Context = Context;
            return E;
        }

        public override Event GetStringyEvent()
            => base.GetStringyEvent()
                .AddParameter(nameof(KillerDetails), KillerDetails)
                .AddParameter(nameof(DeathDescription), DeathDescription)
                .AddParameter(nameof(BaseChance), BaseChance)
                .AddParameter(nameof(Chance), Chance);

        protected override bool UpdateStringyEvent()
        {
            if (!base.UpdateStringyEvent())
                return false;

            StringyEvent.AddParameter(nameof(Chance), Chance);

            return true;
        }

        public static int GetFor(
            GameObject Corpse,
            GameObject Killer,
            GameObject Weapon,
            KillerDetails? KillerDetails,
            DeathDescription DeathDescription,
            int BaseChance,
            string Context)
            => FromPool(Corpse, Killer, Weapon, KillerDetails, DeathDescription, BaseChance, Context)
                ?.ValidateAndProcess()
                ?.Chance
            ?? DeathMemory.BaseAmnesiaChance;
    }
}
