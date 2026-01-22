using System.Reflection;
using Engine3.Graphics;
using Engine3.Graphics.Vulkan;
using Engine3.Test.Graphics.Test;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Graphics {
	public unsafe class VkRenderer2 : VkRenderer {
		private GraphicsPipeline? graphicsPipeline;

		private VkBuffer? vertexBuffer;
		private VkDeviceMemory? vertexBufferMemory;

		private readonly TestVertex[] vertices = [ new(0, 0.5f, 0, 1, 0, 0), new(-0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, -0.5f, 0, 0, 0, 1), ];
		private readonly Assembly shaderAssembly;

		public VkRenderer2(VkWindow window, byte maxFramesInFlight, Assembly shaderAssembly) : base(window, maxFramesInFlight) => this.shaderAssembly = shaderAssembly;

		public override void Setup() {
			ShaderModule vertexShaderModule = new(LogicalDevice, "HLSL.Test", ShaderLanguage.Hlsl, ShaderType.Vertex, shaderAssembly);
			ShaderModule fragmentShaderModule = new(LogicalDevice, "HLSL.Test", ShaderLanguage.Hlsl, ShaderType.Fragment, shaderAssembly);
			ShaderStageInfo[] shaderCreateInfos = [
					new(LogicalDevice, vertexShaderModule.VkShaderModule, VkShaderStageFlagBits.ShaderStageVertexBit), new(LogicalDevice, fragmentShaderModule.VkShaderModule, VkShaderStageFlagBits.ShaderStageFragmentBit),
			];

			GraphicsPipeline.Builder builder = new(LogicalDevice, SwapChain, shaderCreateInfos, TestVertex.GetAttributeDescriptions(), TestVertex.GetBindingDescriptions());
			graphicsPipeline = builder.MakePipeline();

			vertexShaderModule.Destroy();
			fragmentShaderModule.Destroy();

			VkH.CreateBufferAndMemory(PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit,
				(ulong)(sizeof(TestVertex) * vertices.Length), out VkBuffer vertexBuffer, out VkDeviceMemory vertexBufferMemory);

			VkH.MapMemory(LogicalDevice, vertexBufferMemory, vertices);

			this.vertexBuffer = vertexBuffer;
			this.vertexBufferMemory = vertexBufferMemory;
		}

		protected override void RecordCommandBuffer(VkCommandBuffer graphicsCommandBuffer, float delta) {
			if (this.graphicsPipeline is not { } graphicsPipeline) { return; }
			if (this.vertexBuffer is not { } vertexBuffer) { return; }

			graphicsPipeline.CmdBind(graphicsCommandBuffer);

			VkH.CmdSetViewport(graphicsCommandBuffer, 0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			VkH.CmdSetScissor(graphicsCommandBuffer, SwapChain.Extent, new(0, 0));

			VkH.CmdBindVertexBuffer(graphicsCommandBuffer, vertexBuffer, 0);

			Vk.CmdDraw(graphicsCommandBuffer, (uint)vertices.Length, 1, 0, 0);
		}

		protected override void Destroy() {
			Vk.DeviceWaitIdle(LogicalDevice);

			if (this.vertexBuffer is { } vertexBuffer) { Vk.DestroyBuffer(LogicalDevice, vertexBuffer, null); }
			if (this.vertexBufferMemory is { } vertexBufferMemory) { Vk.FreeMemory(LogicalDevice, vertexBufferMemory, null); }

			graphicsPipeline?.Destroy();

			base.Destroy();
		}
	}
}