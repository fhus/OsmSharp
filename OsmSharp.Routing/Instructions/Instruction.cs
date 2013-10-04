﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2012 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OsmSharp.Math.Geo;
using OsmSharp.Routing.ArcAggregation.Output;
using GeoAPI.Geometries;

namespace OsmSharp.Routing.Instructions
{
    /// <summary>
    /// Represents an instruction.
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// Creates a new instruction with only a location.
        /// </summary>
        /// <param name="location"></param>
        public Instruction(Envelope location)
        {
            this.Location = location;
        }

        /// <summary>
        /// Creates a new instruction with a location and points of interest.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="pois"></param>
        public Instruction(Envelope location, List<PointPoi> pois)
        {
            this.Location = location;
            this.Pois = pois;
        }

        /// <summary>
        /// The points of interest for this instruction.
        /// </summary>
        public List<PointPoi> Pois { get; private set; }

        /// <summary>
        /// The location of this instruction.
        /// </summary>
        public Envelope Location { get; private set; }

        /// <summary>
        /// The instruction text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets/sets some extras if needed.
        /// </summary>
        public Dictionary<string, object> Extras { get; set; }
    }
}
