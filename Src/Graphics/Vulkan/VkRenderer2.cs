using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Graphics.Test;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Graphics.Vulkan {
	public unsafe class VkRenderer2 : VkRenderer {
		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private VkBufferObject? vertexBuffer;

		private readonly TestVertex[] vertices = [ new(0, 0.5f, 0, 1, 0, 0), new(-0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, -0.5f, 0, 0, 0, 1), ];
		private readonly Assembly gameAssembly;

		public VkRenderer2(VulkanGraphicsBackend graphicsBackend, VkWindow window, Assembly gameAssembly) : base(graphicsBackend, window) => this.gameAssembly = gameAssembly;

		public override void Setup() {
			VkShaderObject vertexShader = new("Test Vertex Shader", LogicalDevice, TestShaderName, ShaderLanguage.Hlsl, ShaderType.Vertex, gameAssembly);
			VkShaderObject fragmentShader = new("Test Fragment Shader", LogicalDevice, TestShaderName, ShaderLanguage.Hlsl, ShaderType.Fragment, gameAssembly);

			graphicsPipeline = new(PhysicalDevice, LogicalDevice,
				new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], TestVertex.GetAttributeDescriptions(), TestVertex.GetBindingDescriptions()));

			vertexShader.Destroy();
			fragmentShader.Destroy();

			vertexBuffer = new("Test Vertex Buffer", (ulong)(sizeof(TestVertex) * vertices.Length), PhysicalDeviceMemoryProperties, LogicalDevice, VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

			vertexBuffer.Copy(vertices);
		}

		protected override void RecordCommandBuffer(GraphicsCommandBufferObject graphicsCommandBuffer, float delta) {
			if (this.graphicsPipeline is not { } graphicsPipeline) { return; }
			if (this.vertexBuffer is not { } vertexBuffer) { return; }

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			graphicsCommandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);

			graphicsCommandBuffer.CmdDraw((uint)vertices.Length);
		}

		protected override void Cleanup() {
			vertexBuffer?.Destroy();

			graphicsPipeline?.Destroy();
		}
	}
}