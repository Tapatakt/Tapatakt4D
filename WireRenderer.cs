using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Tapatakt4D;

/// <summary>
/// GPU-accelerated 4D wireframe renderer using tiled rasterization.
/// </summary>
public sealed class WireRenderer : IDisposable
{
    private const int TileSize = 16;
    private const int MaxEdgesPerTile = 256;
    private const int LocalGroupSize = 64;
    
    private int _width;
    private int _height;
    private int _gridW;
    private int _gridH;
    private int _tileCount;
    
    /// <summary>Screen width in pixels.</summary>
    public int Width => _width;
    
    /// <summary>Screen height in pixels.</summary>
    public int Height => _height;
    
    // OpenGL objects
    private int _computeProgram;
    private int _renderProgram;
    private int _vao;
    
    // SSBOs
    private int _edge4DBuffer;
    private int _projectionBuffer;
    private int _tileCountBuffer;
    private int _tileDataBuffer;
    
    // Shader uniform locations
    private int _projDistLoc;
    private int _screenSizeLoc;
    private int _gridSizeLoc;
    private int _tileSizeLoc;
    private int _maxEdgesLoc;
    private int _edgeCountLoc;
    private int _frameNumberLoc;
    
    // Frame counter
    private int _frameNumber;
    private uint[] _zeroArray;
    
    /// <summary>
    /// Creates a new wire renderer.
    /// </summary>
    /// <param name="width">Screen width in pixels.</param>
    /// <param name="height">Screen height in pixels.</param>
    public WireRenderer(int width, int height)
    {
        _width = width;
        _height = height;
        _gridW = (width + TileSize - 1) / TileSize;
        _gridH = (height + TileSize - 1) / TileSize;
        _tileCount = _gridW * _gridH;
        _zeroArray = new uint[_tileCount]; // Zero-initialized by default
        
        InitializeOpenGL();
    }
    
    private void InitializeOpenGL()
    {
        // Compile shaders
        _computeProgram = CreateComputeProgram();
        _renderProgram = CreateRenderProgram();
        
        // Create VAO for fullscreen quad (no VBO needed, using gl_VertexID)
        _vao = GL.GenVertexArray();
        
        // Get uniform locations for compute shader
        _projDistLoc = GL.GetUniformLocation(_computeProgram, "projectionDist");
        _screenSizeLoc = GL.GetUniformLocation(_computeProgram, "screenSize");
        _gridSizeLoc = GL.GetUniformLocation(_computeProgram, "gridSize");
        _tileSizeLoc = GL.GetUniformLocation(_computeProgram, "tileSize");
        _maxEdgesLoc = GL.GetUniformLocation(_computeProgram, "maxEdgesPerTile");
        _edgeCountLoc = GL.GetUniformLocation(_computeProgram, "edgeCount");
        
        // Get uniform locations for render shader
        _frameNumberLoc = GL.GetUniformLocation(_renderProgram, "frameNumber");
        
        // Create SSBOs
        CreateBuffers(maxEdges: 1024); // Initial capacity, will resize if needed
    }
    
    private int CreateComputeProgram()
    {
        string source = System.IO.File.ReadAllText("Shaders/EdgeRasterize.comp.glsl");
        int shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        
        CheckCompileError(shader, "Compute");
        
        int program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);
        
        CheckLinkError(program);
        
