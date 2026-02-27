using System.Reflection;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vertex;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Utility;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class WorldRenderPass : VulkanRenderPass {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string Name = "Chunk";

		public override bool ShouldRender { get => field && World != null; set; } = true;

		internal World.World? World { private get; set; }

		private readonly TransferCommandPool transferCommandPool;
		private readonly DescriptorSets descriptorSets;

		private uint indexCount;

		private bool shouldRegenerateChunk;

		public WorldRenderPass(SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu, SwapChain swapChain, TransferCommandPool transferCommandPool, Assembly assembly, byte maxFramesInFlight,
			DescriptorBuffers cameraUniformBuffer) : base(logicalGpu, CreatePipeline(logicalGpu, swapChain, assembly, out DescriptorSetLayout descriptorSetLayout)) {
			this.transferCommandPool = transferCommandPool;

			ChunkVertex[] vertices = ChunkMeshBuilder.GetChunkVertices();

			VertexBuffer = logicalGpu.CreateBuffer($"{Name} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(ChunkVertex) * vertices.Length));

			IndexBuffer = logicalGpu.CreateBuffer($"{Name} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				1);

			transferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, vertices), ]);

			// textures
			TextureSampler textureSampler = LogicalGpu.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, physicalGpu.PhysicalDeviceProperties2.properties.limits));
			VulkanImage image;

			using (StbiImage stbiImage = AssetH.LoadImage("Test.64x64", "png", 4, assembly)) {
				image = LogicalGpu.CreateImage($"{Name} Test 64x64 Image", (uint)stbiImage.Width, (uint)stbiImage.Height, VkFormat.FormatR8g8b8a8Srgb);
				transferCommandPool.CopyToImage(image, physicalGpu.QueueFamilyIndices, LogicalGpu.TransferQueue, stbiImage);
			}

			// descriptors
			DescriptorPool descriptorPool = logicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
				maxFramesInFlight);

			descriptorSets = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created world descriptor sets");

			descriptorSets.UpdateDescriptorSet(0, cameraUniformBuffer);
			descriptorSets.UpdateDescriptorSet(1, image.ImageView, textureSampler.Sampler);
		}

		private static GraphicsPipeline CreatePipeline(LogicalGpu logicalGpu, SwapChain swapChain, Assembly assembly, out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = logicalGpu.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = logicalGpu.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			descriptorSetLayout = logicalGpu.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 1), //
			]);

			GraphicsPipeline pipeline = logicalGpu.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", swapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzUv.GetAttributeDescriptions(), VertexXyzUv.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ], EnableDepthTest = true, EnableDepthWrite = true,
				});

			logicalGpu.EnqueueDestroy(vertexShader);
			logicalGpu.EnqueueDestroy(fragmentShader);

			return pipeline;
		}

		protected override void CopyBuffers(float delta, byte frameIndex) {
			if (shouldRegenerateChunk && World is not null) {
				RegenerateChunk(World);
				shouldRegenerateChunk = false;
			}
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			commandBuffer.CmdBindDescriptorSet(GraphicsPipeline.Layout, descriptorSets.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageVertexBit);
			commandBuffer.CmdDrawIndexed(indexCount);
		}

		private void RegenerateChunk(World.World world) {
			uint[] indices = ChunkMeshBuilder.CreateChunkIndices(world.Chunk);
			indexCount = (uint)indices.Length;

			if (indexCount != 0) {
				ulong bufferSize = indexCount * sizeof(uint);

				if (IndexBuffer!.BufferSize < bufferSize) { // index buffer should never be null. just smol
					LogicalGpu.EnqueueDestroy(IndexBuffer);

					IndexBuffer = LogicalGpu.CreateBuffer(IndexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
						VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, bufferSize);
				}

				transferCommandPool.CopyToBuffer(IndexBuffer, indices);
			}
		}

		public void MarkChunkDirty() => shouldRegenerateChunk = true;
	}
}