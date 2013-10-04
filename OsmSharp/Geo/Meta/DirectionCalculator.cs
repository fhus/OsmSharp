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
using System.Linq;
using System.Text;
using OsmSharp.Units.Angle;
using GeoAPI.Geometries;

namespace OsmSharp.Math.Geo.Meta
{
    /// <summary>
    /// Direction calculator.
    /// </summary>
    public static class DirectionCalculator
    {
        /// <summary>
        /// Calculates the direction.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static DirectionEnum Calculate(Coordinate from, Coordinate to)
        {
            double offset = 0.01;

            // calculate the angle with the horizontal and vertical axes.
            Coordinate from_vertical = new Coordinate(from.X, from.Y + offset);

            // calculate angle.
            Degree vertical_angle = (Radian)NetTopologySuite.Algorithm.AngleUtility.AngleBetweenOriented(from_vertical, from, to);
            if (vertical_angle.Value < 0)
            {
                vertical_angle = 360 - vertical_angle.Value;
            }

            if (vertical_angle < new Degree(22.5)
                || vertical_angle >= new Degree(360- 22.5))
            { // north
                return DirectionEnum.North;
            }
            else if (vertical_angle >= new Degree(22.5)
                 && vertical_angle < new Degree(90 - 22.5))
            { // north-east.
                return DirectionEnum.NorthEast;
            }
            else if (vertical_angle >= new Degree(90 - 22.5)
                 && vertical_angle < new Degree(90 + 22.5))
            { // east.
                return DirectionEnum.East;
            }
            else if (vertical_angle >= new Degree(90 + 22.5)
                 && vertical_angle < new Degree(180 - 22.5))
            { // south-east.
                return DirectionEnum.SouthEast;
            }
            else if (vertical_angle >= new Degree(180 - 22.5)
                 && vertical_angle < new Degree(180 + 22.5))
            { // south
                return DirectionEnum.South;
            }
            else if (vertical_angle >= new Degree(180 + 22.5)
                 && vertical_angle < new Degree(270 - 22.5))
            { // south-west.
                return DirectionEnum.SouthWest;
            }
            else if (vertical_angle >= new Degree(270 - 22.5)
                 && vertical_angle < new Degree(270 + 22.5))
            { // south-west.
                return DirectionEnum.West;
            }
            else if (vertical_angle >= new Degree(270 + 22.5)
                 && vertical_angle < new Degree(360-22.5))
            { // south-west.
                return DirectionEnum.NorhtWest;
            }
            throw new ArgumentOutOfRangeException();
        }
    }
}
