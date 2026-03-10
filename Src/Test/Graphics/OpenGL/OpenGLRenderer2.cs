using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Client.Graphics.OpenGL.Objects;
using Engine3.Client.Graphics.OpenGL.Renderers;
using Engine3.Client.Graphics.VertexLayouts;
using OpenTK.Graphics.OpenGL;
using ShaderType = Engine3.Client.Graphics.ShaderType;

namespace Engine3.Test.Test.Graphics.OpenGL {
	public unsafe class OpenGLRenderer2 : OpenGLRendererBase {
		private const string TestShaderName = "Test";

		private OpenGLShader? vertexShader;
		private OpenGLShader? fragmentShader;
		private ProgramPipeline? programPipeline;

		private OpenGLBuffer? vertexBuffer;
		private OpenGLBuffer? indexBuffer;

		private readonly VertexXyzRgb[] vertices = [ new(0f, -0.5f, 0, 1, 0, 0), new(0.5f, 0.5f, 0, 0, 1, 0), new(-0.5f, 0.5f, 0, 0, 0, 1), ];
		private readonly uint[] indices = [ 0, 1, 2, ];
		private readonly Assembly gameAssembly;

		public OpenGLRenderer2(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window, Assembly gameAssembly) : base(graphicsBackend, window) => this.gameAssembly = gameAssembly;

		protected override void Setup() {
			base.Setup();

			vertexShader = GraphicsResourceProvider.CreateShader("Test Vertex Shader", TestShaderName, ShaderType.Vertex, gameAssembly);
			fragmentShader = GraphicsResourceProvider.CreateShader("Test Fragment Shader", TestShaderName, ShaderType.Fragment, gameAssembly);
			programPipeline = GraphicsResourceProvider.CreateProgramPipeline("Test Program Pipeline", vertexShader, fragmentShader);

			// GraphicsResourceProvider.EnqueueDestroy(vertexShader);
			// GraphicsResourceProvider.EnqueueDestroy(fragmentShader);

			vertexBuffer = GraphicsResourceProvider.CreateBuffer("Test Vertex Buffer", BufferStorageMask.DynamicStorageBit, (ulong)(sizeof(VertexXyzRgb) * vertices.Length));
			vertexBuffer.Copy(vertices);

			indexBuffer = GraphicsResourceProvider.CreateBuffer("Test Index Buffer", BufferStorageMask.DynamicStorageBit, (ulong)(sizeof(uint) * indices.Length));
			indexBuffer.Copy(indices);
		}

		protected override void DrawFrame() {
			if (vertexBuffer == null || indexBuffer == null || programPipeline == null) { throw new NullReferenceException(); }

			GL.BindProgramPipeline(programPipeline.ProgramPipelineHandle.Handle);

			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, (int)vertexBuffer.BufferHandle);
			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, (int)indexBuffer.BufferHandle);

			GL.DrawArrays(PrimitiveType.Triangles, 0, indices.Length);
		}

		protected override void CopyBuffers(float delta) { }

		protected override void Cleanup() {
			//

			base.Cleanup();
		}
	}
}