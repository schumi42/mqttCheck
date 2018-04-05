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
using System;


using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mqttCheck
{
    public static class RegressionModelParser
    {


        public static void parseAndAnnotateDistributions(Model model, string modelFilePath, string targetVar)
        {
            string[] lines = File.ReadAllLines(modelFilePath);

            var distributions = new Dictionary<string, Tuple<double, double>>();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] linParts = line.Split(' ');
                if (!linParts[1].Contains("na") && !linParts[1].Contains("Estimate"))
                {
                    var tuple = new Tuple<double, double>(Double.Parse(linParts[1], System.Globalization.NumberStyles.Any, CultureInfo.GetCultureInfo("en-US")), Double.Parse(linParts[2], System.Globalization.NumberStyles.Any, CultureInfo.GetCultureInfo("en-US")));
                    linParts[0] = linParts[0].Replace("\"", "");
                    distributions.Add(linParts[0], tuple);
                }
            }

            Tuple<double, double> intercept = distributions["(Intercept)"];
            Tuple<double, double> xActiveRequests = distributions["X.ActiveRequests"];
            Tuple<double, double> xActiveRequests1 = distributions["X.ActiveRequests1"];
            Tuple<double, double> xTotalSubscriptions = distributions["X.TotalSubscriptions"];
            Tuple<double, double> xSubscribers = distributions["X.Subscribers"];

            Func<string, int, int, int, Tuple<double, double>> latency = delegate(string msg, int activeRequests, int totalSubscriptions, int subscribers)
            {
                //Console.WriteLine("Msg: " + msg + " activeReq: " + activeRequests + " totalSubs: " + totalSubscriptions + " subs: " + subscribers);

                var activeRequests1 = activeRequests;
                if (msg == "disconnect" || msg == "subscribe" || msg == "unsubscribe" || msg == "publish")
                {
                    activeRequests = 0;
                    totalSubscriptions = 0;
                }
                else if (msg == "connect") {
                    totalSubscriptions = 0;
                    activeRequests1 = 0;
                }
                double mean = intercept.Item1;
                double variance = Math.Pow(intercept.Item2, 2);
                mean += activeRequests * xActiveRequests.Item1;
                variance += Math.Pow(activeRequests, 2) * Math.Pow(xActiveRequests.Item2, 2);
                mean += activeRequests1 * xActiveRequests1.Item1;
                variance += Math.Pow(activeRequests1, 2) * Math.Pow(xActiveRequests1.Item2, 2);

                mean += totalSubscriptions * xTotalSubscriptions.Item1;
                variance += Math.Pow(totalSubscriptions, 2) * Math.Pow(xTotalSubscriptions.Item2, 2);
                mean += subscribers * xSubscribers.Item1;
                variance += Math.Pow(subscribers, 2) * Math.Pow(xSubscribers.Item2, 2);

                var tempKey = "Msg" + msg;
                if (distributions.ContainsKey(tempKey))
                {
                    mean += distributions[tempKey].Item1;
                    variance += Math.Pow(distributions[tempKey].Item2, 2);
                }
                var sd = Math.Sqrt(variance);
                return new Tuple<double, double>(mean, sd);
            };

            foreach (var t in model.Transitions.Values)
            {
                var name = t.Input + "_" + targetVar;
                var gen = new LatencyGenerator(name, model, t.Input, latency, t.DataGenerators);
                t.DataGenerators = new OrderedDictionary();
                t.DataGenerators.Add(name,gen);
            }
        }

    }
}
