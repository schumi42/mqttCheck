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
 
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FsCheck.Experimental;
using FsCheck;
using System.IO;

namespace mqttCheck
{
    //[Category("SMC")]
    public class Tests
    {
        string regressionPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\")) + "LinearRegression\\";
        string mosquittoRegressionModel = "mosquitto.txt";
        string emqttRegressionModel = "emqtt.txt";

        public Tests(){ }


        [Test]
        public void SmcOfTheSystemUnderTest()//SPRT
        {
           for (int threshold = 20; threshold <= 120; threshold += 10)
           {
               for (int clientNumber = 50; clientNumber <= 150; clientNumber += 20)
               {
                   SingleSPRTTestForSingleSetting(MQTTImplementation.mosquitto, clientNumber, 10, threshold, regressionPath + mosquittoRegressionModel);
                   SingleSPRTTestForSingleSetting(MQTTImplementation.emqtt, clientNumber, 10, threshold, regressionPath + emqttRegressionModel);
               }
           }
        }

        [Test]
        public void SmcOfTheModel() // Monte Carlo
        { 
            var modelPath = regressionPath + mosquittoRegressionModel; 
            int testCaseLen = 10;
            int samples = 100;//1060;

            for (int threshold = 20; threshold <= 150; threshold += 10)
            {
                for (int clientNumber = 50; clientNumber <= 150; clientNumber += 20)
                {
                    Console.WriteLine("threshold: " + threshold + " clientNumber: " + clientNumber);
                    MonteCarloSimulation(clientNumber, testCaseLen, threshold, modelPath, samples);
                }
            }
        }

       
        [Test]
        public void FunctionalTestOfTheSutForLogData()
        {
            var implementaion = MQTTImplementation.emqtt;
            BrokerHelper.start(implementaion);
            var config = Configuration.QuickThrowOnFailure;
            var models = new List<Model>();
            var suts = new List<Sut>();
            int maxClientNumer = 100;
            for (int i = 0; i < maxClientNumer; i++)
            {
                var m = new Model();
                models.Add(m);
                suts.Add(new Sut("client"+i));
            }
            for (int t = 0; t < 100; t++)
            {
                int clientNumber = StaticRandom.Next(3, maxClientNumer + 1);

                Log.ClientPopulationSize = clientNumber;
                Console.WriteLine("Testcase " + t + ": current Population Size: " + clientNumber);
                var properties = new List<Property>();
                var configs = new List<Configuration>();
                var machines = new List<StateMachineSpec>();
                for (int i = 0; i < clientNumber; i++)
                {
                    config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
                    config.MaxNbOfTest = 1;
                    config.EndSize = 50;
                    configs.Add(config.Copy());
                    machines.Add(new StateMachineSpec(suts[i], models[i]));
                    properties.Add(machines[i].ToProperty());
                }
                ParallelProperty p = new ParallelProperty(properties, configs);
                p.QuickCheck();
                machines.ForEach(m => m.Dispose());
            }
            BrokerHelper.stop(implementaion);
        }


        public double MonteCarloSimulation(int clientNumber, int testCaseLen, int threshold, string modelPath, int samples = 1060)
        {
            int thresholdTmp = (int)System.Diagnostics.Stopwatch.Frequency / 1000 * threshold;

            var config = Configuration.QuickThrowOnFailure;
            config.MaxNbOfTest = 1;
            config.QuietOnSuccess = true;
            config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());

            Log.loggingEnabled = false;
            var machines = new List<StateMachineSpec>();
            var properties = new List<QuantitativeSMCProperty>();
            for (int i = 0; i < clientNumber; i++)
            {
                var m = new Model();
                m.UsageProfile = new UsageProfile();
                RegressionModelParser.parseAndAnnotateDistributions(m, modelPath, "latency");
                machines.Add(new StateMachineSpec(m, testCaseLen, thresholdTmp));
                properties.Add(new MonteCarloProperty(machines[i].ToProperty(), config, samples));
                //properties.Add(new ChernoffProperty(machines[i].ToProperty(), config, 0.05, 0.01));
            }
            double res = new ParallelProperty(properties).QCheck();
            machines.ForEach(m => m.Dispose());
            Console.WriteLine("Model Simulation Probability: " + res);
            return res;
        }

