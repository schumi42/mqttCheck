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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mqttCheck
{
    public static class Const
    {
        public const double virtualTimeFactor = 0.1;
        public static string timeExceededMsg = "Time exceeded";
        public static readonly string[] acceptedSmcExceptionMessages = { timeExceededMsg };

        public const int skipSampleNumberAtStart = 1;
    }

    public static class Helper
    {
        public static void ThrowLoggedException(Exception e)
        {
            Console.WriteLine("EXCEPTION: " + e);
            throw e;
        }

        public static void printExceptionDuringSMC(Exception e) {
            //if (!(e is AssertionException || (e.Message.Contains("Falsifiable, after 1 test") && e.Message.Contains("shrink"))) || !(e.Message.ContainsAny(Const.acceptedSmcExceptionMessages)))
            //if (!(e is AssertionException) || !(e.Message.ContainsAny(AppConfig.acceptedSmcExceptionMessages)))
            if(!(e.Message.ContainsAny(Const.acceptedSmcExceptionMessages)))
            {
                Console.WriteLine("EXCEPTION: " + e);
                Console.WriteLine("EXCEPTIONMSG: " + e.Message);
            }
        }

        public static int GenerateWaitingTime(int start, int end)
        {
            return StaticRandom.Next(start, end + 1);
        }
    }
}
