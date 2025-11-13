using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

using XRL;

namespace UD_FleshGolems.Logging
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    public static class Debug
    {
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
        public static void LogHeader(string Message, out Indents Indent)
        {
            GetIndents(out Indent);
            Log(GetCallingTypeAndMethod(true) + Message, Indent);
        }
    }
}
