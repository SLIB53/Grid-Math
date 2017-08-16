/*
    BSPMath32
    ————————————————————————————————————————————————————————————————————

    32-bit, binary space partitioning based, grid indexing library.


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


using UnityEngine;


namespace GridMath
{
    public static partial class BSPMath32
    {
        public const int MAX_LEVEL = 10;
        public const int L10_NUM_GRID_CELLS = 1024;


        // Public API

        /// <summary>
        /// Maps 'point' to a grid spatial index.
        ///
        /// The BSP spatial index is a 30-bit (packed right in 32-bit) hash for
        /// a level 10 binary space partition (BSP) node. The index must be
        /// paired with the level number to specify an index for levels 1-9.
        ///
        /// The hash results from bit packing parent octant indices.
        /// </summary>
        public static int MapBSPSpatialIndex(
            Vector3 point,
            float gridScale= 1f,
            int maxLevel = 10)
        {
            GuardLevelParam(maxLevel);
            GuardGridScaleParam(gridScale);
            GuardPointParam(point, gridScale);

            return _MapBSPSpatialIndex(point, gridScale, maxLevel);
        }

        /// <summary>
        ///  Decodes a BSP spatial index hash and returns an octant id
        /// at the 'level' in the hash's encoded path.
        /// </summary>
        public static int GetOctantIndex(int BSPSpatialIndex, int level)
        {
            GuardLevelParam(level);

            return _GetOctantIndex(BSPSpatialIndex, level);
        }

        /// <summary>
        /// Gets the position of the center of the cell identified by the
        /// BSP spatial index and other paramater(s) defining an
        /// associated grid.
        /// </summary>
        public static Vector3 GetCellCenter(
            int BSPSpatialIndex,
            float gridScale= 1f,
            int level = 10)
        {
            GuardLevelParam(level);
            GuardGridScaleParam(gridScale);

            return _GetCellCenter(BSPSpatialIndex, gridScale, level);
        }

        /// <summary>
        /// Gets the lowest value allowed for a position for a grid defined by
        /// the parameter(s).
        /// </summary>
        public static float GetLowerLimit(float gridScale = 1)
        {
            GuardGridScaleParam(gridScale);

            return _GetLowerLimit(gridScale);
        }

        /// <summary>
        /// Gets the highest value allowed for a position for a grid defined by
        /// the parameter(s).
        /// </summary>
        public static float GetUpperLimit(float gridScale = 1)
        {
            GuardGridScaleParam(gridScale);

            return _GetUpperLimit(gridScale);
        }


        // Public Util

        /// <summary>
        /// Creates a 3-bit bit field (XYZ) containing sign bit flags for each
        /// field in the vector. For example, the vector (0, 1, -1)
        /// would return 001.
        ///
        /// The resulting 3-bit enumeration can be used to identify the octant
        /// which contains the 'point'.
        /// </summary>
        public static int CreateSignBitField(Vector3 point, Vector3 origin)
        {
            /*
                Binary     Value      Octant
                --------   --------   -------------
                000        0          +x, +y, +z
                001        1          +x, +y, -z
                010        2          +x, -y, +z
                011        3          +x, -y, -z
                100        4          -x, +y, +z
                101        5          -x, +y, -z
                110        6          -x, -y, +z
                111        7          -x, -y, -z

            */

            int bitFieldBuffer = 0;
            if (point.x < origin.x) bitFieldBuffer |= 4;
            if (point.y < origin.y) bitFieldBuffer |= 2;
            if (point.z < origin.z) bitFieldBuffer |= 1;

            return bitFieldBuffer;
        }

        /// <summary>
        /// Makes fields of a vector positive or negative based on bit field of
        /// sign bit flags. For example, the bit field 010 indicates +x, -y, +z.
        /// </summary>
        public static Vector3 SignVector(Vector3 vector, int signBitField)
        {
            /*
                    xyz      0__      1__
                  & 100    & 100    & 100  (100 binary = 4 decimal)
                -------  -------  -------
                    x00      000      100
            */
            var xSign = (signBitField & 4) == 0 ? 1.0f : -1.0f;

            /*
                    xyz      _0_      _1_
                  & 010    & 010    & 010  (010 binary = 2 decimal)
                -------  -------  -------
                    0y0      000      010
            */
            var ySign = (signBitField & 2) == 0 ? 1.0f : -1.0f;

            /*
                    xyz      __0      __1
                  & 001    & 001    & 001  (001 binary = 1 decimal)
                -------  -------  -------
                    00z      000      001
            */
            var zSign = (signBitField & 1) == 0 ? 1.0f : -1.0f;

            var signedVector = new Vector3(
                x: Mathf.Abs(vector.x) * xSign,
                y: Mathf.Abs(vector.y) * ySign,
                z: Mathf.Abs(vector.z) * zSign
            );

            return signedVector;
        }

        /// <summary>
        /// Calculates the edge length of a grid cell for a grid defined by
        /// the parameter(s).
        /// </summary>
        public static float CalcCellSize(float gridScale, int level = 10)
        {
            GuardGridScaleParam(gridScale);
            GuardLevelParam(level);

            return gridScale * StandardSpace.CalcCellSize(level);
        }


        // Public API Implementation

        private static int _MapBSPSpatialIndex(
            Vector3 point,
            float gridScale = 1f,
            int maxLevel = 10)
        {
            /*
                Example
                ----------------------------------------------------------------

                Given the arguments:

                    Octant Path Hash    0011 1100 0000 0000 0000 0000 0000 0000
                    Level           3

                Annotated Octant Path Hash:

                00 || 111 | 100 | 000 | 000 | 000 | 000 | 000 | 000 | 000 | 000
                        1 | 2   | 3   | 4   | 5   | 6   | 7   | 8   | 9   | 10

                ...specifies the octant that is in octant 0 (gen 3), of
                octant 4 (gen 2), octant 7 (gen 1) of the root:

                    root > 7 > 4 > 0

            */

            // 'point' local to the standard scale grid
            var point_standardized = point / gridScale;

            var final_BSPSpatialIndex = StandardSpace.MapBSPSpatialIndex(
                point_standardized,
                maxLevel
            );

            return final_BSPSpatialIndex;
        }

        private static int _GetOctantIndex(int BSPSpatialIndex, int level)
        {
            /*
                Clear bits to the left of octant index,
                then logical shift all the way right.

                Calculating Number of Left Bits
                ============================================================

                Level      1    2    3    4    5    6    7    8    9    10
                # Bits     2    5    8    11   14   17   20   23   26   29

                Formula: f(x) = 3x-1, where x is the level.

                Example
                ------------------------------------------------------------

                Given the arguments:

                    signature   0011 1100 0000 0000 0000 0000 0000 0000
                    level  2

                Annotated 32-bit signature:

                    00 || 111 | 100 | 000 | 000 | 000 | 000 | 000 | 000 | 000 | 000
                          1   | 2   | 3   | 4   | 5   | 6   | 7   | 8   | 9   | 10


                **STEP 1** - Clear left bits:

                    f(2) = 3(2) - 1
                         = 5
                    0011 1100 0000 0000 0000 0000 0000 0000 << 5
                        = 1000 0000 0000 0000 0000 0000 0000 0000

                **STEP 2** - Logical shift to right end:

                    1000 0000 0000 0000 0000 0000 0000 0000 >> 29
                        = 0000 0000 0000 0000 0000 0000 0000 0100

            */

            var lshNumBits = 3 * level - 1;
            return (BSPSpatialIndex << lshNumBits) >> 29;
        }

        private static Vector3 _GetCellCenter(
            int BSPSpatialIndex,
            float gridScale= 1f,
            int level = 10)
        {
            var cellCenterSimplified = StandardSpace.GetCellCenter(
                BSPSpatialIndex,
                level
            );

            return gridScale * cellCenterSimplified;
        }

        private static float _GetLowerLimit(float gridScale = 1)
        {
            return StandardSpace.LOWER_LIMIT * gridScale;
        }

        private static float _GetUpperLimit(float gridScale = 1)
        {
            return StandardSpace.UPPER_LIMIT * gridScale;
        }


        // Guards

        private static void GuardLevelParam(int level)
        {
            if (level < 0 || level > MAX_LEVEL)
                throw new System.ArgumentOutOfRangeException(
                    paramName: "level",
                    message: string.Format(
                        "Value is not between {1} and {2} (inclusive).",
                        0, MAX_LEVEL
                    )
                );
        }

        private static void GuardGridScaleParam(float gridScale)
        {
            // TODO: Guard upper limit of tick spacing
            //       (gridScale must not cause float infinity or NaN)
            if (gridScale < 0f)
                throw new System.ArgumentOutOfRangeException(
                    "gridScale",
                    "Value is not greater than 0."
                );
        }

        private static void GuardPointParam(Vector3 point, float gridScale)
        {
            var scaledlowerLimit = _GetLowerLimit(gridScale);
            var scaledUpperLimit = _GetUpperLimit(gridScale);

            if (
                   (point.x < scaledlowerLimit || point.x > scaledUpperLimit)
                || (point.y < scaledlowerLimit || point.y > scaledUpperLimit)
                || (point.z < scaledlowerLimit || point.z > scaledUpperLimit))
            {
                throw new System.ArgumentOutOfRangeException(
                    paramName: "position",
                    message: string.Format(
                        "A vector dimension is not between {1} and {2} (inclusive).",
                        scaledlowerLimit, scaledUpperLimit
                    )
                );
            }
        }


        private static class StandardSpace
        {
            public const float LOWER_LIMIT = -512f;
            public const float UPPER_LIMIT = 512f;


            public static int MapBSPSpatialIndex(Vector3 point, int maxLevel = 10)
            {
                // Computes by stepping through levels,
                // while compounding level metadata.

                var step_level = 0;
                var step_apothem = 0.5f * L10_NUM_GRID_CELLS;
                var step_center = Vector3.zero;
                var step_octantPathHashBuffer = 0;

                while (step_level < maxLevel)
                {
                    int step_octantId;

                    step_level++;
                    step_apothem *= 0.5f;
                    step_octantId = CreateSignBitField(
                        point: point,
                        origin: step_center // XXX: previous step center
                    );
                    step_center += SignVector(
                        vector: new Vector3(
                            x: step_apothem,
                            y: step_apothem,
                            z: step_apothem
                        ),
                        signBitField: step_octantId
                    );
                    step_octantPathHashBuffer =
                        (step_octantPathHashBuffer << 3) | step_octantId;
                }

                // Finalize stepping buffer result as final hash

                var final_octantPathHash =
                    step_octantPathHashBuffer << (3 * (10 - maxLevel));

                return final_octantPathHash;
            }

            public static Vector3 GetCellCenter(int BSPSpatialIndex, int level = 10)
            {
                // Computes by stepping through levels,
                // while compounding level metadata.

                var step_level = 0;
                var step_apothem = 0.5f * L10_NUM_GRID_CELLS;
                var step_center = Vector3.zero;

                while (step_level < level)
                {
                    step_level++;
                    step_apothem *= 0.5f;
                    step_center += SignVector(
                        vector: new Vector3(
                            x: step_apothem,
                            y: step_apothem,
                            z: step_apothem
                        ),
                        signBitField: GetOctantIndex(
                            BSPSpatialIndex,
                            step_level
                        )
                    );
                }

                return step_center;
            }

            public static float CalcCellSize(int level = 10)
            {
                switch (level)
                {
                    case 10: return 1f;
                    case 9: return 2f;
                    case 8: return 4f;
                    case 7: return 8f;
                    case 6: return 16f;
                    case 5: return 32f;
                    case 4: return 64f;
                    case 3: return 128f;
                    case 2: return 256f;
                    case 1: return 512f;
                    default:
                        throw new System.ArgumentException(
                            paramName: "level",
                            message: "Value is not between 1 and 10 (inclusive)."
                        );
                }
            }
        }
    }
}
