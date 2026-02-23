using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Tapatakt4D;

/// <summary>
/// Interface for 4D shapes that can be transformed and provide edges.
/// </summary>
public interface IShape4D
{
    /// <summary>
    /// Returns a new shape translated by the specified offset.
    /// </summary>
    IShape4D Translate(Vector4 offset);

    /// <summary>
    /// Returns a new shape rotated around the specified center point.
    /// </summary>
    IShape4D Rotate(Matrix4 rotation, Vector4 center);

    /// <summary>
    /// Returns a new shape rotated around its origin.
    /// </summary>
    IShape4D Rotate(Matrix4 rotation);

    /// <summary>
    /// Gets all edges of this shape.
    /// </summary>
    IEnumerable<Edge4D> GetEdges();
}
