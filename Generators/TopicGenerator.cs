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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mqttCheck
{
    class TopicGenerator : MsgGenerator
    {
        private int randomTopicWeight = 0;
        private int defaultTopicWeight = 1;
        private static string[] defaultTopics;// = { "topic1", "topic2", "topic3", "topic4", "topic5", "topic6", "topic7", "topic8", "topic9", "topic10" };



        //public TopicGenerator(string name, int randomTopicWeight, int defaultTopicWeight, int minLength = 12) : base(name, minLength) 
        //{
        //    this.randomTopicWeight = randomTopicWeight;
        //    this.defaultTopicWeight = defaultTopicWeight;
        //} 

        private void  initDefaultTopic(){
            int topicNumber = 100;
            defaultTopics = new string[topicNumber];
            for(int i = 0; i < topicNumber; i++){
                defaultTopics[i] = "topic" +i;
            }
        }

        public void setTopicsWithPostfix(string postfix, int topicNumber = 100) {
            defaultTopics = new string[topicNumber];
            for (int i = 0; i < topicNumber; i++)
            {
                defaultTopics[i] = "topic" + i+ postfix;
            }
        }

        public TopicGenerator(string name, int minLength = 12) :base(name,minLength) {
            initDefaultTopic();
        }
        public TopicGenerator(string name, int minLength, int maxLength) :base(name,minLength,maxLength) {
            initDefaultTopic();
        }

        protected TopicGenerator(TopicGenerator a)
            : base(a)
        {
            initDefaultTopic();
            this.minLength = a.minLength;
            this.maxLength = a.maxLength;
            this.randomTopicWeight = a.randomTopicWeight;
            this.defaultTopicWeight = a.defaultTopicWeight;
        }

        public override object Clone()
        {
            var o = new TopicGenerator(this);
            return o;
        }


        public override Gen<object> Generator(Dictionary<string, object> data = null)
        {
            var gen1 = base.Generator(data);
            var gen2 = Gen.Elements(defaultTopics).Select(s=>(object)s);

            return Gen.Frequency(new Tuple<int, Gen<object>>(randomTopicWeight, gen1), 
                                 new Tuple<int, Gen<object>>(defaultTopicWeight, gen2));
        }
    }
}
