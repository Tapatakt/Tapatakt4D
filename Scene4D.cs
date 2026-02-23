using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Tapatakt4D;

/// <summary>
/// Abstract base class for 4D scenes.
/// Manages shapes and provides edges for rendering.
/// </summary>
public abstract class Scene4D : IDisposable
{
    private readonly List<IShape4D> _shapes = new();

    /// <summary>
    /// Creates a new scene.
    /// </summary>
    protected Scene4D()
    {
    }

    /// <summary>
    /// Adds a shape to the scene.
    /// </summary>
    protected void AddShape(IShape4D shape) => _shapes.Add(shape);

    /// <summary>
    /// Clears all shapes from the scene.
    /// </summary>
    protected void ClearShapes() => _shapes.Clear();

    /// <summary>
    /// Removes a specific shape from the scene.
    /// </summary>
    protected bool RemoveShape(IShape4D shape) => _shapes.Remove(shape);

    /// <summary>
    /// Updates scene state. Called once per frame.
    /// </summary>
    /// <param name="dt">Delta time in seconds.</param>
    public abstract void Update(float dt);

    /// <summary>
    /// Gets all edges from shapes in world space.
    /// Called once per render frame.
    /// </summary>
    public List<Edge4D> GetEdges()
    {
        List<Edge4D> result = new();

        foreach (IShape4D shape in _shapes)
        {
            foreach (Edge4D edge in shape.GetEdges())
            {
                result.Add(edge);
            }
        }

        return result;
    }

    /// <summary>
    /// Handles window resize. Override to react to size changes.
    /// </summary>
    public virtual void Resize(int width, int height)
    {
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
