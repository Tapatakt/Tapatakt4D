using OpenTK.Mathematics;
using System;

namespace Tapatakt4D.Examples;

/// <summary>
/// A tesseract (4D hypercube) shape.
/// </summary>
public sealed class Tesseract : Shape4D
{
    /// <summary>
    /// Creates a tesseract centered at the specified origin.
    /// </summary>
    public Tesseract(Vector4 origin, float size = 1.0f, float thickness = 5.0f)
        : base(origin)
    {
     
        // A tesseract has 16 vertices: all combinations of (±size, ±size, ±size, ±size)
        Vector4[] vertices = new Vector4[16];
        int idx = 0;
        float halfSize = size / 2;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    for (int w = -1; w <= 1; w += 2)
                        vertices[idx++] = new Vector4(
                            origin.X + x * halfSize,
                            origin.Y + y * halfSize,
                            origin.Z + z * halfSize,
                            origin.W + w * halfSize
                        );

        // Connect vertices that differ in exactly one coordinate
        for (int i = 0; i < 16; i++)
            for (int j = i + 1; j < 16; j++)
            {
                int diffCount = 0;
                if (Math.Abs(vertices[i].X - vertices[j].X) > 0.001f) diffCount++;
                if (Math.Abs(vertices[i].Y - vertices[j].Y) > 0.001f) diffCount++;
                if (Math.Abs(vertices[i].Z - vertices[j].Z) > 0.001f) diffCount++;
                if (Math.Abs(vertices[i].W - vertices[j].W) > 0.001f) diffCount++;

                if (diffCount == 1)
                    Children.Add(new Edge4D(vertices[i], vertices[j], thickness));
            }
    }
}
