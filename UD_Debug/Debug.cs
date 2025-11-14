using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

using XRL;

using UD_FleshGolems;
using static UD_FleshGolems.Options;

namespace UD_FleshGolems.Logging
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    public static class Debug
    {
        private static bool _DoDebug => DebugEnableLogging;
        public static bool DoDebug
        {
            get
            {
                string callingTypeAndMethod = GetCallingTypeAndMethod();
                if (GetRegistry().ContainsKey(callingTypeAndMethod))
                {
                    if (!GetRegistry()[callingTypeAndMethod])
                    {
                        if (DebugEnableAllLogging)
                        {
                            return _DoDebug;
                        }
                        return false;
                    }
                }
                if (!_DoDebug)
                {
                    return false;
                }
                return true;
            }
        }
        private static Dictionary<string, bool> _DoDebugRegistry = new()
        {
            { "Example", true },
        };
        public static Dictionary<string, bool> DoDebugRegistry => GetRegistry();
        public static void Register(
            Type Class,
            string MethodName,
            bool Value,
            Dictionary<string, bool> Registry,
            out Dictionary<string, bool> ReturnRegistry)
        {
            UnityEngine.Debug.Log(nameof(Debug) + "." + nameof(Register) + "(" + Class.Name + "." + MethodName + ": " + Value + ")");
            Registry.Add(Class.Name + "." + MethodName, Value);
            ReturnRegistry = Registry;
        }
        public static void Register(this Dictionary<string, bool> Registry, Type Class, string MethodName, bool Value)
        {
            Register(Class, MethodName, Value, Registry, out _DoDebugRegistry);
        }
        public static Dictionary<string, bool> GetRegistry()
        {
            if (_GotRegistry)
            {
                return _DoDebugRegistry;
            }
            Type classWithDebugRegistryAttribute = typeof(UD_FleshGolems_HasDebugRegistryAttribute);
            Type methodWithDebugRegistryAttribute = typeof(UD_FleshGolems_DebugRegistryAttribute);
            foreach (Type hasDebugRegistry in ModManager.GetClassesWithAttribute(classWithDebugRegistryAttribute))
            {
                List<MethodInfo> debugRegistryMethods = ModManager.GetMethodsWithAttribute(methodWithDebugRegistryAttribute, hasDebugRegistry);
                foreach (MethodInfo debugRegistryMethod in debugRegistryMethods)
                {
                    UnityEngine.Debug.Log(nameof(Debug) + "." + nameof(GetRegistry) + "(" + hasDebugRegistry.Name + "." + debugRegistryMethod.Name + ")");
                    _DoDebugRegistry = debugRegistryMethod.Invoke(null, new object[] { _DoDebugRegistry }) as Dictionary<string, bool>;
                }
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

        public static Indent GetNewIndent()
        {
            return new(0, 4, ' ');
        }

        public static void ResetIndent(out Indent Indent)
        {
            _LastIndent = null;
            Indent = LastIndent;
        }
        public static void ResetIndent(out Indents Indent)
        {
            _LastIndent = null;
            Indent = new(LastIndent);
        }
        [GameBasedCacheInit]
        [ModSensitiveCacheInit]
        public static void ResetIndent()
        {
            ResetIndent(out Indents _);
        }
        public static void GetIndent(out Indent Indent)
        {
            Indent = new(LastIndent);
        }
        public static void SetIndent(Indent Indent)
        {
            LastIndent = Indent;
        }
        public static void GetIndents(int Offset, out Indents Indents)
        {
            LastIndent.GetIndents(Offset, out Indents);
        }
        public static void GetIndents(out Indents Indents)
        {
            GetIndents(0, out Indents);
        }

        private static string GetCallingTypeAndMethod(bool AppendSpace = false)
        {
            StackFrame[] stackFrames = new StackTrace().GetFrames();
            int stackTraceCount = Math.Min(stackFrames.Length, 8);
            for (int i = 0; i < stackTraceCount; i++)
            {
                if (stackFrames[i].GetMethod() is MethodBase methodBase
                    && methodBase.DeclaringType is Type declaringType
                    && declaringType != typeof(Debug))
                {
                    return declaringType.Name + "." + methodBase.Name + (AppendSpace ? " " : "");
                }
            }
            return null;
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
    }
}
