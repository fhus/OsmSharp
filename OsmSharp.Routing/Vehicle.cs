﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2013 Abelshausen Ben
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

using System.Collections.Generic;
using OsmSharp.Collections.Tags;
using OsmSharp.Math.Geo;
using OsmSharp.Units.Speed;
using OsmSharp.Routing.Interpreter.Roads;
using OsmSharp.Osm;

namespace OsmSharp.Routing
{
    /// <summary>
    ///     Vehicle class contains routing info
    /// </summary>
    public abstract class Vehicle
    {
        /// <summary>
        /// Default Car
        /// </summary>
        public static readonly Vehicle Car = new Car();

        /// <summary>
        /// Default Pedestrian
        /// </summary>
        public static readonly Vehicle Pedestrian = new Pedestrian();

        /// <summary>
        /// Default Bicycle
        /// </summary>
        public static readonly Vehicle Bicycle = new Bicycle();

        /// <summary>
        /// Default Moped
        /// </summary>
        public static readonly Vehicle Moped = new Moped();

        /// <summary>
        /// Default MotorCycle
        /// </summary>
        public static readonly Vehicle MotorCycle = new MotorCycle();

        /// <summary>
        /// Default SmallTruck
        /// </summary>
        public static readonly Vehicle SmallTruck = new SmallTruck();

        /// <summary>
        /// Default BigTruck
        /// </summary>
        public static readonly Vehicle BigTruck = new BigTruck();

        /// <summary>
        /// Default BigTruck
        /// </summary>
        public static readonly Vehicle Bus = new Bus();

        /// <summary>
        /// Contains Accessiblity Rules
        /// </summary>
        protected readonly Dictionary<string, string> AccessibleTags = new Dictionary<string, string>();
        /// <summary>
        /// Trys to return the highwaytype from the tags
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected bool TryGetHighwayType(TagsCollection tags, out string highwayType)
        {
            return tags.TryGetValue("highway", out highwayType);
        }

        /// <summary>
        ///     Returns true if the edge with the given tags can be traversed by the vehicle.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public virtual bool CanTraverse(TagsCollection tags)
        {
            string highwayType;
            if (TryGetHighwayType(tags, out highwayType))
            {
                return IsVehicleAllowed(tags, highwayType);
            }
            return false;
        }

        /// <summary>
        /// Returns the Max Speed for the highwaytype in Km/h
        /// </summary>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected abstract KilometerPerHour MaxSpeedAllowed(string highwayType);

        /// <summary>
        /// Returns the max speed this vehicle can handle.
        /// </summary>
        /// <returns></returns>
        protected abstract KilometerPerHour MaxSpeed();

        /// <summary>
        /// Returns the maximum speed.
        /// </summary>
        /// <returns></returns>
        public KilometerPerHour MaxSpeedAllowed(TagsCollection tags)
        {
            // THESE ARE THE MAX SPEEDS FOR BELGIUM. 
            // TODO: Find a way to make this all configurable.
            KilometerPerHour speed = 5;

            // get max-speed tag if any.
            if(tags.TryGetMaxSpeed(out speed))
            {
                return speed;
            }

            string highwayType;
            if (TryGetHighwayType(tags, out highwayType))
            {
                speed = this.MaxSpeedAllowed(highwayType);
            }

            return speed;
        }

        /// <summary>
        /// Estimates the probable speed of this vehicle on a way with the given tags.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public virtual KilometerPerHour ProbableSpeed(TagsCollection tags)
        {
            KilometerPerHour maxSpeedAllowed = this.MaxSpeedAllowed(tags);
            KilometerPerHour maxSpeed = this.MaxSpeed();
            if (maxSpeed.Value < maxSpeedAllowed.Value)
            {
                return maxSpeed;
            }
            return maxSpeedAllowed;
        }

