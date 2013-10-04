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
using OsmSharp.Collections;
using OsmSharp.Collections.Tags;
using OsmSharp.Osm;
using OsmSharp.Osm.Cache;
using OsmSharp.Osm.Data;
using GeoAPI.Geometries;

namespace OsmSharp.Osm
{
    /// <summary>
    /// Relation class.
    /// </summary>
    public class CompleteRelation : CompleteOsmGeo
    {
        /// <summary>
        /// Holds the members of this relation.
        /// </summary>
        private readonly IList<CompleteRelationMember> _members;

        /// <summary>
        /// Creates a new relation.
        /// </summary>
        /// <param name="id"></param>
        internal protected CompleteRelation(long id)
            : base(id)
        {
            _members = new List<CompleteRelationMember>();
        }

        /// <summary>
        /// Creates a new relation using a string table.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="stringTable"></param>
        internal protected CompleteRelation(ObjectTable<string> stringTable, long id)
            : base(stringTable, id)
        {
            _members = new List<CompleteRelationMember>();
        }

        /// <summary>
        /// Returns the relation type.
        /// </summary>
        public override CompleteOsmType Type
        {
            get { return CompleteOsmType.Relation; }
        }

        /// <summary>
        /// Gets the relation members.
        /// </summary>
        public IList<CompleteRelationMember> Members
        {
            get
            {
                return _members;
            }
        }

