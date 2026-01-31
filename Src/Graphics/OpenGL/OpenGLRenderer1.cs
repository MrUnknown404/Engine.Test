using System.Numerics;
using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Client.Graphics.OpenGL.Objects;
using Engine3.Test.Graphics.Test;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;
using USharpLibs.Common.Math;
using ShaderType = Engine3.Client.Graphics.ShaderType;

namespace Engine3.Test.Graphics.OpenGL {
	public unsafe class OpenGLRenderer1 : OpenGLRenderer {
		private const string TestShaderName = "Test";

		private OpenGLBuffer? vertexBuffer;
		private OpenGLBuffer? indexBuffer;

		private OpenGLShader? vertexShader;
		private OpenGLShader? fragmentShader;
		private ProgramPipeline? programPipeline;

		private Camera? camera;

		private readonly TestVertex[] vertices = [ new(0, 0.5f, 0, 1, 0, 0), new(-0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, -0.5f, 0, 0, 0, 1), ];
		private readonly uint[] indices = [ 0, 1, 2, 2, 3, 0, ];
		private readonly Assembly gameAssembly;

		public OpenGLRenderer1(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window, Assembly gameAssembly) : base(graphicsBackend, window) => this.gameAssembly = gameAssembly;

		public override void Setup() {
			base.Setup();

			vertexShader = new("Test Vertex Shader", TestShaderName, ShaderType.Vertex, gameAssembly);
			fragmentShader = new("Test Fragment Shader", TestShaderName, ShaderType.Fragment, gameAssembly);
			programPipeline = new("Test Program Pipeline", vertexShader, fragmentShader);

			vertexBuffer = new("Test Vertex Buffer", (ulong)(sizeof(TestVertex) * vertices.Length), BufferStorageMask.DynamicStorageBit);
			vertexBuffer.Copy(vertices);

			indexBuffer = new("Test Index Buffer", (ulong)(sizeof(uint) * indices.Length), BufferStorageMask.DynamicStorageBit);
			indexBuffer.Copy(indices);

			// camera = new OrthographicCamera(10, 10, 0.1f, 10)
			Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i framebufferSize);
			camera = new PerspectiveCamera((float)framebufferSize.X / framebufferSize.Y, 0.1f, 10) { Position = new(0, 0, 5), YawDegrees = 270, };

			GL.Disable(EnableCap.CullFace);
		}

		protected override void DrawFrame(float delta) {
			if (this.vertexBuffer is not { } vertexBuffer) { return; }
			if (this.indexBuffer is not { } indexBuffer) { return; }
			if (this.programPipeline is not { } programPipeline) { return; }
			if (this.vertexShader is not { } vertexShader) { return; }
			if (this.camera is not { } camera) { return; }

			// TODO gl graphics pipeline class. bind program pipeline -> grants access to shaders -> bind buffers -> draw
			GL.BindProgramPipeline(programPipeline.Handle.Handle);

			// camera.YawDegrees += 0.5f;

			vertexShader.SetUniform("projection", camera.CreateProjectionMatrix());
			vertexShader.SetUniform("view", camera.CreateViewMatrix());
			vertexShader.SetUniform("model", Matrix4x4.CreateRotationY(FrameCount / 1000f * MathH.ToRadians(90f)));

			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, (int)vertexBuffer.Handle);
			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, (int)indexBuffer.Handle);

			GL.DrawArrays(PrimitiveType.Triangles, 0, indices.Length);
		}

		protected override void Cleanup() {
			programPipeline?.Destroy();
			vertexShader?.Destroy();
			fragmentShader?.Destroy();

			vertexBuffer?.Destroy();
			indexBuffer?.Destroy();
		}
	}
}