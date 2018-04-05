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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mqttCheck
{

    public enum MQTTImplementation { mosquitto, emqtt }
    static class BrokerHelper
    {
        private static string emqttPath = "C:/Users/rschumi/Downloads/emqttd";
        private static string mosquittoPath = "C:/Program Files (x86)/mosquitto";

        public static void start(MQTTImplementation i)
        {
            if (i == MQTTImplementation.emqtt)
            {
                var cmd = "cd " + emqttPath + "; ./bin/emqttd start; exit";
                run(cmd);
            }
            else if (i == MQTTImplementation.mosquitto)
            {
                var cmd = "cd '"+mosquittoPath+"'; ./mosquitto.exe -d; exit";
                run(cmd);
            }
        }
        public static void restart(MQTTImplementation i)
        {
            if (i == MQTTImplementation.emqtt)
            {
                var cmd = "cd "+emqttPath+"; ./bin/emqttd restart; exit";
                run(cmd);
            }
            else if (i == MQTTImplementation.mosquitto)
            {
                var cmd = "taskkill /f /t /im mosquitto.exe; cd '"+mosquittoPath+"'; ./mosquitto.exe -d; exit";
                run(cmd);
            }
        }

        public static void stop(MQTTImplementation i)
        {
            if (i == MQTTImplementation.emqtt)
            {
                var cmd = "cd "+emqttPath+"; ./bin/emqttd stop; exit";
                run(cmd);
            }
            else if (i == MQTTImplementation.mosquitto) {
                var cmd = "taskkill /f /t /im mosquitto.exe; exit";
                run(cmd);
            }
        }

        private static void run(string cmd) {
            int timeout = 30000;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"powershell.exe";
            startInfo.Arguments = @" "+cmd;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;

            startInfo.CreateNoWindow = true;
            Process process = new Process();
            process.StartInfo = startInfo;
            //Console.WriteLine("start");
            //process.Start();
            //Console.WriteLine("started");

            //StringBuilder output = new StringBuilder();
            //StringBuilder error = new StringBuilder();

            //using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            //using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            //{
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        Console.WriteLine("stdout:  " + e.Data);
                };
                process.ErrorDataReceived += (sender, e) =>
                {

                    if(e.Data != null)
                        Console.WriteLine("stderr:  " + e.Data);
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (process.WaitForExit(timeout))
                {
                    Console.WriteLine("broker cmd done");
                    Thread.Sleep(5000);  
                }
                //else if (outputWaitHandle.WaitOne(timeout) || errorWaitHandle.WaitOne(timeout))
                //{
                //    Console.WriteLine(output);
                //    Console.WriteLine(error);
                //    Thread.Sleep(5000); 
                //}
                else
                {
                    Console.WriteLine("timeout");
                }
           // }
        }
    }
}