        /// <summary>
        /// Returns true if the edges with the given properties are equal for the vehicle.
        /// </summary>
        /// <param name="tags1"></param>
        /// <param name="tags2"></param>
        /// <returns></returns>
        public virtual bool IsEqualFor(TagsCollection tags1, TagsCollection tags2)
        {
            if (this.GetName(tags1) != this.GetName(tags2))
            {
                // the name have to be equal.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the weight between two points on an edge with the given tags for the vehicle.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public virtual float Weight(TagsCollection tags, GeoCoordinate from, GeoCoordinate to)
        {
            var distance = from.DistanceEstimate(to).Value;

            return (float)(distance / (this.ProbableSpeed(tags).Value) * 3.6);
        }

        /// <summary>
        ///     Returns true if the edge is one way forward, false if backward, null if bidirectional.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public virtual bool? IsOneWay(TagsCollection tags)
        {
            string oneway;
            if (tags.TryGetValue("oneway", out oneway))
            {
                if (oneway == "yes")
                {
                    return true;
                }
                return false;
            }
            string junction;
            if (tags.TryGetValue("junction", out junction))
            {
                if (junction == "roundabout")
                {
                    return true;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the name of a given way.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        private string GetName(TagsCollection tags)
        {
            var name = string.Empty;
            if (tags.ContainsKey("name"))
            {
                name = tags["name"];
            }
            return name;
        }

        /// <summary>
        /// Returns true if the vehicle is allowed on the way represented by these tags
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected abstract bool IsVehicleAllowed(TagsCollection tags, string highwayType);
    }

    /// <summary>
    /// Represents a pedestrian
    /// </summary>
    public class Pedestrian : Vehicle
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Pedestrian()
        {
            AccessibleTags.Add("footway", string.Empty);
            AccessibleTags.Add("cycleway", string.Empty);
            AccessibleTags.Add("path", string.Empty);
            AccessibleTags.Add("road", string.Empty);
            AccessibleTags.Add("pedestrian", string.Empty);
            AccessibleTags.Add("living_street", string.Empty);
            AccessibleTags.Add("residential", string.Empty);
            AccessibleTags.Add("unclassified", string.Empty);
            AccessibleTags.Add("secondary", string.Empty);
            AccessibleTags.Add("secondary_link", string.Empty);
            AccessibleTags.Add("primary", string.Empty);
            AccessibleTags.Add("primary_link", string.Empty);
            AccessibleTags.Add("tertiary", string.Empty);
            AccessibleTags.Add("tertiary_link", string.Empty);
            //AccessibleTags.Add("trunk", string.Empty);
        }

        /// <summary>
        /// Returns true if the vehicle is allowed on the way represented by these tags
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected override bool IsVehicleAllowed(TagsCollection tags, string highwayType)
        {
            if (tags.ContainsKey("foot"))
            {
                if (tags["foot"] == "designated")
                {
                    return true; // designated foot
                }
                if (tags["foot"] == "yes")
                {
                    return true; // yes for foot
                }
                if (tags["foot"] == "no")
                {
                    return false; // no for foot
                }
            }
            if (tags.ContainsKey("bicycle"))
            {
                if (tags["bicycle"] == "designated")
                {
                    return false; // designated bicycle
                }
            }
            return AccessibleTags.ContainsKey(highwayType);
        }

        /// <summary>
        /// Returns the Max Speed for the highwaytype in Km/h.
        /// 
        /// This does not take into account how fast this vehicle can go just the max possible speed.
        /// </summary>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected override KilometerPerHour MaxSpeedAllowed(string highwayType)
        {
            switch (highwayType)
            {
                case "services":
                case "proposed":
                case "cycleway":
                case "pedestrian":
                case "steps":
                case "path":
                case "footway":
                case "living_street":
                    return 5;
                case "track":
                case "road":
                    return 30;
                case "residential":
                case "unclassified":
                    return 50;
                case "motorway":
                case "motorway_link":
                    return 120;
                case "trunk":
                case "trunk_link":
                case "primary":
                case "primary_link":
                    return 90;
                default:
                    return 70;
            }
        }

        /// <summary>
        ///     Returns true if the edge is one way forward, false if backward, null if bidirectional.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public override bool? IsOneWay(TagsCollection tags)
        {
            return null;
        }

        /// <summary>
        /// Returns the maximum possible speed this vehicle can achieve.
        /// </summary>
        /// <returns></returns>
        protected override KilometerPerHour MaxSpeed()
        {
            return 5;
        }
    }

    /// <summary>
    /// Represents a bicycle
    /// </summary>
    public class Bicycle : Vehicle
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Bicycle()
        {
            AccessibleTags.Add("cycleway", string.Empty);
            AccessibleTags.Add("path", string.Empty);
            AccessibleTags.Add("road", string.Empty);
            AccessibleTags.Add("living_street", string.Empty);
            AccessibleTags.Add("residential", string.Empty);
            AccessibleTags.Add("unclassified", string.Empty);
            AccessibleTags.Add("secondary", string.Empty);
            AccessibleTags.Add("secondary_link", string.Empty);
            AccessibleTags.Add("primary", string.Empty);
            AccessibleTags.Add("primary_link", string.Empty);
            AccessibleTags.Add("tertiary", string.Empty);
            AccessibleTags.Add("tertiary_link", string.Empty);
            //AccessibleTags.Add("trunk", string.Empty);
        }

        /// <summary>
        /// Returns true if the vehicle is allowed on the way represented by these tags
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected override bool IsVehicleAllowed(TagsCollection tags, string highwayType)
        {
            // do the designated tags.
            if (tags.ContainsKey("bicycle"))
            {
                if (tags["bicycle"] == "designated")
                {
                    return true; // designated bicycle
                }
                if (tags["bicycle"] == "yes")
                {
                    return true; // yes for bicycle
                }
                if (tags["bicycle"] == "no")
                {
                    return false; //  no for bicycle
                }
            }
            if (tags.ContainsKey("foot"))
            {
                if (tags["foot"] == "designated")
                {
                    return false; // designated foot
                }
            }
            return AccessibleTags.ContainsKey(highwayType);
        }

        /// <summary>
        /// Returns the Max Speed for the highwaytype in Km/h.
        /// 
        /// This does not take into account how fast this vehicle can go just the max possible speed.
        /// </summary>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected override KilometerPerHour MaxSpeedAllowed(string highwayType)
        {
            switch (highwayType)
            {
                case "services":
                case "proposed":
                case "cycleway":
                case "pedestrian":
                case "steps":
                case "path":
                case "footway":
                case "living_street":
                    return 5;
                case "track":
                case "road":
                    return 30;
                case "residential":
                case "unclassified":
                    return 50;
                case "motorway":
                case "motorway_link":
                    return 120;
                case "trunk":
                case "trunk_link":
                case "primary":
                case "primary_link":
                    return 90;
                default:
                    return 70;
            }
        }

        /// <summary>
        /// Returns true if the edge is one way forward, false if backward, null if bidirectional.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public override bool? IsOneWay(TagsCollection tags)
        {
            return null;
        }

        /// <summary>
        /// Returns the maximum possible speed this vehicle can achieve.
        /// </summary>
        /// <returns></returns>
        protected override KilometerPerHour MaxSpeed()
        {
            return 15;
        }
    }

    /// <summary>
    /// Represents a MotorVehicle
    /// </summary>
    public abstract class MotorVehicle : Vehicle
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        protected MotorVehicle()
        {
            AccessibleTags.Add("road", string.Empty);
            AccessibleTags.Add("living_street", string.Empty);
            AccessibleTags.Add("residential", string.Empty);
            AccessibleTags.Add("unclassified", string.Empty);
            AccessibleTags.Add("secondary", string.Empty);
            AccessibleTags.Add("secondary_link", string.Empty);
            AccessibleTags.Add("primary", string.Empty);
            AccessibleTags.Add("primary_link", string.Empty);
            AccessibleTags.Add("tertiary", string.Empty);
            AccessibleTags.Add("tertiary_link", string.Empty);
            AccessibleTags.Add("trunk", string.Empty);
            AccessibleTags.Add("trunk_link", string.Empty);
            AccessibleTags.Add("motorway", string.Empty);
            AccessibleTags.Add("motorway_link", string.Empty);
        }

        /// <summary>
        /// Returns true if the vehicle is allowed on the way represented by these tags
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected override bool IsVehicleAllowed(TagsCollection tags, string highwayType)
        {
            if (tags.ContainsKey("motor_vehicle"))
            {
                if (tags["motor_vehicle"] == "no")
                {
                    return false;
                }
            }
            if (tags.ContainsKey("foot"))
            {
                if (tags["foot"] == "designated")
                {
                    return false; // designated foot
                }
            }
            if (tags.ContainsKey("bicycle"))
            {
                if (tags["bicycle"] == "designated")
                {
                    return false; // designated bicycle
                }
            }
            return AccessibleTags.ContainsKey(highwayType);
        }

        /// <summary>
        /// Returns the Max Speed for the highwaytype in Km/h.
        /// 
        /// This does not take into account how fast this vehicle can go just the max possible speed.
        /// </summary>
        /// <param name="highwayType"></param>
        /// <returns></returns>
        protected override KilometerPerHour MaxSpeedAllowed(string highwayType)
        {
            switch (highwayType)
            {
                case "services":
                case "proposed":
                case "cycleway":
                case "pedestrian":
                case "steps":
                case "path":
                case "footway":
                case "living_street":
                    return 5;
                case "track":
                case "road":
                    return 30;
                case "residential":
                case "unclassified":
                    return 50;
                case "motorway":
                case "motorway_link":
                    return 120;
                case "trunk":
                case "trunk_link":
                case "primary":
                case "primary_link":
                    return 90;
                default:
                    return 70;
            }
        }

        /// <summary>
        /// Returns the maximum possible speed this vehicle can achieve.
        /// </summary>
        /// <returns></returns>
        protected override KilometerPerHour MaxSpeed()
        {
            return 200;
        }
    }

    /// <summary>
    /// Represents a moped
    /// </summary>
    public class Moped : MotorVehicle
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Moped()
        {
            AccessibleTags.Remove("motorway");
            AccessibleTags.Remove("motorway_link");
        }

        /// <summary>
        /// Returns the maximum possible speed this vehicle can achieve.
        /// </summary>
        /// <returns></returns>
        protected override KilometerPerHour MaxSpeed()
        {
            return 40;
        }
    }

    /// <summary>
    /// Represents a MotorCycle
    /// </summary>
    public class MotorCycle : MotorVehicle
    {

    }

    /// <summary>
    /// Represents a Car
    /// </summary>
    public class Car : MotorVehicle
    {

    }

    /// <summary>
    /// Represents a SmallTruck
    /// </summary>
    public class SmallTruck : MotorVehicle
    {

    }

    /// <summary>
    /// Represents a BigTruck
    /// </summary>
    public class BigTruck : MotorVehicle
    {

    }

    /// <summary>
    /// Represents a Bus
    /// </summary>
    public class Bus : MotorVehicle
    {

    }
}