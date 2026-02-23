using OpenTK.Mathematics;

namespace Tapatakt4D;

/// <summary>
/// 4D camera with position, rotation, and projection settings.
/// </summary>
public sealed class Camera
{
    /// <summary>
    /// Camera position in 4D world space.
    /// </summary>
    public Vector4 Position { get; set; }

    /// <summary>
    /// Camera orientation as a 4D rotation matrix.
    /// </summary>
    public Matrix4 Rotation { get; set; }

    /// <summary>
    /// Inverse camera rotation for transforming world to camera space.
    /// </summary>
    public Matrix4 RotationInverse => Rotation.Inverted();

    /// <summary>
    /// Projection distance for perspective.
    /// </summary>
    public float ProjectionDistance { get; set; }

    /// <summary>
    /// Creates a new camera at the specified position.
    /// Default position is (0, 0, 8, 0) - 8 units back from origin.
    /// </summary>
    public Camera(Vector4? position = null, float projectionDistance = 2.0f)
    {
        Position = position ?? new Vector4(0.0f, 0.0f, 8.0f, 0.0f);
        Rotation = Matrix4.Identity;
        ProjectionDistance = projectionDistance;
    }

    /// <summary>
    /// Moves the camera by the specified delta relative to camera orientation.
    /// </summary>
    public void Move(Vector4 delta)
    {
        Vector4 transformedDelta = Rotation * delta;
        Position += transformedDelta;
    }

    /// <summary>
    /// Rotates the camera by the specified angles in the given planes.
    /// Angles are applied in world space.
    /// </summary>
    public void Rotate(float angleXY = 0, float angleXZ = 0, float angleXW = 0,
                        float angleYZ = 0, float angleYW = 0, float angleZW = 0)
    {
        // Build rotation - order matters for 4D!
        Matrix4 rot = Matrix4.Identity;
        
        if (angleZW != 0) rot = Rotation4D.CreateRotationZW(angleZW) * rot;
        if (angleXW != 0) rot = Rotation4D.CreateRotationXW(angleXW) * rot;
        if (angleYW != 0) rot = Rotation4D.CreateRotationYW(angleYW) * rot;
        if (angleXZ != 0) rot = Rotation4D.CreateRotationXZ(angleXZ) * rot;
        if (angleYZ != 0) rot = Rotation4D.CreateRotationYZ(angleYZ) * rot;
        if (angleXY != 0) rot = Rotation4D.CreateRotationXY(angleXY) * rot;

        // Apply rotation in world space
        Rotation = rot * Rotation;
    }
}
