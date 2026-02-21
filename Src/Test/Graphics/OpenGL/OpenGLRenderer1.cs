using System.Numerics;
using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.ImGui.Providers;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Client.Graphics.OpenGL.Objects;
using Engine3.Client.Graphics.Vertex;
using Engine3.Utility;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;
using StbiSharp;
using ShaderType = Engine3.Client.Graphics.ShaderType;

namespace Engine3.Test.Test.Graphics.OpenGL {
	public unsafe class OpenGLRenderer1 : OpenGLRenderer {
		private const string TestShaderName = "Test";

		private OpenGLShader? vertexShader;
		private OpenGLShader? fragmentShader;
		private ProgramPipeline? programPipeline;

		private OpenGLBuffer? vertexBuffer;
		private OpenGLBuffer? indexBuffer;

		private OpenGLImage? image;

		private readonly Camera camera;

		private readonly VertexXyzUvRgb[] vertices = [ new(0, 0.5f, 0, 0.5f, 1, 1, 0, 0), new(-0.5f, -0.5f, 0, 0, 0, 0, 1, 0), new(0.5f, -0.5f, 0, 1, 0, 0, 0, 1), ];
		private readonly uint[] indices = [ 0, 1, 2, ];
		private readonly Assembly gameAssembly;

		public OpenGLRenderer1(GameClient game, OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window, Assembly gameAssembly) : base(graphicsBackend, window) {
			this.gameAssembly = gameAssembly;

			ImGuiBackend = new(window, new DemoWindowImGui()) { ShowDebugUI = true, DebugUIImGui = new DebugUIImGui(game, window), };

			Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i framebufferSize);

			camera = Camera.CreatePerspective((float)framebufferSize.X / framebufferSize.Y, 90, 0.1f, 10);
			camera.Position = new(0, 0, 5f);
		}

		protected override void Setup() {
			base.Setup();

			vertexShader = ResourceProvider.CreateShader("Test Vertex Shader", $"{TestShaderName}UVs", ShaderType.Vertex, gameAssembly);
			fragmentShader = ResourceProvider.CreateShader("Test Fragment Shader", $"{TestShaderName}UVs", ShaderType.Fragment, gameAssembly);
			programPipeline = ResourceProvider.CreateProgramPipeline("Test Program Pipeline", vertexShader, fragmentShader);

			// ResourceProvider.EnqueueDestroy(vertexShader); // TODO RenderDoc gives an error when i destroy these but it renders fine. i think i'm doing something wrong?
			// ResourceProvider.EnqueueDestroy(fragmentShader);

			vertexBuffer = ResourceProvider.CreateBuffer("Test Vertex Buffer", BufferStorageMask.DynamicStorageBit, (ulong)(sizeof(VertexXyzUvRgb) * vertices.Length));
			vertexBuffer.Copy(vertices);

			indexBuffer = ResourceProvider.CreateBuffer("Test Index Buffer", BufferStorageMask.DynamicStorageBit, (ulong)(sizeof(uint) * indices.Length));
			indexBuffer.Copy(indices);

			using (StbiImage stbiImage = AssetH.LoadImage("Test.64x64", "png", 4, gameAssembly)) {
				image = ResourceProvider.CreateImage("Test Image");
				image.Copy(stbiImage);
			}

			GL.Enable(EnableCap.DepthTest);
			GL.Disable(EnableCap.CullFace);
		}

		protected override void DrawFrame() {
			if (vertexBuffer == null || indexBuffer == null || programPipeline == null || image == null) { throw new NullReferenceException(); }

			// TODO gl graphics pipeline class? bind program pipeline -> grants access to shaders -> bind buffers -> draw ?
			GL.BindProgramPipeline(programPipeline.ProgramPipelineHandle.Handle);

			// camera.YawDegrees += 0.5f;

			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, (int)vertexBuffer.BufferHandle);
			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, (int)indexBuffer.BufferHandle);

			GL.BindTexture(TextureTarget.Texture2d, (int)image.TextureHandle);

			GL.DrawArrays(PrimitiveType.Triangles, 0, indices.Length);
		}

		protected override void CopyBuffers(float delta) {
			if (vertexShader == null) { throw new NullReferenceException(); }

			vertexShader.SetUniform("projection", camera.Projection);
			vertexShader.SetUniform("view", camera.View);
			vertexShader.SetUniform("model", Matrix4x4.CreateRotationY(float.Lerp(OpenGLTest.PrevCubeRotation, OpenGLTest.CubeRotation, delta) * float.DegreesToRadians(90f)));
		}

		protected override void Cleanup() {
			//

			base.Cleanup();
		}
	}
}