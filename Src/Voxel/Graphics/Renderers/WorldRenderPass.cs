using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vertex;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using Engine3.Utility;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class WorldRenderPass : VulkanRenderPass {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string Name = "Chunk";

		public override bool ShouldRender { get => field && World != null; set; } = true;

		internal World.World? World { private get; set; }

		private readonly TransferCommandPool transferCommandPool;
		private readonly DescriptorSets descriptorSets;
		private VulkanBuffer indirectCmdBuffer;

		private DescriptorBuffers perChunkDataDescriptorBuffer;
		private PerChunkDataBuffer perChunkDataBuffer = new(0);

		private bool shouldRegenerateChunks;
		private uint drawCount;

		private readonly byte maxFramesInFlight;

		public WorldRenderPass(VoxelRenderPassRenderer renderer, Assembly assembly, DescriptorBuffers cameraUniformBuffer) : base(renderer,
			CreatePipeline(renderer.GraphicsResourceProvider, renderer.SwapChain, assembly, out DescriptorSetLayout descriptorSetLayout)) {
			transferCommandPool = TransferCommandPool;
			maxFramesInFlight = MaxFramesInFlight;

			ChunkVertex[] vertices = ChunkMeshBuilder.GetChunkVertices();

			VertexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(ChunkVertex) * vertices.Length));

			IndexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, sizeof(uint));

			indirectCmdBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Indirect Command Buffer", VkBufferUsageFlagBits.BufferUsageIndirectBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit, 1);

			transferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, vertices), ]);

			perChunkDataDescriptorBuffer = GraphicsResourceProvider.CreateDescriptorBuffers("PerChunkData Storage Buffer", (ulong)sizeof(PerChunkData), maxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			// textures
			TextureSampler textureSampler = GraphicsResourceProvider.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, PhysicalGpu.PhysicalDeviceProperties2.properties.limits));
			VulkanImage image;

			using (StbiImage stbiImage = AssetH.LoadImage("Test.64x64", "png", 4, assembly)) {
				image = GraphicsResourceProvider.CreateImage($"{Name} Test 64x64 Image", (uint)stbiImage.Width, (uint)stbiImage.Height, VkFormat.FormatR8g8b8a8Srgb);
				transferCommandPool.CopyToImage(image, PhysicalGpu.QueueFamilyIndices, LogicalGpu.TransferQueue, stbiImage);
			}

			// descriptors
			DescriptorPool descriptorPool =
					GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
						maxFramesInFlight);

			descriptorSets = descriptorPool.AllocateDescriptorSets(descriptorSetLayout);
			Logger.Debug("Created world descriptor sets");

			descriptorSets.UpdateDescriptorSet(0, cameraUniformBuffer);
			descriptorSets.UpdateDescriptorSet(1, perChunkDataDescriptorBuffer);
			descriptorSets.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);
		}

		private static GraphicsPipeline CreatePipeline(VulkanResourceProvider graphicsResourceProvider, SwapChain swapChain, Assembly assembly, out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = graphicsResourceProvider.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = graphicsResourceProvider.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			descriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 2), //
			]);

			GraphicsPipeline pipeline = graphicsResourceProvider.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", swapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzUv.GetAttributeDescriptions(), VertexXyzUv.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ], EnableDepthTest = true, EnableDepthWrite = true,
				});

			graphicsResourceProvider.EnqueueDestroy(vertexShader);
			graphicsResourceProvider.EnqueueDestroy(fragmentShader);

			return pipeline;
		}

		protected override void CopyBuffers(float delta, byte frameIndex) {
			if (shouldRegenerateChunks && World is not null) {
				RegenerateChunk(World);
				shouldRegenerateChunks = false;
			}
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			commandBuffer.CmdBindDescriptorSet(GraphicsPipeline.Layout, descriptorSets.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			commandBuffer.CmdDrawIndexedIndirect(indirectCmdBuffer.Buffer, 0, drawCount, (uint)sizeof(VkDrawIndexedIndirectCommand)); // should stride ever be anything else?
		}

		private void RegenerateChunk(World.World world) {
			// buffers
			ChunkPos[] positions = world.CleanRendererDirtyChunks();

			List<uint> indices = new();
			List<VkDrawIndexedIndirectCommand> cmds = new();

			uint indexOffset = 0;

			foreach (ChunkPos position in positions) {
				uint[] chunkIndices = ChunkMeshBuilder.CreateChunkIndices(world, position, true);
				indices.AddRange(chunkIndices);

				cmds.Add(new() {
						indexCount = (uint)chunkIndices.Length, //
						instanceCount = 1, //
						firstIndex = indexOffset, //
						vertexOffset = 0, //
						firstInstance = 0, //
				});

				indexOffset += (uint)chunkIndices.Length;
			}

			drawCount = (uint)positions.Length;

			if (indices.Count != 0) {
				ulong indexBufferSize = (ulong)(indices.Count * sizeof(uint));

				if (IndexBuffer!.BufferSize < indexBufferSize) { // index buffer should never be null. just smol
					GraphicsResourceProvider.EnqueueDestroy(IndexBuffer);

					IndexBuffer = GraphicsResourceProvider.CreateBuffer(IndexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
						VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, indexBufferSize);
				}

				transferCommandPool.CopyToBuffer(IndexBuffer, CollectionsMarshal.AsSpan(indices));
			}

			// chunk data
			if (positions.Length > (int)perChunkDataBuffer.Count) {
				perChunkDataBuffer = new((uint)positions.Length);

				GraphicsResourceProvider.EnqueueDestroy(perChunkDataDescriptorBuffer);

				perChunkDataDescriptorBuffer = GraphicsResourceProvider.CreateDescriptorBuffers(perChunkDataDescriptorBuffer.DebugName, (ulong)sizeof(PerChunkData) * perChunkDataBuffer.Count, maxFramesInFlight,
					VkDescriptorType.DescriptorTypeStorageBuffer, VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

				descriptorSets.UpdateDescriptorSet(1, perChunkDataDescriptorBuffer);
			}

			for (int i = 0; i < positions.Length; i++) { perChunkDataBuffer.Data[i] = new(positions[i]); }
			for (byte i = 0; i < maxFramesInFlight; i++) { perChunkDataDescriptorBuffer.Copy(perChunkDataBuffer.Data, i); } // TODO what should i be doing? should i pass FrameIndex or copy all?

			// cmds
			if (cmds.Count != 0) {
				ulong cmdBufferSize = (ulong)(sizeof(VkDrawIndexedIndirectCommand) * cmds.Count);

				if (indirectCmdBuffer.BufferSize < cmdBufferSize) {
					GraphicsResourceProvider.EnqueueDestroy(indirectCmdBuffer);

					indirectCmdBuffer = GraphicsResourceProvider.CreateBuffer(indirectCmdBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageIndirectBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit, cmdBufferSize);
				}

				indirectCmdBuffer.Copy(CollectionsMarshal.AsSpan(cmds));
			}
		}

		public void CheckIfWorldIsDirty() {
			if (World?.HasDirtyChunksForRenderer ?? false) { shouldRegenerateChunks = true; }
		}
	}
}