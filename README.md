# Cuttlefish

Cuttlefish draws graphic patterns in Grasshopper, from a grid of points to the final curves.

It is a set of 20 Grasshopper components for drawing graphic patterns: mazes, pebbles, weaves, contour lines, spirals, Voronoi cells and more. Every pattern starts from simple geometry, a rectangle or a circle, and ends as plain Rhino curves that can be baked, offset, cut, engraved or plotted.

## Components

The components follow the order of the work.

**1. Points.** CreateGrid fills a rectangle with a grid of points, one branch per row. CircularGrid fills a circle with concentric rings of points, the centre included, with a maximum spacing X between two points of a ring and a spacing Y between two rings.

**2. Transform.** Attractor pulls the points towards one or more curves, within a given distance and with a given strength. Blur moves each point at random in X, Y and Z. Both keep the tree structure, so the grid stays a grid.

**3. Draw.** Sixteen components turn the points, or the cells built from them, into curves:

| Component | What it draws | Main inputs |
|---|---|---|
| Voronoi | Voronoi cells of a set of points inside a rectangle | Rectangle, Points |
| Delaunay | Triangles between neighbouring points | Points |
| Pebble | Cells shrunk and rounded into pebbles | Cells, Offset, Radius |
| Hatch | Parallel lines at a random angle in each cell | Cells, Spacing, Seed |
| Worm | Curves crossing each cell from edge to edge | Cells, Head, Seed |
| Bubble | One circle per point, sized by its nearest neighbour | Points, Factor |
| Truchet | Truchet tiles (quarter arcs or diagonals) on the grid | Points, Type, Seed |
| Maze | The walls of a random maze on the grid | Points, Seed |
| Wander | Filleted polylines jumping between neighbouring rows | Points, Radius, Seed |
| Lining | One curve per row of points, sharp or smooth | Points, Line type, Rectangle |
| Meshing | A zigzag between each pair of rows; together they form a net, sharp or smooth | Points, Line type, Rectangle |
| Dune | Filleted polylines that step at random towards the previous row | Points, Radius |
| Contour | Contour lines of the distance to attractor curves or points | Points, Attractors, Levels |
| Flow | Streamlines following guide curves | Seeds, Guides, Step, Count |
| Spiral | A spiral through the rings of a CircularGrid | Points |
| Weave | Two sets of curves woven over and under each other | Curves A, Curves B, Gap |

Components that use randomness take a Seed input: the same seed gives the same pattern, so a result can be found again and shared.

Patterns are drawn in the XY plane.

## Installation

1. Download `Cuttlefish.gha` from Food4Rhino or from the releases of this repository.
2. In Windows, right-click the file, open Properties and tick "Unblock" if the option is shown.
3. Copy it into the Grasshopper libraries folder (in Grasshopper: File > Special Folders > Components Folder).
4. Restart Rhino. The components appear in the Cuttlefish tab.

The file `Exemples/Cuttlefish-Examples.gh` contains one example per component, all starting from a single grid.

## Building from source

The project targets .NET Framework 4.8 and RhinoCommon / Grasshopper for Rhino 8. Open `Cuttlefish.csproj` in Visual Studio and build in Release.

## Compatibility

Tested with Rhino 8 for Windows.

## Author and support

Cuttlefish is developed by Jérémy Carolus (CJ Développement, https://www.cj-developpement.fr). Bug reports and requests are welcome in the GitHub issues.

## License

MIT. See [LICENSE](LICENSE).
