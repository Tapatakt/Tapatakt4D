using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Tapatakt4D.Examples;

/// <summary>
/// Walking environment with floor-bound movement.
/// Camera stays at Y=1, can only yaw (limited), movement projected to XZW plane.
/// </summary>
public class WalkingEnv : IDisposable
{
    private GameWindow? _window;
    private WireRenderer? _renderer;
    private int _frameNumber;
    private ICamera _camera;
    private float _currentPitch = 0.0f;

    private const float MaxPitchAngle = 80.0f * MathF.PI / 180.0f;
    private const float MouseSensitivity = 0.002f;
    private const float MoveSpeed = 3.0f;
    private const float RotationSpeed = 1.5f;

    /// <summary>
    /// The scene being rendered.
    /// </summary>
    public Scene4D Scene { get; }

    /// <summary>
    /// Window width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Window height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Window title.
    /// </summary>
    public string Title { get; set; } = "Tapatakt4D Walking";

    /// <summary>
    /// Gets or sets the camera.
    /// </summary>
    public ICamera Camera
    {
        get => _camera;
        set => _camera = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Creates a new walking environment with the specified scene.
    /// </summary>
    public WalkingEnv(Scene4D scene, int width = 1280, int height = 720)
    {
        Scene = scene ?? throw new ArgumentNullException(nameof(scene));
        Width = width;
        Height = height;
        _camera = new WalkingCamera(new Vector4(0.0f, 1.0f, 8.0f, 0.0f));
    }

    /// <summary>
    /// Runs the walking demo loop. Blocks until window is closed.
    /// </summary>
    public void Run()
    {
        GameWindowSettings gws = GameWindowSettings.Default;
        NativeWindowSettings nws = new()
        {
            ClientSize = new Vector2i(Width, Height),
            Title = Title,
            Profile = ContextProfile.Core,
            APIVersion = new Version(4, 3),
            Flags = ContextFlags.Debug
        };

        _window = new GameWindow(gws, nws);
        _renderer = new WireRenderer(Width, Height);

        _window.Load += OnLoad;
        _window.Resize += OnResize;
        _window.UpdateFrame += OnUpdateFrame;
        _window.RenderFrame += OnRenderFrame;
        _window.Unload += OnUnload;

        _window.Run();
    }

    private void OnLoad()
    {
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Disable(EnableCap.DepthTest);

        if (_window != null)
            _window.CursorState = CursorState.Grabbed;
    }

    private void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, e.Width, e.Height);
        _renderer?.Resize(e.Width, e.Height);
        Scene.Resize(e.Width, e.Height);
    }

    private void OnUpdateFrame(FrameEventArgs args)
    {
        if (_window == null)
            return;

        float dt = (float)args.Time;

        if (_window.KeyboardState.IsKeyDown(Keys.Escape))
        {
            _window.Close();
            return;
        }

        Controls(dt);
        Scene.Update(dt);
    }

    private void OnRenderFrame(FrameEventArgs args)
    {
        if (_renderer == null)
            return;

        GL.Clear(ClearBufferMask.ColorBufferBit);
        _renderer.Render(Scene.GetEdges(), _camera);
        _window?.SwapBuffers();
        _frameNumber++;
    }

    private void OnUnload()
    {
        Scene.Dispose();
        _renderer?.Dispose();
        _window?.Dispose();
    }

    /// <summary>
    /// Walking controls: Y locked, no roll or YW rotation, yaw limited, movement projected to XZW.
    /// </summary>
    public virtual void Controls(float dt)
    {
        if (_window == null)
            return;

        HandleMovement(dt, _window);
        HandleRotations(dt, _window);

        if (_window.KeyboardState.IsKeyDown(Keys.Space))
        {
            Console.WriteLine($"\nForward = {Camera.Forward}\nRight = {Camera.Right}\nUp = {Camera.Up}\nAna = {Camera.Ana}");
            Quaternion l = _camera.LeftQuaternionInverse;
            Quaternion r = _camera.RightQuaternionInverse;
            Console.WriteLine($"Yaw: {_currentPitch * 180.0f / MathF.PI:F1}°");
            Console.WriteLine($"LeftInv =({l.X:F4}, {l.Y:F4}, {l.Z:F4}, {l.W:F4})");
            Console.WriteLine($"RightInv=({r.X:F4}, {r.Y:F4}, {r.Z:F4}, {r.W:F4})");
        }
    }

    /// <summary>
    /// Handles movement like a first-person game:
    /// - XZW movement uses camera's projected directions (pitch doesn't affect speed)
    /// - Y stays locked to 1 (no vertical movement)
    /// </summary>
    private void HandleMovement(float dt, GameWindow window)
    {
        float speed = MoveSpeed * dt;

        // Build camera-space movement delta
        // X = right/left, Z = forward/back, W = ana/kata, Y = 0 (locked)
        Vector4 cameraDelta = Vector4.Zero;

        // Forward/back - W/S
        if (window.KeyboardState.IsKeyDown(Keys.W))
            cameraDelta.Z += speed;
        if (window.KeyboardState.IsKeyDown(Keys.S))
            cameraDelta.Z -= speed;

        // Left/right - A/D
        if (window.KeyboardState.IsKeyDown(Keys.A))
            cameraDelta.X -= speed;
        if (window.KeyboardState.IsKeyDown(Keys.D))
            cameraDelta.X += speed;

        // Ana/kata - Q/E
        if (window.KeyboardState.IsKeyDown(Keys.Q))
            cameraDelta.W -= speed;
        if (window.KeyboardState.IsKeyDown(Keys.E))
            cameraDelta.W += speed;

        if (cameraDelta != Vector4.Zero)
        {
            Camera.Move(cameraDelta);
            // Lock Y to 1 after movement
            Vector4 pos = Camera.Position;
            Camera.Position = new Vector4(pos.X, 1.0f, pos.Z, pos.W);
        }
    }

    /// <summary>
    /// Handles rotations: only YZ (yaw) allowed, limited to +/- 80 degrees.
    /// No XY (roll), no YW rotation.
    /// </summary>
    private void HandleRotations(float dt, GameWindow window)
    {
        float rotSpeed = RotationSpeed * dt;
        float xz = 0, yz = 0, zw = 0;

        Vector2 delta = window.MouseState.Delta;
        // XZ plane - mouse X
        if (delta.X != 0)
            xz = -delta.X * MouseSensitivity;
        
        // YZ plane - mouse Y, with limits (look up/down)
        if (delta.Y != 0)
            yz = -delta.Y * MouseSensitivity;

        
        // ZW rotation - Mouse wheel
        float wheelDelta = window.MouseState.ScrollDelta.Y;
        if (Math.Abs(wheelDelta) > 0.01f)
            zw = Math.Sign(wheelDelta) * rotSpeed * 50.0f;
            
        Camera.Rotate(angleXZ:xz, angleYZ:yz, angleZW:zw);
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        OnUnload();
        GC.SuppressFinalize(this);
    }
}
