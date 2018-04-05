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
using System.Threading.Tasks;

namespace mqttCheck
{
    public abstract class QualitativeSMCProperty
    {
        abstract public Tuple<bool?, int> QCheck();
        abstract public void QuickCheck();
        abstract public void Continue();
        abstract public Configuration Config { get; set; }

        public virtual bool isDoubleProperty()
        {
            return (this.GetType() == typeof(DoubleSPRTProperty));
        }

        public virtual Tuple<bool?, bool?, int, int> DoubleQCheck()
        {
            throw new NotImplementedException();
        }
    }
}
