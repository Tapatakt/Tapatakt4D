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
    /// Delta is in camera space (e.g., (0,0,-1,0) = forward), transformed to world space.
    /// </summary>
    public void Move(Vector4 delta)
    {
        Vector4 transformedDelta = RotationInverse * delta;
        Position += transformedDelta;
    }

    /// <summary>
    /// Rotates the camera by the specified angles in the given planes.
    /// Angles are applied relative to camera orientation (FPS-style).
    /// </summary>
    public void Rotate(float angleXY = 0, float angleXZ = 0, float angleXW = 0,
                        float angleYZ = 0, float angleYW = 0, float angleZW = 0)
    {
        // Build local rotation in camera space
        Matrix4 localRot = Matrix4.Identity;
        
        if (angleXY != 0) localRot *= Rotation4D.CreateRotationXY(angleXY);
        if (angleYZ != 0) localRot *= Rotation4D.CreateRotationYZ(angleYZ);
        if (angleXZ != 0) localRot *= Rotation4D.CreateRotationXZ(angleXZ);
        if (angleYW != 0) localRot *= Rotation4D.CreateRotationYW(angleYW);
        if (angleXW != 0) localRot *= Rotation4D.CreateRotationXW(angleXW);
        if (angleZW != 0) localRot *= Rotation4D.CreateRotationZW(angleZW);

        // Pre-multiply: local rotation happens first (in camera space), then existing rotation
        Rotation = Rotation * localRot;
    }
}
