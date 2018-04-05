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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace mqttCheck
{
    class Sut
    {
        private static string mqttBrokerHostName = "localhost";

        public string Name {get; set;}
        public enum ResponseType { Error, ConnAck, ConnectionClosed, PubAck, Publish, SubAck, UnSubAck, Publish__PubAck };
        const int timeout = 15;
        AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        int operationIndex = 0;
        HashSet<string> subscriptions = new HashSet<string>();

        string clientId = null;

        MqttClient client = null;
        public Sut(string name) {
            this.Name = name;
            clientId = Guid.NewGuid().ToString();
            initClient();
            Log.createLogFile(name);
        }
        private void initClient() {
            

            if (client != null)
            {
                client.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
                client.MqttMsgPublished -= client_MqttMsgPublished;
                client.ConnectionClosed -= client_ConnectionClosed;
                client.MqttMsgSubscribed -= client_MqttMsgSubscribed;
                client.MqttMsgUnsubscribed -= client_MqttMsgUnsubscribed;
            }
            client = new MqttClient(mqttBrokerHostName);
            //client = new MqttClient(IPAddress.Parse("192.168.88.129"));
            //client = new MqttClient("test.mosquitto.org");
            //Console.WriteLine("Connected " + client.IsConnected);
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.MqttMsgPublished += client_MqttMsgPublished;
            client.ConnectionClosed += client_ConnectionClosed;
            client.MqttMsgSubscribed += client_MqttMsgSubscribed;
            client.MqttMsgUnsubscribed += client_MqttMsgUnsubscribed;
        }

        public ResponseType connect(Dictionary<string, object> data)
        {
            return connect();
        }
        public ResponseType connect()
        {
            var wasConnected = client.IsConnected;
            if (wasConnected) {
                //return this.disconnect();
                Console.WriteLine("Unknown Error!");
                return ResponseType.Error;
            }
            
            Log.increaseRequestCount();
            //clientId = Guid.NewGuid().ToString();
            var duration = connectWrapper(clientId);
            //Console.WriteLine("Connected " + client.IsConnected);
            if (client.IsConnected) {
                Log.IncreaseSubscriptionNumbers(this.subscriptions.ToArray(), true);
                Log.Write(operationIndex++, Name, "connect", wasConnected.ToString(), client.IsConnected.ToString(), duration);
                Log.decreaseRequestCount();
                return ResponseType.ConnAck;
            }
            Log.Write(operationIndex++, Name, "connect", wasConnected.ToString(), client.IsConnected.ToString(), duration, null, null, false, 0, 0, false, "Unknown Error!");
            Log.decreaseRequestCount();

            Console.WriteLine("Unknown Error!");
            return ResponseType.Error;
        }


        AutoResetEvent autoResetEventSubscribe = new AutoResetEvent(false);
        public ResponseType subscribe(Dictionary<string, object> data)
        {
            if (data.ContainsKey("topic"))
            {
                return subscribe(new string[] { (string)data["topic"] });
            }
            else if (data.ContainsKey("topics"))
            {
                return subscribe((string[])data["topics"]);
            }
            else
            {
                return subscribe("testtopic");
            }
        }
        public ResponseType subscribe()
        {
            return subscribe("testtopic");
        }
        public ResponseType subscribe(string topic = "testtopic")
        {
            return subscribe(new string[] { topic });
        }
        public ResponseType subscribe(string[] topics) {
            //Console.WriteLine("Subscribing for topic: '" + topics[0]+"'");
            if (!client.IsConnected)
            {
                return ResponseType.ConnectionClosed;
            }
            Log.increaseRequestCount();
            var watch = Stopwatch.StartNew();
            client.Subscribe(topics, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            if (!autoResetEventSubscribe.WaitOne(TimeSpan.FromSeconds(timeout)))
            {
                watch.Stop();
                
                Console.WriteLine("Timeout!!");
                Log.Write(operationIndex++, Name, "subscribe", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks, topics, null, false, 0, 0, false, "Timeout!");
                Log.decreaseRequestCount();
                return ResponseType.Error;
            }
            watch.Stop();
            Log.Write(operationIndex++, Name, "subscribe", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks, topics);
            Log.decreaseRequestCount();
            foreach (var t in topics) {
                if (!subscriptions.Contains(t)) {
                    Log.IncreaseSubscriptionNumbers(t);
                    subscriptions.Add(t);
                }
            }
            return ResponseType.SubAck;
        }
        void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            autoResetEventSubscribe.Set();
        }


        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
           //Console.WriteLine("Message received: "+System.Text.Encoding.UTF8.GetString(e.Message) +" for Topic " + e.Topic);
            Log.SignalMsgsReceived(e.Topic, System.Text.Encoding.UTF8.GetString(e.Message)); 
        }



        AutoResetEvent autoResetEventPublish = new AutoResetEvent(false);
        private bool publishSuccess = false;
        public ResponseType publish(Dictionary<string, object> data)
        {
            return publish((string)data["topic"], (string)data["message"]);
        }
        public ResponseType publish()
        {
            return publish("testtopic", "test");
        }
        public ResponseType publish(string topic, string msg)
        {
            //Console.WriteLine("Publishing: Msg: '" + msg + "' for topic: '" + topic + "'");
            if (!client.IsConnected)
            {
                return ResponseType.ConnectionClosed;
            }
            publishSuccess = false;
            autoResetEventPublish.Reset();

            int initialSubscriptionNumber = 0;
            int initialUnsubscriptionCounter = 0;
            Log.initPublish(topic, msg, out initialSubscriptionNumber, out initialUnsubscriptionCounter);

            Log.increaseRequestCount();
            var watch = Stopwatch.StartNew();
            bool retain = false;
            client.Publish(topic, Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, retain);
            if (!autoResetEventPublish.WaitOne(TimeSpan.FromSeconds(timeout)))
            {
                watch.Stop();
                Log.decreaseRequestCount();
                Log.Write(operationIndex++, Name, "publish", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks, new string[] { topic }, msg, retain, 0, 0, false, "Timeout!");
                Console.WriteLine("Timeout!!");
                return ResponseType.Error;
            }
            //watch.Stop();
            if (retain)
            {
                Log.storeRetainedMsg(topic, msg);
            }
            //Log.Write(operationIndex++, Name, "publish", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks, new string[] { topic }, msg, retain, 0, 0);
            //Log.decreaseRequestCount();
                

            long duration = watch.ElapsedTicks;
            string error = "";
            int publishReceiver = 0;
                //watch = Stopwatch.StartNew();
            if (initialUnsubscriptionCounter > 0 && !Log.WaitForMsgsReceivedSignals(topic, msg, initialSubscriptionNumber, initialUnsubscriptionCounter, watch, out duration, out error, out publishReceiver))
            {
                Log.Write(operationIndex++, Name, "publish", true.ToString(), client.IsConnected.ToString(), duration, new string[] { topic }, msg, retain, 0, 0, false, error);
                //Log.Write(operationIndex++, Name, "waitforreception", true.ToString(), client.IsConnected.ToString(), duration, new string[] { topic }, msg, retain, 0, 0, false, error);
                Log.decreaseRequestCount();
                return ResponseType.Error;
            }
            Log.Write(operationIndex++, Name, "publish", true.ToString(), client.IsConnected.ToString(), duration, new string[] { topic }, msg, retain, initialSubscriptionNumber, publishReceiver);
            //Log.Write(operationIndex++, Name, "waitforreception", true.ToString(), client.IsConnected.ToString(), duration, new string[] { topic }, msg, retain, initialSubscriptionNumber, publishReceiver);
            Log.decreaseRequestCount();
            if (publishSuccess)
            {
                return ResponseType.PubAck;
            }

            Console.WriteLine("Unkown Error!!");
            return ResponseType.Error;
        }
        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            //Console.WriteLine("Message published: " + e.IsPublished );
            publishSuccess = e.IsPublished;
            autoResetEventPublish.Set();
        }


        AutoResetEvent autoResetEventDisconnect = new AutoResetEvent(false);
        public ResponseType disconnect(Dictionary<string, object> data)
        {
            return disconnect();
        }
        public ResponseType disconnect()
        {
            Log.DecreaseSubscriptionNumbers(this.subscriptions.ToArray(), true);
            //subsciptions.Clear();
            //foreach(var t in this.subsciptions){
            //    Log.IncreaseUnsubscriptionCounter(t);
            //}
            if (!client.IsConnected) {
                return ResponseType.Error;
            }
            Log.increaseRequestCount();
            var watch = Stopwatch.StartNew();
            client.Disconnect();
            if (!autoResetEventDisconnect.WaitOne(TimeSpan.FromSeconds(timeout)))
            {
                watch.Stop();
                Console.WriteLine("Timeout!!");
                Log.Write(operationIndex++, Name, "disconnect", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks, null, null, false, 0, 0, false, "Timeout!");
                Log.decreaseRequestCount();
                return ResponseType.Error;
            }
            watch.Stop();
            Log.Write(operationIndex++, Name, "disconnect", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks);
            Log.decreaseRequestCount();
            if (client.IsConnected)
            {
                return ResponseType.Error;
            }
            return ResponseType.ConnectionClosed;
        }
        void client_ConnectionClosed(object sender, EventArgs e)
        {
            //Console.WriteLine("Connection Closed!");
            autoResetEventDisconnect.Set();
        }




        AutoResetEvent autoResetEventUnsubscribe = new AutoResetEvent(false);
        public ResponseType unsubscribe(Dictionary<string, object> data)
        {
            if (data.ContainsKey("topic"))
            {
                return unsubscribe(new string[] { (string)data["topic"] });
            }
            else if (data.ContainsKey("topics"))
            {
                return unsubscribe((string[])data["topics"]);
            }
            else
            {
                return unsubscribe("testtopic");
            }
        }
        public ResponseType unsubscribe()
        {
            return unsubscribe("testtopic");
        }
        public ResponseType unsubscribe(string topic)
        {
            return unsubscribe(new string[] { topic });
        }
        public ResponseType unsubscribe(string[] topics)
        {
            //Console.WriteLine("Unsubscribing for topic: '" + topics[0] + "'");
            if (!client.IsConnected)
            {
                return ResponseType.ConnectionClosed;
            }
            Log.increaseRequestCount();
            var watch = Stopwatch.StartNew();
            client.Unsubscribe(topics);
            if (!autoResetEventUnsubscribe.WaitOne(TimeSpan.FromSeconds(timeout)))
            {
                watch.Stop();
                
                Console.WriteLine("Timeout!!");
                Log.Write(operationIndex++, Name, "unsubscribe", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks, topics, null, false, 0, 0, false, "Timeout!");
                Log.decreaseRequestCount();
                return ResponseType.Error;
            }
            watch.Stop();
            Log.Write(operationIndex++, Name, "unsubscribe", true.ToString(), client.IsConnected.ToString(), watch.ElapsedTicks,topics);
            Log.decreaseRequestCount();
            foreach (var t in topics)
            {
                if (subscriptions.Contains(t))
                {
                    Log.DecreaseSubscriptionNumbers(t);
                    subscriptions.Remove(t);
                }
            }
            return ResponseType.UnSubAck;
        }
        void client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            autoResetEventUnsubscribe.Set();
        }




        public void Reset() {
            //Thread.Sleep(1000);
            //Console.WriteLine("---------Reset Start subscriptions count: " + subscriptions.Count + " connected: " + client.IsConnected + "total Subs" + Log.TotalSubscriptions);
           
            if (client.IsConnected)
            {
                try{
                   this.disconnect();
                }
                catch(Exception e){
                    Log.decreaseRequestCount();
                    throw e;
                }
            }
            ////Log.DecreaseSubscriptionNumbers(this.subsciptions.ToArray());
            Log.DecreaseTotalSubscriptions(subscriptions.Count);
            subscriptions.Clear();
            if(clientId != null)
            {
                try
                {
                    Log.increaseRequestCount();
                    connectWrapper(clientId, true);
                    Log.decreaseRequestCount();
                }
                catch(Exception e){
                    Log.decreaseRequestCount();
                    throw e;
                }
                try
                {
                    this.disconnect();
                }
                catch (Exception e)
                {
                    Log.decreaseRequestCount();
                    throw e;
                }
                client.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
                client.MqttMsgPublished -= client_MqttMsgPublished;
                client.ConnectionClosed -= client_ConnectionClosed;
                client.MqttMsgSubscribed -= client_MqttMsgSubscribed;
                client.MqttMsgUnsubscribed -= client_MqttMsgUnsubscribed;
            }

            //Console.WriteLine("---------SUT resetted. subscriptions count: " + subscriptions.Count + " connected: " + client.IsConnected + "total Subs" + Log.TotalSubscriptions);
           
        }

        private long connectWrapper(string clientId, bool cleanSession = false) {
            //if (client == null)
            //{
                initClient();
            //}
            //Log.increaseRequestCount();
            var watch = Stopwatch.StartNew();
            client.Connect(clientId, null, null, false, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false, null, null, cleanSession, 60);
            watch.Stop();
            //Log.decreaseRequestCount();
            return watch.ElapsedTicks;
        }



        public ResponseType run(string name, Dictionary<string, object> data = null)
        {
            //if (data != null)
            //{
            //    data["topic"] = Gen.Elements(new object[]{"topic1", "topic2", "topic3"}).Sample(1, 1).First();//"testtopic";
            //    data["message"] = Guid.NewGuid().ToString();
            //}
            try
            {
                MethodInfo mi = this.GetType().GetMethod(name, new Type[] { typeof(Dictionary<string, object>) });
                return (ResponseType)mi.Invoke(this, new object[] { data });
            }
            catch (Exception e) {
                Log.decreaseRequestCount();
                throw e;
            }
        }

        public bool isConnected() {
            if (client != null && client.IsConnected) {
                return true;
            }
            return false;
        }

    }
}