        GL.DeleteShader(shader);
        return program;
    }
    
    private int CreateRenderProgram()
    {
        string vertSource = System.IO.File.ReadAllText("Shaders/Vertex.vert.glsl");
        string fragSource = System.IO.File.ReadAllText("Shaders/Fragment.frag.glsl");
        
        int vertShader = GL.CreateShader(ShaderType.VertexShader);
        int fragShader = GL.CreateShader(ShaderType.FragmentShader);
        
        GL.ShaderSource(vertShader, vertSource);
        GL.ShaderSource(fragShader, fragSource);
        
        GL.CompileShader(vertShader);
        GL.CompileShader(fragShader);
        
        CheckCompileError(vertShader, "Vertex");
        CheckCompileError(fragShader, "Fragment");
        
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertShader);
        GL.AttachShader(program, fragShader);
        GL.LinkProgram(program);
        
        CheckLinkError(program);
        
        GL.DeleteShader(vertShader);
        GL.DeleteShader(fragShader);
        
        return program;
    }
    
    private static void CheckCompileError(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status != 1)
        {
            string log = GL.GetShaderInfoLog(shader);
            throw new Exception($"{type} shader compilation failed: {log}");
        }
    }
    
    private static void CheckLinkError(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
        if (status != 1)
        {
            string log = GL.GetProgramInfoLog(program);
            throw new Exception($"Shader linking failed: {log}");
        }
    }
    
    private void CreateBuffers(int maxEdges)
    {
        // Edge4D buffer (input)
        int edge4DSize = maxEdges * Marshal.SizeOf<Edge4D>();
        _edge4DBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _edge4DBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, edge4DSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        // Projection buffer (output from compute)
        int projSize = maxEdges * Marshal.SizeOf<EdgeProjection>();
        _projectionBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _projectionBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, projSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        // Tile count buffer (atomic counters)
        int countSize = _tileCount * sizeof(uint);
        _tileCountBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _tileCountBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, countSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        // Tile data buffer (edge IDs per tile)
        int dataSize = _tileCount * MaxEdgesPerTile * sizeof(uint);
        _tileDataBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _tileDataBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, dataSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }
    
    /// <summary>
    /// Renders a frame of 4D wireframe geometry using dual quaternion camera representation.
    /// </summary>
    /// <param name="edges">The edges to render in world space.</param>
    /// <param name="camera">The camera with dual quaternion rotation.</param>
    public void Render(List<Edge4D> edges, ICamera camera)
    {
        int edgeCount = edges.Count;

        // Upload edge data
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _edge4DBuffer);
        int edgeDataSize = edgeCount * Marshal.SizeOf<Edge4D>();
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, edgeDataSize, edges.ToArray());

        // Clear tile counts to zero
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _tileCountBuffer);
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, _tileCount * sizeof(uint), _zeroArray);

        // Run compute shader
        GL.UseProgram(_computeProgram);

        // Bind SSBOs
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _edge4DBuffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _projectionBuffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, _tileCountBuffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, _tileDataBuffer);

        // Set uniforms - dual quaternion representation
        // OpenTK Quaternion is (X, Y, Z, W), GLSL vec4 is (x, y, z, w) - matches directly
        Quaternion leftInv = camera.LeftQuaternionInverse;
        Quaternion rightInv = camera.RightQuaternionInverse;
        GL.Uniform4(GL.GetUniformLocation(_computeProgram, "cameraLeftQuat"), leftInv.X, leftInv.Y, leftInv.Z, leftInv.W);
        GL.Uniform4(GL.GetUniformLocation(_computeProgram, "cameraRightQuat"), rightInv.X, rightInv.Y, rightInv.Z, rightInv.W);
        GL.Uniform4(GL.GetUniformLocation(_computeProgram, "cameraPosition"), camera.Position);
        GL.Uniform1(_projDistLoc, camera.ProjectionDistance);
        GL.Uniform2(_screenSizeLoc, _width, _height);
        GL.Uniform2(_gridSizeLoc, _gridW, _gridH);
        GL.Uniform1(_tileSizeLoc, TileSize);
        GL.Uniform1(_maxEdgesLoc, MaxEdgesPerTile);
        GL.Uniform1(_edgeCountLoc, edgeCount);
        
        // Dispatch
        int groups = (edgeCount + LocalGroupSize - 1) / LocalGroupSize;
        GL.DispatchCompute(groups, 1, 1);
        
        // Ensure compute shader is done before fragment shader reads
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
        
        // Run render pass
        GL.UseProgram(_renderProgram);
        GL.BindVertexArray(_vao);
        
        // Bind SSBOs for fragment shader
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _projectionBuffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _tileCountBuffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, _tileDataBuffer);
        
        // Set uniforms
        GL.Uniform2(GL.GetUniformLocation(_renderProgram, "screenSize"), _width, _height);
        GL.Uniform2(GL.GetUniformLocation(_renderProgram, "gridSize"), _gridW, _gridH);
        GL.Uniform1(GL.GetUniformLocation(_renderProgram, "tileSize"), TileSize);
        GL.Uniform1(GL.GetUniformLocation(_renderProgram, "maxEdgesPerTile"), MaxEdgesPerTile);
        GL.Uniform1(_frameNumberLoc, _frameNumber++);
        
        // Draw fullscreen quad
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }
    
    /// <summary>
    /// Resizes the renderer to new dimensions.
    /// </summary>
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _gridW = (width + TileSize - 1) / TileSize;
        _gridH = (height + TileSize - 1) / TileSize;
        _tileCount = _gridW * _gridH;
        
        // Recreate tile buffers with new size
        GL.DeleteBuffer(_tileCountBuffer);
        GL.DeleteBuffer(_tileDataBuffer);
        
        int countSize = _tileCount * sizeof(uint);
        _tileCountBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _tileCountBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, countSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        int dataSize = _tileCount * MaxEdgesPerTile * sizeof(uint);
        _tileDataBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _tileDataBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, dataSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        _zeroArray = new uint[_tileCount];
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }
    
    /// <summary>
    /// Disposes of OpenGL resources.
    /// </summary>
    public void Dispose()
    {
        GL.DeleteProgram(_computeProgram);
        GL.DeleteProgram(_renderProgram);
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_edge4DBuffer);
        GL.DeleteBuffer(_projectionBuffer);
        GL.DeleteBuffer(_tileCountBuffer);
        GL.DeleteBuffer(_tileDataBuffer);
    }
}
