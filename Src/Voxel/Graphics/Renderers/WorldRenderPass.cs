using System.Numerics;
using System.Reflection;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class WorldRenderPass : VulkanIndirectRenderPass {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string AssetName = "Chunk";
		private const byte BlockTextureSize = 16;

		public WorldLightingPushConstants WorldLightingPushConstants { get; init; } = new(0xFFFFFF00u, Vector3.Normalize(Vector3.UnitY + Vector3.UnitX / 2 + Vector3.UnitZ / 2));

		public override bool ShouldRender { get => field && World != null; set; } = true;

		public uint ChunkCount => chunkMeshBuffer.ChunkCount;

		private readonly ChunkRenderQueue chunkRenderQueue = new();
		private readonly ChunkMeshBuffer chunkMeshBuffer = new();

		private readonly DescriptorSets descriptorSets;

		private DescriptorBuffers perChunkDataDescriptorBuffer;
		private StructBuffer<PerChunkData> perChunkDataBuffer = new(0);

		private readonly VoxelTest game;
		private World.World? World => game.World;

		private uint drawCount;

		private readonly BlockTextureAtlas blockTextureAtlas;

		// TODO move

		public WorldRenderPass(VoxelTest game, VoxelRenderPassRenderer renderer, Assembly assembly, DescriptorBuffers cameraUniformBuffer) : base("World Render Pass", renderer,
			CreatePipeline(renderer.GraphicsResourceProvider, renderer.SwapChain, assembly, out DescriptorSetLayout descriptorSetLayout)) {
			this.game = game;

			const ulong InitialChunkCount = 100;
			const byte VerticesPerBlock = 4 * 6; // vertices per face * faces
			const byte IndicesPerBlock = 6 * 6; // indices per face * faces
			const byte SmallObjectMultiplier = 10;

			const ulong MaxChunkVertexSize = Chunk.ArraySize * VerticesPerBlock;
			const ulong MaxChunkIndexSize = Chunk.ArraySize * IndicesPerBlock;

			VertexBuffer = GraphicsResourceProvider.CreateBuffer($"{AssetName} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)sizeof(ChunkVertex) * MaxChunkVertexSize * InitialChunkCount);

			IndexBuffer = GraphicsResourceProvider.CreateBuffer($"{AssetName} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, sizeof(uint) * MaxChunkIndexSize * InitialChunkCount);

			IndirectCmdBuffer = GraphicsResourceProvider.CreateBuffer($"{AssetName} Indirect Command Buffer", VkBufferUsageFlagBits.BufferUsageIndirectBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit,
				(ulong)sizeof(VkDrawIndexedIndirectCommand) * InitialChunkCount * SmallObjectMultiplier); // TODO for some reason things break if i change the initial size

			perChunkDataDescriptorBuffer = GraphicsResourceProvider.CreateDescriptorBuffers("PerChunkData Storage Buffer", (ulong)sizeof(PerChunkData) * InitialChunkCount * SmallObjectMultiplier, MaxFramesInFlight,
				VkDescriptorType.DescriptorTypeStorageBuffer, VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			// textures
			TextureSampler textureSampler = GraphicsResourceProvider.CreateSampler(new(VkFilter.FilterNearest, VkFilter.FilterNearest, PhysicalGpu.PhysicalDeviceProperties2.properties.limits) { EnableAnisotropy = false, });
			blockTextureAtlas = new(GraphicsResourceProvider, PhysicalGpu, LogicalGpu, TransferCommandPool, game.MasterBlockRegistry, BlockTextureSize);

			// descriptors
			DescriptorPool descriptorPool =
					GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
						MaxFramesInFlight);

			descriptorSets = descriptorPool.AllocateDescriptorSets(descriptorSetLayout);
			Logger.Debug("Created world descriptor sets");

			descriptorSets.UpdateDescriptorSet(0, cameraUniformBuffer);
			descriptorSets.UpdateDescriptorSet(1, perChunkDataDescriptorBuffer);
			descriptorSets.UpdateDescriptorSet(2, blockTextureAtlas.Image.ImageView, textureSampler.Sampler);
		}

		private static GraphicsPipeline CreatePipeline(VulkanResourceProvider graphicsResourceProvider, SwapChain swapChain, Assembly assembly, out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = graphicsResourceProvider.CreateShader($"{AssetName} Vertex Shader", AssetName, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = graphicsResourceProvider.CreateShader($"{AssetName} Fragment Shader", AssetName, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			descriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 2), //
			]);

			GraphicsPipeline pipeline = graphicsResourceProvider.CreateGraphicsPipeline(
				new($"{AssetName} Graphics Pipeline", swapChain.ImageFormat, [ vertexShader, fragmentShader, ], ChunkVertex.GetAttributeDescriptions(), ChunkVertex.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageFragmentBit, offset = 0, size = (uint)sizeof(WorldLightingPushConstants), }, ],
						EnableDepthTest = true,
						EnableDepthWrite = true,
				});

			graphicsResourceProvider.EnqueueDestroy(vertexShader);
			graphicsResourceProvider.EnqueueDestroy(fragmentShader);

			return pipeline;
		}

		protected override void CopyBuffers(float delta, byte frameIndex) {
			if (chunkRenderQueue.ShouldRenderChunks && World is not null) { TryRegenerateWorld(World); }
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			if (drawCount == 0) { return; }

			commandBuffer.CmdPushConstants(GraphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageFragmentBit, WorldLightingPushConstants);

			commandBuffer.CmdBindDescriptorSet(GraphicsPipeline.Layout, descriptorSets.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			commandBuffer.CmdDrawIndexedIndirect(IndirectCmdBuffer!.Buffer, 0, drawCount, (uint)sizeof(VkDrawIndexedIndirectCommand)); // should stride ever be anything else?
		}

		private void TryRegenerateWorld(IWorldReader world) {
			ChunkPos[] chunksToBuild = chunkRenderQueue.DequeueAll();

			chunkMeshBuffer.BuildDrawData(world, chunksToBuild, blockTextureAtlas);
			chunkMeshBuffer.GetDrawData(out ChunkVertex[] vertices, out uint[] indices, out VkDrawIndexedIndirectCommand[] commands, out StructBuffer<PerChunkData> perChunkBuffer);

			Logger.Trace($"Found {chunksToBuild.Length} queued chunks. Rendering {perChunkBuffer.Count}/{ChunkCount} chunks");

			if (vertices.Length == 0 || indices.Length == 0) {
				drawCount = 0;
				return;
			}

			drawCount = (uint)commands.Length;

			if (drawCount == 0) { return; }

			// vertex buffer
			ulong vertexBufferSize = (ulong)(vertices.Length * sizeof(ChunkVertex));
			if (vertexBufferSize > VertexBuffer!.BufferSize) { // vertex buffer should never be null. just smol
				GraphicsResourceProvider.EnqueueDestroy(VertexBuffer);

				VertexBuffer = GraphicsResourceProvider.CreateBuffer(VertexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
					VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, vertexBufferSize);
			}

			// index buffer
			ulong indexBufferSize = (ulong)(indices.Length * sizeof(uint));
			if (indexBufferSize > IndexBuffer!.BufferSize) { // index buffer should never be null. just smol
				GraphicsResourceProvider.EnqueueDestroy(IndexBuffer);

				IndexBuffer = GraphicsResourceProvider.CreateBuffer(IndexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
					VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, indexBufferSize);
			}

			// cmd buffer
			ulong cmdBufferSize = (ulong)(sizeof(VkDrawIndexedIndirectCommand) * drawCount);
			if (cmdBufferSize > IndirectCmdBuffer!.BufferSize) { // indirect buffer should never be null. just smol
				GraphicsResourceProvider.EnqueueDestroy(IndirectCmdBuffer);

				IndirectCmdBuffer = GraphicsResourceProvider.CreateBuffer(IndirectCmdBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageIndirectBufferBit,
					VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, cmdBufferSize);
			}

			// per chunk buffer
			if (perChunkBuffer.Count > (int)perChunkDataBuffer.Count) {
				perChunkDataBuffer = new(perChunkBuffer.Data);

				ulong perChunkBufferSize = perChunkDataBuffer.Size;
				if (perChunkBufferSize > perChunkDataDescriptorBuffer.BufferSize) {
					GraphicsResourceProvider.EnqueueDestroy(perChunkDataDescriptorBuffer);

					perChunkDataDescriptorBuffer = GraphicsResourceProvider.CreateDescriptorBuffers(perChunkDataDescriptorBuffer.DebugName, perChunkBufferSize, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
						VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

					descriptorSets.UpdateDescriptorSet(1, perChunkDataDescriptorBuffer);
				}
			}

			// copy
			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, vertices), TransferCommandPool.CopyDataToBufferInfo.Copy(IndexBuffer, indices), ]);
			IndirectCmdBuffer.Copy(commands);
			for (byte i = 0; i < MaxFramesInFlight; i++) { perChunkDataDescriptorBuffer.Copy(perChunkDataBuffer.Data, i); } // TODO what should i be doing? should i pass FrameIndex or copy all?
		}

		internal void MarkAllChunksDirty() {
			foreach (ChunkPos position in chunkMeshBuffer.CachedPositions) { EnqueueChunk(position); }
		}

		internal void EnqueueChunk(ChunkPos position) => chunkRenderQueue.Enqueue(position);

		internal bool EnqueueChunkIfCached(ChunkPos position) {
			if (chunkMeshBuffer.Contains(position)) {
				EnqueueChunk(position);
				return true;
			}

			return false;
		}

		internal bool EnqueueChunkIfNotCached(ChunkPos position) {
			if (!chunkMeshBuffer.Contains(position)) {
				EnqueueChunk(position);
				return true;
			}

			return false;
		}

		internal void ClearCache() {
			chunkMeshBuffer.Clear();
			drawCount = 0;
		}
	}
}