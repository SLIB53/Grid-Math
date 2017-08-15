Grid Math
================================================================================
_**Version 1.0.0**_


Ruler Math
--------------------------------------------------------------------------------
"Ruler Math" (for lack of better distinguishing terminology) simply bit packs 3
integers that identify the discrete cells of a 3D grid. Each cell's size can be
by the "tick spacing" that defines the distance between "ticks" on each axis,
or "ruler".

For example, the point at (0.5, 0.5, 0.5) that samples a grid with a
"tick spacing" of 1 (tick marks every 1 unit) is in the grid cell with the index
of (1, 1, 1); meaning that the point is associated with the first tick mark along
each axis. This 3D cell's 32-bit RulerMath hash, or `3DTickMarkHash`, would be
the binary `00 0000000000 0000000000 0000000000`. Because the library uses the
full range of bits (2's complement 10-bit in 32-bit, 21-bit in 64-bit), the
binary for positive integers will be the tick mark less than 1. This hash is
very fast to both create and decode. It provides a simple way to identify grid
coordinates, and is useful for local spatial reasoning.

To build a minimal spatial query structure, you can simply build a
`Dictionary<int, object[]>` (32-bit) or `Dictionary<long, object[]>` (64-bit),
where the key stores a 3D tick mark hash. You can then sample points freely in
your logic, and lookup associated objects in the dictionary.

**Note:**  
32-bit can support up to a _1,024³_ grid.  
64-bit can support up to a _2,097,152³_ grid.  
