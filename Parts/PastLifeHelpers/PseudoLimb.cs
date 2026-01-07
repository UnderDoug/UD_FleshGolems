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
using static XRL.World.Parts.UD_FleshGolems_PastLife;
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
        protected string Type;

        [SerializeField]
        protected string VariantType;

        [SerializeField]
        protected string Manager;

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
            Type = null;
            VariantType = null;
            Manager = null;

            Root = null;
            AttachedTo = null;
            Attached = new();

            DistanceFromRoot = 0;
        }
        public PseudoLimb(BodyPart BodyPart, PseudoLimb AttachedTo, PseudoLimb Root, ref int AmountStored)
            : this ()
        {
            AmountStored++;
            Type = BodyPart.Type;
            VariantType = BodyPart.VariantType;
            Manager = BodyPart.Manager;
            this.AttachedTo = AttachedTo;
            this.Root = Root;
            PseudoLimb root = Root ?? this;
            DistanceFromRoot = AttachedTo == null ? 0 : AttachedTo.DistanceFromRoot + 1;

            foreach (BodyPart subPart in BodyPart?.Parts?.Where(IsConcreteIntrinsic)?.ToList() ?? new())
                Attached.Add(new PseudoLimb(subPart, this, Root ?? this, ref AmountStored));
        }

        public override string ToString()
        {
            if (GetModel() is not BodyPartType limbModel)
                return "Invalid";

            string variant = VariantType ?? "base";
            string manager = Manager.IsNullOrEmpty() ? null : ("[::" + Manager + "]");
            return Type + "/" + variant + "" + manager;
        }

        public BodyPartType GetModel()
            => Anatomies.GetBodyPartTypeOrFail(VariantType ?? Type);

        public bool GiveToEntity(GameObject Entity, BodyPart AttachAt, ref int AmountGiven)
        {
            string attachAtString = AttachAt?.BodyPartString();

            if (Entity.Body is not Body parentBody)
                return false;

            if (GetModel() is not BodyPartType bodyPartType)
                return false;

            BodyPart newPart = new(
                Base: bodyPartType,
                ParentBody: parentBody,
                Manager: Manager,
                Dynamic: true);

            if (newPart == null)
                return false;

            if (newPart.Laterality == 0)
            {
                if (!bodyPartType.UsuallyOn.IsNullOrEmpty() && bodyPartType.UsuallyOn != AttachAt.Type)
                {
                    BodyPartType attachAtPartType = AttachAt.VariantTypeModel();
                    newPart.ModifyNameAndDescriptionRecursively(attachAtPartType.Name.Replace(" ", "-"), attachAtPartType.Description.Replace(" ", "-"));
                }
                if (AttachAt.Laterality != 0)
                    newPart.ChangeLaterality(AttachAt.Laterality | newPart.Laterality, Recursive: true);
            }

            AttachAt.AddPart(newPart, newPart.Type, new string[2] { "Thrown Weapon", "Floating Nearby" });

            AmountGiven++;
            foreach (PseudoLimb subPart in Attached)
                subPart.GiveToEntity(Entity, newPart, ref AmountGiven);

            return true;
        }

        public void DebugPseudoLimb(int Indent = 0)
        {
            UnityEngine.Debug.Log(" ".ThisManyTimes(Indent * 4) + ToString());
            foreach (PseudoLimb pseudoLimb in Attached)
                pseudoLimb.DebugPseudoLimb(++Indent);
        }
    }
}
