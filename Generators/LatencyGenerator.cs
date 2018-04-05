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
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mqttCheck
{
    class LatencyGenerator: BaseGenerator
    {
        private Model model;
        public string Msg { get; set; }
        public IOrderedDictionary EncapsulatedGens { get; set;}

        Func<string, int, int, int, Tuple<double, double>> Latency { get; set; }
        public Func<string, string, int, int, long, long, string, double> Sample { get; set; }
        public LatencyGenerator(string name, Model m, string msg, Func<string, int, int, int, Tuple<double, double>> latency, IOrderedDictionary encapsulatedGens = null)
            : base(name)
        {
            this.model = m;
            this.Msg = msg;
            this.Type = DataType.Latency;
            this.Latency = latency;
            this.EncapsulatedGens = encapsulatedGens;
        }

        protected LatencyGenerator(LatencyGenerator a)
            : base(a)
        {
            this.Name = a.Name;
            this.Type = DataType.Latency;
            this.Msg = a.Msg;
            this.Latency = a.Latency;
            this.EncapsulatedGens = a.EncapsulatedGens;
        }

        public override object Clone()
        {
            var o = new LatencyGenerator(this);
            return o;
        }

        public override Gen<object> Generator(Dictionary<string, object> data = null)
        {
            
            return StateMachineSpec.GenerateData(this.EncapsulatedGens.Values.Cast<BaseGenerator>()).SelectMany(data1 =>
            {
                if (model.UsageProfile != null)
                {
                    int waitingTime = Helper.GenerateWaitingTime(model.UsageProfile.BeforeMsgWaitingTimeMin, model.UsageProfile.BeforeMsgWaitingTimeMax);
                    System.Threading.Thread.Sleep((int)(waitingTime * Const.virtualTimeFactor));
                }
                Log.increaseRequestCount();
                int subscriber = 0;
                if(Msg == "publish"){
                    subscriber = Log.SubscriptionNumbers.ContainsKey((string)data1["topic"]) ?  Log.SubscriptionNumbers[(string)data1["topic"]] : 0;
                }

                var distParams = Latency(Msg, Log.ActiveRequestCount, Log.TotalSubscriptions, subscriber);
                var sample = sampleNormalDistribution(distParams.Item1, distParams.Item2);

                sample = Math.Abs(sample);
                yieldOrSleep((int)(sample * Const.virtualTimeFactor));
                Log.decreaseRequestCount();

                MethodInfo mi = model.GetType().GetMethod(Msg, new Type[] { typeof(Dictionary<string, object>) });
                if (mi != null)
                {
                    mi.Invoke(model, new object[] { data1 });
                }
                
                return Gen.Constant<double>(sample).Select(d => (object)d);
            });
            
        }

        private void yieldOrSleep(int ticks) {
            if ((double)ticks / System.Diagnostics.Stopwatch.Frequency*1000 >= 1)
            {
                Thread.Sleep((int)((double)ticks / System.Diagnostics.Stopwatch.Frequency * 1000));
            }
            else {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (ticks >= watch.ElapsedTicks) {
                    Thread.Yield();
                }
                watch.Stop();
                //Console.WriteLine("Wanted ticks: "+ticks+" Actual ticks: "+watch.ElapsedTicks);
            }
        }
    }
}
