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

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace mqttCheck
{
    public class Transition
    {

        public int Weight { get; set; }
        internal IOrderedDictionary DataGenerators { get; set; }

        public string From { get; private set; }
        public string To { get; private set; }

        public string Input { get; set; }
        public string Output { get; set; }

        public String Name
        {
            get { return From + " " + Input +" / "+ Output + " " + To; }
            set{}
        }
            
        public Transition(Transition t)
        {
            Weight = t.Weight;
            DataGenerators = new OrderedDictionary();
            foreach (DictionaryEntry kv in t.DataGenerators) {
                DataGenerators.Add(kv.Key, kv.Value);
            }
            Input = t.Input;
            Output = t.Output;
            From = From;
            To = To;
        }

        public Transition(string input, string output, string from, string to)
        {
            Weight = 1;
            Input = input;
            Output = output;
            From = from;
            To = to;
            DataGenerators = new OrderedDictionary();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
