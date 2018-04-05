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

using FsCheck.Experimental;
using System;
using FsCheck;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NUnit.Framework;

namespace mqttCheck
{
    class StateMachineSpec : Machine<Sut, Model>, IDisposable
    {
        bool modelOnlyMode = false;
        Model m;
        Sut s;
        int? threshold = null;

        public StateMachineSpec(Sut s, Model m)
        {
            this.m = m;
            this.s = s;
            Dispose();
        }

        public StateMachineSpec(Model m,  int maxCommands, int threshold)
            : base(maxCommands)
        {
            this.m = m;
            modelOnlyMode = true;
            this.threshold = threshold;
        }

        public StateMachineSpec(Sut s, Model m, int maxCommands)
            : base(maxCommands)
        {
            this.m = m;
            this.s = s;
            Dispose();
        }

        public StateMachineSpec(Sut s, Model m, int maxCommands, int threshold)
            : base(maxCommands)
        {
            this.m = m;
            this.s = s;
            this.threshold = threshold;
            Dispose();
        }

        public override IEnumerable<Microsoft.FSharp.Collections.FSharpList<Operation<Sut, Model>>> ShrinkOperations(Microsoft.FSharp.Collections.FSharpList<Operation<Sut, Model>> s)
        {
            return new List<Microsoft.FSharp.Collections.FSharpList<Operation<Sut, Model>>>();//base.ShrinkOperations(s);
        }



        public void Dispose()
        {
            //Console.WriteLine("Dispose Called!");
            m.Reset();
            if (!modelOnlyMode)
            {
                try
                {
                    s.Reset();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw e;
                }
            }
        }
        public override TearDown<Sut> TearDown
        {
            get
            {
                //Console.WriteLine("teardown called!");
                Dispose();
                return base.TearDown;
            }
        }


        public override Arbitrary<Setup<Sut, Model>> Setup
        {
            get
            {
                //Console.WriteLine("Setup Called!");

                //Dispose();
                return Arb.From(Gen.Constant((Setup<Sut, Model>)new StateMachineSetup(s, m)));
            }
        }

        public override Gen<Operation<Sut, Model>> Next(Model m)
        {
            //var gens = new List<WeightAndValue<Gen<Operation<Sut, Model>>>>();
            //// int dynWeight = m.ActiveRuleEngineModel.GetPossibleTransitionsWithWeight().Count > 0 ? m.ActiveRuleEngineModel.Transitions.Count : 0;
            ////int dynWeight = m.GetPossibleTransitionsWeightSum();
            //var dyn = new WeightAndValue<Gen<Operation<Sut, Model>>>(1, Op(m));
            //gens.Add(dyn);

            ////var stops = new WeightAndValue<Gen<Operation<Sut, Model>>>(0, Gen.Constant((Operation<Sut, Model>)new StopOperation<Sut, Model>()));
            ////gens.Add(stops);
            //return Gen.OneOf(Gen.Frequency(gens));
            var wvs = m.GetPossibleTransitionsWithWeight(m.State);
            return Gen.Frequency(wvs).SelectMany(transition =>
            {
                return GenerateData(transition.DataGenerators.Values.Cast<BaseGenerator>())
                    .Select(data => (Operation<Sut, Model>)new MsgOp(transition, data, this.m, threshold, modelOnlyMode));
            });
        }

        public static Gen<Dictionary<string, object>> GenerateData(IEnumerable<BaseGenerator> gens, Dictionary<string, object> existingData = null)
        {
            Assert.True(gens != null);
            if (existingData == null)
                existingData = new Dictionary<string, object>();

            var gen = Gen.Constant(new Dictionary<string, object>());
            foreach (var a in gens)
            {
                gen = gen.SelectMany(data =>
                {
                    return a.Generator(existingData).Select(value =>
                    {
                        existingData[a.Name] = value;
                        data.Add(a.Name, value);
                        return data;
                    });
                });
            }
            return gen;
        }
    }

    class MsgOp : Operation<Sut, Model>
    {
        private bool modelOnlyMode;
        private int? threshold;
        public Transition Transition { get; private set; }
        public Dictionary<string, object> Data { get; private set; }
        Model internM;


        public MsgOp(Transition transition, Dictionary<string, object> data, Model m, int? threshold = null, bool modelOnlyMode = false)
        {
            this.Transition = transition;
            Data = data;
            this.internM = m;
            this.threshold = threshold;
            this.modelOnlyMode = modelOnlyMode;
        }

        public override bool Pre(Model m)
        {
            return true;
        }
        public override Property Check(Sut s, Model m)
        {
            if (modelOnlyMode)
            {
                return true.ToProperty();
            }
            
            if (m.UsageProfile != null) {

                int waitingTime = Helper.GenerateWaitingTime(m.UsageProfile.BeforeMsgWaitingTimeMin, m.UsageProfile.BeforeMsgWaitingTimeMax);
                System.Threading.Thread.Sleep(waitingTime);
            }
            try
            {
                //Console.WriteLine("SUT executes " + Transition.Name + " #" + Transition.Input + "#");
                var res = s.run(Transition.Input, Data);
                //Console.WriteLine("REs: " + res.ToString());
                //Assert.AreEqual(Transition.Output, res.ToString());
                if(Transition.Output !=  res.ToString()){
                    //Console.WriteLine("Expected Output: " + Transition.Output + ", but was: " + res.ToString());
                    //s.Reset();
                    //return false.ToProperty();
                    throw new Exception("Error: Expected Output: " + Transition.Output + ", but was: " + res.ToString());
                }
            }
            catch (Exception e)
            {
                s.Reset();
                Console.WriteLine(e);
                throw e;
                //return false.ToProperty();
            }
            if (threshold != null) {
                if (Log.getLastDurtation(s.Name) > threshold) {
                    var error = Const.timeExceededMsg+ ", Expected Duration: " + threshold + ", but was: " + Log.getLastDurtation(s.Name);
                    //Console.WriteLine(error);
                    s.Reset();
                    throw new Exception(error);
                    //return false.ToProperty();
                }
            }



            return true.ToProperty();
        }

        public override Model Run(Model m)
        {
            var n = new Model(m);
            n.makeTransition(Transition, Data, modelOnlyMode);
            internM.State = n.State;

            if (modelOnlyMode)
            {
                foreach (var kv in Data) {
                    //Console.WriteLine(kv.Key + ": " + kv.Value);
                    if (kv.Key.Contains(Transition.Input))
                    {
                        if ((double)kv.Value > threshold)
                        {
                            n.Reset();
                            throw new Exception(Const.timeExceededMsg + " for " + kv.Key + " (" + kv.Value + " ms)");
                        }
                    }
                }
            }

            return n;
        }

        public override string ToString()
        {
            return Transition.Name;
        }
    }

    class StateMachineSetup : Setup<Sut, Model>
    {
        Sut s;
        Model m;
        public StateMachineSetup(Sut s, Model m)
        {
            this.s = s;
            this.m = m;
        }
        public override Sut Actual()
        {
            if (s != null)
            {
                try
                {
                    s.Reset();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw e;
                }
            }
            return s;
        }

        public override Model Model() { 
            m.Reset(); return new Model(m); }
    }
}
