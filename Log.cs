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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mqttCheck
{
    public class Log
    {
        private static readonly object _lockGlobalOperationIndex = new object(); 
        private static int globalOperationIndex = 0;
        public static bool loggingEnabled = true;
        public static bool deleteOldLogs = true;
        private static int totalSubscriptions = 0;
        public static int TotalSubscriptions { get { return totalSubscriptions; } set { totalSubscriptions = value; } }
        private static readonly object _lockScription = new object();
        private static ConcurrentDictionary<string, int> subscriptionNumbers = new ConcurrentDictionary<string, int>();
        private static readonly object _lockUnsubscription = new object();
        private static ConcurrentDictionary<string, int> unsubscriptionCounter = new ConcurrentDictionary<string, int>();
        private static readonly object _lockMsgsReceived = new object();
        private static ConcurrentDictionary<string, int> msgsReceived = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, AutoResetEvent> msgsReceivedEvents = new ConcurrentDictionary<string, AutoResetEvent>();

        private static int retainSize = 0;
        private static readonly object _lockRetainedMsgs = new object();
        private static ConcurrentDictionary<string, int> retainedMsgs = new ConcurrentDictionary<string, int>();
        private static readonly object _lockLastLogEntries = new object();
        private static ConcurrentDictionary<string, long> lastLogEntries = new ConcurrentDictionary<string, long>();


        public static ConcurrentDictionary<string, int> SubscriptionNumbers
        {
            get { return subscriptionNumbers; }
            set { subscriptionNumbers = value; }
        }
        public static ConcurrentDictionary<string, int> UnsubscriptionCounter
        {
            get { return unsubscriptionCounter; }
            set { unsubscriptionCounter = value; }
        }

        public static ConcurrentDictionary<string, int> MsgsReceived
        {
            get { return msgsReceived; }
            set { msgsReceived = value; }
        }
        public static ConcurrentDictionary<string, AutoResetEvent> MsgsReceivedEvents
        {
            get { return msgsReceivedEvents; }
            set { msgsReceivedEvents = value; }
        }

        public static void reset(){
            subscriptionNumbers = new ConcurrentDictionary<string, int>();
            UnsubscriptionCounter = new ConcurrentDictionary<string, int>();
            msgsReceived = new ConcurrentDictionary<string, int>();
            msgsReceivedEvents = new ConcurrentDictionary<string, AutoResetEvent>();
            totalSubscriptions = 0;
        }





        public static void IncreaseSubscriptionNumbers(string[] topics, bool keepTotalSubscriptions = false)
        {
            foreach (var t in topics)
            {
                IncreaseSubscriptionNumbers(t,keepTotalSubscriptions);
            }
        }
        public static void IncreaseSubscriptionNumbers(string topic, bool keepTotalSubscriptions = false){
            lock (_lockScription)
            {
                if (!subscriptionNumbers.ContainsKey(topic))
                {
                    subscriptionNumbers[topic] = 1;
                }
                else
                {
                    subscriptionNumbers[topic] = subscriptionNumbers[topic] + 1;
                }
                if(!keepTotalSubscriptions){
                    totalSubscriptions++;
                }
            }
        }
        public static void DecreaseSubscriptionNumbers(string[] topics, bool keepTotalSubscriptions = false)
        {
            foreach (var t in topics) {
                DecreaseSubscriptionNumbers(t,keepTotalSubscriptions);
            }
        }
        public static void DecreaseSubscriptionNumbers(string topic, bool keepTotalSubscriptions = false)
        {
            lock (_lockScription)
            {
                Assert.True(subscriptionNumbers.ContainsKey(topic));
                Assert.True(subscriptionNumbers[topic] > 0);
                subscriptionNumbers[topic] = subscriptionNumbers[topic] - 1;
                //if (subscriptionNumbers[topic] == 0)
                //{
                //    int tmp;
                //    subscriptionNumbers.TryRemove(topic, out tmp);
                //    //TODO CHECK while publishing?
                //}
                if (!keepTotalSubscriptions)
                {
                    totalSubscriptions--;
                }
            }
            IncreaseUnsubscriptionCounter(topic);
        }
        public static void DecreaseTotalSubscriptions(int number)
        {
            lock (_lockScription)
            {
                totalSubscriptions -= number;
            }
        }
        public static void IncreaseUnsubscriptionCounter(string topic) {
            lock (_lockUnsubscription)
            {
                if (unsubscriptionCounter.ContainsKey(topic))
                {
                    unsubscriptionCounter[topic] = unsubscriptionCounter[topic] + 1;
                }
                else
                {
                    unsubscriptionCounter[topic] = 1;
                }
            }
        }



        public static void SignalMsgsReceived(string topic, string msg)
        {
            var key = topic + "_" + msg;
            lock (_lockMsgsReceived)
            {
                if (!msgsReceived.ContainsKey(key))
                {
                    msgsReceived[key] = 1;
                }
                else
                {
                    msgsReceived[key] = msgsReceived[key] + 1;
                }
                if (msgsReceivedEvents.ContainsKey(key))
                {
                    msgsReceivedEvents[key].Set();
                }
            }
        }

        public static void initPublish(string topic, string msg, out int initialSubscriptionNumber, out int initialUnsubscriptionCounter) {
            var key = topic + "_" + msg;
            Log.MsgsReceivedEvents[topic + "_" + msg] = new AutoResetEvent(false);
            Log.MsgsReceived[topic + "_" + msg] = 0;
            lock (_lockScription)
            {
                if (!subscriptionNumbers.ContainsKey(topic)) {
                    subscriptionNumbers[topic] = 0;
                }
            }
            lock (_lockUnsubscription)
            {
                if (!unsubscriptionCounter.ContainsKey(topic))
                {
                    unsubscriptionCounter[topic] = 0;
                }
            }
            initialSubscriptionNumber = subscriptionNumbers[topic];
            initialUnsubscriptionCounter = unsubscriptionCounter[topic];
        }


        public static bool WaitForMsgsReceivedSignals(string topic, string msg, int initialSubscriptionNumber, int initialUnsubscriptionCounter, Stopwatch watch, out long duration, out string error, out int publishReceiver)
        {
            var key = topic + "_" + msg;
            int previousUnsubscriptions =  unsubscriptionCounter[topic] - initialUnsubscriptionCounter;
            duration = watch.ElapsedTicks;
            error = "";
            publishReceiver = 0;
            while (initialSubscriptionNumber - previousUnsubscriptions > msgsReceived[key])
            {
                if (!msgsReceivedEvents[key].WaitOne(TimeSpan.FromSeconds(15)))
                {

                    //Console.WriteLine("TEMP subscriptionnumber: " + subscriptionNumbers[topic]+ " "+ initialSubscriptionNumber);
                    //Console.WriteLine("TEMP msg received: "+msgsReceived[key]);
                    previousUnsubscriptions = unsubscriptionCounter[topic] - initialUnsubscriptionCounter;
                    //Console.WriteLine("TEMP previousUnsubscriptions: " + previousUnsubscriptions);
                    if (initialSubscriptionNumber - previousUnsubscriptions <= msgsReceived[key]) {
                        //Console.WriteLine("Test temp!!");
                        //return true;
                        break;
                    }
                    watch.Stop();
                    Console.WriteLine("Timeout Waiting for received msg signal!! " + " topic: " +topic +" Msg: " + msg + " initialSubscriptionNumber: " + initialSubscriptionNumber + " previousUnsubscriptions: " + previousUnsubscriptions + "msg received: " + msgsReceived[key]);
                    duration = watch.ElapsedTicks;
                    publishReceiver = msgsReceived[key];
                    error = "Timeout Waiting for received msg signal!";
                    return false;
                }
                duration = watch.ElapsedTicks;
                //Console.WriteLine("TEMP DEL: " + subscriptionNumbers[topic] +" " + initialSubscriptionNumber + " " + msgsReceived[key]);
                //Console.WriteLine("Received msg signal!!");
                previousUnsubscriptions = unsubscriptionCounter[topic] - initialUnsubscriptionCounter;
                //Console.WriteLine("TEMP previousUnsubscriptions: " + previousUnsubscriptions);
            }
            watch.Stop();
            publishReceiver = msgsReceived[key];
            lock (_lockMsgsReceived)
            {
                AutoResetEvent tmpOut;
                msgsReceivedEvents.TryRemove(key, out tmpOut);
                int tmp;
                msgsReceived.TryRemove(key, out tmp);
            }
            //Console.WriteLine("All Signals received!!");
            return true;
        }

        public static void createLogFile(string client)
        {
            if (!loggingEnabled) {
                return;
            }
            var path = client + ".csv";
            if (!File.Exists(path))
            {
                File.Delete(path);
            }

            //if (!File.Exists(path))
            //{
                using (StreamWriter sw = File.CreateText(path))
                {
                    var parts = new List<string>();
                    parts.Add("Time");
                    parts.Add("OperationIDForClient");
                    parts.Add("GlobalOperationID");
                    parts.Add("Client");
                    parts.Add("#ActiveRequests");
                    parts.Add("#TotalSubscriptions");
                    parts.Add("Msg");
                    parts.Add("From");
                    parts.Add("To");
                    parts.Add("Retain");
                    parts.Add("TopicSize");
                    parts.Add("CumulativeTopicSize");
                    parts.Add("MsgSize");
                    parts.Add("CumulativeMsgSize");
                    parts.Add("#Subscribers");
                    parts.Add("#PublishReceiver");
                    parts.Add("Success");
                    parts.Add("ExceptionMsg");
                    parts.Add("PopulationSize");
                    parts.Add("Duration");

                    string toWrite = ToSCVLine(parts);
                    sw.WriteLine(toWrite);
                }
            //}
        }

        public static string ToSCVLine<T>(IEnumerable<T> toConvert)
        {
            return String.Join(";", toConvert.Select(x => x.ToString()).ToArray());
        }


        private static readonly object _lockCumulativeMsgSize = new object();
        public static long CumulativeMsgSize { get; set; }
        public static void increaseCumulativeMsgSize(long size)
        {
            lock (_lockCumulativeMsgSize)
            {
                CumulativeMsgSize += size;
            }
        }
        private static readonly object _lockCumulativeTopicSize = new object();
        public static long CumulativeTopicSize { get; set; }
        public static void increaseCumulativeTopicSize(long size)
        {
            lock (_lockCumulativeTopicSize)
            {
                CumulativeTopicSize += size;
            }
        }

        private static readonly object _lockActiveRequestCount = new object();
        public static int ActiveRequestCount { get; set; }
        public static void increaseRequestCount()
        {
            lock (_lockActiveRequestCount)
            {
                ActiveRequestCount++;
            }
        }
        public static void decreaseRequestCount()
        {
            lock (_lockActiveRequestCount)
            {
                ActiveRequestCount--;
            }
        }

        public static int ClientPopulationSize { get; set; }

        public static void storeRetainedMsg(string topic, string msg) { 
            int msgSize = System.Text.ASCIIEncoding.Unicode.GetByteCount(msg);
            int diff = msgSize;
            lock(_lockRetainedMsgs){
                if(retainedMsgs.ContainsKey(topic)){
                    diff = msgSize - retainedMsgs[topic];
                }
                retainedMsgs[topic] = msgSize;
                retainSize += diff;
            }
        }


        public static void Write(int operationIndex, string client, string msg, string currentState, string nextState, long duration, string[] topics = null, string msgText = null, bool retain = false, int subscribercount = 0, int publishReceiver = 0, bool success = true, string exceptionMsg = "")
        {
            if (!loggingEnabled)
            {
                return;
            }
            int msgSize = msgText != null ? System.Text.ASCIIEncoding.Unicode.GetByteCount(msgText) : 0;

            int topicSize = 0;
            if(topics != null){
                foreach(var t in topics){
                    topicSize += System.Text.ASCIIEncoding.Unicode.GetByteCount(t);
                }
            }

            if (msg == "publish" && retain)
            {
                increaseCumulativeMsgSize(msgSize);
                if (SubscriptionNumbers.ContainsKey(topics[0]))
                {
                    increaseCumulativeTopicSize(topicSize);
                }
            }
            else if(msg == "subscribe"){
                foreach (var t in topics)
                {
                    if (!SubscriptionNumbers.ContainsKey(t))
                    {
                        increaseCumulativeTopicSize(System.Text.ASCIIEncoding.Unicode.GetByteCount(t));
                    }
                }  
            }

            int globalOpIndex;
            lock (_lockGlobalOperationIndex)
            {
                globalOpIndex =globalOperationIndex++;
            }
            var parts = new List<string>();
            parts.Add(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
            parts.Add(operationIndex.ToString());
            parts.Add(globalOpIndex.ToString());
            parts.Add(client);
            parts.Add(ActiveRequestCount.ToString());
            parts.Add(totalSubscriptions.ToString());
            parts.Add(msg);
            parts.Add(currentState);
            parts.Add(nextState);
            parts.Add(retain.ToString());
            parts.Add(topicSize.ToString());
            parts.Add(CumulativeTopicSize.ToString());
            parts.Add(msgSize.ToString());
            parts.Add(retainSize.ToString());
            parts.Add(subscribercount.ToString());
            parts.Add(publishReceiver.ToString());
            parts.Add(success.ToString());
            parts.Add(exceptionMsg);
            //parts.Add(exceptionMsg != null && exceptionMsg != "" ? exceptionMsg : "");

            parts.Add(ClientPopulationSize.ToString());
            parts.Add(duration.ToString());
            var line = ToSCVLine(parts);
            write(client, line);
            lock (_lockLastLogEntries) {
                lastLogEntries[client] = duration;
            }
        }

        public static long getLastDurtation(string client) {
            return lastLogEntries[client];
        }

        private static void write(string client, string message)
        {
            using (StreamWriter sw = File.AppendText(client+".csv"))
            {
                sw.WriteLine(message);
            }
        }
        
    }
}
