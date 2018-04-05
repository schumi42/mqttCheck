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
    class MsgGenerator : BaseGenerator
    {
        protected int minLength = 12;
        protected int? maxLength = null;

        public MsgGenerator(string name, int minLength = 12)
            : base(name)
        {
            this.Type = DataType.String;
            this.minLength = minLength;
        }
        public MsgGenerator(string name, int minLength, int maxLength)
            : base(name)
        {
            this.Type = DataType.String;
            this.minLength = minLength;
            this.maxLength = maxLength;
        }

        protected MsgGenerator(MsgGenerator a)
            : base(a)
        {
            this.minLength = a.minLength;
            this.maxLength = a.maxLength;
        }

        public override object Clone()
        {
            var o = new MsgGenerator(this);
            return o;
        }

        private string adjustString(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                s = "-";
            }
            
            string specialChars = "[\x00-\x08\x0E-\x1F\x26\x7F\'\x23\x2b\x2f\x24\x25]";
            string whitespaces = "[\x0A\x0B\x0C\x0D]";
            string invalidSurrogateCharacter = "[\ud800-\udfff]";

            s = Regex.Replace(s, specialChars, "-", RegexOptions.Compiled);
            s = Regex.Replace(s, whitespaces, "-", RegexOptions.Compiled);
            s = Regex.Replace(s, invalidSurrogateCharacter, "-", RegexOptions.Compiled);

            if (maxLength.HasValue && s.Length >= maxLength)
                s = s.Substring(0, (int)maxLength - 1);

            var normaldChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            while (s.Length < minLength)
                s += normaldChars[StaticRandom.Next(0, normaldChars.Length-1)];
            return s;
        }

        public override Gen<object> Generator(Dictionary<string, object> data = null)
        {
            return Arb.Generate<string>().Select(s =>
            {
                var val = adjustString(s);
                return (object)val;
            });
        }
    }
}
