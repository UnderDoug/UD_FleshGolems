using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.Anatomy;
using XRL.World.Effects;

using UD_FleshGolems.Logging;
using static UD_FleshGolems.Const;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_EnforceCyberneticRejectionSyndrome : IScribedPart
    {
        public bool AppliedInitial;

        public double CostStackMultiplier;

        public UD_FleshGolems_EnforceCyberneticRejectionSyndrome()
        {
            AppliedInitial = false;
            CostStackMultiplier = 1.0;
        }

        public static int GetCyberneticRejectionSyndromeChance(GameObject Implantee, GameObject InstalledCybernetic, int Cost, string Slots)
        {
            if (!Implantee.IsMutant())
            {
                return 0;
            }
            int cyberneticModifier = InstalledCybernetic.GetIntProperty("CyberneticRejectionSyndromeModifier");
            int implanteeModifier = Implantee.GetIntProperty("CyberneticRejectionSyndromeModifier");
            int chance = 5 + Cost + cyberneticModifier + implanteeModifier;
            if (Implantee.TryGetPart<Mutations>(out var Part) && Part.MutationList != null)
            {
                foreach (BaseMutation mutation in Part.MutationList)
                {
                    if (!mutation.IsPhysical() && Slots != "Head")
                    {
                        // mental mutations only give their full level if the cybernetic is install in the head.
                        chance += 1;
                    }
                    else
                    {
                        chance += mutation.Level;
                    }
                }
            }
            return chance;
        }

        public static bool ProcessCybernetic(GameObject Implantee, GameObject InstalledCybernetic, double CostStackMultiplier = 1.0)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Implantee), Implantee?.DebugName ?? NULL),
                    Debug.Arg(nameof(InstalledCybernetic), InstalledCybernetic?.DebugName ?? NULL),
                });

            if (!InstalledCybernetic.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItemPart))
            {
                Debug.CheckNah("No " + nameof(CyberneticsBaseItem), indent[1]);
                return false;
            }
            int chance = GetCyberneticRejectionSyndromeChance(
                Implantee: Implantee,
                InstalledCybernetic: InstalledCybernetic,
                Cost: cyberneticsBaseItemPart.Cost,
                Slots: cyberneticsBaseItemPart.Slots);

            if (chance < 1)
            {
                Debug.CheckNah(nameof(chance) + " < " + 1, indent[1]);
                return false;
            }
            string cRS_Key = CyberneticsBaseItem.GetCyberneticRejectionSyndromeKey(Implantee);
            if (!InstalledCybernetic.HasIntProperty(cRS_Key))
            {
                InstalledCybernetic.SetIntProperty(cRS_Key, chance.in100() ? 1 : 0);
            }
            if (InstalledCybernetic.GetIntProperty(cRS_Key) < 1)
            {
                Debug.CheckNah(nameof(InstalledCybernetic) + " (" + cRS_Key + ")", indent[1]);
                return false;
            }
            Debug.CheckYeh(nameof(InstalledCybernetic) + " (" + cRS_Key + ")", indent[1]);
            int cost = (int)Math.Ceiling(cyberneticsBaseItemPart.Cost * CostStackMultiplier);
            return Implantee.ForceApplyEffect(new CyberneticRejectionSyndrome(cost));
        }
        public bool ProcessCybernetic(GameObject InstalledCybernetic)
        {
            return ProcessCybernetic(ParentObject, InstalledCybernetic);
        }

        public static bool UnprocessCybernetic(GameObject Implantee, GameObject InstalledCybernetic)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Implantee), Implantee?.DebugName ?? NULL),
                    Debug.Arg(nameof(InstalledCybernetic), InstalledCybernetic?.DebugName ?? NULL),
                });

            string cRS_Key = CyberneticsBaseItem.GetCyberneticRejectionSyndromeKey(Implantee);
            if (InstalledCybernetic.GetIntProperty(cRS_Key) < 1)
            {
                Debug.CheckNah(nameof(InstalledCybernetic) + " (" + cRS_Key + ")", indent[1]);
                return false;
            }
            if (!Implantee.TryGetEffect(out CyberneticRejectionSyndrome cRS))
            {
                Debug.CheckNah("No " + nameof(CyberneticRejectionSyndrome), indent[1]);
                return false;
            }
            if (!InstalledCybernetic.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItemPart))
            {
                Debug.CheckNah("No " + nameof(CyberneticsBaseItem), indent[1]);
                return false;
            }
            cRS.Reduce(cyberneticsBaseItemPart.Cost);
            Debug.CheckYeh(nameof(CyberneticRejectionSyndrome) + "." + nameof(cRS.Reduce), cyberneticsBaseItemPart.Cost, indent[1]);
            return true;
        }
        public bool UnprocessCybernetic(GameObject InstalledCybernetic)
        {
            return UnprocessCybernetic(ParentObject, InstalledCybernetic);
        }

        public override bool AllowStaticRegistration() => true;

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == ImplantedEvent.ID
            || ID == UnimplantedEvent.ID
            || ID == EnteredCellEvent.ID
            || ID == GetDebugInternalsEvent.ID;

        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (!AppliedInitial)
            {
                using Indent indent = new(1);
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(EnteredCellEvent)),
                        Debug.Arg(nameof(E.Object), E.Object?.DebugName ?? NULL),
                    });

                if (ParentObject != null && E.Object == ParentObject)
                {
                    AppliedInitial = true;

                    foreach (GameObject installedCybernetic in E.Object.GetInstalledCybernetics())
                    {
                        ProcessCybernetic(installedCybernetic);
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ImplantedEvent E)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ImplantedEvent)),
                    Debug.Arg(nameof(E.Implantee), E.Implantee?.DebugName ?? NULL),
                    Debug.Arg(nameof(E.Item), E.Item?.DebugName ?? NULL),
                });

            if (E.Implantee == ParentObject && E.Item != null)
            {
                ProcessCybernetic(E.Item);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnimplantedEvent E)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(UnimplantedEvent)),
                    Debug.Arg(nameof(E.Implantee), E.Implantee?.DebugName ?? NULL),
                    Debug.Arg(nameof(E.Item), E.Item?.DebugName ?? NULL),
                });

            if (E.Implantee == ParentObject && E.Item != null)
            {
                UnprocessCybernetic(E.Item);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(AppliedInitial), AppliedInitial);
            E.AddEntry(this, nameof(CostStackMultiplier), CostStackMultiplier);
            return base.HandleEvent(E);
        }
    }
}
