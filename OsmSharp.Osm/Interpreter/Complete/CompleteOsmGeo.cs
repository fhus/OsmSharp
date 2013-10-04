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

using OsmSharp.Collections;
using OsmSharp.Geo.Geometries;
using OsmSharp.Osm.Interpreter;
using NetTopologySuite.Geometries;
using GeoAPI.Geometries;

namespace OsmSharp.Osm
{
    /// <summary>
    /// Base class for all the osm data that represents an object on the map.
    /// 
    /// Nodes, Ways and Relations
    /// </summary>
    public abstract class CompleteOsmGeo : CompleteOsmBase
    {
        /// <summary>
        /// Creates a new OsmGeo object.
        /// </summary>
        /// <param name="id"></param>
        internal CompleteOsmGeo(long id)
            :base(id)
        {
            this.Visible = true;
            this.UserId = null;
            this.User = null;
        }

        /// <summary>
        /// Creates a new OsmGeo object with a string table.
        /// </summary>
        /// <param name="string_table"></param>
        /// <param name="id"></param>
        internal CompleteOsmGeo(ObjectTable<string> string_table, long id)
            : base(string_table, id)
        {
            this.Visible = true;
            this.UserId = null;
            this.User = null;
        }

        /// <summary>
        /// Converts this OsmGeo object to an OsmGeoSimple object.
        /// </summary>
        /// <returns></returns>
        public abstract OsmSharp.Osm.OsmGeo ToSimple();

        #region Geometry - Interpreter

        /// <summary>
        /// The interpreter for these objects.
        /// </summary>
        public static GeometryInterpreter GeometryInterperter = new SimpleGeometryInterpreter(); // set a default geometry interpreter.

        /// <summary>
        /// The geometries this OSM-object represents.
        /// </summary>
        private GeometryCollection _geometries;

        /// <summary>
        /// Returns the geometries this OSM-object represents.
        /// </summary>
        public GeometryCollection Geometries
        {
            get
            {
                if (_geometries == null)
                {
                    _geometries = CompleteOsmGeo.GeometryInterperter.Interpret(this);
                }
                return _geometries;
            }
        }

        /// <summary>
        /// Make sure the geometries of this objects will be recalculated.
        /// </summary>
        public void ResetGeometries()
        {
            _geometries = null;
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// The bounding box of object.
        /// </summary>
        public override Envelope Envelope
        {
            get
            {
                return this.Geometries.EnvelopeInternal;
            }
        }

        /// <summary>
        /// Gets/Sets the changeset id.
        /// </summary>
        public long? ChangeSetId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the visible flag.
        /// </summary>
        public bool Visible { get; set; }

        #endregion
    }
}
