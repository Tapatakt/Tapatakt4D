using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Tapatakt4D;

/// <summary>
/// GPU-compatible structure for projected edges.
/// xy = screen position
/// z = distance from camera (for perspective-correct W clipping)
/// w = original W (for color)
/// </summary>
public struct EdgeProjection
{
    /// <summary>Start: xy=screen, z=distance, w=originalW.</summary>
    public Vector4 Start;

    /// <summary>End: xy=screen, z=distance, w=originalW.</summary>
    public Vector4 End;

    /// <summary>Thickness in screen pixels.</summary>
    public float Thickness;
}

/// <summary>
/// Represents an edge in 4D space with start and end points and thickness.
/// Layout matches the GLSL struct exactly (48 bytes).
/// </summary>
public readonly struct Edge4D : IShape4D
{
    /// <summary>Start point in 4D space (x, y, z, w).</summary>
    public readonly Vector4 Start;

    /// <summary>End point in 4D space (x, y, z, w).</summary>
    public readonly Vector4 End;

    /// <summary>Thickness in screen pixels.</summary>
    public readonly float Thickness;

    /// <summary>Padding to match GLSL layout.</summary>
    private readonly float _padding0;
    private readonly float _padding1;
    private readonly float _padding2;

    /// <summary>
    /// Creates a new 4D edge.
    /// </summary>
    public Edge4D(Vector4 start, Vector4 end, float thickness = 1.0f)
    {
        Start = start;
        End = end;
        Thickness = thickness;
        _padding0 = 0;
        _padding1 = 0;
        _padding2 = 0;
    }

    /// <inheritdoc />
    public IShape4D Translate(Vector4 offset)
        => new Edge4D(Start + offset, End + offset, Thickness);

    /// <inheritdoc />
    public IShape4D Rotate(Matrix4 rotation, Vector4 center)
        => new Edge4D(
            rotation * (Start - center) + center,
            rotation * (End - center) + center,
            Thickness
        );

    /// <inheritdoc />
    public IShape4D Rotate(Matrix4 rotation)
        => Rotate(rotation, Vector4.Zero);

    /// <inheritdoc />
    public IEnumerable<Edge4D> GetEdges()
    {
        yield return this;
    }
}
