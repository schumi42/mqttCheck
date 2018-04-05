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

using FsCheck;
using System;
using System.Collections.Generic;

namespace mqttCheck
{
    public abstract class BaseGenerator : ICloneable
    {
        public enum DataType
        {
            Enum, Reference, String, Object, Long, Bool,
            Id, DateTime, TimeSpan, Date, Standard, Attachment, Float, Integer, Double, KeywordString, Cost, Wait, Latency
        };

        public DataType Type { get; protected set; }
        public string Name { get; set; }


        public BaseGenerator(String name)
        {
            Name = name;
        }

        protected BaseGenerator(BaseGenerator a)
        {
            Name = a.Name;
            Type = a.Type;
        }

        public Gen<object> Generator()
        {
            return Generator(null);
        }

        public abstract Gen<object> Generator(Dictionary<string, object> data);

        public abstract object Clone();


        public string toDetailString() {
            return Name + " Datatype: " + Type;
        }

        public static double sampleNormalDistribution(double mhu, double sigma) {
            double u1 = StaticRandom.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = StaticRandom.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return  mhu + sigma * randStdNormal;
        }
    }
}
