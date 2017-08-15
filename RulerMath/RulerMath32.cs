/*
    RulerMath32
    ————————————————————————————————————————————————————————————————————

    32-bit, coordinate based, grid hashing and hash introspection
    library.

    '3D Tick Mark Hash' and 'Tick Mark Hash' uniquely identify discrete
    regions, or "cells", of a grid.


    Author: Akilram Krishnan
    Version: 1.0.0


    LICENSE (MIT)
    ====================================================================

    Copyright 2017 Akilram Krishnan

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without
    limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software,
    and to permit persons to whom the Software is furnished to do so,
    subject to the following conditions:

    The above copyright notice and this permission notice shall be included
    in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
    EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
    DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
    OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
    USE OR OTHER DEALINGS IN THE SOFTWARE.

 */


using System;
using UnityEngine;


namespace GridMath
{
    public static class RulerMath32
    {
        const int MAX_GRID_SIZE = 512; // 2^(10 - 1)
        const float LIMIT_EPSILON = 0.9999f;


        // Public API

        /// <summary>
        /// Samples 'point' in a grid and returns the hash id of the cell
        /// associated with the point; where tick marks on grid axis' are set
        /// 'tickSpacing' units apart.
        /// </summary>
        public static int Create3DTickMarkHash(Vector3 point, int tickSpacing = 1)
        {
            GuardTickSpacingParam(tickSpacing);
            var xPosition = point.x; GuardPositionParam(xPosition, tickSpacing);
            var yPosition = point.y; GuardPositionParam(yPosition, tickSpacing);
            var zPosition = point.z; GuardPositionParam(zPosition, tickSpacing);

            return _Create3DTickMarkHash(point, tickSpacing);
        }

        /// <summary>
        /// Gets the position of the center of the cell identified by the
        /// 'n3DTickMarkHash' 3D tick mark hash and other paramater(s) defining
        /// an associated grid.
        /// </summary>
        public static Vector3 GetCellCenter(int n3DTickMarkHash, int tickSpacing = 1)
        {
            GuardTickSpacingParam(tickSpacing);

            return _GetCellCenter(n3DTickMarkHash, tickSpacing);
        }

        /// <summary>
        /// Introspects a 3D tick mark hash and return decode results.
        /// </summary>
        public static void Decode3DTickMarkHash(
            int n3DTickMarkHash,
            out int xTickMark,
            out int yTickMark,
            out int zTickMark)
        {
            _Decode3DTickMarkHash(
                n3DTickMarkHash,
                out xTickMark, out yTickMark, out zTickMark
            );
        }

        /// <summary>
        /// Samples 'position' in an arbitrary grid axis and returns the
        /// the hash id of the associated tick mark; where tick marks on the
        /// axis are set 'tickSpacing' units apart.
        /// </summary>
        public static int CreateTickMarkHash(float position, int tickSpacing = 1)
        {
            GuardTickSpacingParam(tickSpacing);
            GuardPositionParam(position, tickSpacing);

            return _CreateTickMarkHash(position, tickSpacing);
        }

        /// <summary>
        /// Introspects a tick mark hash and return the tick mark identified by
        /// the hash.
        /// </summary>
        public static int DecodeTickMarkHash(int tickMarkHash)
        {
            return _DecodeTickMarkHash(tickMarkHash);
        }

        /// <summary>
        /// Samples 'position' on an axis and return the associated tick mark.
        /// </summary>
        public static int GetTickMark(float position, int tickSpacing = 1)
        {
            GuardTickSpacingParam(tickSpacing);
            GuardPositionParam(position, tickSpacing);

            return _GetTickMark(position, tickSpacing);
        }

        /// <summary>
        /// Gets the lowest value allowed for a position for a grid defined by
        /// the parameter(s).
        /// </summary>
        public static float GetLowerLimit(int tickSpacing = 1)
        {
            GuardTickSpacingParam(tickSpacing);

            return _GetLowerLimit(tickSpacing);
        }

        /// <summary>
        /// Gets the highest value allowed for a position for a grid defined by
        /// the parameter(s).
        /// </summary>
        public static float GetUpperLimit(int tickSpacing = 1)
        {
            GuardTickSpacingParam(tickSpacing);

            return _GetUpperLimit(tickSpacing);
        }


