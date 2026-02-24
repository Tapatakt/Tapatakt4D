using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Tapatakt4D.Examples;

/// <summary>
/// A 24-cell (icositetrachoron) - one of the six regular convex 4D polytopes.
/// Has 24 vertices, 96 edges, 96 triangular faces, and 24 octahedral cells.
/// </summary>
public sealed class Cell24 : Shape4D
{
    /// <summary>
    /// Creates a 24-cell centered at the specified origin.
    /// </summary>
    /// <param name="origin">Center position.</param>
    /// <param name="size">Scale factor (default 1.0).</param>
    /// <param name="thickness">Edge thickness.</param>
    public Cell24(Vector4 origin, float size = 1.0f, float thickness = 5.0f)
        : base(origin)
    {
        BuildCell24(size, thickness);
    }

    private void BuildCell24(float size, float thickness)
    {
        // 24-cell vertices: all permutations of (±1, ±1, 0, 0)
        // We generate these by picking 2 positions out of 4 for the non-zero values
        List<Vector4> vertices = new(24);
        
        int[] indices = [0, 1, 2, 3];
        // All pairs of positions
        for (int i = 0; i < 4; i++)
            for (int j = i + 1; j < 4; j++)
                for (int s1 = -1; s1 <= 1; s1 += 2) // All sign combinations for this pair
                    for (int s2 = -1; s2 <= 1; s2 += 2)
                    {
                        Vector4 v = Vector4.Zero;
                        v[i] = s1 * size;
                        v[j] = s2 * size;
                        vertices.Add(v);
                    }

        // Connect vertices that are at distance √2 * size
        // This creates the 96 edges of the 24-cell
        float edgeLengthSq = 2.0f * size * size;
        float tolerance = 0.001f * size * size;

        for (int i = 0; i < vertices.Count; i++)
            for (int j = i + 1; j < vertices.Count; j++)
            {
                float distSq = (vertices[i] - vertices[j]).LengthSquared;
                if (Math.Abs(distSq - edgeLengthSq) < tolerance)
                    Children.Add(new Edge4D(vertices[i], vertices[j], thickness));
            }
    }
}