        public void SingleSPRTTestForSingleSetting(MQTTImplementation implementation, int clientNumber, int testCaseLen, int threshold, string modelPath, double? simulationProb = null)
        {
            double difference = 0.25;
            Configuration c = Configuration.QuickThrowOnFailure;
            c.QuietOnSuccess = true;
            c.MaxNbOfTest = 1;
            c.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
            Console.WriteLine("SPRT threshold: " + threshold + ", clientNumber: " + clientNumber + ", testCaseLength: " + testCaseLen + ", Implementation: " + implementation.ToString());

            if (!simulationProb.HasValue)
            {
                simulationProb = MonteCarloSimulation(clientNumber, testCaseLen, threshold, modelPath);
            }

            int thresholdTmp = (int)System.Diagnostics.Stopwatch.Frequency / 1000 * threshold;
            var properties = new List<QualitativeSMCProperty>();
            var machines = new List<StateMachineSpec>();

            BrokerHelper.start(implementation);
            Log.loggingEnabled = true;
            Log.ClientPopulationSize = clientNumber;
            for (int i = 0; i < clientNumber; i++)
            {
                var model = new Model();
                model.UsageProfile = new UsageProfile();
                var sut = new Sut("client" + i);

                machines.Add(new StateMachineSpec(sut, model, testCaseLen, thresholdTmp));
            }


            for (int i = 0; i < clientNumber; i++)
            {
                var p = new SPRTProperty(machines[i].ToProperty(), c, simulationProb.Value - difference, simulationProb.Value, 0.01, 0.01);
                properties.Add(p);
            }
            //Console.WriteLine(("&" + Math.Round((simulationProb.Value - difference), 2) + "&" + Math.Round((simulationProb.Value), 2)).Replace(',', '.'));
            Console.WriteLine("H0: " + (simulationProb.Value - difference) + " H1: " + (simulationProb.Value));
            new ParallelProperty(properties).QuickCheck();

            machines.ForEach(m => m.Dispose());
            BrokerHelper.stop(implementation);
        }

        public void DoubleOrSingleSPRTTestForSingleSetting(MQTTImplementation implementation, int clientNumber, int testCaseLen, int threshold, string modelPath, double? simulationProb = null)
        {
            double difference = 0.25;
            Configuration c = Configuration.QuickThrowOnFailure;
            c.QuietOnSuccess = true;
            c.MaxNbOfTest = 1;
            c.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
            Console.WriteLine("SPRT threshold: " + threshold + ", clientNumber: " + clientNumber + ", testCaseLength: " + testCaseLen + ", Implementation: " + implementation.ToString());

            if (!simulationProb.HasValue)
            {
                simulationProb = MonteCarloSimulation(clientNumber, testCaseLen, threshold, modelPath);
            }

            int thresholdTmp = (int)System.Diagnostics.Stopwatch.Frequency / 1000 * threshold;
            var properties = new List<QualitativeSMCProperty>();
            var machines = new List<StateMachineSpec>();

            BrokerHelper.start(implementation);
            Log.loggingEnabled = true;
            Log.ClientPopulationSize = clientNumber;
            for (int i = 0; i < clientNumber; i++)
            {
                var model = new Model();
                model.UsageProfile = new UsageProfile();
                var sut = new Sut("client" + i);

                machines.Add(new StateMachineSpec(sut, model, testCaseLen, thresholdTmp));
            }
            if (simulationProb.Value + difference > 1.05)
            {
                Console.WriteLine("Only upper bound hypotheses will be checked!");

                for (int i = 0; i < clientNumber; i++)
                {
                    var p = new SPRTProperty(machines[i].ToProperty(), c, simulationProb.Value - difference, simulationProb.Value, 0.01, 0.01);
                    properties.Add(p);
                }
                Console.WriteLine("H0: " + (simulationProb.Value - difference) + " H1: " + (simulationProb.Value));
                new ParallelProperty(properties).QuickCheck();
            }
            else if (simulationProb.Value - difference < -0.05)
            {
                Console.WriteLine("Only lower bound hypotheses will be checked!");
                for (int i = 0; i < clientNumber; i++)
                {
                    var p = new SPRTProperty(machines[i].ToProperty(), c, simulationProb.Value + difference, simulationProb.Value, 0.01, 0.01);
                    properties.Add(p);
                }
                Console.WriteLine("H0: " + (simulationProb.Value + difference) + " H1: " + (simulationProb.Value));
                new ParallelProperty(properties).QuickCheck();
            }
            else
            {
                for (int i = 0; i < clientNumber; i++)
                {
                    var p = new DoubleSPRTProperty(machines[i].ToProperty(), c, simulationProb.Value, difference, 0.01, 0.01);
                    properties.Add(p);
                }
                Console.WriteLine("H0: " + ((simulationProb.Value - difference) < 0.0 ? 0.0 : simulationProb.Value - difference) + " H1: " + (simulationProb.Value) + " H2: " + ((simulationProb.Value + difference) > 1.0 ? 1.0 : simulationProb.Value + difference) + " H3: " + simulationProb.Value);
                new ParallelProperty(properties).DoubleQuickCheck();
            }
            machines.ForEach(m => m.Dispose());
            BrokerHelper.stop(implementation);
        }



