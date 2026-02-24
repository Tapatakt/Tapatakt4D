using OpenTK.Mathematics;

namespace Tapatakt4D.Examples;

/// <summary>
/// A scene featuring a rotating tesseract in 4D space.
/// </summary>
public sealed class TesseractScene : Scene4D
{
    private readonly float _rotationSpeed = 1f;
    private readonly Tesseract _tesseract;
    private readonly GridFloor _floor;

    /// <summary>
    /// Creates a new tesseract scene.
    /// </summary>
    public TesseractScene()
    {
        _tesseract = new(new(0f, 0.6f, 0f, 0f), 1.0f, 5f);
        _floor = new(new(0f, 0f, 0f, 0f), 20, 4f, 2f);
        AddShape(_tesseract);
        AddShape(_floor);
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

        // Rotate tesseract around its center
        _tesseract.Rotate(frameRotation);
        
    }
}
