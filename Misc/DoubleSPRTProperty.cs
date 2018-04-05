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
    class DoubleSPRTProperty : SPRTProperty
    {
        protected double p2;
        protected double p3;

        public DoubleSPRTProperty(Property p, Configuration c, double p0, double p1, double p2, double p3, double alpha, double beta) :base(p,c,p0,p1,alpha,beta)
        {
            this.p2 = p2;
            this.p3 = p3;
        }

        public DoubleSPRTProperty(Property property, Configuration c, double p, double difference, double alpha, double beta)
            : base(property, c, p - difference < 0.0 ? 0.0 : p - difference, p, alpha, beta)
        {
            this.p2 = p + difference > 1.0 ? 1.0 : p + difference;
            this.p3 = p;
        }

        public override void QuickCheck()
        {
            var res = this.DoubleQCheck();
            if(res.Item1.HasValue){
                Console.WriteLine("H" +(res.Item1.Value ? 0 : 1) + " accepted after " + res.Item3 + " samples.");
            }
            else{
                Console.WriteLine("Unknown error occured after " + res.Item3 + " samples.");
            }

            if(res.Item2.HasValue){
                Console.WriteLine("H" +(res.Item2.Value ? 2 : 3) + " accepted after " + res.Item4 + " samples.");
            }
            else{
                Console.WriteLine("Unknown error occured after " + res.Item4 + " samples.");
            }
            
        }

        public override Tuple<bool?, int> QCheck()
        {
            throw new NotImplementedException();
        }

        public override Tuple<bool?, bool?, int, int> DoubleQCheck()
        {
            for (int s = 0; s < 3; s++)
            {
                this.Continue();
            }

            int i = 0;
            double s_i = 0;
            int j = 0;
            double s_j = 0;
            do
            {
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

                if (i < 5) {
                    double ratio = success ? (p1 / p0) : ((1 - p1) / (1 - p0));
                    s_i = s_i + Math.Log(ratio);
                    i++;
                    j++;
                    double ratio1 = success ? (p3 / p2) : ((1 - p3) / (1 - p2));
                    s_j = s_j + Math.Log(ratio1);
                }
                else if (log_a < s_i && s_i < log_b)
                {
                    double ratio = success ? (p1 / p0) : ((1 - p1) / (1 - p0));
                    s_i = s_i + Math.Log(ratio);
                    i++;
                    if (log_a < s_j && s_j < log_b)
                    {
                        j++;
                        double ratio1 = success ? (p3 / p2) : ((1 - p3) / (1 - p2));
                        s_j = s_j + Math.Log(ratio1);
                    }
                }
                else if (log_a < s_j && s_j < log_b)
                {
                    j++;
                    double ratio1 = success ? (p3 / p2) : ((1 - p3) / (1 - p2));
                    s_j = s_j + Math.Log(ratio1);
                }
                else
                {
                    break;
                }
            } while (true);

            bool? res1 = null;
            if (s_i >= log_b)
            {
                //Console.WriteLine("H1 accepted after " + i + " samples.");b
                res1 =  false;
            }
            else if (s_i <= log_a)
            {
                //Console.WriteLine("H0 accepted after " + i + " samples.");
                res1=  true;
            }
            bool? res2 = null;
            if (s_j >= log_b)
            {
                //Console.WriteLine("H1 accepted after " + i + " samples.");b
                res2 = false;
            }
            else if (s_j <= log_a)
            {
                //Console.WriteLine("H0 accepted after " + i + " samples.");
                res2 = true;
            }
            return new Tuple<bool?,bool?,int,int>(res1,res2,i,j);
        }




    }
}
