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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mqttCheck
{
    class SPRTProperty : QualitativeSMCProperty{

        

        protected Property property;
        protected Configuration config;
        public override Configuration Config
        {
            get { return config; }
            set { config = value; }
        }
        protected double p0;
        protected double p1;
        protected double log_a;
        protected double log_b;

        public SPRTProperty(Property p, Configuration c, double p0, double p1, double alpha, double beta){
            p0 = p0 < 0.0 ? 0 : p0;
            p1 = p1 < 0.0 ? 0 : p1;
            p0 = p0 > 1.0 ? 1.0 : p0;
            p1 = p1 > 1.0 ? 1.0 : p1;
            this.property = p;
            this.config = c.Copy();
            this.p0 = p0; this.p1 = p1;
            this.log_a = Math.Log(beta / (1 - alpha));
            this.log_b = Math.Log((1 - beta) / alpha); 
        }
        public override void QuickCheck()
        {
            for (int s = 0; s < Const.skipSampleNumberAtStart; s++)
            {
                this.Continue();
            }
            int i = 0;
            double s_i = 0;
            do{
                i++;
                bool success = false;
                try{
                    this.config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                    property.Check(config);
                    success = true;
                }
                catch (Exception e)
                {
                    Helper.printExceptionDuringSMC(e);
                }
                double ratio = success ? (p1 / p0) : ((1 - p1) / (1 - p0));
                s_i = s_i + Math.Log(ratio);
            } while ((log_a < s_i && s_i < log_b) || i < 5);
            if (s_i >= log_b){
                Console.WriteLine("H1 accepted after " + i + " samples.");
            }
            else if (s_i <= log_a){
                Console.WriteLine("H0 accepted after " + i + " samples.");
            }
        }
        public override Tuple<bool?, int> QCheck()
        {
            for (int s = 0; s < Const.skipSampleNumberAtStart; s++)
            {
                this.Continue();
            }
            int i = 0;
            double s_i = 0;
            do
            {
                i++;
                bool success = false;
                try
                {
                    this.config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                    property.Check(config);
                    success = true;
                }
                catch (Exception e)
                {
                    Helper.printExceptionDuringSMC(e);
                }
                double ratio = success ? (p1 / p0) : ((1 - p1) / (1 - p0));
                s_i = s_i + Math.Log(ratio);
            } while ((log_a < s_i && s_i < log_b) || i < 5);
            if (s_i >= log_b)
            {
                //Console.WriteLine("H1 accepted after " + i + " samples.");b
                return new Tuple<bool?, int>(false, i);//false;
            }
            else if (s_i <= log_a)
            {
                //Console.WriteLine("H0 accepted after " + i + " samples.");
                return new Tuple<bool?, int>(true, i);//true;
            }
            return null;
        }

        public override void Continue()
        {
            try
            {
                this.config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                property.Check(config);
            }
            catch (Exception e)
            {
                Helper.printExceptionDuringSMC(e);
            }
        }
    }
}
