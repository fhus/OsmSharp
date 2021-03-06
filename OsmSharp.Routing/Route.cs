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

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using OsmSharp.Collections.Tags;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Primitives;
using OsmSharp.Units.Distance;
using OsmSharp.Math;

namespace OsmSharp.Routing
{
    /// <summary>
    /// Class representing a route generated by OsmSharp.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Creates a new route.
        /// </summary>
        public Route()
        { 
            this.TimeStamp = DateTime.Now;
        }

        /// <summary>
        /// The vehicle type this route was created for.
        /// </summary>
        public Vehicle Vehicle { get; set; }

        /// <summary>
        /// Tags for this route.
        /// </summary>
        public RouteTags[] Tags { get; set; }

        /// <summary>
        /// Route metrics.
        /// </summary>
        public RouteMetric[] Metrics { get; set; }
        
        /// <summary>
        /// An ordered array of route entries reprenting the details of the route to the next
        /// route point.
        /// </summary>
        public RoutePointEntry[] Entries { get; set; }

        /// <summary>
        /// A timestamp for this route.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        #region Save / Load

#if !WINDOWS_PHONE
        #region Raw Route

        /// <summary>
        /// Saves a serialized version to a file.
        /// </summary>
        /// <param name="file"></param>
        public void Save(FileInfo file)
        {
            Stream stream = file.OpenWrite();
            this.Save(stream);
            stream.Flush();
            stream.Close();
            stream.Dispose();
        }

        /// <summary>
        /// Saves a serialized version to a stream.
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Route));
            ser.Serialize(stream, this);
            ser = null;
        }

        /// <summary>
        /// Saves the route as a byte stream.
        /// </summary>
        /// <returns></returns>
        public byte[] SaveToByteArray()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Route));
            MemoryStream mem_stream = new MemoryStream();
            //GZipStream stream = new GZipStream(mem_stream, CompressionMode.Compress);
            Stream stream = mem_stream;
            serializer.Serialize(stream, this);
            stream.Flush();
            mem_stream.Flush();
            return mem_stream.ToArray();
        }

        /// <summary>
        /// Loads a route from file.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static Route Load(FileInfo info)
        {
            return Route.Load(info.OpenRead());
        }

        /// <summary>
        /// Parses a route from a data stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Route Load(Stream stream)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Route));
            Route route = ser.Deserialize(stream) as Route;
            ser = null;
            return route;
        }

        /// <summary>
        /// Parses a route from a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Route Load(byte[] bytes)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Route));
            MemoryStream mem_stream = new MemoryStream(bytes);
            //GZipStream stream = new GZipStream(mem_stream, CompressionMode.Decompress);
            Stream stream = mem_stream;
            Route route = (serializer.Deserialize(stream) as Route);

            return route;
        }

        #endregion

#if !IOS
        #region Gpx

        /// <summary>
        /// Save the route as GPX.
        /// </summary>
        /// <param name="file"></param>
        public void SaveAsGpx(FileInfo file)
        {
            OsmSharp.Routing.Gpx.RouteGpx.Save(file, this);
        }
		#endregion
#endif

        #region Kml

        /// <summary>
        /// Saves the route as KML.
        /// </summary>
        /// <param name="file"></param>
        public void SaveAsKml(FileInfo file)
        {
            OsmSharp.Routing.Kml.OsmSharpRouteKml.Save(file, this);
        }

        #endregion
