using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;

using HarmonyLib;

using XRL;

using UD_FleshGolems;
using static UD_FleshGolems.Options;
using static UD_FleshGolems.Utils;

namespace UD_FleshGolems.Logging
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    public static class Debug
    {
        private static bool SilenceLogging = false;

        public static void SetSilenceLogging(bool Value)
        {
            SilenceLogging = Value;
        }
        public static void ToggleLogging()
        {
            SetSilenceLogging(!SilenceLogging);
        }

        private static bool _DoDebug => DebugEnableLogging && !SilenceLogging;
        public static bool DoDebug
        {
            get
            {
                try
                {
                    if (TryGetCallingTypeAndMethod(out _, out MethodBase callingMethod)
                        && GetRegistry() is List<MethodRegistryEntry> registry
                        && registry.Contains(callingMethod))
                    {
                        if (!registry.GetValue(callingMethod))
                        {
                            if (!DebugEnableAllLogging)
                            {
                                return false;
                            }
                        }
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(typeof(Debug) + "." + nameof(DoDebug), x, "game_mod_exception");
                }
                return _DoDebug;
            }
        }
        private static List<MethodRegistryEntry> _DoDebugRegistry = new();
        public static List<MethodRegistryEntry> DoDebugRegistry => GetRegistry();
        public static void Register(
            Type Class,
            string MethodName,
            bool Value,
            List<MethodRegistryEntry> Registry,
            out List<MethodRegistryEntry> ReturnRegistry)
        {
            Register(Class?.GetMethod(MethodName), Value, Registry, out ReturnRegistry);
        }
        public static void Register(
            MethodBase MethodBase,
            bool Value,
            List<MethodRegistryEntry> Registry,
            out List<MethodRegistryEntry> ReturnRegistry)
        {
            string declaringType = MethodBase?.DeclaringType?.Name;
            UnityEngine.Debug.Log(nameof(Debug) + "." + nameof(Register) + "(" + declaringType + "." + MethodBase.Name + ": " + Value + ")");
            Registry.Add(new(MethodBase, Value));
            ReturnRegistry = Registry;
        }
        public static void Register(this List<MethodRegistryEntry> Registry, Type Class, string MethodName, bool Value)
        {
            Register(Class, MethodName, Value, Registry, out _DoDebugRegistry);
        }
        public static void Register(this List<MethodRegistryEntry> Registry, MethodBase MethodBase, bool Value)
        {
            Register(MethodBase, Value, Registry, out _DoDebugRegistry);
        }
        public static void Register(this List<MethodRegistryEntry> Registry, string MethodName, bool Value)
        {
            TryGetCallingTypeAndMethod(out Type CallingType, out _);
            foreach (MethodBase methodBase in CallingType.GetMethods())
            {
                if (methodBase.Name == MethodName)
                {
                    Register(methodBase, Value, Registry, out _DoDebugRegistry);
                }
            }
        }
        public static void Register(this List<MethodRegistryEntry> Registry, MethodRegistryEntry RegisterEntry)
        {
            Register((MethodBase)RegisterEntry, RegisterEntry, Registry, out _DoDebugRegistry);
        }
        public static List<MethodRegistryEntry> GetRegistry()
        {
            if (_GotRegistry)
            {
                return _DoDebugRegistry;
            }
            try
            {
                List<MethodInfo> debugRegistryMethods = ModManager.GetMethodsWithAttribute(typeof(UD_FleshGolems_DebugRegistryAttribute));
                foreach (MethodInfo debugRegistryMethod in debugRegistryMethods)
                {
                    debugRegistryMethod.Invoke(null, new object[] { _DoDebugRegistry });
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(Debug) + "." + nameof(GetRegistry), x, "game_mod_exception");
                _GotRegistry = true;
            }
            _GotRegistry = true;
            return _DoDebugRegistry;
        }

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [GameBasedStaticCache( ClearInstance = false )]
        private static bool _GotRegistry = false;

        [ModSensitiveStaticCache(CreateEmptyInstance = true)]
        [GameBasedStaticCache(ClearInstance = false)]
        private static Stack<Indent> Indents = new();

        public static Indent LastIndent => Indents?.Peek();

        public static Indent GetNewIndent(int Offset)
        {
            return new(Offset, 4, ' ');
        }
        public static Indent GetNewIndent()
        {
            return GetNewIndent(0);
        }

        [GameBasedCacheInit]
        [ModSensitiveCacheInit]
        public static void ResetIndent()
        {
            Indents ??= new();
            Indents.Clear();
            Indents.Push(GetNewIndent());
        }
        public static void ResetIndent(out Indent Indent)
        {
            ResetIndent();
            Indent = Indents.Peek();
        }

        public static Indent DiscardIndent()
        {
            if (Indents.Count > 0)
            {
                Indents.Pop();
            }
            if (Indents.IsNullOrEmpty())
            {
                ResetIndent();
            }
            return LastIndent;
        }
        public static Indent SetIndent(Indent Indent)
        {
            DiscardIndent();
            return LastIndent.SetIndent(Indent);
        }

        public static Indent GetIndent(out Indent Indent)
        {
            Indent = new(LastIndent);
            Indents.Push(Indent);
            return Indent;
        }

        public static string GetCallingTypeAndMethod(bool AppendSpace = false, bool TrimModPrefix = true)
        {
            if (TryGetCallingTypeAndMethod(out Type declaringType, out MethodBase methodBase))
            {
                string declaringTypeName = declaringType.Name;
                if (TrimModPrefix)
                {
                    declaringTypeName = declaringTypeName.Replace(ThisMod.ID + "_", "");
                }
                return declaringTypeName + "." + methodBase.Name + (AppendSpace ? " " : "");
            }
            return null;
        }
        public static string GetCallingMethod(bool AppendSpace = false)
        {
            if (TryGetCallingTypeAndMethod(out _, out MethodBase methodBase))
            {
                return methodBase.Name + (AppendSpace ? " " : "");
            }
            return null;
        }
        public static bool TryGetCallingTypeAndMethod(out Type CallingType, out MethodBase Method)
        {
            CallingType = null;
            Method = null;
            StackFrame[] stackFrames = new StackTrace().GetFrames();
            int stackTraceCount = Math.Min(stackFrames.Length, 8);
            for (int i = 0; i < stackTraceCount; i++)
            {
                if (stackFrames[i].GetMethod() is MethodBase methodBase
                    && methodBase.DeclaringType is Type declaringType
                    && !declaringType.Equals(typeof(UD_FleshGolems.Logging.Debug)))
                {
                    CallingType = declaringType;
                    Method = methodBase;
                    return true;
                }
            }
            return false;
        }

        public static Indent Log<T>(string Field, T Value, Indent Indent = null)
        {
            if (!DoDebug)
            {
                return Indent;
            }
            Indent ??= LastIndent;
            string output = Field;
            if (Value != null &&
                !Value.ToString().IsNullOrEmpty())
            {
                output += ": " + Value;
            }
            UnityEngine.Debug.Log(Indent.ToString() + output);
            return Indent;
        }
        public static Indent Log<T>(string Field, T Value, out Indent Indent)
        {
            GetIndent(out Indent);
            return Log(Field, Value, Indent);
        }
        public static Indent Log(string Message, Indent Indent = null)
        {
            return Log(Message, (string)null, Indent);
        }

        public readonly struct ArgPair
        {
            private readonly string Name;
            private readonly object Value;
            public ArgPair(string Name, object Value)
            {
                this.Name = Name;
                this.Value = Value;
            }
            public override readonly string ToString()
            {
                if (Name.IsNullOrEmpty())
                {
                    return Value?.ToString();
                }
                return Name + ": " + Value?.ToString();
            }
        }
        public static ArgPair LogArg(string Name, object Value)
        {
            return new ArgPair(Name, Value);
        }
        public static ArgPair LogArg(object Value)
        {
            return LogArg(null, Value);
        }
        public static Indent LogCaller(Indent Indent = null)
        {
            return Log(GetCallingTypeAndMethod(), Indent);
        }

        public static Indent LogCaller(string MessageAfter, Indent Indent = null, params ArgPair[] ArgPairs)
        {
            string output = "";
            if (!ArgPairs.IsNullOrEmpty())
            {
                output += "(" + ArgPairs.ToList().ConvertAll(ap => ap.ToString()).Join() + ")";
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += " " + MessageAfter;
            }
            return Log(GetCallingTypeAndMethod() + output, Indent);
        }
        public static Indent LogCaller(Indent Indent = null, params ArgPair[] ArgPairs)
        {
            return LogCaller(null, Indent, ArgPairs);
        }
        public static Indent LogCaller(string MessageAfter, out Indent Indent, params ArgPair[] ArgPairs)
        {
            GetIndent(out Indent);
            return LogCaller(MessageAfter, Indent[1], ArgPairs);
        }
        public static Indent LogCaller(out Indent Indent, params ArgPair[] ArgPairs)
        {
            return LogCaller(null, out Indent, ArgPairs);
        }
        public static Indent LogMethod(string MessageAfter, Indent Indent = null, params ArgPair[] ArgPairs)
        {
            string output = "";
            if (!ArgPairs.IsNullOrEmpty())
            {
                output += "(" + ArgPairs.ToList().ConvertAll(ap => ap.ToString()).Join() + ")";
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += " " + MessageAfter;
            }
            return Log(GetCallingMethod() + output, Indent);
        }
        public static Indent LogMethod(Indent Indent = null, params ArgPair[] ArgPairs)
        {
            return LogMethod(null, Indent, ArgPairs);
        }
        public static Indent LogMethod(string MessageAfter, out Indent Indent, params ArgPair[] ArgPairs)
        {
            GetIndent(out Indent);
            return LogMethod(MessageAfter, Indent[1], ArgPairs);
        }
        public static Indent LogMethod(out Indent Indent, params ArgPair[] ArgPairs)
        {
            return LogMethod(null, out Indent, ArgPairs);
        }

        public static Indent LogHeader(string Message, out Indent Indent)
        {
            GetIndent(out Indent);
            return Log(GetCallingTypeAndMethod(true) + Message, Indent);
        }
        public static Indent YehNah(string Message, object Value, bool? Good = null, Indent Indent = null)
        {
            string append = null;
            if (Good != null)
            {
                if (!Good.GetValueOrDefault())
                {
                    append = AppendCross("");
                }
                else
                {
                    append = AppendTick("");
                }
            }
            else
            {
                append = "[-] ";
            }
            return Log(append + Message, Value, Indent);
        }
        public static Indent YehNah(string Message, bool? Good = null, Indent Indent = null)
        {
            return YehNah(Message, null, Good, Indent);
        }
        public static Indent CheckYeh(string Message, object Value, Indent Indent = null)
        {
            return YehNah(Message, Value, true, Indent);
        }
        public static Indent CheckYeh(string Message, Indent Indent = null)
        {
            return YehNah(Message, null, true, Indent);
        }
        public static Indent CheckNah(string Message, object Value, Indent Indent = null)
        {
            return YehNah(Message, Value, false, Indent);
        }
        public static Indent CheckNah(string Message, Indent Indent = null)
        {
            return YehNah(Message, null, false, Indent);
        }
    }
}
