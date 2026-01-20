using System.Numerics;
using System.Reflection;
using Engine3.Graphics;
using Engine3.Graphics.Test;
using Engine3.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine3.Test.Graphics {
	public unsafe class VkRenderer1 : VkRenderer {
		private VkPipelineLayout? pipelineLayout; // i'm pretty sure these are gonna need to be reworked later
		private VkPipeline? graphicsPipeline;

		private VkBuffer? vertexBuffer;
		private VkBuffer? indexBuffer;
		private VkDeviceMemory? vertexBufferMemory;
		private VkDeviceMemory? indexBufferMemory;

		private readonly VkBuffer[] uniformBuffers;
		private readonly VkDeviceMemory[] uniformBuffersMemory;
		private readonly void*[] uniformBuffersMapped;

		private VkDescriptorSetLayout? descriptorSetLayout;
		private VkDescriptorPool? descriptorPool;
		private readonly VkDescriptorSet[] descriptorSets;

		private readonly TestUniformBufferObject testUniformBufferObject = new();

		private readonly TestVertex[] vertices = [
				// new(0, -0.5f, 1, 0, 0), new(0.5f, 0.5f, 0, 1, 0), new(-0.5f, 0.5f, 0, 0, 1),
				new(-0.5f, -0.5f, 1, 0, 0), new(0.5f, -0.5f, 0, 1, 0), new(0.5f, 0.5f, 0, 0, 1), new(-0.5f, 0.5f, 1, 1, 1),
		];
		private readonly uint[] indices = [ 0, 1, 2, 2, 3, 0, ];
		private readonly Assembly assembly;

		private VkDescriptorSet CurrentDescriptorSet => descriptorSets[CurrentFrame];

		public VkRenderer1(GameClient gameClient, VkWindow window, VkCommandPool graphicsCommandPool, VkCommandPool transferCommandPool) : base(gameClient, window, graphicsCommandPool, transferCommandPool) {
			assembly = gameClient.Assembly;

			uniformBuffers = new VkBuffer[MaxFramesInFlight];
			uniformBuffersMemory = new VkDeviceMemory[MaxFramesInFlight];
			uniformBuffersMapped = new void*[MaxFramesInFlight];
			descriptorSets = new VkDescriptorSet[MaxFramesInFlight];
		}

		public override void Setup() {
			descriptorSetLayout = VkH.CreateDescriptorSetLayout(LogicalDevice, 0, VkShaderStageFlagBits.ShaderStageVertexBit);

			CreateGraphicsPipeline(LogicalDevice, SwapChain.ImageFormat, [ descriptorSetLayout.Value, ], "GLSL.Test", ShaderLanguage.Glsl, assembly, out VkPipeline graphicsPipeline, out VkPipelineLayout pipelineLayout);
			this.graphicsPipeline = graphicsPipeline;
			this.pipelineLayout = pipelineLayout;

			CreateBufferUsingStagingBuffer(PhysicalDevice, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, vertices, VkBufferUsageFlagBits.BufferUsageVertexBufferBit, out vertexBuffer, out vertexBufferMemory);
			CreateBufferUsingStagingBuffer(PhysicalDevice, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, indices, VkBufferUsageFlagBits.BufferUsageIndexBufferBit, out indexBuffer, out indexBufferMemory);

			uint uniformBufferSize = TestUniformBufferObject.Size;
			CreateUniformBuffers();

			descriptorPool = VkH.CreateDescriptorPool(LogicalDevice, MaxFramesInFlight);
			VkH.CreateDescriptorSets(LogicalDevice, descriptorPool.Value, descriptorSetLayout.Value, descriptorSets, MaxFramesInFlight, uniformBufferSize, uniformBuffers);

			return;

			void CreateUniformBuffers() {
				fixed (void** uniformBufferMapped = uniformBuffersMapped) {
					for (int i = 0; i < MaxFramesInFlight; i++) {
						VkH.CreateBufferAndMemory(PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageUniformBufferBit,
							VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, uniformBufferSize, out uniformBuffers[i], out uniformBuffersMemory[i]);

						Vk.MapMemory(LogicalDevice, uniformBuffersMemory[i], 0, uniformBufferSize, 0, &uniformBufferMapped[i]); // TODO 2
					}
				}
			}

			static void CreateBufferUsingStagingBuffer<T>(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferPool, VkQueue transferQueue, T[] bufferData, VkBufferUsageFlagBits bufferUsage,
				out VkBuffer? buffer, out VkDeviceMemory? bufferMemory) where T : unmanaged {
				VkH.CreateBufferUsingStagingBuffer(physicalDevice, logicalDevice, transferPool, transferQueue, bufferData, bufferUsage, out VkBuffer vertexBuffer, out VkDeviceMemory vertexBufferMemory);

				buffer = vertexBuffer;
				bufferMemory = vertexBufferMemory;
			}
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
				UpdateUniformBuffer(delta);
				EndFrame(CurrentGraphicsCommandBuffer, swapChainImageIndex);
				PresentFrame(swapChainImageIndex);
			}
		}

		protected override void UpdateUniformBuffer(float delta) {
			testUniformBufferObject.Projection = Matrix4x4.CreatePerspective(MathH.ToRadians(45f), (float)SwapChain.Extent.width / SwapChain.Extent.height, 0.5f, 10); // TODO invert y? ubo.proj[1][1] *= -1;
			testUniformBufferObject.View = Matrix4x4.CreateLookAt(new(2, 2, 2), new(0, 0, 0), Vector3.UnitZ);
			testUniformBufferObject.Model = Matrix4x4.CreateRotationX(FrameCount / 1000f * MathH.ToRadians(90f)); // TODO currently affected by frame rate

			byte[] data = testUniformBufferObject.CollectBytes();
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[CurrentFrame], (ulong)data.Length, (ulong)data.Length); }
		}

		protected override void DrawFrame(VkPipeline graphicsPipeline, VkCommandBuffer graphicsCommandBuffer, float delta) {
			if (this.vertexBuffer is not { } vertexBuffer) { return; }
			if (this.indexBuffer is not { } indexBuffer) { return; }
			if (this.pipelineLayout is not { } pipelineLayout) { return; }

			Vk.CmdBindPipeline(graphicsCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, graphicsPipeline);

			VkH.CmdSetViewport(graphicsCommandBuffer, 0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			VkH.CmdSetScissor(graphicsCommandBuffer, SwapChain.Extent, new(0, 0));

			VkH.CmdBindVertexBuffer(graphicsCommandBuffer, vertexBuffer, 0);
			Vk.CmdBindIndexBuffer(graphicsCommandBuffer, indexBuffer, 0, VkIndexType.IndexTypeUint32);

			VkDescriptorSet descriptorSet = CurrentDescriptorSet;
			Vk.CmdBindDescriptorSets(graphicsCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, pipelineLayout, 0, 1, &descriptorSet, 0, null); // TODO 2

			Vk.CmdDrawIndexed(graphicsCommandBuffer, (uint)indices.Length, 1, 0, 0, 0);
		}

		protected override void Cleanup() {
			Vk.DeviceWaitIdle(LogicalDevice);

			if (this.vertexBuffer is { } vertexBuffer) { Vk.DestroyBuffer(LogicalDevice, vertexBuffer, null); }
			if (this.vertexBufferMemory is { } vertexBufferMemory) { Vk.FreeMemory(LogicalDevice, vertexBufferMemory, null); }

			if (this.indexBuffer is { } indexBuffer) { Vk.DestroyBuffer(LogicalDevice, indexBuffer, null); }
			if (this.indexBufferMemory is { } indexBufferMemory) { Vk.FreeMemory(LogicalDevice, indexBufferMemory, null); }

			foreach (VkBuffer uniformBuffer in uniformBuffers) { Vk.DestroyBuffer(LogicalDevice, uniformBuffer, null); }

			foreach (VkDeviceMemory uniformBufferMemory in uniformBuffersMemory) {
				//Vk.UnmapMemory(LogicalDevice, uniformBufferMemory); // i don't think i need to call this?
				Vk.FreeMemory(LogicalDevice, uniformBufferMemory, null);
			}

			if (this.descriptorPool is { } descriptorPool) { Vk.DestroyDescriptorPool(LogicalDevice, descriptorPool, null); }
			if (this.descriptorSetLayout is { } descriptorSetLayout) { Vk.DestroyDescriptorSetLayout(LogicalDevice, descriptorSetLayout, null); }

			if (this.pipelineLayout is { } pipelineLayout) { Vk.DestroyPipelineLayout(LogicalDevice, pipelineLayout, null); }
			if (this.graphicsPipeline is { } graphicsPipeline) { Vk.DestroyPipeline(LogicalDevice, graphicsPipeline, null); }

			base.Cleanup();
		}
	}
}