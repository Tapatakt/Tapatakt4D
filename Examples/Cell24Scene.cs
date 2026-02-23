using OpenTK.Mathematics;

namespace Tapatakt4D.Examples;

/// <summary>
/// A scene featuring a rotating 24-cell in 4D space.
/// </summary>
public sealed class Cell24Scene : Scene4D
{
    private readonly float _rotationSpeed;
    private readonly Cell24 _cell24;

    /// <summary>
    /// Creates a new 24-cell scene.
    /// </summary>
    /// <param name="rotationSpeed">Speed of rotation in radians per second.</param>
    /// <param name="cellSize">Size of the 24-cell.</param>
    public Cell24Scene(float rotationSpeed = 1.0f, float cellSize = 1.0f)
    {
        _rotationSpeed = rotationSpeed;

        // Create 24-cell at origin - camera starts at Z=8 looking at origin
        Vector4 position = Vector4.Zero;
        _cell24 = new Cell24(position, cellSize, 2.0f);
        AddShape(_cell24);
    }

    /// <inheritdoc />
    public override void Update(float dt)
    {
        // Small incremental rotation for this frame
        float deltaAngle = dt * _rotationSpeed;
        Matrix4 rotXW = Rotation4D.CreateRotationXW(deltaAngle * 0.05f);
        Matrix4 rotYW = Rotation4D.CreateRotationYW(deltaAngle * 0.03f);
        Matrix4 rotXY = Rotation4D.CreateRotationXY(deltaAngle * 0.02f);
        Matrix4 frameRotation = rotXW * rotYW * rotXY;

        // Rotate 24-cell around its center (origin)
        _cell24.Rotate(frameRotation);
    }
}
