using System.Reflection;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Graphics.Vertex;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class WorldRecorder : VulkanRecorderNode {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public World.World? World { private get; set; }

		private readonly GraphicsPipeline chunkGraphicsPipeline;

		private readonly VulkanBuffer chunkVertexBuffer;
		private VulkanBuffer chunkIndexBuffer;
		private readonly DescriptorSets chunkDescriptorSet;

		private readonly PerChunkDataBuffer chunkDataBufferValue = new(1); // TODO offset chunk by this

		private readonly Assembly gameAssembly;
		private readonly LogicalGpu logicalGpu;
		private readonly TransferCommandPool transferCommandPool;

		private bool shouldRegenerateChunk = true;
		private ulong chunkIndicesCount;

		public WorldRecorder(LogicalGpu logicalGpu, TransferCommandPool transferCommandPool, SwapChain swapChain, Assembly gameAssembly, byte maxFramesInFlight, DescriptorBuffers cameraUniformBuffer, VulkanImage image,
			TextureSampler textureSampler) {
			this.gameAssembly = gameAssembly;
			this.logicalGpu = logicalGpu;
			this.transferCommandPool = transferCommandPool;

			CreateBuffers(logicalGpu, transferCommandPool, ChunkMeshBuilder.GetChunkVertices(), out chunkVertexBuffer, out chunkIndexBuffer);

			chunkGraphicsPipeline = CreateGraphicsPipeline(logicalGpu, swapChain, out DescriptorSetLayout chunkLayout);

			DescriptorPool descriptorPool = logicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
				maxFramesInFlight);

			chunkDescriptorSet = descriptorPool.AllocateDescriptorSet(chunkLayout);
			Logger.Debug("Created world descriptor sets");

			chunkDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			chunkDescriptorSet.UpdateDescriptorSet(1, image.ImageView, textureSampler.Sampler);
		}

		private void CreateBuffers(LogicalGpu logicalGpu, TransferCommandPool transferCommandPool, ChunkVertex[] vertices, out VulkanBuffer vertexBuffer, out VulkanBuffer indexBuffer) {
			vertexBuffer = logicalGpu.CreateBuffer("Chunk Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(ChunkVertex) * vertices.Length));

			indexBuffer = logicalGpu.CreateBuffer("Chunk Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, 1);

			transferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(chunkVertexBuffer, vertices), ]);

			Logger.Debug("Created world buffers");
		}

		[MustUseReturnValue]
		private GraphicsPipeline CreateGraphicsPipeline(LogicalGpu logicalGpu, SwapChain swapChain, out DescriptorSetLayout descriptorSetLayout) {
			const string Name = "Chunk";

			VulkanShader vertexShader = logicalGpu.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = logicalGpu.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = logicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 1), //
			]);

			GraphicsPipeline pipeline = logicalGpu.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", swapChain.ImageFormat, [ vertexShader, fragmentShader, ], ChunkVertex.GetAttributeDescriptions(), ChunkVertex.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ], EnableDepthTest = true, EnableDepthWrite = true,
				});

			Logger.Debug("Created world graphics pipeline");

			logicalGpu.EnqueueDestroy(vertexShader);
			logicalGpu.EnqueueDestroy(fragmentShader);

			return pipeline;
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			if (World is null || chunkIndicesCount == 0) { return; }

			commandBuffer.CmdBindGraphicsPipeline(chunkGraphicsPipeline.Pipeline);

			commandBuffer.CmdBindDescriptorSet(chunkGraphicsPipeline.Layout, chunkDescriptorSet.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageVertexBit);
			commandBuffer.CmdBindVertexBuffer(chunkVertexBuffer, 0);
			commandBuffer.CmdBindIndexBuffer(chunkIndexBuffer, chunkIndexBuffer.BufferSize);
			commandBuffer.CmdDrawIndexed((uint)chunkIndicesCount, 1, 0, 0, 0);
		}

		protected override void OnSwapChainChange(SwapChain newSwapChain) { }

		protected override void CopyBuffers(float delta, byte frameIndex) {
			if (shouldRegenerateChunk && World is not null) {
				RegenerateChunk(World);
				shouldRegenerateChunk = false;
			}
		}

		private void RegenerateChunk(World.World world) {
			uint[] indices = ChunkMeshBuilder.CreateChunkIndices(world.Chunk);
			chunkIndicesCount = (ulong)indices.Length;

			if (chunkIndicesCount != 0) {
				ulong bufferSize = chunkIndicesCount * sizeof(uint);

				if (chunkIndexBuffer.BufferSize < bufferSize) {
					logicalGpu.EnqueueDestroy(chunkIndexBuffer);

					chunkIndexBuffer = logicalGpu.CreateBuffer(chunkIndexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
						VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, bufferSize);
				}

				transferCommandPool.CopyToBuffer(chunkIndexBuffer, indices);
			}
		}

		public void MarkChunkDirty() => shouldRegenerateChunk = true;
	}
}