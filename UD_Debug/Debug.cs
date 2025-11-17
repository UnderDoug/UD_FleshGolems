using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

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
            Register(Class.GetMethod(MethodName), Value, Registry, out ReturnRegistry);
        }
        public static void Register(
            MethodBase MethodBase,
            bool Value,
            List<MethodRegistryEntry> Registry,
            out List<MethodRegistryEntry> ReturnRegistry)
        {
            string declaringType = MethodBase.DeclaringType.Name;
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

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [GameBasedStaticCache( ClearInstance = false )]
        private static Indent _LastIndent = null;

        public static Indent LastIndent
        {
            get => _LastIndent ??= GetNewIndent();
            set => _LastIndent = value;
        }

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [GameBasedStaticCache( ClearInstance = false )]
        private static Indents _LastIndents = null;

        public static Indents LastIndents
        {
            get => _LastIndents ??= new(GetNewIndent());
            set => _LastIndents = value;
        }

        public static Indent GetNewIndent()
        {
            return new(0, 4, ' ');
        }

        public static void ResetIndent(out Indent Indent)
        {
            _LastIndents = null;
            Indent = LastIndents;
            _LastIndent = Indent;
        }
        public static void ResetIndent(out Indents Indent)
        {
            _LastIndents = null;
            Indent = LastIndents;
            _LastIndent = Indent;
        }
        [GameBasedCacheInit]
        [ModSensitiveCacheInit]
        public static void ResetIndent()
        {
            ResetIndent(out Indents _);
        }
        public static void GetIndent(out Indent Indent)
        {
            Indent = LastIndents;
        }
        public static void SetIndent(Indent Indent)
        {
            LastIndent = Indent;
            LastIndents = new(Indent);
        }
        public static void GetIndents(int Offset, out Indents Indents)
        {
            Indents = new(Offset, LastIndents);
            LastIndents = Indents;
        }
        public static void GetIndents(out Indents Indents)
        {
            GetIndents(0, out Indents);
        }

        public static string GetCallingTypeAndMethod(bool AppendSpace = false)
        {
            if (TryGetCallingTypeAndMethod(out Type declaringType, out MethodBase methodBase))
            {
                return declaringType.Name + "." + methodBase.Name + (AppendSpace ? " " : "");
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

        public static void Log<T>(string Field, T Value, Indent Indent = null)
        {
            if (!DoDebug)
            {
                return;
            }
            Indent ??= GetNewIndent();
            string output = Field;
            if (Value != null &&
                !Value.ToString().IsNullOrEmpty())
            {
                output += ": " + Value;
            }
            UnityEngine.Debug.Log(Indent + output);
            SetIndent(Indent);
        }
        public static void Log<T>(string Field, T Value, out Indents Indent)
        {
            GetIndents(out Indent);
            Log(Field, Value, Indent);
        }
        public static void Log(string Message, Indent Indent = null)
        {
            Log(Message, (string)null, Indent);
        }
        public static void Log(string Message, out Indent Indent)
        {
            GetIndent(out Indent);
            Log(Message, (string)null, Indent);
        }
        public static void LogCaller(Indent Indent = null) => Log(GetCallingTypeAndMethod(), Indent);
        public static void LogHeader(string Message, out Indents Indent)
        {
            GetIndents(out Indent);
            Log(GetCallingTypeAndMethod(true) + Message, Indent);
        }
        public static void CheckYeh(string Message, Indent Indent = null)
        {
            Log(AppendTick("") + " " + Message, (string)null, Indent);
        }
        public static void CheckNah(string Message, Indent Indent = null)
        {
            Log(AppendCross("") + " " + Message, (string)null, Indent);
        }
        public static void YehNah(string Message, bool? Good = null, Indent Indent = null)
        {
            string append = null;
            if (Good != null)
            {
                if (!Good.GetValueOrDefault())
                {
                    append = AppendCross("") + " ";
                }
                else
                {
                    append = AppendTick("") + " ";
                }
            }
            Log(append + Message, (string)null, Indent);
        }
    }
}
