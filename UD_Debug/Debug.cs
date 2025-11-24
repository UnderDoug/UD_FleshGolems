using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;

using HarmonyLib;

using XRL;
using XRL.Wish;

using UD_FleshGolems;
using static UD_FleshGolems.Options;
using static UD_FleshGolems.Utils;
using static UD_FleshGolems.Const;

namespace UD_FleshGolems.Logging
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasWishCommand]
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

        private static bool DoDebugSetting => DebugEnableLogging && !SilenceLogging;
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
                    MetricsManager.LogException(typeof(Debug) + "." + nameof(DoDebug), x, GAME_MOD_EXCEOPTION);
                }
                return DoDebugSetting;
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
                MetricsManager.LogException(nameof(Debug) + "." + nameof(GetRegistry), x, GAME_MOD_EXCEOPTION);
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

        public static Indent LastIndent
            => Indents.TryPeek(out Indent peek)
            ? peek
            : ResetIndent();

        public static Indent GetNewIndent(int Offset)
            => new(Offset);

        public static Indent GetNewIndent()
            => GetNewIndent(0);

        public static bool HaveIndents()
            => !Indents.IsNullOrEmpty();

        public static void PushToIndents(Indent Indent)
        {
            Indents.Push(Indent);
        }

        [GameBasedCacheInit]
        [ModSensitiveCacheInit]
        public static Indent ResetIndent()
        {
            Indents ??= new();
            Indents.Clear();
            return GetNewIndent();
        }

        public static Indent DiscardIndent()
        {
            if (!Indents.TryPop(out _))
            {
                ResetIndent();
            }
            return LastIndent;
        }
        public static bool HasIndent(Indent Indent)
            => Indents.Contains(Indent);

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
            try
            {
                StackFrame[] stackFrames = new StackTrace().GetFrames();
                int stackTraceCount = Math.Min(stackFrames.Length, 8);
                for (int i = 0; i < stackTraceCount; i++)
                {
                    if (stackFrames[i].GetMethod() is MethodBase methodBase
                        && methodBase.DeclaringType is Type declaringType
                        && !declaringType.Equals(typeof(UD_FleshGolems.Logging.Debug))
                        && !declaringType.Equals(typeof(UD_FleshGolems.Logging.Indent)))
                    {
                        CallingType = declaringType;
                        Method = methodBase;
                        return true;
                    }
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(TryGetCallingTypeAndMethod), x, GAME_MOD_EXCEOPTION);
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
        public static Indent Log(string Message, Indent Indent = null)
            => Log(Message, (string)null, Indent);

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
                => Name.IsNullOrEmpty() 
                ? Value?.ToString() 
                : Name + ": " + Value?.ToString();

            public Indent Log(Indent Indent)
                => Debug.Log(Name, Value, Indent ?? LastIndent);
            public Indent Log()
                => Log(null);
            public Indent Log(int Offset)
                => Log(LastIndent[Offset]);
        }
        public static ArgPair Arg(string Name, object Value)
        {
            return new ArgPair(Name, Value);
        }
        public static ArgPair Arg(object Value)
        {
            return Arg(null, Value);
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

        public static Indent YehNah(string Message, object Value, bool? Good = null, Indent Indent = null)
        {
            string append;
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

        public static void MetricsManager_LogCallingModError(object Message)
        {
            if (!TryGetFirstCallingModNot(ThisMod, out ModInfo callingMod))
            {
                callingMod = ThisMod;
            }
            MetricsManager.LogModError(callingMod, Message);
        }

        /*
         * 
         * Wishes!
         * 
         */
        [WishCommand( Command = "UD_FleshGolems debug indents" )]
        public static void DebugIndents_WishHandler()
        {
            Stack<Indent> oldIndents = new();
            List<Indent> oldIndentsList = new();
            foreach (Indent oldindent in Indents)
            {
                oldIndentsList.AddItem(new(oldindent, false));
            }
            for (int i = oldIndentsList.Count; i < 0; i--)
            {
                oldIndents.Push(oldIndentsList[i - 1]);
            }

            static string padthisString(string StringToPad)
            {
                return StringToPad.PadRight(20, ' ');
            }

            ResetIndent();

            UnityEngine.Debug.Log("Indent Test (New, using)");
            UnityEngine.Debug.Log(nameof(Indents) + "." + nameof(Indents.Count) + ": " + Indents.Count);
            UnityEngine.Debug.Log(padthisString(nameof(Indent) + " peek: ") + "\"" + Indents.Peek().ToString() + "\"(" + (int)Indents.Peek() + ")");

            using Indent uIndent = new();
            UnityEngine.Debug.Log("using Indent uIndent = new();");
            UnityEngine.Debug.Log(nameof(Indents) + "." + nameof(Indents.Count) + ": " + Indents.Count);
            UnityEngine.Debug.Log(padthisString(nameof(Indent) + " peek: ") + "\"" + Indents.Peek().ToString() + "\"(" + (int)Indents.Peek() + ")");

            UnityEngine.Debug.Log(" ");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[1]: ") + "\"" + uIndent[1].ToString() + "\"(" + (int)uIndent[1] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[2]: ") + "\"" + uIndent[2].ToString() + "\"(" + (int)uIndent[2] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[3]: ") + "\"" + uIndent[3].ToString() + "\"(" + (int)uIndent[3] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[2]: ") + "\"" + uIndent[2].ToString() + "\"(" + (int)uIndent[2] + ")");

            UnityEngine.Debug.Log(" ");
            using Indent uIndent2 = new();
            UnityEngine.Debug.Log("using Indent uIndent2 = new();");
            UnityEngine.Debug.Log(nameof(Indents) + "." + nameof(Indents.Count) + ": " + Indents.Count);
            UnityEngine.Debug.Log(padthisString(nameof(Indent) + " peek: ") + "\"" + Indents.Peek().ToString() + "\"(" + (int)Indents.Peek() + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");

            UnityEngine.Debug.Log(" ");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2) + ": ") + "\"" + uIndent2.ToString() + "\"(" + (int)uIndent2 + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2) + "[1]: ") + "\"" + uIndent2[1].ToString() + "\"(" + (int)uIndent2[1] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2) + "[2]: ") + "\"" + uIndent2[2].ToString() + "\"(" + (int)uIndent2[2] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2) + "[3]: ") + "\"" + uIndent2[3].ToString() + "\"(" + (int)uIndent2[3] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2) + "[2]: ") + "\"" + uIndent2[2].ToString() + "\"(" + (int)uIndent2[2] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2) + ": ") + "\"" + uIndent2.ToString() + "\"(" + (int)uIndent2 + ")");

            UnityEngine.Debug.Log(" ");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[2]: ") + "\"" + uIndent[2].ToString() + "\"(" + (int)uIndent[2] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[3]: ") + "\"" + uIndent[3].ToString() + "\"(" + (int)uIndent[3] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");

            UnityEngine.Debug.Log(" ");
            uIndent2.Dispose();
            UnityEngine.Debug.Log("uIndent2.Dispose();");
            UnityEngine.Debug.Log(nameof(Indents) + "." + nameof(Indents.Count) + ": " + Indents.Count);
            UnityEngine.Debug.Log(padthisString(nameof(Indent) + " peek: ") + "\"" + Indents.Peek().ToString() + "\"(" + (int)Indents.Peek() + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");

            UnityEngine.Debug.Log(" ");
            using Indent uIndent2_2 = new();
            UnityEngine.Debug.Log("using Indent uIndent2_2 = new()");
            UnityEngine.Debug.Log(nameof(Indents) + "." + nameof(Indents.Count) + ": " + Indents.Count);
            UnityEngine.Debug.Log(padthisString(nameof(Indent) + " peek: ") + "\"" + Indents.Peek().ToString() + "\"(" + (int)Indents.Peek() + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");

            UnityEngine.Debug.Log(" ");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2_2) + ": ") + "\"" + uIndent2_2.ToString() + "\"(" + (int)uIndent2_2 + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2_2) + "[1]: ") + "\"" + uIndent2_2[1].ToString() + "\"(" + (int)uIndent2_2[1] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2_2) + "[2]: ") + "\"" + uIndent2_2[2].ToString() + "\"(" + (int)uIndent2_2[2] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2_2) + "[3]: ") + "\"" + uIndent2_2[3].ToString() + "\"(" + (int)uIndent2_2[3] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent2_2) + ": ") + "\"" + uIndent2_2.ToString() + "\"(" + (int)uIndent2_2 + ")");

            UnityEngine.Debug.Log(" ");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[4]: ") + "\"" + uIndent[4].ToString() + "\"(" + (int)uIndent[4] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + "[5]: ") + "\"" + uIndent[5].ToString() + "\"(" + (int)uIndent[5] + ")");
            UnityEngine.Debug.Log(padthisString(nameof(uIndent) + ": ") + "\"" + uIndent.ToString() + "\"(" + (int)uIndent + ")");

            UnityEngine.Debug.Log(" ");
            UnityEngine.Debug.Log("Finished Test");
            Indents = oldIndents;
        }
    }
}