        //[Test]
        //public void SPRTWithoutModelExecution() {
        //    var modelSimProbabilities = new Dictionary<int, double[]>();
        //    var modelSimProbabilities1 = new Dictionary<int, double[]>();

        //    modelSimProbabilities[20] = new double[]{ 0.99388679245283, 0.910458221024259, 0.826383647798742, 0.739725557461406, 0.592198838896952, 0.427955974842767};
        //    modelSimProbabilities[30] = new double[]{ 0.99988679245283, 0.954878706199461, 0.866163522012579, 0.778147512864494, 0.643011611030479, 0.492786163522012};
        //    modelSimProbabilities[40] = new double[]{ 0.999981132075472, 0.983692722371967, 0.899832285115304, 0.811329331046312, 0.684593613933236, 0.544672955974843};
        //    modelSimProbabilities[50] = new double[]{ 1, 0.996671159029649, 0.935807127882599, 0.845051457975987, 0.719680696661829, 0.590647798742138};
        //    modelSimProbabilities[60] = new double[]{ 1,  0.999474393530997, 0.971509433962264, 0.883910806174957, 0.759600870827286, 0.631037735849057};
        //    modelSimProbabilities[70] = new double[]{ 1, 0.999973045822102, 0.989821802935011, 0.916003430531733, 0.784941944847606, 0.66514465408805};
        //    modelSimProbabilities[80] = new double[] { 1, 1, 0.997285115303983, 0.955042881646655, 0.827902757619739, 0.704459119496855 };
        //    modelSimProbabilities[90] = new double[] { 1, 1, 0.998899371069182, 0.978567753001715, 0.867982583454281, 0.739377358490566 };
        //    modelSimProbabilities[100] = new double[]{ 1, 1, 0.999874213836478, 0.989159519725557, 0.90644412191582, 0.770037735849057};
        //    modelSimProbabilities[110] = new double[] { 1, 1, 1, 0.996183533447684, 0.941596516690856, 0.805918238993711 };
        //    modelSimProbabilities[120] = new double[] { 1, 1, 1, 0.998421955403087, 0.964905660377358, 0.835 };

        //    modelSimProbabilities1[20] = new double[] { 0.946509433962264, 0.845229110512129, 0.739433962264151, 0.616466552315609, 0.437148040638607, 0.285880503144654 };
        //    modelSimProbabilities1[30] = new double[] { 0.975962264150943, 0.884797843665769, 0.793176100628931, 0.687435677530017, 0.500072568940493, 0.336578616352201 };
        //    modelSimProbabilities1[40] = new double[] { 0.99488679245283, 0.915296495956873, 0.828018867924529, 0.73827615780446, 0.564934687953556, 0.377308176100629 };
        //    modelSimProbabilities1[50] = new double[] { 0.999415094339622, 0.94989218328841, 0.859622641509434, 0.768070325900515, 0.615870827285922, 0.419987421383648 };
        //    modelSimProbabilities1[60] = new double[] { 0.999962264150943, 0.976051212938005, 0.889098532494759, 0.795034305317324, 0.658933236574746, 0.498056603773585 };
        //    modelSimProbabilities1[70] = new double[] { 1, 0.992129380053908, 0.922117400419287, 0.822281303602058, 0.688853410740203, 0.55237106918239 };
        //    modelSimProbabilities1[80] = new double[] { 1, 0.998436657681941, 0.9541928721174, 0.854965694682676, 0.724172714078375, 0.598503144654088 };
        //    modelSimProbabilities1[90] = new double[] { 1, 0.999528301886792, 0.975691823899371, 0.885951972555747, 0.754941944847605, 0.627805031446541 };
        //    modelSimProbabilities1[100] = new double[] { 1, 0.999973045822102, 0.989046121593292, 0.921732418524871, 0.787663280116111, 0.663169811320755 };
        //    modelSimProbabilities1[110] = new double[] { 1, 1, 0.994528301886792, 0.948979416809605, 0.816226415094339, 0.687949685534591 };
        //    modelSimProbabilities1[120] = new double[] { 1, 1, 0.998364779874213, 0.971655231560892, 0.850957910014513, 0.721452830188679 };