        // Public API Implementations

        private static int _Create3DTickMarkHash(Vector3 point, int tickSpacing = 1)
        {
            var xTickMarkHash = _CreateTickMarkHash(point.x, tickSpacing);
            var yTickMarkHash = _CreateTickMarkHash(point.y, tickSpacing);
            var zTickMarkHash = _CreateTickMarkHash(point.z, tickSpacing);

            return xTickMarkHash << 20 | yTickMarkHash << 10 | zTickMarkHash;
        }

        private static Vector3 _GetCellCenter(int n3DTickMarkHash, int tickSpacing = 1)
        {
            int xAxisTickMark, yAxisTickMark, zAxisTickMark;
            _Decode3DTickMarkHash(
                n3DTickMarkHash,
                out xAxisTickMark, out yAxisTickMark, out zAxisTickMark
            );

            return new Vector3(
                tickSpacing * (xAxisTickMark >= 0 ? xAxisTickMark - 0.5f : xAxisTickMark + 0.5f),
                tickSpacing * (yAxisTickMark >= 0 ? yAxisTickMark - 0.5f : yAxisTickMark + 0.5f),
                tickSpacing * (zAxisTickMark >= 0 ? zAxisTickMark - 0.5f : zAxisTickMark + 0.5f)
            );
        }

        private static void _Decode3DTickMarkHash(
            int n3DTickMarkHash,
            out int xTickMark,
            out int yTickMark,
            out int zTickMark)
        {
            // Get binary aligned right (keep sign)
            int xTickMark_buffer = (n3DTickMarkHash << 2) >> 22;
            int yTickMark_buffer = (n3DTickMarkHash << 12) >> 22;
            int zTickMark_buffer = (n3DTickMarkHash << 22) >> 22;

            // Handle Zeroth index in positive range
            if (xTickMark_buffer >= 0) xTickMark_buffer++;
            if (yTickMark_buffer >= 0) yTickMark_buffer++;
            if (zTickMark_buffer >= 0) zTickMark_buffer++;

            xTickMark = xTickMark_buffer;
            yTickMark = yTickMark_buffer;
            zTickMark = zTickMark_buffer;
        }

        private static int _CreateTickMarkHash(float position, int tickSpacing = 1)
        {
            // tick mark, with 22 leading bits set to 0
            return _GetTickMark(position, tickSpacing) & 0x3FF;
        }

        private static int _DecodeTickMarkHash(int tickMarkHash)
        {
            // Get binary aligned right (keep sign)
            int tickMarkBuffer = (tickMarkHash << 22) >> 22;

            // Handle Zeroth index in positive range
            if (tickMarkBuffer >= 0)
                tickMarkBuffer++;

            return tickMarkBuffer;
        }

        private static int _GetTickMark(float position, int tickSpacing = 1)
        {
            return
                (position >= 0 ?
                    Mathf.FloorToInt(position) :
                    Mathf.CeilToInt(position) - tickSpacing)
                / tickSpacing;
        }

        private static float _GetLowerLimit(int tickSpacing = 1)
        {
            return -(((MAX_GRID_SIZE * tickSpacing) - 1) + LIMIT_EPSILON);
        }

        private static float _GetUpperLimit(int tickSpacing = 1)
        {
            return ((MAX_GRID_SIZE * tickSpacing) - 1) + LIMIT_EPSILON;
        }


        // Guards

        private static void GuardPositionParam(float position, int tickSpacing)
        {
            var scaledlowerLimit = _GetLowerLimit(tickSpacing);
            var scaledUpperLimit = _GetUpperLimit(tickSpacing);
            if (position < scaledlowerLimit || position > scaledUpperLimit)
                throw new System.ArgumentOutOfRangeException(
                    paramName: "position",
                    message: string.Format(
                        "Value is not between {1} and {2} (inclusive).",
                        position, scaledlowerLimit, scaledUpperLimit
                    )
                );
        }

        private static void GuardTickSpacingParam(int tickSpacing)
        {
            // TODO: Guard upper limit of tick spacing
            //       (tick spacing must not cause tick overflows)
            if (tickSpacing <= 0)
                throw new System.ArgumentOutOfRangeException(
                    paramName: "tickSpacing",
                    message: "Value must be greater than 0."
                );
        }
    }
}
