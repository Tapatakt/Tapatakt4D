using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Tapatakt4D.Examples;

/// <summary>
/// Demo environment that hosts a window and runs a 4D scene.
/// </summary>
public class DemoEnv : IDisposable
{
    private GameWindow? _window;
    private WireRenderer? _renderer;
    private int _frameNumber;
    private Camera _camera;
    
    /// <summary>
    /// Gets or sets the camera.
    /// </summary>
    public Camera Camera
    {
        get => _camera;
        set => _camera = value ?? throw new ArgumentNullException(nameof(value));
    }

    // Sensitivity settings
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
    public string Title { get; set; } = "Tapatakt4D";

    /// <summary>
    /// Creates a new demo environment with the specified scene and dimensions.
    /// </summary>
    public DemoEnv(Scene4D scene, int width = 1280, int height = 720)
    {
        Scene = scene ?? throw new ArgumentNullException(nameof(scene));
        Width = width;
        Height = height;
        _camera = new();
    }

    /// <summary>
    /// Runs the demo loop. Blocks until window is closed.
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

        // Hide cursor and capture mouse
        if (_window != null)
        {
            _window.CursorState = CursorState.Grabbed;
        }
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

        // Process user controls
        Controls(dt);

        // Update scene
        Scene.Update(dt);
    }

    private void OnRenderFrame(FrameEventArgs args)
    {
        if (_renderer == null)
            return;

        GL.Clear(ClearBufferMask.ColorBufferBit);

        // Render edges with camera transformation done in shader
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
    /// Processes user input controls.
    /// </summary>
    public virtual void Controls(float dt)
    {
        if (_window == null)
            return;

        HandleMovement(dt, _window);
        HandleRotations(dt, _window);
    }

    /// <summary>
    /// Handles movement controls (W/S Z, A/D X, Shift/Ctrl Y, Z/X W).
    /// </summary>
    private void HandleMovement(float dt, GameWindow window)
    {
        float speed = MoveSpeed * dt;
        Vector4 moveDelta = Vector4.Zero;

        // Z axis (forward/back) - W/S
        // Camera looks at negative Z, so forward is negative Z
        if (window.KeyboardState.IsKeyDown(Keys.W))
            moveDelta.Z -= speed;
        if (window.KeyboardState.IsKeyDown(Keys.S))
            moveDelta.Z += speed;

        // X axis (left/right) - A/D
        if (window.KeyboardState.IsKeyDown(Keys.A))
            moveDelta.X -= speed;
        if (window.KeyboardState.IsKeyDown(Keys.D))
            moveDelta.X += speed;

        // Y axis (up/down) - Shift/Ctrl
        if (window.KeyboardState.IsKeyDown(Keys.LeftShift) || window.KeyboardState.IsKeyDown(Keys.RightShift))
            moveDelta.Y += speed;
        if (window.KeyboardState.IsKeyDown(Keys.LeftControl) || window.KeyboardState.IsKeyDown(Keys.RightControl))
            moveDelta.Y -= speed;

        // W axis (ana/kata) - Z/X
        if (window.KeyboardState.IsKeyDown(Keys.Z))
            moveDelta.W -= speed;
        if (window.KeyboardState.IsKeyDown(Keys.X))
            moveDelta.W += speed;

        if (moveDelta != Vector4.Zero)
            _camera.Move(moveDelta);
    }

    /// <summary>
    /// Handles rotation controls.
    /// Mouse X: Yaw (XZ) | Mouse Y: Pitch (YZ) | Q/E: Roll (XY) | Wheel: ZW | LMB/RMB: XW | 1/3: YW
    /// </summary>
    private void HandleRotations(float dt, GameWindow window)
    {
        float rotSpeed = RotationSpeed * dt;

        // Use raw mouse delta for rotation (works with grabbed cursor)
        Vector2 delta = window.MouseState.Delta;

        
        // Yaw (XZ plane) - mouse X
        if (delta.X != 0)
            _camera.Rotate(angleXZ: -delta.X * MouseSensitivity);

        // Pitch (YZ plane) - mouse Y (inverted because camera looks at negative Z)
        if (delta.Y != 0)
            _camera.Rotate(angleYZ: -delta.Y * MouseSensitivity);
        

        // Roll (XY plane) - Q/E
        if (window.KeyboardState.IsKeyDown(Keys.Q))
            _camera.Rotate(angleXY: rotSpeed);
        if (window.KeyboardState.IsKeyDown(Keys.E))
            _camera.Rotate(angleXY: -rotSpeed);

        // ZW rotation - Mouse wheel
        float wheelDelta = window.MouseState.ScrollDelta.Y;
        if (Math.Abs(wheelDelta) > 0.01f)
            _camera.Rotate(angleZW: Math.Sign(wheelDelta) * rotSpeed * 50.0f);

        // XW rotation - LMB/RMB for opposite directions
        if (window.MouseState.IsButtonDown(MouseButton.Left))
            _camera.Rotate(angleXW: rotSpeed);
        if (window.MouseState.IsButtonDown(MouseButton.Right))
            _camera.Rotate(angleXW: -rotSpeed);

        // YW rotation - 1/3 for opposite directions
        if (window.KeyboardState.IsKeyDown(Keys.D1))
            _camera.Rotate(angleYW: rotSpeed);
        if (window.KeyboardState.IsKeyDown(Keys.D3))
            _camera.Rotate(angleYW: -rotSpeed);
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
