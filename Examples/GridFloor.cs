using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;

namespace Tapatakt4D.Examples;

/// <summary>
/// A 3D grid floor in XZW space at Y = 0.
/// Creates a 10x10x10 cube with grid lines every 0.5 units.
/// </summary>
public sealed class GridFloor : Shape4D
{
    /// <summary>
    /// Creates a grid floor centered at the specified origin.
    /// </summary>
    /// <param name="origin">Center position of the grid.</param>
    /// <param name="size">Total size of the grid cube (default 20).</param>
    /// <param name="step">Distance between grid lines (default 0.5).</param>
    /// <param name="thickness">Edge thickness.</param>
    public GridFloor(Vector4 origin, float size = 20.0f, float step = 0.5f, float thickness = 2.0f)
        : base(origin)
    {
        float y = origin.Y;
        float halfSize = size / 2.0f;
        int divisions = (int)(size / step);
        // Create grid lines along X axis (varying X, constant Z and W)
        for (int z = 0; z <= divisions; z++)
        {
            for (int w = 0; w <= divisions; w++)
            {
                float zPos = -halfSize + z * step;
                float wPos = -halfSize + w * step;

                Vector4 start = new(-halfSize, y, zPos, wPos);
                Vector4 end = new(halfSize, y, zPos, wPos);
                Children.Add(new Edge4D(start, end, thickness));
            }
        }

        // Create grid lines along Z axis (varying Z, constant X and W)
        for (int x = 0; x <= divisions; x++)
        {
            for (int w = 0; w <= divisions; w++)
            {
                float xPos = -halfSize + x * step;
                float wPos = -halfSize + w * step;

                Vector4 start = new(xPos, y, -halfSize, wPos);
                Vector4 end = new(xPos, y, halfSize, wPos);
                Children.Add(new Edge4D(start, end, thickness));
            }
        }
        
        // Create grid lines along W axis (varying W, constant X and Z)
        for (int x = 0; x <= divisions; x++)
        {
            for (int z = 0; z <= divisions; z++)
            {
                float xPos = -halfSize + x * step;
                float zPos = -halfSize + z * step;

                Vector4 start = new(xPos, y, zPos, -halfSize);
                Vector4 end = new(xPos, y, zPos, halfSize);
                Children.Add(new Edge4D(start, end, thickness));
            }
        }
        
    }
}
