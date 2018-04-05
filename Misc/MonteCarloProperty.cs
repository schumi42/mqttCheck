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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FsCheck.Experimental;
using NUnit.Framework;

namespace mqttCheck
{
    public class MonteCarloProperty : QuantitativeSMCProperty
    {
        protected int samples;
        protected Property property;
        protected Configuration config;

        public override Configuration Config
        {
            get { return config; }
            set { config = value; }
        }
        public static bool enableExceptionOutput = false;

        public MonteCarloProperty(Property p, Configuration c, int samples)
        {
            this.property = p;
            this.config = c.Copy();
            
            this.samples = samples;
        }
        public void QuickCheck()
        {
            for (int s = 0; s < Const.skipSampleNumberAtStart; s++)
            {
               this.Continue();
            }
            int passCnt = 0;
            for (int i = 0; i < samples; i++)
            {
                //Console.WriteLine("Sample starts: " + Log.TotalSubscriptions);
                try
                {
                    this.config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                    property.Check(config);//Check.One(config, property);
                    passCnt++;
                }
                catch (Exception e)
                {
                    Helper.printExceptionDuringSMC(e);
                }
                //Console.WriteLine("Total Sub: " + Log.TotalSubscriptions);
                //Console.WriteLine("Active reqs: " + Log.ActiveRequestCount);
            }
            Console.Write("Property true for: " + passCnt + " samples\n");
            Console.Write("Property false for: " + (samples - passCnt) + " samples\n");
            Console.Write("Property holds " + ((double)passCnt / samples * 100) + "%\n");
        }

        public override double QCheck()
        {
            for (int s = 0; s < Const.skipSampleNumberAtStart; s++)
            {
                this.Continue();
            }
            int passCnt = 0;
            for (int i = 0; i < samples; i++)
            {
                try
                {
                    this.config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                    property.Check(config); //Check.One(config, property);
                    passCnt++;
                }
                catch (Exception e)
                {
                    Helper.printExceptionDuringSMC(e);
                }
            }
            return ((double)passCnt / samples);
        }
        public override void Continue()
        {
            try
            {
                this.config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                property.Check(config); //Check.One(config, property);
            }
            catch (Exception e)
            {
                Helper.printExceptionDuringSMC(e);
            }
        }
    }
}
