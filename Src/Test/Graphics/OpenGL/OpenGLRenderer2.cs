using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Client.Graphics.OpenGL.Objects;
using Engine3.Test.Core.Graphics;
using OpenTK.Graphics.OpenGL;
using ShaderType = Engine3.Client.Graphics.ShaderType;

namespace Engine3.Test.Test.Graphics.OpenGL {
	public unsafe class OpenGLRenderer2 : OpenGLRenderer {
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

		public override void Setup() {
			base.Setup();

			vertexShader = ResourceProvider.CreateShader("Test Vertex Shader", TestShaderName, ShaderType.Vertex, gameAssembly);
			fragmentShader = ResourceProvider.CreateShader("Test Fragment Shader", TestShaderName, ShaderType.Fragment, gameAssembly);
			programPipeline = ResourceProvider.CreateProgramPipeline("Test Program Pipeline", vertexShader, fragmentShader);

			// ResourceProvider.EnqueueDestroy(vertexShader);
			// ResourceProvider.EnqueueDestroy(fragmentShader);

			vertexBuffer = ResourceProvider.CreateBuffer("Test Vertex Buffer", BufferStorageMask.DynamicStorageBit, (ulong)(sizeof(VertexXyzRgb) * vertices.Length));
			vertexBuffer.Copy(vertices);

			indexBuffer = ResourceProvider.CreateBuffer("Test Index Buffer", BufferStorageMask.DynamicStorageBit, (ulong)(sizeof(uint) * indices.Length));
			indexBuffer.Copy(indices);
		}

		protected override void DrawFrame(float delta) {
			if (vertexBuffer == null || indexBuffer == null || programPipeline == null) { throw new NullReferenceException(); }

			GL.BindProgramPipeline(programPipeline.ProgramPipelineHandle.Handle);

			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, (int)vertexBuffer.BufferHandle);
			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, (int)indexBuffer.BufferHandle);

			GL.DrawArrays(PrimitiveType.Triangles, 0, indices.Length);
		}

		protected override void Cleanup() {
			//

			base.Cleanup();
		}
	}
}