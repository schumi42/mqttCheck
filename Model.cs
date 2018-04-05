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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace mqttCheck
{
    public class Model
    {
        public UsageProfile UsageProfile { get; set; }
        public readonly string InitState;
        public string State { get; set; } 
        public readonly HashSet<string> States;
        public readonly Dictionary<String, Transition> Transitions;
        public HashSet<string> Subscriptions;

        public Model() {
            this.UsageProfile = null;
            States = new HashSet<string>() { "disconnected", "connected" };
            InitState = "disconnected";
            State = InitState;
            Transitions = new Dictionary<string, Transition>();
            Subscriptions = new HashSet<string>();

            Transition t = new Transition("connect", "ConnAck", "disconnected", "connected");
            Transitions.Add(t.Name, t);
            Transition t1 = new Transition("subscribe", "SubAck", "connected", "connected");
            t1.DataGenerators.Add("topic", new TopicGenerator("topic"));
            Transitions.Add(t1.Name, t1);
            Transition t2 = new Transition("publish", "PubAck", "connected", "connected");
            t2.DataGenerators.Add("topic", new TopicGenerator("topic"));
            t2.DataGenerators.Add("message", new MsgGenerator("message"));
            Transitions.Add(t2.Name, t2);
            Transition t3 = new Transition("unsubscribe", "UnSubAck", "connected", "connected");
            t3.DataGenerators.Add("topic", new TopicGenerator("topic"));
            Transitions.Add(t3.Name, t3);
            Transition t4 = new Transition("disconnect", "ConnectionClosed", "connected", "disconnected");
            Transitions.Add(t4.Name, t4);

        }


        public Model(HashSet<String> states, Dictionary<String, Transition> transitions, String initState)
        {
            this.UsageProfile = null;
            InitState = initState;
            States = states;
            Transitions = transitions;
            Subscriptions = new HashSet<string>();
        }

        public Model(Model m)
        {
            InitState = m.InitState;
            State = m.State;
            States = m.States;
            Transitions = m.Transitions;
            Subscriptions = m.Subscriptions;
            UsageProfile = m.UsageProfile;
        }

        public override string ToString()
        {
            string s = "";
            foreach (string state in States)
            {
                s += "State: " + state + "\n";
            }
            //foreach (Transition t in Transitions.Values)
            //{
            //    s += t + "\n";
            //}
            return s;
        }

        public Transition[] GetPossibleTransitions(string state = null)
        {
            var s = (state != null) ? state : State;
            List<Transition> ts = new List<Transition>();
            Transitions.Values.ForEach(t =>
            {
                if (t.From.Equals(state))
                    ts.Add(t);
            });
            return ts.ToArray();
        }

        public List<WeightAndValue<Gen<Transition>>> GetPossibleTransitionsWithWeight(string state = null)
        {
            List<WeightAndValue<Gen<Transition>>> ts = new List<WeightAndValue<Gen<Transition>>>();
            Transitions.Values.ForEach(t =>
            {
                if (t.From.Equals(state))
                {
                    var weight = 1;
                    if (UsageProfile != null) {
                        weight = UsageProfile.MsgWeights[t.Input];
                    }
                    ts.Add(new WeightAndValue<Gen<Transition>>(weight,Gen.Constant(t)));
                }
            });
            return ts;
        }


        public string makeTransition(Transition t, Dictionary<string, object> data, bool modelOnlyMode = false)
        {
            State = t.To;
            //MethodInfo mi = this.GetType().GetMethod(t.Input, new Type[] { typeof(Dictionary<string, object>) });
            //if(smcMode && mi != null){
            //   mi.Invoke(this, new object[] { data });
            //}
            //Console.WriteLine(t +" "+ State);
            return t.Output;
        }

        public void subscribe(Dictionary<string, object> data)
        {
            if (!Subscriptions.Contains((string)data["topic"]))
            {
                Log.IncreaseSubscriptionNumbers((string)data["topic"]);
                Subscriptions.Add((string)data["topic"]);
            }
        }

        public void unsubscribe(Dictionary<string, object> data)
        {
            if (Subscriptions.Contains((string)data["topic"]))
            {
                Log.DecreaseSubscriptionNumbers((string)data["topic"]);
                Subscriptions.Remove((string)data["topic"]);
            }
        }

        public void connect(Dictionary<string, object> data) {
            Log.IncreaseSubscriptionNumbers(Subscriptions.ToArray(), true);
        }

        public void disconnect(Dictionary<string, object> data)
        {
            Log.DecreaseSubscriptionNumbers(Subscriptions.ToArray(), true);
        }


        public void publish(Dictionary<string, object> data)
        {
        }

        public void Reset()
        {
            //Console.WriteLine("reset state " + State + " subCount: " + Subscriptions.Count);
            if (Subscriptions.Count > 0)
            {
                if (State == "connected")
                {
                    Log.DecreaseSubscriptionNumbers(Subscriptions.ToArray());
                }
                else
                {
                    Log.DecreaseTotalSubscriptions(Subscriptions.Count);
                }
                Subscriptions.Clear();
            }
            State = InitState;
        }

    }
}
