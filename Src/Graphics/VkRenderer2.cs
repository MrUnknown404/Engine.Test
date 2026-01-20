using System.Reflection;
using Engine3.Graphics;
using Engine3.Graphics.Test;
using Engine3.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Graphics {
	public unsafe class VkRenderer2 : VkRenderer {
		private VkPipelineLayout? pipelineLayout; // i'm pretty sure these are gonna need to be reworked later
		private VkPipeline? graphicsPipeline;

		private VkBuffer? vertexBuffer;
		private VkDeviceMemory? vertexBufferMemory;

		private readonly TestVertex[] vertices = [ new(0, 0.5f, 1, 0, 0), new(-0.5f, -0.5f, 0, 1, 0), new(0.5f, -0.5f, 0, 0, 1), ];
		private readonly Assembly assembly;

		public VkRenderer2(GameClient gameClient, VkWindow window, VkCommandPool graphicsCommandPool, VkCommandPool transferCommandPool) : base(gameClient, window, graphicsCommandPool, transferCommandPool) =>
				assembly = gameClient.Assembly;

		public override void Setup() {
			CreateGraphicsPipeline(LogicalDevice, SwapChain.ImageFormat, null, "HLSL.Test", ShaderLanguage.Hlsl, assembly, out VkPipeline graphicsPipeline, out VkPipelineLayout pipelineLayout);
			this.graphicsPipeline = graphicsPipeline;
			this.pipelineLayout = pipelineLayout;

			VkH.CreateBufferAndMemory(PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit,
				(ulong)(sizeof(TestVertex) * vertices.Length), out VkBuffer vertexBuffer, out VkDeviceMemory vertexBufferMemory);

			VkH.MapMemory(LogicalDevice, vertexBufferMemory, vertices);

			this.vertexBuffer = vertexBuffer;
			this.vertexBufferMemory = vertexBufferMemory;
		}

		/*
		   Wait for the previous frame to finish
		   Acquire an image from the swap chain
		   Record a command buffer which draws the scene onto that image
		   Submit the recorded command buffer
		   Present the swap chain image
		 */
		protected override void DrawFrame(float delta) {
			if (!CanRender) { return; }
			if (this.graphicsPipeline is not { } graphicsPipeline) { return; }

			// TODO if i want to move BeginFrame/EndFrame/PresentFrame out of this method, i'll need to redo things

			if (BeginFrame(CurrentGraphicsCommandBuffer, out uint swapChainImageIndex)) {
				DrawFrame(graphicsPipeline, CurrentGraphicsCommandBuffer, delta);
				EndFrame(CurrentGraphicsCommandBuffer, swapChainImageIndex);
				PresentFrame(swapChainImageIndex);
			}
		}

		protected override void DrawFrame(VkPipeline graphicsPipeline, VkCommandBuffer graphicsCommandBuffer, float delta) {
			if (this.vertexBuffer is not { } vertexBuffer) { return; }

			Vk.CmdBindPipeline(graphicsCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, graphicsPipeline);

			VkH.CmdSetViewport(graphicsCommandBuffer, 0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			VkH.CmdSetScissor(graphicsCommandBuffer, SwapChain.Extent, new(0, 0));

			VkH.CmdBindVertexBuffer(graphicsCommandBuffer, vertexBuffer, 0);

			Vk.CmdDraw(graphicsCommandBuffer, (uint)vertices.Length, 1, 0, 0);
		}

		protected override void Cleanup() {
			Vk.DeviceWaitIdle(LogicalDevice);

			if (this.vertexBuffer is { } vertexBuffer) { Vk.DestroyBuffer(LogicalDevice, vertexBuffer, null); }
			if (this.vertexBufferMemory is { } vertexBufferMemory) { Vk.FreeMemory(LogicalDevice, vertexBufferMemory, null); }

			if (this.pipelineLayout is { } pipelineLayout) { Vk.DestroyPipelineLayout(LogicalDevice, pipelineLayout, null); }
			if (this.graphicsPipeline is { } graphicsPipeline) { Vk.DestroyPipeline(LogicalDevice, graphicsPipeline, null); }

			base.Cleanup();
		}
	}
}