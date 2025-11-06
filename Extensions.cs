using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using XRL.CharacterBuilds.Qud;
using XRL.World;

using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;

namespace UD_FleshGolems
{
    public static class Extensions
    {
        public static T RequirePart<T>(this GameObject Object, out T Part) where T : IPart, new() => Part = Object.RequirePart<T>();

        public static bool TryRequirePart<T>(this GameObject Object, out T Part)
            where T : IPart, new()
        {
            return (Part = Object.RequirePart<T>()) != null;
        }

        public static bool IsPlayerBlueprint(this string Blueprint)
        {
            return Blueprint == Startup.PlayerBlueprint;
        }

        public static string ThisManyTimes(this string @string, int Times = 1)
        {
            if (Times < 1)
            {
                return null;
            }
            string output = "";

            for (int i = 0; i < Times; i++)
            {
                output += @string;
            }

            return output;
        }
        public static string ThisManyTimes(this char @char, int Times = 1)
        {
            return @char.ToString().ThisManyTimes(Times);
        }
    }
}
