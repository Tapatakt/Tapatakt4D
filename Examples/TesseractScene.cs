using OpenTK.Mathematics;

namespace Tapatakt4D.Examples;

/// <summary>
/// A scene featuring a rotating tesseract in 4D space.
/// </summary>
public sealed class TesseractScene : Scene4D
{
    private readonly float _rotationSpeed;
    private readonly Tesseract _tesseract;

    /// <summary>
    /// Creates a new tesseract scene.
    /// </summary>
    /// <param name="rotationSpeed">Speed of rotation in radians per second.</param>
    /// <param name="tesseractSize">Size of the tesseract.</param>
    public TesseractScene(float rotationSpeed = 1.0f, float tesseractSize = 1.0f)
    {
        _rotationSpeed = rotationSpeed;

        // Create tesseract at origin - camera starts at Z=8 looking at origin
        Vector4 position = Vector4.Zero;
        _tesseract = new Tesseract(position, tesseractSize, 2.0f);
        AddShape(_tesseract);
    }

    /// <inheritdoc />
    public override void Update(float dt)
    {
        
        // Small incremental rotation for this frame
        float deltaAngle = dt * _rotationSpeed;
        Matrix4 rotXW = Rotation4D.CreateRotationXW(deltaAngle * 0.005f);
        Matrix4 rotYW = Rotation4D.CreateRotationYW(deltaAngle * 0.003f);
        Matrix4 rotXY = Rotation4D.CreateRotationXY(deltaAngle * 0.002f);
        Matrix4 frameRotation = rotXW * rotYW * rotXY;

        // Rotate tesseract around its center (origin)
        _tesseract.Rotate(frameRotation);
        
    }
}
