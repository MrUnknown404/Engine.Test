using System.Reflection;
using Engine3.Graphics;
using Engine3.Graphics.Vulkan;
using Engine3.Graphics.Vulkan.Objects;
using Engine3.Test.Graphics.Test;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Graphics.Vulkan {
	public unsafe class VkRenderer2 : VkRenderer {
		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private VkBufferObject? vertexBuffer;

		private readonly TestVertex[] vertices = [ new(0, 0.5f, 0, 1, 0, 0), new(-0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, -0.5f, 0, 0, 0, 1), ];
		private readonly Assembly shaderAssembly;

		public VkRenderer2(GameClient gameClient, VkWindow window, Assembly shaderAssembly) : base(gameClient, window) => this.shaderAssembly = shaderAssembly;

		public override void Setup() {
			VkShaderObject vertexShader = new("Test Vertex Shader", LogicalDevice, TestShaderName, ShaderLanguage.Hlsl, ShaderType.Vertex, shaderAssembly);
			VkShaderObject fragmentShader = new("Test Fragment Shader", LogicalDevice, TestShaderName, ShaderLanguage.Hlsl, ShaderType.Fragment, shaderAssembly);

			GraphicsPipeline.Builder builder = new("Test Graphics Pipeline", LogicalDevice, SwapChain, [ vertexShader, fragmentShader, ], TestVertex.GetAttributeDescriptions(), TestVertex.GetBindingDescriptions());
			graphicsPipeline = builder.MakePipeline();

			vertexShader.Destroy();
			fragmentShader.Destroy();

			vertexBuffer = new("Test Vertex Buffer", PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, (ulong)(sizeof(TestVertex) * vertices.Length));

			vertexBuffer.Copy(vertices);
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			if (this.graphicsPipeline is not { } graphicsPipeline) { return; }
			if (this.vertexBuffer is not { } vertexBuffer) { return; }

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			graphicsCommandBuffer.CmdBindVertexBuffer(vertexBuffer.Buffer, 0);

			graphicsCommandBuffer.CmdDraw((uint)vertices.Length);
		}

		protected override void Cleanup() {
			vertexBuffer?.Destroy();

			graphicsPipeline?.Destroy();
		}
	}
}