        /// <summary>
        /// Find a member in this relation with the given role.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public CompleteOsmBase FindMember(string role)
        {
            if (this.Members != null)
            {
                foreach (CompleteRelationMember member in this.Members)
                {
                    if (member.Role == role)
                    {
                        return member.Member;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Converts this relation into it's simple counterpart.
        /// </summary>
        /// <returns></returns>
        public override OsmGeo ToSimple()
        {
            var relation = new Relation();
            relation.Id = this.Id;
            relation.ChangeSetId = this.ChangeSetId;
            relation.Tags = this.Tags;
            relation.TimeStamp = this.TimeStamp;
            relation.UserId = this.UserId;
            relation.UserName = this.User;
            relation.Version = (ulong?)this.Version;
            relation.Visible = this.Visible;

            relation.Members = new List<RelationMember>();
            foreach (CompleteRelationMember member in this.Members)
            {
                var simple_member = new RelationMember();
                simple_member.MemberId = member.Member.Id;
                simple_member.MemberRole = member.Role;
                switch (member.Member.Type)
                {
                    case CompleteOsmType.Node:
                        simple_member.MemberType = OsmGeoType.Node;
                        break;
                    case CompleteOsmType.Relation:
                        simple_member.MemberType = OsmGeoType.Relation;
                        break;
                    case CompleteOsmType.Way:
                        simple_member.MemberType = OsmGeoType.Way;
                        break;
                }
                relation.Members.Add(simple_member);
            }
            return relation;
        }

        /// <summary>
        /// Returns all the coordinates in this way in the same order as the nodes.
        /// </summary>
        /// <returns></returns>
        public IList<Coordinate> GetCoordinates()
        {
            var coordinates = new List<Coordinate>();

            for (int idx = 0; idx < this.Members.Count; idx++)
            {
                if (this.Members[idx].Member is CompleteNode)
                {
                    var node = this.Members[idx].Member as CompleteNode;
                    coordinates.Add(node.Coordinate);
                }
                else if (this.Members[idx].Member is CompleteWay)
                {
                    var way = this.Members[idx].Member as CompleteWay;
                    coordinates.AddRange(way.GetCoordinates());
                }
                else if (this.Members[idx].Member is CompleteRelation)
                {
                    var relation = this.Members[idx].Member as CompleteRelation;
                    coordinates.AddRange(relation.GetCoordinates());
                }
            }

            return coordinates;
        }

        #region Relation factory functions

        /// <summary>
        /// Creates a relation with a given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static CompleteRelation Create(long id)
        {
            return new CompleteRelation(id);
        }

        /// <summary>
        /// Creates a relation from a SimpleRelation.
        /// </summary>
        /// <param name="simpleRelation"></param>
        /// <param name="nodes"></param>
        /// <param name="ways"></param>
        /// <param name="relations"></param>
        /// <returns></returns>
        public static CompleteRelation CreateFrom(Relation simpleRelation,
            IDictionary<long, CompleteNode> nodes,
            IDictionary<long, CompleteWay> ways,
            IDictionary<long, CompleteRelation> relations)
        {
            if (simpleRelation == null) throw new ArgumentNullException("simpleRelation");
            if (nodes == null) throw new ArgumentNullException("nodes");
            if (ways == null) throw new ArgumentNullException("ways");
            if (relations == null) throw new ArgumentNullException("relations");
            if (simpleRelation.Id == null) throw new Exception("simpleRelation.Id is null");

            CompleteRelation relation = Create(simpleRelation.Id.Value);

            relation.ChangeSetId = simpleRelation.ChangeSetId;
            foreach (Tag pair in simpleRelation.Tags)
            {
                relation.Tags.Add(pair);
            }
            for (int idx = 0; idx < simpleRelation.Members.Count; idx++)
            {
                long memberId = simpleRelation.Members[idx].MemberId.Value;
                string role = simpleRelation.Members[idx].MemberRole;

                var member = new CompleteRelationMember();
                member.Role = role;
                switch (simpleRelation.Members[idx].MemberType.Value)
                {
                    case OsmGeoType.Node:
                        CompleteNode node = null;
                        if (nodes.TryGetValue(memberId, out node))
                        {
                            member.Member = node;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case OsmGeoType.Way:
                        CompleteWay way = null;
                        if (ways.TryGetValue(memberId, out way))
                        {
                            member.Member = way;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case OsmGeoType.Relation:
                        CompleteRelation relationMember = null;
                        if (relations.TryGetValue(memberId, out relationMember))
                        {
                            member.Member = relationMember;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                }
                relation.Members.Add(member);
            }
            relation.TimeStamp = simpleRelation.TimeStamp;
            relation.User = simpleRelation.UserName;
            relation.UserId = simpleRelation.UserId;
            relation.Version = simpleRelation.Version.HasValue ? (long)simpleRelation.Version.Value : (long?)null;
            relation.Visible = simpleRelation.Visible.HasValue && simpleRelation.Visible.Value;

            return relation;
        }

        /// <summary>
        /// Creates a relation from a SimpleRelation.
        /// </summary>
        /// <param name="simpleRelation"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static CompleteRelation CreateFrom(Relation simpleRelation, IDataSourceReadOnly cache)
        {
            if (simpleRelation == null) throw new ArgumentNullException("simpleRelation");
            if (cache == null) throw new ArgumentNullException("cache");
            if (simpleRelation.Id == null) throw new Exception("simpleRelation.Id is null");

            CompleteRelation relation = Create(simpleRelation.Id.Value);

            relation.ChangeSetId = simpleRelation.ChangeSetId;
            foreach (Tag pair in simpleRelation.Tags)
            {
                relation.Tags.Add(pair);
            }
            for (int idx = 0; idx < simpleRelation.Members.Count; idx++)
            {
                long memberId = simpleRelation.Members[idx].MemberId.Value;
                string role = simpleRelation.Members[idx].MemberRole;

                var member = new CompleteRelationMember();
                member.Role = role;
                switch (simpleRelation.Members[idx].MemberType.Value)
                {
                    case OsmGeoType.Node:
                        Node simpleNode = cache.GetNode(memberId);
                        if (simpleNode != null)
                        {
                            member.Member = CompleteNode.CreateFrom(simpleNode);
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case OsmGeoType.Way:
                        Way simpleWay = cache.GetWay(memberId);
                        if (simpleWay != null)
                        {
                            member.Member = CompleteWay.CreateFrom(simpleWay, cache);
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case OsmGeoType.Relation:
                        Relation simpleRelationMember = cache.GetRelation(memberId);
                        if (simpleRelationMember != null)
                        {
                            member.Member = CompleteRelation.CreateFrom(simpleRelationMember, cache);
                        }
                        else
                        {
                            return null;
                        }
                        break;
                }
                relation.Members.Add(member);
            }
            relation.TimeStamp = simpleRelation.TimeStamp;
            relation.User = simpleRelation.UserName;
            relation.UserId = simpleRelation.UserId;
            relation.Version = simpleRelation.Version.HasValue ? (long)simpleRelation.Version.Value : (long?)null;
            relation.Visible = simpleRelation.Visible.HasValue && simpleRelation.Visible.Value;

            return relation;
        }

        /// <summary>
        /// Creates a new relation.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static CompleteRelation Create(ObjectTable<string> table, long id)
        {
            return new CompleteRelation(table, id);
        }

        /// <summary>
        /// Creates a new relation from a SimpleRelation.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="simpleRelation"></param>
        /// <param name="nodes"></param>
        /// <param name="ways"></param>
        /// <param name="relations"></param>
        /// <returns></returns>
        public static CompleteRelation CreateFrom(ObjectTable<string> table, Relation simpleRelation,
            IDictionary<long, CompleteNode> nodes,
            IDictionary<long, CompleteWay> ways,
            IDictionary<long, CompleteRelation> relations)
        {
            if (simpleRelation == null) throw new ArgumentNullException("simpleRelation");
            if (nodes == null) throw new ArgumentNullException("nodes");
            if (ways == null) throw new ArgumentNullException("ways");
            if (relations == null) throw new ArgumentNullException("relations");
            if (simpleRelation.Id == null) throw new Exception("simpleRelation.Id is null");

            CompleteRelation relation = Create(table, simpleRelation.Id.Value);

            relation.ChangeSetId = simpleRelation.ChangeSetId;
            foreach (Tag pair in simpleRelation.Tags)
            {
                relation.Tags.Add(pair);
            }
            for (int idx = 0; idx < simpleRelation.Members.Count; idx++)
            {
                long memberId = simpleRelation.Members[idx].MemberId.Value;
                string role = simpleRelation.Members[idx].MemberRole;

                var member = new CompleteRelationMember();
                member.Role = role;
                switch (simpleRelation.Members[idx].MemberType.Value)
                {
                    case OsmGeoType.Node:
                        CompleteNode node = null;
                        if (nodes.TryGetValue(memberId, out node))
                        {
                            member.Member = node;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case OsmGeoType.Way:
                        CompleteWay way = null;
                        if (ways.TryGetValue(memberId, out way))
                        {
                            member.Member = way;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case OsmGeoType.Relation:
                        CompleteRelation relationMember = null;
                        if (relations.TryGetValue(memberId, out relationMember))
                        {
                            member.Member = relationMember;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                }
                relation.Members.Add(member);
            }
            relation.TimeStamp = simpleRelation.TimeStamp;
            relation.User = simpleRelation.UserName;
            relation.UserId = simpleRelation.UserId;
            relation.Version = simpleRelation.Version.HasValue ? (long)simpleRelation.Version.Value : (long?)null;
            relation.Visible = simpleRelation.Visible.HasValue && simpleRelation.Visible.Value;

            return relation;
        }

        /// <summary>
        /// Creates a new changeset.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static CompleteChangeSet CreateChangeSet(long id)
        {
            return new CompleteChangeSet(id);
        }

        #endregion
    }
}
