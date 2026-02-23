using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Tapatakt4D;

/// <summary>
/// Abstract base class for composite 4D shapes.
/// Contains child shapes and an origin point that moves with translations.
/// Transformations are done in-place for efficiency.
/// </summary>
public abstract class Shape4D : IShape4D
{
    /// <summary>
    /// Child shapes that make up this composite shape.
    /// </summary>
    protected readonly List<IShape4D> Children = new();

    /// <summary>
    /// Origin point of this shape. Translated along with the shape.
    /// Used as default center for rotation.
    /// </summary>
    public Vector4 Origin { get; protected set; }

    /// <summary>
    /// Creates a new shape with origin at zero.
    /// </summary>
    protected Shape4D()
    {
        Origin = Vector4.Zero;
    }

    /// <summary>
    /// Creates a new shape with the specified origin.
    /// </summary>
    public Shape4D(Vector4 origin)
    {
        Origin = origin;
    }

    /// <inheritdoc />
    /// <remarks>Modifies this shape in place and returns it for chaining.</remarks>
    public IShape4D Translate(Vector4 offset)
    {
        Origin += offset;

        for (int i = 0; i < Children.Count; i++)
            Children[i] = Children[i].Translate(offset);

        return this;
    }

    /// <inheritdoc />
    /// <remarks>Modifies this shape in place and returns it for chaining.</remarks>
    public IShape4D Rotate(Matrix4 rotation, Vector4 center)
    {
        Origin = rotation * (Origin - center) + center;

        for (int i = 0; i < Children.Count; i++)
            Children[i] = Children[i].Rotate(rotation, center);

        return this;
    }

    /// <inheritdoc />
    /// <remarks>Modifies this shape in place and returns it for chaining.</remarks>
    public IShape4D Rotate(Matrix4 rotation)
        => Rotate(rotation, Origin);

    /// <inheritdoc />
    public virtual IEnumerable<Edge4D> GetEdges()
    {
        foreach (IShape4D child in Children)
            foreach (Edge4D edge in child.GetEdges())
                yield return edge;
    }
}
