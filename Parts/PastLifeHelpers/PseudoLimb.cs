using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;
using Qud.API;

using XRL;
using XRL.UI;
using XRL.Wish;
using XRL.Rules;
using XRL.Language;
using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;
using XRL.World.Effects;
using XRL.World.Capabilities;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;

namespace UD_FleshGolems.Parts.PastLifeHelpers
{
    [Serializable]
    public class PseudoLimb : IComposite
    {
        [SerializeField]
        protected int ID;

        [SerializeField]
        protected BodyPartType Model;

        [SerializeField]
        protected bool Dynamic;

        [SerializeField]
        protected PseudoLimb Root;

        [SerializeField]
        protected PseudoLimb AttachedTo;

        [SerializeField]
        protected List<PseudoLimb> Attached;

        [SerializeField]
        protected int DistanceFromRoot;

        public bool IsRoot => Root == null && AttachedTo == null;

        public PseudoLimb()
        {
            ID = 0;
            Model = null;
            Dynamic = false;

            Root = null;
            AttachedTo = null;
            Attached = new();

            DistanceFromRoot = 0;
        }
        public PseudoLimb(BodyPart BodyPart, PseudoLimb AttachedTo = null, PseudoLimb Root = null)
        {
            ID = BodyPart?.ID ?? 0;
            Model = Anatomies.GetBodyPartType(BodyPart.Name);
            Dynamic = BodyPart.Dynamic;
            this.AttachedTo = AttachedTo;
            this.Root = Root;
            PseudoLimb root = Root ?? this;
            DistanceFromRoot = AttachedTo == null ? 0 : AttachedTo.DistanceFromRoot + 1;
            foreach (BodyPart subPart in BodyPart?.Parts?.Where(bp => bp.Native && !bp.Extrinsic))
            {
                Attached.Add(new PseudoLimb(BodyPart, this, Root ?? this));
            }
        }
        public PseudoLimb(Body Body) : this(Body?.GetBody()) { }

        public override string ToString()
            => "[" + ID + "]" + Model.FinalType + "(" + (Model.Name ?? "base") + ")";

        public bool GiveToEntity(GameObject Entity)
        {
            if (!IsRoot)
            {
                return false;
            }
            if (Entity.Body is not Body parentBody)
            {
                return false;
            }
            foreach (BodyPart BodyPart in parentBody.GetBody().Parts.Where(bp => bp.Native && !bp.Extrinsic))
            {
                //PseudoLimb pseudoLimb 
                //if (BodyPart.Dynamic)
            }
            return true;
        }
        public bool GiveToEntity(GameObject Entity, BodyPart AttachAt)
        {
            if (Entity.Body is not Body parentBody)
            {
                return false;
            }
            if (Dynamic)
            {
                AttachAt.AddPart(new BodyPart(Model, parentBody, Dynamic: Dynamic));
            }
            return true;
        }
    }
}
