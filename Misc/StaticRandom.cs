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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mqttCheck
{
    public static class StaticRandom
    {
        //private static Random globalRandom = new Random();
        public static void setSeed(int s)
        {
            lock (globalLock)
            {
                //globalRandom = new Random(seed);
                seed = s;
            }
        }
        static int seed = Environment.TickCount;

        private static readonly object globalLock = new object();

        private static ThreadLocal<Random> threadRandom = new ThreadLocal<Random>(() => NewRandom());

        public static void ResetThreadLocalRandoms() {
            threadRandom = new ThreadLocal<Random>(() => NewRandom());
        }


        public static Random NewRandom()
        {
            return new Random(Interlocked.Increment(ref seed));
        }


        public static Random Instance { get { return threadRandom.Value; } }


        public static int Next()
        {
            return Instance.Next();
        }

        /// <summary>See <see cref="Random.Next(int)" /></summary> 
        public static int Next(int maxValue)
        {
            return Instance.Next(maxValue);
        }

        /// <summary>See <see cref="Random.Next(int, int)" /></summary> 
        public static int Next(int minValue, int maxValue)
        {
            return Instance.Next(minValue, maxValue);
        }

        /// <summary>See <see cref="Random.NextDouble()" /></summary> 
        public static double NextDouble()
        {
            return Instance.NextDouble();
        }

        /// <summary>See <see cref="Random.NextBytes(byte[])" /></summary> 
        public static void NextBytes(byte[] buffer)
        {
            Instance.NextBytes(buffer);
        }

    }
}
