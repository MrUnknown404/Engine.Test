using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Test.Graphics.Test;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Test.Graphics.Vulkan {
	public unsafe class VulkanRenderer2 : VulkanRenderer {
		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private VulkanBuffer? vertexBuffer;

		private readonly TestVertex[] vertices = [ new(0, 0.5f, 0, 1, 0, 0), new(-0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, -0.5f, 0, 0, 0, 1), ];
		private readonly Assembly gameAssembly;

		public VulkanRenderer2(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Assembly gameAssembly) : base(graphicsBackend, window) => this.gameAssembly = gameAssembly;

		public override void Setup() {
			base.Setup();

			VulkanShader vertexShader = LogicalGpu.CreateShader("Test Vertex Shader", TestShaderName, ShaderLanguage.Hlsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader("Test Fragment Shader", TestShaderName, ShaderLanguage.Hlsl, ShaderType.Fragment, gameAssembly);

			graphicsPipeline = LogicalGpu.CreateGraphicsPipeline(new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], TestVertex.GetAttributeDescriptions(), TestVertex.GetBindingDescriptions()));

			LogicalGpu.EnqueueDestroy(vertexShader);
			LogicalGpu.EnqueueDestroy(fragmentShader);

			vertexBuffer = LogicalGpu.CreateBuffer("Test Vertex Buffer", VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, (ulong)(sizeof(TestVertex) * vertices.Length));

			vertexBuffer.Copy(vertices);
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			if (vertexBuffer == null || graphicsPipeline == null) { return; }

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(new(0, 0), SwapChain.Extent);

			graphicsCommandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);

			graphicsCommandBuffer.CmdDraw((uint)vertices.Length);
		}
	}
}