#endif
        #endregion

        #region Create Routes

        /// <summary>
        /// Concatenates two routes.
        /// </summary>
        /// <param name="route1"></param>
        /// <param name="route2"></param>
        /// <returns></returns>
        public static Route Concatenate(Route route1, Route route2)
        {
            return Route.Concatenate(route1, route2, true);
        }

        /// <summary>
        /// Concatenates two routes.
        /// </summary>
        /// <param name="route1"></param>
        /// <param name="route2"></param>
        /// <param name="clone"></param>
        /// <returns></returns>
        public static Route Concatenate(Route route1, Route route2, bool clone)
        {
            if (route1 == null) return route2;
            if (route2 == null) return route1;
            if (route1.Entries.Length == 0) return route2;
            if (route2.Entries.Length == 0) return route1;
            if (route1.Vehicle != route2.Vehicle) { throw new ArgumentException("Route vechicles do not match!"); }

            // get the end/start point.
            RoutePointEntry end = route1.Entries[route1.Entries.Length - 1];
            RoutePointEntry start = route2.Entries[0];

            // only do all this if the routes are 'concatenable'.
            if (end.Latitude == start.Latitude &&
                end.Longitude == start.Longitude)
            {
                // construct the new route.
                Route route = new Route();

                // concatenate points.
                List<RoutePointEntry> entries = new List<RoutePointEntry>();
                // add points for the first route except the last point.
                for (int idx = 0; idx < route1.Entries.Length - 1; idx++)
                {
                    if (clone)
                    {
                        entries.Add(route1.Entries[idx].Clone() as RoutePointEntry);
                    }
                    else
                    {
                        entries.Add(route1.Entries[idx]);
                    }
                }

                // merge last and first entry.
                RoutePointEntry mergedEntry =
                    route1.Entries[route1.Entries.Length - 1].Clone() as RoutePointEntry;
                mergedEntry.Type = RoutePointEntryType.Along;
                if (route2.Entries[0].Points != null && route2.Entries[0].Points.Length > 0)
                { // merge in important points from the second route too but do not keep duplicates.
                    List<RoutePoint> points = new List<RoutePoint>(mergedEntry.Points);
                    for (int otherIdx = 0; otherIdx < route2.Entries[0].Points.Length; otherIdx++)
                    {
                        bool found = false;
                        for (int idx = 0; idx < points.Count; idx++)
                        {
                            if (points[idx].RepresentsSame(
                                route2.Entries[0].Points[otherIdx]))
                            { // the points represent the same info!
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        { // the point was not in there yet!
                            points.Add(route2.Entries[0].Points[otherIdx]);
                        }
                    }
                    mergedEntry.Points = points.ToArray();
                }
                entries.Add(mergedEntry);

                // add points of the next route.
                for (int idx = 1; idx < route2.Entries.Length; idx++)
                {
                    if (clone)
                    {
                        entries.Add(route2.Entries[idx].Clone() as RoutePointEntry);
                    }
                    else
                    {
                        entries.Add(route2.Entries[idx]);
                    }
                }
                route.Entries = entries.ToArray();

                // concatenate tags.
                List<RouteTags> tags = new List<RouteTags>();
                if (route1.Tags != null) { tags.AddRange(route1.Tags); }
                if (route2.Tags != null) { tags.AddRange(route2.Tags); }
                route.Tags = tags.ToArray();

                //// calculate metrics.
                //Routing.Core.Metrics.Time.TimeCalculator calculator = new OsmSharp.Routing.Metrics.Time.TimeCalculator();
                //Dictionary<string, double> metrics = calculator.Calculate(route);
                //route.TotalDistance = metrics[Routing.Core.Metrics.Time.TimeCalculator.DISTANCE_KEY];
                //route.TotalTime = metrics[Routing.Core.Metrics.Time.TimeCalculator.TIME_KEY];

                // set the vehicle.
                route.Vehicle = route1.Vehicle;
                return route;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Contatenation routes can only be done when the end point of the first route equals the start of the second.");
            }
        }

        #endregion

        #region Metrics and Calculations

        #region Total Distance

        /// <summary>
        /// The distance in meter.
        /// </summary>
        public double TotalDistance { get; set; }

        /// <summary>
        /// The time in seconds.
        /// </summary>
        public double TotalTime { get; set; }

        #endregion

        #region Bounding Box

        /// <summary>
        /// Returns the bounding box around this route.
        /// </summary>
        /// <returns></returns>
        public GeoCoordinateBox GetBox()
        {
            return new GeoCoordinateBox(this.GetPoints().ToArray());
        }

        #endregion

        #region Points List

        /// <summary>
        /// Returns the points along the route for the entire route in the correct order.
        /// </summary>
        /// <returns></returns>
        public List<GeoCoordinate> GetPoints()
        {
            List<GeoCoordinate> coordinates = new List<GeoCoordinate>();
            for (int p = 0; p < this.Entries.Length; p++)
            {
                coordinates.Add(new GeoCoordinate(this.Entries[p].Latitude, this.Entries[p].Longitude));
            }
            return coordinates;
        }

        #endregion

        #endregion

        /// <summary>
        /// Calculates the position on the route after the given distance from the starting point.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public GeoCoordinate PositionAfter(Meter m)
        {
            double distanceMeter = 0;
            List<GeoCoordinate> points = this.GetPoints();
            for (int idx = 0; idx < points.Count - 1; idx++)
            {
                double currentDistance = points[idx].DistanceReal(points[idx + 1]).Value;
                if (distanceMeter + currentDistance >= m.Value)
                { // the current distance should be in this segment.
                    double segmentDistance = m.Value - distanceMeter;
                    VectorF2D direction = points[idx + 1] - points[idx];
                    direction = direction * (segmentDistance / currentDistance);
                    PointF2D position = points[idx] + direction;
                    return new GeoCoordinate(position[1], position[0]);
                }
				distanceMeter += currentDistance;
            }
            return null;
        }

        /// <summary>
        /// Calculates the closest point on the route.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public GeoCoordinate ProjectOn(GeoCoordinate coordinates)
        {
            double distance = double.MaxValue;
            GeoCoordinate closests = null;
            List<GeoCoordinate> points = this.GetPoints();
            for (int idx = 0; idx < points.Count - 1; idx++)
            {
                GeoCoordinateLine line = new GeoCoordinateLine(points[idx], points[idx + 1]);
                PointF2D projectedPoint = line.ProjectOn(coordinates);
                GeoCoordinate projected = new GeoCoordinate(projectedPoint[1], projectedPoint[0]);
                double currentDistance = coordinates.Distance(projected);
                if (currentDistance < distance)
                {
                    closests = projected;
                    distance = currentDistance;
                }
            }
            return closests;
        }

        /// <summary>
        /// Returns an enumerable of route positions with the given interval between them.
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public IEnumerable<GeoCoordinate> GetRouteEnumerable(Meter interval)
        {
            return new RouteEnumerable(this, interval);
        }
    }

    /// <summary>
    /// Structure representing one point in a route that has been routed to.
    /// </summary>
    public class RoutePoint : ICloneable
    {
        /// <summary>
        /// The name of the point.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The latitude of the entry.
        /// </summary>
        public float Latitude { get; set; }

        /// <summary>
        /// The longitude of the entry.
        /// </summary>
        public float Longitude { get; set; }
        
        /// <summary>
        /// Tags for this route point.
        /// </summary>
        public RouteTags[] Tags { get; set; }

        /// <summary>
        /// Route metrics.
        /// </summary>
        public RouteMetric[] Metrics { get; set; }

        /// <summary>
        /// Distance in meter to reach this point.
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Estimated time in seconds to reach this point.
        /// </summary>
        public double Time { get; set; }

        #region ICloneable Members

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RoutePoint clone = new RoutePoint();
            clone.Distance = this.Distance;
            clone.Latitude = this.Latitude;
            clone.Longitude = this.Longitude;
            if (this.Metrics != null)
            {
                clone.Metrics = new RouteMetric[this.Metrics.Length];
                for (int idx = 0; idx < this.Metrics.Length; idx++)
                {
                    clone.Metrics[idx] = this.Metrics[idx].Clone() as RouteMetric;
                }
            }
            clone.Name = this.Name;
            if (this.Tags != null)
            {
                clone.Tags = new RouteTags[this.Tags.Length];
                for (int idx = 0; idx < this.Tags.Length; idx++)
                {
                    clone.Tags[idx] = this.Tags[idx].Clone() as RouteTags;
                }
            }
            clone.Time = this.Time;
            return clone;            
        }

        #endregion

        /// <summary>
        /// Returns true if the given point has the same name tags and positiong.
        /// </summary>
        /// <param name="routePoint"></param>
        /// <returns></returns>
        internal bool RepresentsSame(RoutePoint routePoint)
        {
            if (routePoint == null) return false;

            if (this.Longitude == routePoint.Longitude &&
                this.Latitude == routePoint.Latitude &&
                this.Name == routePoint.Name)
            {
                if (routePoint.Tags != null || routePoint.Tags.Length == 0)
                { // there are tags in the other point.
                    if (this.Tags != null || this.Tags.Length == 0)
                    { // there are also tags in this point.
                        if (this.Tags.Length == routePoint.Tags.Length)
                        { // and they have the same number of tags!
                            for (int idx = 0; idx < this.Tags.Length; idx++)
                            {
                                if (this.Tags[idx].Key != routePoint.Tags[idx].Key ||
                                    this.Tags[idx].Value != routePoint.Tags[idx].Value)
                                { // tags don't equal.
                                    return false;
                                }
                            }
                            return true;
                        }
                        return false;
                    }
                }
                return (this.Tags != null || this.Tags.Length == 0);
            }
            return false;
        }
    }

    /// <summary>
    /// Structure representing one point in a route.
    /// </summary>
    public class RoutePointEntry : ICloneable
    {
        /// <summary>
        /// The type of this entry.
        /// Start: Has no way from, distance from, angle or angles on poi's.
        /// Along: Has all data.
        /// Stop: Has all data but is the end point.
        /// </summary>
        public RoutePointEntryType Type { get; set; }

        /// <summary>
        /// The latitude of the entry.
        /// </summary>
        public float Latitude { get; set; }

        /// <summary>
        /// The longitude of the entry.
        /// </summary>
        public float Longitude { get; set; }

        /// <summary>
        /// Tags of this entry.
        /// </summary>
        public RouteTags[] Tags { get; set; }

        /// <summary>
        /// Route metrics.
        /// </summary>
        public RouteMetric[] Metrics { get; set; }

        /// <summary>
        /// Distance in meter to reach this part of the route.
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Estimated time in seconds to reach this part of the route.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// The points this route travels along.
        /// 
        /// Between each two points there exists a route represented by the entries array.
        /// </summary>
        public RoutePoint[] Points { get; set; }

        #region Ways

        /// <summary>
        /// The name of the way the route comes from.
        /// </summary>
        public string WayFromName { get; set; }

        /// <summary>
        /// All the names of the ways indexed according to the alpha-2 code of ISO 639-1.
        /// </summary>
        public RouteTags[] WayFromNames { get; set; }

        #endregion

        /// <summary>
        /// The side streets entries.
        /// </summary>
        public RoutePointEntrySideStreet[] SideStreets { get; set; }

        #region ICloneable Members

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RoutePointEntry clone = new RoutePointEntry();
            clone.Distance = this.Distance;
            clone.Latitude = this.Latitude;
            clone.Longitude = this.Longitude;
            if (this.Metrics != null)
            {
                clone.Metrics = new RouteMetric[this.Metrics.Length];
                for (int idx = 0; idx < this.Metrics.Length; idx++)
                {
                    clone.Metrics[idx] = this.Metrics[idx].Clone() as RouteMetric;
                }
            }
            if (this.Points != null)
            {
                clone.Points = new RoutePoint[this.Points.Length];
                for (int idx = 0; idx < this.Points.Length; idx++)
                {
                    clone.Points[idx] = this.Points[idx].Clone() as RoutePoint;
                }
            }
            if (this.SideStreets != null)
            {
                clone.SideStreets = new RoutePointEntrySideStreet[this.SideStreets.Length];
                for (int idx = 0; idx < this.SideStreets.Length; idx++)
                {
                    clone.SideStreets[idx] = this.SideStreets[idx].Clone() as RoutePointEntrySideStreet;
                }
            }
            if (this.Tags != null)
            {
                clone.Tags = new RouteTags[this.Tags.Length];
                for (int idx = 0; idx < this.Tags.Length; idx++)
                {
                    clone.Tags[idx] = this.Tags[idx].Clone() as RouteTags;
                }
            }
            clone.Time = this.Time;
            clone.Type = this.Type;
            clone.WayFromName = this.WayFromName;
            if (this.WayFromNames != null)
            {
                clone.WayFromNames = new RouteTags[this.WayFromNames.Length];
                for (int idx = 0; idx < this.WayFromNames.Length; idx++)
                {
                    clone.WayFromNames[idx] = this.WayFromNames[idx].Clone() as RouteTags;
                }
            }
            return clone;
        }

        #endregion
    }

    /// <summary>
    /// Represents a type of point entry.
    /// </summary>
    public enum RoutePointEntryType
    {
        /// <summary>
        /// Start type.
        /// </summary>
        Start,
        /// <summary>
        /// Along type.
        /// </summary>
        Along,
        /// <summary>
        /// Stop type.
        /// </summary>
        Stop
    }

    /// <summary>
    /// Route point entry.
    /// </summary>
    public class RoutePointEntrySideStreet : ICloneable
    {
        /// <summary>
        /// The latitude of the entry.
        /// </summary>
        public float Latitude { get; set; }

        /// <summary>
        /// The longitude of the entry.
        /// </summary>
        public float Longitude { get; set; }

        /// <summary>
        /// Tags of this entry.
        /// </summary>
        public RouteTags[] Tags { get; set; }

        #region Ways

        /// <summary>
        /// The name of the way the route comes from.
        /// </summary>
        public string WayName { get; set; }

        /// <summary>
        /// All the names of the ways indexed according to the alpha-2 code of ISO 639-1.
        /// </summary>
        public RouteTags[] WayNames { get; set; }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of this object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RoutePointEntrySideStreet clone = new RoutePointEntrySideStreet();
            clone.Latitude = this.Latitude;
            clone.Longitude = this.Longitude;
            if (this.Tags != null)
            {
                clone.Tags = new RouteTags[this.Tags.Length];
                for (int idx = 0; idx < this.Tags.Length; idx++)
                {
                    clone.Tags[idx] = this.Tags[idx].Clone() as RouteTags;
                }
            }
            clone.WayName = this.WayName;
            if (this.WayNames != null)
            {
                clone.WayNames = new RouteTags[this.WayNames.Length];
                for (int idx = 0; idx < this.WayNames.Length; idx++)
                {
                    clone.WayNames[idx] = this.WayNames[idx].Clone() as RouteTags;
                }
            }
            return clone;
        }

        #endregion
    }

    /// <summary>
    /// Represents a key value pair.
    /// </summary>
    public class RouteTags : ICloneable
    {
        /// <summary>
        /// The key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The value.
        /// </summary>
        public string Value { get; set; }

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of this object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RouteTags clone = new RouteTags();
            clone.Key = this.Key;
            clone.Value = this.Value;
            return clone;
        }

        #endregion
    }

    /// <summary>
    /// Contains extensions for route tags.
    /// </summary>
    public static class RouteTagsExtensions
    {        
        /// <summary>
        /// Converts a dictionary of tags to a RouteTags array.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static RouteTags[] ConvertFrom(this TagsCollection tags)
        {
            var tagsList = new List<RouteTags>();
            foreach (Tag pair in tags)
            {
                var tag = new RouteTags();
                tag.Key = pair.Key;
                tag.Value = pair.Value;
                tagsList.Add(tag);
            }
            return tagsList.ToArray();
        }

        /// <summary>
        /// Converts a RouteTags array to a list of KeyValuePairs.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static TagsCollection ConvertToTagsCollection(this RouteTags[] tags)
        {
            var tagsList = new SimpleTagsCollection();
            if (tags != null)
            {
                foreach (RouteTags pair in tags)
                {
                    tagsList.Add(new Tag(pair.Key, pair.Value));
                }
            }
            return tagsList;
        }

        /// <summary>
        /// Converts a dictionary of tags to a RouteTags array.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static RouteTags[] ConvertFrom(this IDictionary<string, string> tags)
        {
            List<RouteTags> tags_list = new List<RouteTags>();
            foreach (KeyValuePair<string, string> pair in tags)
            {
                RouteTags tag = new RouteTags();
                tag.Key = pair.Key;
                tag.Value = pair.Value;
                tags_list.Add(tag);
            }
            return tags_list.ToArray();
        }

        /// <summary>
        /// Converts a list of KeyValuePairs to a RouteTags array.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static RouteTags[] ConvertFrom(this List<KeyValuePair<string, string>> tags)
        {
            List<RouteTags> tags_list = new List<RouteTags>();
            if (tags != null)
            {
                foreach (KeyValuePair<string, string> pair in tags)
                {
                    RouteTags tag = new RouteTags();
                    tag.Key = pair.Key;
                    tag.Value = pair.Value;
                    tags_list.Add(tag);
                }
            }
            return tags_list.ToArray();
        }

        /// <summary>
        /// Converts a RouteTags array to a list of KeyValuePairs.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> ConvertTo(this RouteTags[] tags)
        {
            List<KeyValuePair<string, string>> tags_list = new List<KeyValuePair<string, string>>();
            if (tags != null)
            {
                foreach (RouteTags pair in tags)
                {
                    tags_list.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
                }
            }
            return tags_list;
        }

        /// <summary>
        /// Returns the value of the first tag with the key given.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetValueFirst(this RouteTags[] tags, string key)
        {
            string first_value = null;
            if (tags != null)
            {
                foreach (RouteTags tag in tags)
                {
                    if (tag.Key == key)
                    {
                        first_value = tag.Value;
                        break;
                    }
                }
            }
            return first_value;
        }

        /// <summary>
        /// Returns all values for a given key.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static List<string> GetValues(this RouteTags[] tags, string key)
        {
            List<string> values = new List<string>();
            if (tags != null)
            {
                foreach (RouteTags tag in tags)
                {
                    if (tag.Key == key)
                    {
                        values.Add(tag.Value);
                    }
                }
            }
            return values;
        }
    }

    /// <summary>
    /// Represents a key value pair.
    /// </summary>
    public class RouteMetric : ICloneable
    {
        /// <summary>
        /// The key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Convert from a regular tag dictionary.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static RouteMetric[] ConvertFrom(IDictionary<string, double> tags)
        {
            List<RouteMetric> tags_list = new List<RouteMetric>();
            foreach (KeyValuePair<string, double> pair in tags)
            {
                RouteMetric tag = new RouteMetric();
                tag.Key = pair.Key;
                tag.Value = pair.Value;
                tags_list.Add(tag);
            }
            return tags_list.ToArray();
        }

        /// <summary>
        /// Converts to regular tags list.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, double>> ConvertTo(RouteMetric[] tags)
        {
            List<KeyValuePair<string, double>> tags_list = new List<KeyValuePair<string, double>>();
            if (tags != null)
            {
                foreach (RouteMetric pair in tags)
                {
                    tags_list.Add(new KeyValuePair<string, double>(pair.Key, pair.Value));
                }
            }
            return tags_list;
        }

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of this object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RouteMetric clone = new RouteMetric();
            clone.Key = this.Key;
            clone.Value = this.Value;
            return clone;
        }

        #endregion
    }
}
