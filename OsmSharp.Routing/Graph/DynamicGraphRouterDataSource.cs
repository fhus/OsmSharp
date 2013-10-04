﻿using System;
using System.Collections.Generic;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Collections;
using OsmSharp.Collections.Tags;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Structures;
using OsmSharp.Math;
using OsmSharp.Math.Structures.QTree;
using GeoAPI.Geometries;

namespace OsmSharp.Routing.Graph
{
    /// <summary>
    /// A router data source that uses a IDynamicGraph as it's main datasource.
    /// </summary>
    /// <typeparam name="TEdgeData"></typeparam>
    public class DynamicGraphRouterDataSource<TEdgeData> : IDynamicGraphRouterDataSource<TEdgeData>
        where TEdgeData : IDynamicGraphEdgeData
    {
        /// <summary>
        /// Holds the basic graph.
        /// </summary>
        private readonly IDynamicGraph<TEdgeData> _graph;

        /// <summary>
        /// Holds the index of vertices per bounding box.
        /// </summary>
        private readonly ILocatedObjectIndex<Coordinate, uint> _vertexIndex;

        /// <summary>
        /// Holds the tags index.
        /// </summary>
        private readonly ITagsIndex _tagsIndex;

        /// <summary>
        /// Holds the supported vehicle profiles.
        /// </summary>
        private readonly HashSet<Vehicle> _supportedVehicles; 

        /// <summary>
        /// Creates a new osm memory router data source.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public DynamicGraphRouterDataSource(ITagsIndex tagsIndex)
        {
            if (tagsIndex == null) throw new ArgumentNullException("tagsIndex");

            _graph = new MemoryDynamicGraph<TEdgeData>();
            _vertexIndex = new QuadTree<GeoCoordinate, uint>();
            _tagsIndex = tagsIndex;

            _supportedVehicles = new HashSet<Vehicle>();
        }

        /// <summary>
        /// Creates a new osm memory router data source.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="tagsIndex"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DynamicGraphRouterDataSource(IDynamicGraph<TEdgeData> graph, ITagsIndex tagsIndex)
        {
            if (graph == null) throw new ArgumentNullException("graph");
            if (tagsIndex == null) throw new ArgumentNullException("tagsIndex");

            _graph = graph;
            _vertexIndex = new QuadTree<GeoCoordinate, uint>();
            _tagsIndex = tagsIndex;

            _supportedVehicles = new HashSet<Vehicle>();

            // add the current graph's vertices to the vertex index.
            for (uint newVertexId = 1; newVertexId < graph.VertexCount + 1; newVertexId++)
            {
                // add to the CHRegions.
                float latitude, longitude;
                graph.GetVertex(newVertexId, out latitude, out longitude);
                _vertexIndex.Add(new GeoCoordinate(latitude, longitude), newVertexId);
            }
        }

        /// <summary>
        /// Returns true if the given vehicle profile is supported.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public bool SupportsProfile(Vehicle vehicle)
        {
            return _supportedVehicles.Contains(vehicle); // for backwards compatibility.
        }

        /// <summary>
        /// Adds one more supported profile.
        /// </summary>
        /// <param name="vehicle"></param>
        public void AddSupportedProfile(Vehicle vehicle)
        {
            _supportedVehicles.Add(vehicle);
        }

        /// <summary>
        /// Returns all arcs inside the given bounding box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public KeyValuePair<uint, KeyValuePair<uint, TEdgeData>>[] GetArcs(
            Envelope box)
        {
            // get all the vertices in the given box.
            IEnumerable<uint> vertices = _vertexIndex.GetInside(
                box);

            // loop over all vertices and get the arcs.
            var arcs = new List<KeyValuePair<uint, KeyValuePair<uint, TEdgeData>>>();
            foreach (uint vertex in vertices)
            {
                KeyValuePair<uint, TEdgeData>[] localArcs = this.GetArcs(vertex);
                foreach (KeyValuePair<uint, TEdgeData> localArc in localArcs)
                {
                    arcs.Add(new KeyValuePair<uint, KeyValuePair<uint, TEdgeData>>(
                        vertex, localArc));
                }
            }
            return arcs.ToArray();
        }

        /// <summary>
        /// Returns true if a given vertex is in the graph.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public bool GetVertex(uint id, out float latitude, out float longitude)
        {
            return _graph.GetVertex(id, out latitude, out longitude);
        }

        /// <summary>
        /// Returns all arcs starting at a given vertex.
        /// </summary>
        /// <param name="vertexId"></param>
        /// <returns></returns>
        public KeyValuePair<uint, TEdgeData>[] GetArcs(uint vertexId)
        {
            return _graph.GetArcs(vertexId);
        }

        /// <summary>
        /// Returns true if the given vertex has neighbour as a neighbour.
        /// </summary>
        /// <param name="vertexId"></param>
        /// <param name="neighbour"></param>
        /// <returns></returns>
        public bool HasNeighbour(uint vertexId, uint neighbour)
        {
            return _graph.HasNeighbour(vertexId, neighbour);
        }

        /// <summary>
        /// Adds a new vertex to this graph.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="neighboursEstimate"></param>
        /// <returns></returns>
        public uint AddVertex(float latitude, float longitude, byte neighboursEstimate)
        {
            uint vertex = _graph.AddVertex(latitude, longitude, neighboursEstimate);
            _vertexIndex.Add(new GeoCoordinate(latitude, longitude),
                vertex);
            return vertex;
        }

        /// <summary>
        /// Adds a new vertex.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public uint AddVertex(float latitude, float longitude)
        {
            uint vertex = _graph.AddVertex(latitude, longitude);
            _vertexIndex.Add(new GeoCoordinate(latitude, longitude),
                vertex);
            return vertex;
        }

        /// <summary>
        /// Adds a new arc.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="data"></param>
        /// <param name="comparer"></param>
        public void AddArc(uint from, uint to, TEdgeData data, IDynamicGraphEdgeComparer<TEdgeData> comparer)
        {
            _graph.AddArc(from, to, data, comparer);
        }

        /// <summary>
        /// Deletes an arc.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void DeleteArc(uint from, uint to)
        {
            _graph.DeleteArc(from, to);
        }

        /// <summary>
        /// Returns the tags index.
        /// </summary>
        public ITagsIndex TagsIndex
        {
            get
            {
                return _tagsIndex;
            }
        }

        /// <summary>
        /// Returns the number of vertices in this graph.
        /// </summary>
        public uint VertexCount
        {
            get { return _graph.VertexCount; }
        }
    }
}
