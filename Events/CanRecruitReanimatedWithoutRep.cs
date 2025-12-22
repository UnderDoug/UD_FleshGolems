using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.World.Parts;

namespace UD_FleshGolems.Events
{
    [GameEvent(Base = true, Cascade = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS, Cache = Cache.Pool)]
    public class CanRecruitReanimatedWithoutRep : ModSingletonEvent<CanRecruitReanimatedWithoutRep>
    {
        public new static readonly int CascadeLevel = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS;

        public GameObject Speaker;

        public GameObject Player;

        public CanRecruitReanimatedWithoutRep()
        {
            Speaker = null;
            Player = null;
        }
        public override void Reset()
        {
            base.Reset();
            Speaker = null;
            Player = null;
        }

        public override int GetCascadeLevel()
            => CascadeLevel;

        public static bool Check(GameObject Player, GameObject Speaker)
        {
            Instance.Player = Player;
            Instance.Speaker = Speaker;

            bool success = Instance.Check(Instance.Player)
                && Instance.Check(Instance.Player);

            Instance.Reset();

            return success;
        }
        public bool Check(GameObject Handler)
            => Handler == null
            || !Handler.WantEvent(ID, CascadeLevel)
            || Handler.HandleEvent(Instance)
            ;
    }
}
