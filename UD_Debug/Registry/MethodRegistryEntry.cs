using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace UD_FleshGolems.Logging
{
    public struct MethodRegistryEntry : IEquatable<MethodBase>
    {
        public MethodBase MethodBase;
        public bool Value;

        public MethodRegistryEntry(MethodBase MethodBase, bool Value)
            : this()
        {
            this.MethodBase = MethodBase;
            this.Value = Value;
        }

        public readonly void Deconstruct(out MethodBase MethodBase)
        {
            MethodBase = this.MethodBase;
        }

        public readonly void Deconstruct(out bool Value)
        {
            Value = this.Value;
        }

        public readonly void Deconstruct(out MethodBase MethodBase, out bool Value)
        {
            Deconstruct(out MethodBase);
            Deconstruct(out Value);
        }

        public readonly string GetTypeAndMethodName()
        {
            return MethodBase.DeclaringType.Name + "." + MethodBase.Name;
        }
        public override readonly string ToString() => GetTypeAndMethodName() + ": " + Value;

        public static implicit operator bool(MethodRegistryEntry Operand)
        {
            return Operand.Value;
        }

        public static explicit operator MethodBase(MethodRegistryEntry Operand)
        {
            return Operand.MethodBase;
        }

        public static explicit operator MethodRegistryEntry(MethodBase Operand)
        {
            return new(Operand, true);
        }

        public static explicit operator KeyValuePair<MethodBase, bool>(MethodRegistryEntry Operand)
        {
            return new(Operand.MethodBase, Operand.Value);
        }

        public static explicit operator MethodRegistryEntry(KeyValuePair<MethodBase, bool> Operand)
        {
            return new(Operand.Key, Operand.Value);
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is KeyValuePair<MethodBase, bool> kvp)
            {
                return kvp.Key.Equals(MethodBase) && kvp.Value.Equals(Value);
            }
            if (obj is MethodBase methodBase)
            {
                return MethodBase.Equals(methodBase);
            }
            if (obj is MethodInfo methodInfo)
            {
                return MethodBase.Equals(methodInfo);
            }
            return base.Equals(obj);
        }

        public override readonly int GetHashCode()
        {
            int methodBase = MethodBase.GetHashCode();
            int value = Value.GetHashCode();
            return methodBase ^ value;
        }

        public readonly bool Equals(MethodBase other)
        {
            return other.Equals(MethodBase);
        }

        public static bool operator ==(MethodRegistryEntry Operand1, MethodBase Operand2) => Operand1.MethodBase == Operand2;
        public static bool operator !=(MethodRegistryEntry Operand1, MethodBase Operand2) => !(Operand1 == Operand2);

        public static bool operator ==(MethodBase Operand1, MethodRegistryEntry Operand2) => Operand2 == Operand1;
        public static bool operator !=(MethodBase Operand1, MethodRegistryEntry Operand2) => Operand2 != Operand1;

        public static bool operator ==(MethodRegistryEntry Operand1, KeyValuePair<MethodBase, bool> Operand2) => Operand1.Equals(Operand2);
        public static bool operator !=(MethodRegistryEntry Operand1, KeyValuePair<MethodBase, bool> Operand2) => !(Operand1 == Operand2);

        public static bool operator ==(KeyValuePair<MethodBase, bool> Operand1, MethodRegistryEntry Operand2) => Operand2 == Operand1;
        public static bool operator !=(KeyValuePair<MethodBase, bool> Operand1, MethodRegistryEntry Operand2) => Operand2 != Operand1;
    }
}
