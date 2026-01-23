using System.Reflection;
using Engine3.Graphics.OpenGL;
using Engine3.Graphics.OpenGL.Objects;
using Engine3.Test.Graphics.Test;
using OpenTK.Graphics.OpenGL;
using ShaderType = Engine3.Graphics.ShaderType;

namespace Engine3.Test.Graphics.OpenGL {
	public unsafe class GlRenderer2 : GlRenderer {
		private const string TestShaderName = "Test";

		public override bool IsWindowValid => !Window.WasDestroyed;

		private GlBufferObject? vertexBuffer;
		private GlBufferObject? indexBuffer;

		private GlShaderObject? vertexShader;
		private GlShaderObject? fragmentShader;
		private GlProgramPipeline? programPipeline;

		private readonly TestVertex[] vertices = [ new(0f, -0.5f, 0, 1, 0, 0), new(0.5f, 0.5f, 0, 0, 1, 0), new(-0.5f, 0.5f, 0, 0, 0, 1), ];
		private readonly uint[] indices = [ 0, 1, 2, 2, 3, 0, ];
		private readonly Assembly shaderAssembly;

		public GlRenderer2(GlWindow window, Assembly shaderAssembly) : base(window) => this.shaderAssembly = shaderAssembly;

		public override void Setup() {
			base.Setup();

			vertexShader = new("Test Vertex Shader", TestShaderName, ShaderType.Vertex, shaderAssembly);
			fragmentShader = new("Test Fragment Shader", TestShaderName, ShaderType.Fragment, shaderAssembly);
			programPipeline = new("Test Program Pipeline", vertexShader, fragmentShader);

			vertexBuffer = new("Test Vertex Buffer", BufferStorageMask.DynamicStorageBit, sizeof(TestVertex) * vertices.Length);
			vertexBuffer.Copy(vertices);

			indexBuffer = new("Test Index Buffer", BufferStorageMask.DynamicStorageBit, sizeof(uint) * indices.Length);
			indexBuffer.Copy(indices);
		}

		protected override void Draw(float delta) {
			if (this.vertexBuffer is not { } vertexBuffer) { return; }
			if (this.indexBuffer is not { } indexBuffer) { return; }
			if (this.programPipeline is not { } programPipeline) { return; }

			GL.BindProgramPipeline(programPipeline.Handle.Handle);

			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, (int)vertexBuffer.Handle);
			GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, (int)indexBuffer.Handle);

			GL.DrawArrays(PrimitiveType.Triangles, 0, indices.Length);
		}

		protected override void Destroy() {
			programPipeline?.Destroy();
			vertexShader?.Destroy();
			fragmentShader?.Destroy();

			vertexBuffer?.Destroy();
			indexBuffer?.Destroy();
		}
	}
}