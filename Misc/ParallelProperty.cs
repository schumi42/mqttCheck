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
using System.Threading;
using System.Threading.Tasks;

namespace mqttCheck
{
    class ParallelProperty//<T> where T : Property, QualitativeSMCProperty, QuantitativeSMCProperty
    {
        List<Property> properties;
        List<QualitativeSMCProperty> qualitativeProperties;
        List<QuantitativeSMCProperty> quantitativeProperties;
        Configuration config = null;
        List<Configuration> configs = null;

        public ParallelProperty(List<Property> properties, Configuration config)
        {
            this.properties = properties;
            this.config = config;
        }
        public ParallelProperty(List<Property> properties, List<Configuration> configs)
        {
            this.properties = properties;
            this.configs = configs;
        }
        public ParallelProperty(List<QualitativeSMCProperty> properties)
        {
            this.qualitativeProperties = properties;
        }
        public ParallelProperty(List<QuantitativeSMCProperty> properties)
        {
            this.quantitativeProperties = properties;
        }

        private readonly object _locker = new object();
        public void QuickCheck(){
            if (properties != null)
            { 
                List<Task<bool>> tasks = new List<Task<bool>>();
                for (int i = 0; i< properties.Count; i++)
                {
                    var p = properties[i];
                    var c = config != null ? config : configs[i];

                    var t = Task<bool>.Factory.StartNew(() =>
                    {
                        try
                        {
                            c = c.Copy();
                            c.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                            //Check.One(config, p);
                            p.Check(c);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            return false;
                        }
                        return true;
                    });
                    tasks.Add(t);

                }
                bool allPassed = true;
                int notPassedCnt = 0;
                foreach (var t in tasks)
                {
                    //Console.WriteLine("Property succeeded: " + t.Result);
                    if (!t.Result) {
                        allPassed = false;
                        notPassedCnt++;
                    }
                }
                if (!allPassed)
                {
                    //throw new Exception("Only " + (tasks.Count - notPassedCnt) + " out of " + tasks.Count + " Properties passed!");
                    Console.WriteLine("Only " + (tasks.Count - notPassedCnt) + " out of " + tasks.Count + " Properties passed!");
                }
                else {
                    Console.WriteLine("All Properties passed!");
                }
            }
            else if (qualitativeProperties != null)
            {
                List<Task<Tuple<bool?, int>>> tasks = new List<Task<Tuple<bool?, int>>>();
                int finishCount = 0;
                foreach (var p in qualitativeProperties)
                {
                    int i = qualitativeProperties.IndexOf(p);
                    var t = Task<Tuple<bool?, int>>.Factory.StartNew(() =>
                    {
                        //p.Config.RenewRandom();
                        bool? h0Accepted;
                        int samples;
                        try
                        {
                            var tuple = p.QCheck();
                            h0Accepted = tuple.Item1;
                            samples = tuple.Item2;
                            lock(_locker){finishCount++;}
                            while(finishCount != qualitativeProperties.Count){
                                p.Continue();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            lock (_locker) { finishCount++; }
                            return null;
                        }
                        return new Tuple<bool?,int>(h0Accepted,samples);
                    });
                    tasks.Add(t);

                }
                int countH0Accepted = 0;
                int totalSamples = 0;
                int countFailure = 0;
                foreach (var t in tasks)
                {
                    if (t.Result.Item1.HasValue)
                    {
                        if(t.Result.Item1.Value){
                            countH0Accepted++;
                        }
                        totalSamples += t.Result.Item2;
                    }
                    else{
                        countFailure++;
                    } 
                }
                //string outstr = "";
                //if (countH0Accepted + 1 >= qualitativeProperties.Count) { 
                //    outstr += "&$H_0$ ";
                //}
                //else if(countH0Accepted-1 <= 0){
                //    outstr += "&$H_1$ ";
                //}
                //else{
                //    outstr += "&" + (int)(Math.Round((double)((double)qualitativeProperties.Count - countH0Accepted - countFailure)*100 / ((double)qualitativeProperties.Count - countFailure))) + "\\% $H_1$ ";
                //}
                //outstr += "&" + (Math.Round((double)totalSamples / (double)(qualitativeProperties.Count - countFailure),2)) + " ";
                //Console.WriteLine(outstr.Replace(',','.'));
                Console.WriteLine("H0 accepted: " + countH0Accepted + " times. H0 rejected: " + (qualitativeProperties.Count - countH0Accepted - countFailure) + " times, after " + ((double)totalSamples / (double)(qualitativeProperties.Count - countFailure)) + " samples. Failures: " + countFailure);
                //Console.WriteLine("####################################################");
                //Console.WriteLine((countH0Accepted + "$H_0$ " + (qualitativeProperties.Count - countH0Accepted - countFailure) + "$H_1$ & " + ((double)totalSamples / (double)(qualitativeProperties.Count - countFailure)) + "  ").Replace(",", "."));
                //Console.WriteLine("####################################################");
            }
            else if (quantitativeProperties != null)
            {
                List<Task<double?>> tasks = new List<Task<double?>>();
                int finishCount = 0;
                foreach (var p in quantitativeProperties)
                {
                    var t = Task<double?>.Factory.StartNew(() =>
                    {
                        //p.Config.RenewRandom();
                        double  probability;
                        try
                        {
                            probability = p.QCheck();
                            lock (_locker) { finishCount++; }
                            while (finishCount != quantitativeProperties.Count)
                            {
                                p.Continue();
                            }
                        }
                        catch (Exception e)
                        {
                            lock (_locker) { finishCount++; }
                            Console.WriteLine(e);
                            return null;
                        }
                        return probability;
                    });
                    tasks.Add(t);

                }
                double probabilitySum = 0;
                int countFailure = 0;
                foreach (var t in tasks)
                {
                    if (t.Result.HasValue)
                    {
                        probabilitySum += t.Result.Value;
                    }
                    else
                    {
                        countFailure++;
                    }
                }
                Console.WriteLine("Average Probability: " + (probabilitySum / ((double) (quantitativeProperties.Count - countFailure))) + " Failures: " + countFailure);
            }


        }
        public double QCheck()
        {
            if (qualitativeProperties != null)
            {
                if (qualitativeProperties[0].isDoubleProperty()) {
                    Console.WriteLine("Function does not support DoubleProperties");
                    return 0;
                }
                List<Task<bool?>> tasks = new List<Task<bool?>>();
                int finishCount = 0;
                foreach (var p in qualitativeProperties)
                {
                    int i = qualitativeProperties.IndexOf(p);
                    var t = Task<bool?>.Factory.StartNew(() =>
                    {
                        //p.Config.RenewRandom();
                        bool? h0Accepted;
                        try
                        {
                            var tuple = p.QCheck();
                            h0Accepted = tuple.Item1;
                            lock(_locker){finishCount++;}
                            while(finishCount != qualitativeProperties.Count){
                                p.Continue();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            lock (_locker) { finishCount++; }
                            return null;
                        }
                        return h0Accepted;
                    });
                    tasks.Add(t);

                }
                int countH0Accepted = 0;
                int countFailure = 0;
                foreach (var t in tasks)
                {
                    if (t.Result.HasValue)
                    {
                        if(t.Result.Value){
                            countH0Accepted++;
                        }
                    }
                    else{
                        countFailure++;
                    } 
                }
                return (double)countH0Accepted / qualitativeProperties.Count;
            }
            else if (quantitativeProperties != null)
            {
                int finishCount = 0;
                List<Task<double?>> tasks = new List<Task<double?>>();
                foreach (var p in quantitativeProperties)
                {
                    var t = Task<double?>.Factory.StartNew(() =>
                    {
                        //p.Config.RenewRandom();
                        double probability;
                        try
                        {
                            probability = p.QCheck();
                            //Console.WriteLine("subprob for " + Thread.CurrentThread.ManagedThreadId + ": " + probability);
                            lock (_locker) { finishCount++; }
                            while (finishCount != quantitativeProperties.Count)
                            {
                                p.Continue();
                            }
                        }
                        catch (Exception e)
                        {
                            lock (_locker) { finishCount++; }
                            Console.WriteLine(e);
                            return null;
                        }
                        return probability;
                    });
                    tasks.Add(t);

                }
                double probabilitySum = 0;
                int countFailure = 0;
                foreach (var t in tasks)
                {
                    if (t.Result.HasValue)
                    {
                        
                        probabilitySum += t.Result.Value;
                    }
                    else
                    {
                        countFailure++;
                    }
                }
                if (countFailure > 0)
                {
                    Console.WriteLine("Failures: " + countFailure);
                }
                //Console.WriteLine("Average Probability: " + (probabilitySum / ((double)(quantitativeProperties.Count - countFailure))) + " Failures: " + countFailure);
                return (probabilitySum / ((double)(quantitativeProperties.Count - countFailure)));

            }
            else 
            {
                Console.WriteLine("Function only supported for qualitative/quantitative properties!");
            }
            return 0;
       }



        public void DoubleQuickCheck()
        {
            if (qualitativeProperties != null)
            {
                List<Task<Tuple<bool?, bool?, int, int>>> tasks = new List<Task<Tuple<bool?, bool?, int, int>>>();
                int finishCount = 0;
                foreach (var p in qualitativeProperties)
                {
                    int i = qualitativeProperties.IndexOf(p);
                    var t = Task<Tuple<bool?, bool?, int, int>>.Factory.StartNew(() =>
                    {
                        //p.Config.RenewRandom();
                        Tuple<bool?, bool?, int, int> res = null;
                        try
                        {
                            res = p.DoubleQCheck();
                            lock (_locker) { finishCount++; }
                            while (finishCount != qualitativeProperties.Count)
                            {
                                p.Continue();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            lock (_locker) { finishCount++; }
                            return null;
                        }
                        return res;
                    });
                    tasks.Add(t);

                }
                int countH0Accepted = 0;
                int countH2Accepted = 0;
                int sampleCount1 = 0;
                int sampleCount2 = 0;
                int countFailure = 0;
                foreach (var t in tasks)
                {
                    if (t.Result != null && t.Result.Item1.HasValue && t.Result.Item2.HasValue)
                    {
                        if (t.Result.Item1.Value)
                        {
                            countH0Accepted++;
                        }
                        sampleCount1 += t.Result.Item3;
                        if (t.Result.Item2.Value)
                        {
                            countH2Accepted++;
                        }
                        sampleCount2 += t.Result.Item4;
                    }
                    else
                    {
                        countFailure++;
                    }
                }
                //Console.WriteLine("success count: " + countH0Accepted + " Failures: " + countFailure);
                //Console.WriteLine("H0 accepted: " + countH0Accepted + " times. H0 rejected: " + (qualitativeProperties.Count - countH0Accepted - countFailure) + " times. Failures: " + countFailure);
                Console.WriteLine("H0 accepted: " + countH0Accepted + " times, H0 rejected " + (qualitativeProperties.Count - countH0Accepted - countFailure) + " times after " + ((double)sampleCount1 / (double)(qualitativeProperties.Count - countFailure)) + " samples, H2 accepted: " + countH2Accepted + " times, H2 rejected " + (qualitativeProperties.Count - countH2Accepted - countFailure) + " times  after " + ((double)sampleCount2 / (double)(qualitativeProperties.Count - countFailure)) + " samples. Failures: " + countFailure);
                
                Console.WriteLine("####################################################");
                Console.WriteLine((countH0Accepted + "$H_0$ " + (qualitativeProperties.Count - countH0Accepted - countFailure) + "$H_1$ & " + ((double)sampleCount1 / (double)(qualitativeProperties.Count - countFailure)) + " & " + countH2Accepted + "$H_2$ " + (qualitativeProperties.Count - countH2Accepted - countFailure) + "$H_3$ &" + ((double)sampleCount2 / (double)(qualitativeProperties.Count - countFailure))+"  ").Replace(",","."));
                Console.WriteLine("####################################################");

            }



        }

    }
}
