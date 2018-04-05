using System;
using System.Collections.Generic;
using System.IO;

using FsCheck;
using System.Security;
/*
 * Copyright (c) 2018, Richard Schumi and Bernhard K. Aichernig,
 * Institute of Software Technology, Graz University of Technology
 * 
 * This file is part of the mqttCheck project.
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.s
 */


namespace mqttCheck
{
    public static class ListExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
            }
        }
    }

    public static class StringExtensions
    {
        public static string ReplaceFirstOccurrence(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);
            if (place < 0)
            {
                return Source;
            }
            return Source.Remove(place, Find.Length).Insert(place, Replace);;
        }

        public static bool ContainsAny(this string Source,  string[] strArray)
        {
            foreach (var str in strArray) {
                if (Source.Contains(str)) {
                    return true;
                }
            }
            return false;
        }

    }

    public static class ConfigurationExtensions
    {
        static public Configuration Copy(this Configuration c)
        {
            var config = Configuration.VerboseThrowOnFailure;
            config.EndSize = c.EndSize;
            config.Every = c.Every;
            config.EveryShrink = c.EveryShrink;
            config.MaxNbOfFailedTests = c.MaxNbOfFailedTests;
            config.MaxNbOfTest = c.MaxNbOfTest;
            config.Name = c.Name;
            config.QuietOnSuccess = c.QuietOnSuccess;
            config.Replay = c.Replay;
            config.Runner = c.Runner;
            config.StartSize = c.StartSize;
            return config;
        }
    }
}