        //     int i = 0;
        //     for (int clientNumber = 50; clientNumber <= 130; clientNumber += 20)
        //     {  
        //          for (int threshold = 30; threshold <= 110; threshold += 20){

        //              Console.WriteLine(threshold + "&" + clientNumber+" ");
        //              SingleSPRTTestForSingleSetting(MQTTImplementation.mosquitto, clientNumber, 10, threshold, regressionPath + mosquittoRegressionModel, modelSimProbabilities[threshold][i]);
        //              SingleSPRTTestForSingleSetting(MQTTImplementation.emqtt, clientNumber, 10, threshold, regressionPath + emqttRegressionModel, modelSimProbabilities1[threshold][i]);
        //          }
        //         i++;
        //     }
        //}

        //[Test]
        //public void MonteCarloonSUT()
        //{
        //    var implementation = MQTTImplementation.emqtt;
        //    //var implementation = MQTTImplementation.mosquitto;
        //    int testCaseLen = 10;
        //    int samples = 30;
        //    Configuration c = Configuration.QuickThrowOnFailure;
        //    c.QuietOnSuccess = true;
        //    c.MaxNbOfTest = 1;
        //    BrokerHelper.start(implementation);
        //    for (int threshold = 30; threshold <= 90; threshold += 20)
        //    {
        //        int thresholdTmp = (int)System.Diagnostics.Stopwatch.Frequency / 1000 * threshold;
        //        var outsting = "" + threshold;
        //        for (int clientNumber = 90; clientNumber <= 130; clientNumber += 20)
        //        {
        //            Log.ClientPopulationSize = clientNumber;
        //            var properties = new List<QuantitativeSMCProperty>();
        //            var machines = new List<StateMachineSpec>();
        //            for (int i = 0; i < clientNumber; i++)
        //            {
        //                var m = new Model();
        //                m.UsageProfile = new UsageProfile();
        //                var s = new Sut("client" + i);
        //                machines.Add(new StateMachineSpec(s, m, testCaseLen, thresholdTmp));
        //                properties.Add(new MonteCarloProperty(machines[i].ToProperty(), c, samples));
        //            }
        //            double res = new ParallelProperty(properties).QCheck();
        //            machines.ForEach(m => m.Dispose());
        //            BrokerHelper.restart(implementation);
        //            outsting += " " + res;
        //            Console.WriteLine("SUT Execution Probability: " + res);
        //            //Console.WriteLine("Total Sub: " + Log.TotalSubscriptions);
        //        }
        //        Console.WriteLine(outsting);
        //    }
        //    BrokerHelper.stop(implementation);
        //}

        //[Test]
        //public void SmcOfTheModel()// Monte Carlo Simulation
        //{
        //    //var modelPath = regressionPath + emqttRegressionModel;
        //    var modelPath = regressionPath + mosquittoRegressionModel; 
        //    int testCaseLen = 10;
        //    int samples = 100;

        //    for (int threshold = 20; threshold <= 150; threshold += 10)
        //    {
        //        int thresholdTmp = (int)System.Diagnostics.Stopwatch.Frequency / 1000 * threshold;
        //        var outsting = "" + threshold + " ";
        //        for (int clientNumber = 50; clientNumber <= 150; clientNumber += 20)
        //        {
        //            var config = Configuration.QuickThrowOnFailure;
        //            config.MaxNbOfTest = 1;
        //            config.QuietOnSuccess = true;
        //            config.Replay = FsCheck.Random.mkStdGen(StaticRandom.Next());
        //            Log.loggingEnabled = false;
        //            var machines = new List<StateMachineSpec>();
        //            var properties = new List<QuantitativeSMCProperty>();
        //            for (int i = 0; i < clientNumber; i++)
        //            {
        //                var m = new Model();
        //                m.UsageProfile = new UsageProfile();
        //                RegressionModelParser.parseAndAnnotateDistributions(m, regressionPath + emqttRegressionModel, "latency");
        //                machines.Add(new StateMachineSpec(m, testCaseLen, thresholdTmp));
        //                properties.Add(new MonteCarloProperty(machines[i].ToProperty(), config, samples));
        //                //properties.Add(new ChernoffProperty(machines[i].ToProperty(), config, 0.05, 0.01));
        //            }
        //            double res = new ParallelProperty(properties).QCheck();
        //            machines.ForEach(m => m.Dispose());
        //            Log.reset();
        //            Console.WriteLine("Model Simulation Probability: " + res);
        //            outsting += " " + res;
        //        }
        //        Console.WriteLine(outsting);
        //    }
        //}

    }
}
