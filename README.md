Grid Math
================================================================================
_**Version 1.0.0**_


Ruler Math
--------------------------------------------------------------------------------
"Ruler Math" (for lack of better distinguishing terminology) simply bit packs 3
integers that identify the discrete cells of a 3D grid. Each cell's size is
determined by the "tick spacing" that defines the distance between "ticks" on
each axis, or "ruler".

For example, the point at (0.5, 0.5, 0.5) that samples a grid with a
"tick spacing" of 1 (ticks every 1 unit) is in the grid cell with the index of
(1, 1, 1) (zeroth index is not used). This cell's 32-bit RulerMath hash, or
`3DTickMarkHash`, would be the binary `00 0000000001 0000000001 0000000001`.
This hash is very fast to both create and decode. It provides a simple way to
identify grid coordinates, and is useful for local spatial querying.

To build a minimal spatial query structure, you can simply build a
`Dictionary<int, object>` (32-bit) or `Dictionary<long, object>`, where the key
is of course a 3D tick mark hash.

**Note:**  
32-bit can support up to a _1,024³_ grid.  
64-bit can support up to a _2,097,152³_ grid.  
