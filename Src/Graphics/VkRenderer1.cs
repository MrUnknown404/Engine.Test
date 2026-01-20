using System.Numerics;
using System.Reflection;
using Engine3.Exceptions;
using Engine3.Graphics;
using Engine3.Graphics.Test;
using Engine3.Graphics.Vulkan;
using JetBrains.Annotations;
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
			descriptorSetLayout = CreateDescriptorSetLayout(LogicalDevice, 0, VkShaderStageFlagBits.ShaderStageVertexBit);

			CreateGraphicsPipeline(LogicalDevice, SwapChain.ImageFormat, [ descriptorSetLayout.Value, ], "GLSL.Test", ShaderLanguage.Glsl, assembly, out VkPipeline graphicsPipeline, out VkPipelineLayout pipelineLayout);
			this.graphicsPipeline = graphicsPipeline;
			this.pipelineLayout = pipelineLayout;

			// vertex buffers
			CreateBufferAndMemoryUsingStagingBuffer(PhysicalDevice, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, vertices, VkBufferUsageFlagBits.BufferUsageVertexBufferBit, out VkBuffer vertexBuffer,
				out VkDeviceMemory vertexBufferMemory);

			this.vertexBuffer = vertexBuffer;
			this.vertexBufferMemory = vertexBufferMemory;

			// index buffers
			CreateBufferAndMemoryUsingStagingBuffer(PhysicalDevice, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, indices, VkBufferUsageFlagBits.BufferUsageIndexBufferBit, out VkBuffer indexBuffer,
				out VkDeviceMemory indexBufferMemory);

			this.indexBuffer = indexBuffer;
			this.indexBufferMemory = indexBufferMemory;

			uint uniformBufferSize = TestUniformBufferObject.Size;

			// uniform buffers
			fixed (void** uniformBufferMapped = uniformBuffersMapped) {
				for (int i = 0; i < MaxFramesInFlight; i++) {
					VkH.CreateBufferAndMemory(PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageUniformBufferBit,
						VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, uniformBufferSize, out uniformBuffers[i], out uniformBuffersMemory[i]);

					Vk.MapMemory(LogicalDevice, uniformBuffersMemory[i], 0, uniformBufferSize, 0, &uniformBufferMapped[i]); // TODO 2
				}
			}

			// descriptor pool
			descriptorPool = CreateDescriptorPool(LogicalDevice, MaxFramesInFlight);
			CreateDescriptorSets(LogicalDevice, descriptorPool.Value, descriptorSetLayout.Value, MaxFramesInFlight, descriptorSets);

			for (int i = 0; i < MaxFramesInFlight; i++) {
				VkDescriptorBufferInfo descriptorBufferInfo = new() { buffer = uniformBuffers[i], offset = 0, range = uniformBufferSize, };
				VkWriteDescriptorSet writeDescriptorSet = new() {
						dstSet = descriptorSets[i],
						dstBinding = 0,
						dstArrayElement = 0,
						descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer,
						descriptorCount = 1,
						pBufferInfo = &descriptorBufferInfo,
						pImageInfo = null,
						pTexelBufferView = null,
				};

				Vk.UpdateDescriptorSets(LogicalDevice, 1, &writeDescriptorSet, 0, null);
			}

			return;

			static void CreateBufferAndMemoryUsingStagingBuffer<T>(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferPool, VkQueue transferQueue, T[] bufferData, VkBufferUsageFlagBits bufferUsage,
				out VkBuffer buffer, out VkDeviceMemory bufferMemory) where T : unmanaged {
				ulong bufferSize = (ulong)(sizeof(T) * bufferData.Length);

				VkH.CreateBufferAndMemory(physicalDevice, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
					VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize, out VkBuffer stagingBuffer, out VkDeviceMemory stagingBufferMemory);

				VkH.MapMemory(logicalDevice, stagingBufferMemory, bufferData);

				VkH.CreateBufferAndMemory(physicalDevice, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferDstBit | bufferUsage, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, bufferSize, out buffer,
					out bufferMemory);

				VkH.CopyBuffer(logicalDevice, transferQueue, transferPool, stagingBuffer, buffer, bufferSize);

				Vk.DestroyBuffer(logicalDevice, stagingBuffer, null);
				Vk.FreeMemory(logicalDevice, stagingBufferMemory, null);
			}
		}

		[MustUseReturnValue]
		private static VkDescriptorSetLayout CreateDescriptorSetLayout(VkDevice logicalDevice, uint binding, VkShaderStageFlagBits shaderStageFlags) {
			VkDescriptorSetLayoutBinding uboLayoutBinding = new() { binding = binding, descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, stageFlags = shaderStageFlags, };
			VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new() { bindingCount = 1, pBindings = &uboLayoutBinding, };

			VkDescriptorSetLayout layout;
			return Vk.CreateDescriptorSetLayout(logicalDevice, &descriptorSetLayoutCreateInfo, null, &layout) != VkResult.Success ? throw new VulkanException("Failed to create descriptor set layout") : layout;
		}

		[MustUseReturnValue]
		private static VkDescriptorPool CreateDescriptorPool(VkDevice logicalDevice, uint maxFramesInFlight) {
			VkDescriptorPoolSize descriptorPoolSize = new() { descriptorCount = maxFramesInFlight, };
			VkDescriptorPoolCreateInfo descriptorPoolCreateInfo = new() { poolSizeCount = 1, pPoolSizes = &descriptorPoolSize, maxSets = maxFramesInFlight, };

			VkDescriptorPool descriptorPool;
			return Vk.CreateDescriptorPool(logicalDevice, &descriptorPoolCreateInfo, null, &descriptorPool) != VkResult.Success ? throw new VulkanException("Failed to create descriptor pool") : descriptorPool;
		}

		private static void CreateDescriptorSets(VkDevice logicalDevice, VkDescriptorPool descriptorPool, VkDescriptorSetLayout descriptorSetLayout, uint maxFramesInFlight, VkDescriptorSet[] descriptorSets) {
			VkDescriptorSetLayout[] layouts = new VkDescriptorSetLayout[maxFramesInFlight];
			for (int i = 0; i < maxFramesInFlight; i++) { layouts[i] = descriptorSetLayout; }

			fixed (VkDescriptorSetLayout* layoutsPtr = layouts) {
				fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
					VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = descriptorPool, descriptorSetCount = maxFramesInFlight, pSetLayouts = layoutsPtr, };
					if (Vk.AllocateDescriptorSets(logicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr) != VkResult.Success) { throw new VulkanException("Failed to allocation descriptor sets"); }
				}
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
			testUniformBufferObject.Model = Matrix4x4.CreateRotationX(FrameCount / 1000f * MathH.ToRadians(90f)); // TODO FrameCount/N is wrong. fix

			byte[] data = testUniformBufferObject.CollectBytes();
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[CurrentFrame], (ulong)data.Length, (ulong)data.Length); }
		}

		protected override void DrawFrame(VkPipeline graphicsPipeline, VkCommandBuffer graphicsCommandBuffer, float delta) {
			if (this.vertexBuffer is not { } vertexBuffer) { return; }
			if (this.indexBuffer is not { } indexBuffer) { return; }
			if (this.pipelineLayout is not { } pipelineLayout) { return; }

			Vk.CmdBindPipeline(graphicsCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, graphicsPipeline);

			VkViewport viewport = new() { x = 0, y = 0, width = SwapChain.Extent.width, height = SwapChain.Extent.height, minDepth = 0, maxDepth = 1, };
			VkRect2D scissor = new() { offset = new(0, 0), extent = SwapChain.Extent, };
			Vk.CmdSetViewport(graphicsCommandBuffer, 0, 1, &viewport);
			Vk.CmdSetScissor(graphicsCommandBuffer, 0, 1, &scissor);

			VkBuffer[] vertexBuffers = [ vertexBuffer, ];
			ulong[] offsets = [ 0, ];

			fixed (VkBuffer* vertexBuffersPtr = vertexBuffers) {
				fixed (ulong* offsetsPtr = offsets) {
					Vk.CmdBindVertexBuffers(graphicsCommandBuffer, 0, 1, vertexBuffersPtr, offsetsPtr); // TODO 2
				}
			}

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
				Vk.FreeMemory(LogicalDevice, uniformBufferMemory, null);
				// Vk.UnmapMemory(LogicalDevice, uniformBufferMemory); // TODO do i need to call this?
			}

			if (this.descriptorPool is { } descriptorPool) { Vk.DestroyDescriptorPool(LogicalDevice, descriptorPool, null); }
			if (this.descriptorSetLayout is { } descriptorSetLayout) { Vk.DestroyDescriptorSetLayout(LogicalDevice, descriptorSetLayout, null); }

			if (this.pipelineLayout is { } pipelineLayout) { Vk.DestroyPipelineLayout(LogicalDevice, pipelineLayout, null); }
			if (this.graphicsPipeline is { } graphicsPipeline) { Vk.DestroyPipeline(LogicalDevice, graphicsPipeline, null); }

			base.Cleanup();
		}
	}
}