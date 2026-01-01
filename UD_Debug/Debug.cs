using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        private static bool DoDebugSetting
            => DebugEnableLogging
            && (!DebugDisableWorldGenLogging || The.Player != null)
            && !SilenceLogging;
            
        private static bool DoDebug
        {
            get
            {
                try
                {
                    if (!DoDebugSetting)
                        return false;

                    if (TryGetCallingTypeAndMethod(out _, out MethodBase callingMethod)
                        && GetRegistry() is List<MethodRegistryEntry> registry
                        && registry.TryGetValue(callingMethod, out bool registryMethodValue)
                        && !registryMethodValue
                        && !DebugEnableAllLogging)
                        return false;
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(typeof(Debug) + "." + nameof(DoDebug), x, GAME_MOD_EXCEPTION);
                }
                return DoDebugSetting;
            }
        }

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        private static List<MethodRegistryEntry> _DoDebugRegistry = null;

        public static List<MethodRegistryEntry> DoDebugRegistry => _DoDebugRegistry ??= GetRegistry();

        public static void Register(
            Type Class,
            string MethodName,
            bool Value,
            List<MethodRegistryEntry> Registry,
            ref List<MethodRegistryEntry> ReturnRegistry)
            => Register(Class?.GetMethod(MethodName), Value, Registry, ref ReturnRegistry);

        public static void Register(
            MethodBase MethodBase,
            bool Value,
            List<MethodRegistryEntry> Registry,
            ref List<MethodRegistryEntry> ReturnRegistry)
        {
            string declaringType = MethodBase?.DeclaringType?.Name;
            UnityEngine.Debug.Log(nameof(Debug) + "." + nameof(Register) + "(" + declaringType + "." + MethodBase?.Name + ": " + Value + ")");
            Registry.Add(new(MethodBase, Value));
            ReturnRegistry = Registry;
        }
        public static void Register(this List<MethodRegistryEntry> Registry, Type Class, string MethodName, bool Value)
            => Register(Class, MethodName, Value, Registry, ref _DoDebugRegistry);

        public static void Register(this List<MethodRegistryEntry> Registry, MethodBase MethodBase, bool Value)
            => Register(MethodBase, Value, Registry, ref _DoDebugRegistry);

        public static void Register(this List<MethodRegistryEntry> Registry, string MethodName, bool Value)
        {
            TryGetCallingTypeAndMethod(out Type CallingType, out _);
            foreach (MethodBase methodBase in CallingType.GetMethods() ?? new MethodInfo[0])
                if (methodBase.Name == MethodName)
                    Register(methodBase, Value, Registry, ref _DoDebugRegistry);
        }
        public static void Register(this List<MethodRegistryEntry> Registry, MethodRegistryEntry RegisterEntry)
            => Register(RegisterEntry.GetMethod(), RegisterEntry.GetValue(), Registry, ref _DoDebugRegistry);

        public static List<MethodRegistryEntry> GetRegistry()
        {
            _DoDebugRegistry ??= new();
            if (_GotRegistry)
            {
                return _DoDebugRegistry;
            }
            try
            {
                List<MethodInfo> debugRegistryMethods = ModManager.GetMethodsWithAttribute(typeof(UD_FleshGolems_DebugRegistryAttribute))
                    ?.Where(m 
                        => m != null
                        && m.IsStatic 
                        && m.ReturnType.EqualsAny(typeof(void), typeof(List<MethodRegistryEntry>))
                        && m.GetParameters() is ParameterInfo[] parameters
                        && parameters[0].GetType() == typeof(List<MethodRegistryEntry>))
                    ?.ToList();

                foreach (MethodInfo debugRegistryMethod in debugRegistryMethods ?? new())
                    debugRegistryMethod.Invoke(null, new object[] { _DoDebugRegistry });
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(Debug) + "." + nameof(GetRegistry), x, GAME_MOD_EXCEPTION);
                _GotRegistry = true;
            }
            _GotRegistry = true;
            return _DoDebugRegistry;
        }

        [ModSensitiveCacheInit]
        [GameBasedCacheInit]
        public static void CacheDoDebugRegistry()
        {
            GetRegistry();
        }

        public static bool GetDoDebug(string CallingMethod = null)
        {
            if (CallingMethod.IsNullOrEmpty())
                return DoDebug;

            if (GetRegistry() is List<MethodRegistryEntry> doDebugRegistry
                && !doDebugRegistry.Any(m => m.GetMethod().Name == CallingMethod))
                return DoDebugSetting;

            return DoDebug;
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
        public static bool TryGetCallingTypeAndMethod(out Type CallingType, out MethodBase CallingMethod)
        {
            CallingType = null;
            CallingMethod = null;
            try
            {
                Type[] debugTypes = new Type[3]
                {
                    typeof(UD_FleshGolems.Logging.Debug),
                    typeof(UD_FleshGolems.Logging.Debug.ArgPair),
                    typeof(UD_FleshGolems.Logging.Indent),
                };
                StackTrace stackTrace = new();
                for (int i = 0; i < 8 && stackTrace?.GetFrame(i) is StackFrame stackFrameI; i++)
                {
                    if (stackFrameI?.GetMethod() is MethodBase methodBase
                        && methodBase.DeclaringType is Type declaringType
                        && !declaringType.EqualsAny(debugTypes))
                    {
                        CallingType = declaringType;
                        CallingMethod = methodBase;
                        return true;
                    }
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(TryGetCallingTypeAndMethod), x, GAME_MOD_EXCEPTION);
            }
            return false;
        }

        public static Indent Log<T>(string Field, T Value, Indent Indent = null, [CallerMemberName] string CallingMethod = "")
        {
            if (!GetDoDebug(CallingMethod))
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
        public static Indent Log(string Message, Indent Indent = null, [CallerMemberName] string CallingMethod = "")
            => Log(Message, (string)null, Indent, CallingMethod);

        public readonly struct ArgPair
        {
            public static ArgPair Empty = default;

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

            public Indent Log(Indent Indent, [CallerMemberName] string CallingMethod = "")
                => Debug.Log(Name, Value, Indent ?? LastIndent, CallingMethod);
            public Indent Log([CallerMemberName] string CallingMethod = "")
                => Log(null, CallingMethod);
            public Indent Log(int Offset, [CallerMemberName] string CallingMethod = "")
                => Log(LastIndent[Offset], CallingMethod);

            public override bool Equals(object obj)
                => (obj is ArgPair argPairObj
                    && Equals(argPairObj))
                || base.Equals(obj);

            public bool Equals(ArgPair Other)
            {
                if (Name != Other.Name)
                {
                    return false;
                }
                if ((Value != null) != (Other.Value != null))
                {
                    return false;
                }
                return Value == Other.Value;
            }

            public override int GetHashCode()
                => (Name?.GetHashCode() ?? 0) ^ (Value?.GetHashCode() ?? 0);

            public static bool operator ==(ArgPair Operand1, ArgPair Operand2)
                => Operand1.Equals(Operand2);
            public static bool operator !=(ArgPair Operand1, ArgPair Operand2)
                => !(Operand1 == Operand2);
        }
        public static ArgPair Arg(string Name, object Value)
            => new(Name, Value);

        public static ArgPair Arg(object Value)
            => Arg(null, Value);

        public static Indent LogCaller(
            string MessageAfter,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
        {
            if (!GetDoDebug(CallingMethod))
            {
                return Indent;
            }
            string output = "";
            if (!ArgPairs.IsNullOrEmpty())
            {
                List<string> joinableArgs = ArgPairs.ToList()
                    ?.Where(ap => ap != ArgPair.Empty)
                    ?.ToList()
                    ?.ConvertAll(ap => ap.ToString())
                    ?.ToList();
                if (!joinableArgs.IsNullOrEmpty())
                {
                    output += "(" + joinableArgs?.SafeJoin() + ")";
                }
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += " " + MessageAfter;
            }
            return Log(GetCallingTypeAndMethod() + output, Indent, CallingMethod);
        }
        public static Indent LogCaller(
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
            => LogCaller(null, Indent, CallingMethod, ArgPairs);

        public static Indent LogMethod(
            string MessageAfter,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
        {
            if (!GetDoDebug(CallingMethod))
            {
                return Indent;
            }
            string output = "";
            if (!ArgPairs.IsNullOrEmpty())
            {
                List<string> joinableArgs = ArgPairs.ToList()
                    ?.Where(ap => ap != ArgPair.Empty)
                    ?.ToList()
                    ?.ConvertAll(ap => ap.ToString())
                    ?.ToList();
                if (!joinableArgs.IsNullOrEmpty())
                {
                    output += "(" + joinableArgs?.SafeJoin() + ")";
                }
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += " " + MessageAfter;
            }
            return Log(CallingMethod + output, Indent, CallingMethod);
        }
        public static Indent LogMethod(
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
            => LogMethod(null, Indent, CallingMethod, ArgPairs);

        public static Indent LogArgs(
            string MessageBefore,
            string MessageAfter,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
        {
            string output = "";
            if (!MessageBefore.IsNullOrEmpty())
            {
                output += MessageBefore;
            }
            if (!ArgPairs.IsNullOrEmpty())
            {
                List<string> joinableArgs = ArgPairs.ToList()
                    ?.Where(ap => ap != ArgPair.Empty)
                    ?.ToList()
                    ?.ConvertAll(ap => ap.ToString())
                    ?.ToList();
                if (!joinableArgs.IsNullOrEmpty())
                {
                    output += joinableArgs?.SafeJoin();
                }
            }
            if (!MessageAfter.IsNullOrEmpty())
            {
                output += MessageAfter;
            }
            return Log(output, Indent, CallingMethod);
        }

        public static Indent LogArgs(
            string MessageBefore,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "",
            params ArgPair[] ArgPairs)
            => LogArgs(MessageBefore, null, Indent, CallingMethod, ArgPairs);

        public static Indent YehNah(
            string Message,
            object Value,
            bool? Good = null,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
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
            return Log(append + Message, Value, Indent, CallingMethod);
        }
        public static Indent YehNah(
            string Message,
            bool?Good = null,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, null, Good, Indent, CallingMethod);

        public static Indent CheckYeh(
            string Message,
            object Value,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, Value, true, Indent, CallingMethod);

        public static Indent CheckYeh(
            string Message,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, null, true, Indent, CallingMethod);

        public static Indent CheckNah(
            string Message,
            object Value,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, Value, false, Indent, CallingMethod);

        public static Indent CheckNah(
            string Message,
            Indent Indent = null,
            [CallerMemberName] string CallingMethod = "")
            => YehNah(Message, null, false, Indent, CallingMethod);

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
