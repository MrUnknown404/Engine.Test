using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using Engine3.Utility;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class WorldRenderPass : VulkanIndirectRenderPass {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string AssetName = "Chunk";

		public WorldLightingPushConstants WorldLightingPushConstants { get; init; } = new(0xFFFFFF00u, Vector3.Normalize(Vector3.UnitY + Vector3.UnitX / 2 + Vector3.UnitZ / 2));

		public override bool ShouldRender { get => field && World != null; set; } = true;

		public uint ChunkCount => (uint)chunkIndices.Count;

		private readonly ChunkRenderQueue chunkRenderQueue = new();
		private readonly Dictionary<ChunkPos, uint[]> chunkIndices = new();

		private readonly DescriptorSets descriptorSets;

		private DescriptorBuffers perChunkDataDescriptorBuffer;
		private StructBuffer<PerChunkData> perChunkDataBuffer = new(0);

		private readonly VoxelTest game;
		private World.World? World => game.World;

		private uint drawCount;

		public WorldRenderPass(VoxelTest game, VoxelRenderPassRenderer renderer, Assembly assembly, DescriptorBuffers cameraUniformBuffer) : base("World Render Pass", renderer,
			CreatePipeline(renderer.GraphicsResourceProvider, renderer.SwapChain, assembly, out DescriptorSetLayout descriptorSetLayout)) {
			this.game = game;

			const ushort InitialChunkBufferCount = 10000;
			const uint SizeOfBiggestChunk = Chunk.ArraySize * 24; // facesPerCube * indicesPerFace
			const ulong InitialIndexBufferSize = sizeof(uint) * InitialChunkBufferCount * SizeOfBiggestChunk;

			ChunkVertex[] vertices = ChunkMeshBuilder.GetChunkVertices();

			VertexBuffer = GraphicsResourceProvider.CreateBuffer($"{AssetName} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(ChunkVertex) * vertices.Length));

			IndexBuffer = GraphicsResourceProvider.CreateBuffer($"{AssetName} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, InitialIndexBufferSize);

			IndirectCmdBuffer = GraphicsResourceProvider.CreateBuffer($"{AssetName} Indirect Command Buffer", VkBufferUsageFlagBits.BufferUsageIndirectBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit,
				(ulong)(sizeof(VkDrawIndexedIndirectCommand) * InitialChunkBufferCount)); // TODO for some reason things break if i change the initial size

			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, vertices), ]);

			perChunkDataDescriptorBuffer = GraphicsResourceProvider.CreateDescriptorBuffers("PerChunkData Storage Buffer", (ulong)sizeof(PerChunkData) * InitialChunkBufferCount, MaxFramesInFlight,
				VkDescriptorType.DescriptorTypeStorageBuffer, VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			// textures
			TextureSampler textureSampler = GraphicsResourceProvider.CreateSampler(new(VkFilter.FilterNearest, VkFilter.FilterNearest, PhysicalGpu.PhysicalDeviceProperties2.properties.limits));

			Block[] validBlocks = game.MasterBlockRegistry.AllObjects.Where(static b => b.Properties.SolidFaceMask != BlockFaceMask.None).ToArray();
			VulkanImage image = CreateBlockAtlas(validBlocks, (uint)validBlocks.Length, 16);

			// descriptors
			DescriptorPool descriptorPool =
					GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
						MaxFramesInFlight);

			descriptorSets = descriptorPool.AllocateDescriptorSets(descriptorSetLayout);
			Logger.Debug("Created world descriptor sets");

			descriptorSets.UpdateDescriptorSet(0, cameraUniformBuffer);
			descriptorSets.UpdateDescriptorSet(1, perChunkDataDescriptorBuffer);
			descriptorSets.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);
		}

		// TODO texture atlas class
		private VulkanImage CreateBlockAtlas(IEnumerable<Block> blocks, uint count, ushort textureSizeInPixels) {
			const byte ColorChannels = 4;

			ushort atlasSize = (ushort)(count == 1 ? 1 : (uint)MathF.Sqrt(count) + 1);
			uint atlasSizeInPixels = (uint)(atlasSize * textureSizeInPixels);

			Logger.Debug($"Creating atlas of size {atlasSize}");

			byte[] data = new byte[atlasSizeInPixels * atlasSizeInPixels * ColorChannels];

			ushort x = 0;
			ushort y = (ushort)(atlasSize - 1);

			foreach (Block block in blocks) {
				if (block.Properties.SolidFaceMask == BlockFaceMask.None) { continue; }

				using (StbiImage stbiImage = AssetH.LoadImage($"Voxel.Blocks.{block.RegistryKey.Key}", "png", ColorChannels, block.RegistryKey.Source.Assembly)) {
					Blit(ref data, stbiImage.Data, x, y); // TODO do on gpu

					x++;

					if (x == atlasSize) {
						x = 0;
						y--;
					}
				}
			}

			VulkanImage textureAtlas = GraphicsResourceProvider.CreateImage("Block Texture Atlas", atlasSizeInPixels, atlasSizeInPixels, VkFormat.FormatR8g8b8a8Srgb);
			TransferCommandPool.CopyToImage(textureAtlas, PhysicalGpu.QueueFamilyIndices, LogicalGpu.TransferQueue, atlasSizeInPixels, atlasSizeInPixels, 4, data);
			return textureAtlas;

			void Blit(ref byte[] destination, ReadOnlySpan<byte> source, ushort x, ushort y) {
				uint textureSizeWithColorChannels = (uint)(textureSizeInPixels * ColorChannels);
				uint atlasSizeWithColorChannels = atlasSizeInPixels * ColorChannels;
				uint yOffset = (uint)(y * textureSizeInPixels);

				fixed (byte* sourcePtr = source) {
					fixed (byte* destinationPtr = destination) {
						for (int yi = 0; yi < textureSizeInPixels; yi++) {
							long dstIndex = (yOffset + yi) * atlasSizeWithColorChannels + x * textureSizeWithColorChannels;
							long yiOffset = yi * textureSizeWithColorChannels;

							Buffer.MemoryCopy(sourcePtr + yiOffset, destinationPtr + dstIndex, textureSizeWithColorChannels, textureSizeWithColorChannels);
						}
					}
				}
			}
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

		private void TryRegenerateWorld(IWorldAccessor world) {
			// get dirty chunks
			ChunkPos[] chunksToAdd = chunkRenderQueue.DequeueAll();
			Logger.Trace($"Attempting to rendering {chunksToAdd.Length} new chunks");

			// create indices
			foreach (ChunkPos position in chunksToAdd) { // TODO do on gpu
				chunkIndices[position] = ChunkMeshBuilder.CreateChunkIndices(world, position, true);
			}

			KeyValuePair<ChunkPos, uint[]>[] chunkPositionIndicesPair = chunkIndices.AsValueEnumerable().Where(static p => p.Value.Length != 0).ToArray();
			Logger.Trace($"Rendering {chunkPositionIndicesPair.Length}/{chunkIndices.Count} chunks");

			List<VkDrawIndexedIndirectCommand> cmds = new();
			List<uint> allIndices = new();

			uint indexOffset = 0;

			// add to all indices & add cmd
			foreach ((_, uint[] indices) in chunkPositionIndicesPair) {
				allIndices.AddRange(indices);

				cmds.Add(new() { indexCount = (uint)indices.Length, instanceCount = 1, firstIndex = indexOffset, vertexOffset = 0, firstInstance = 0, });

				indexOffset += (uint)indices.Length;
			}

			drawCount = (uint)cmds.Count;

			// copy index buffer
			if (allIndices.Count != 0) {
				ulong indexBufferSize = (ulong)(allIndices.Count * sizeof(uint));

				if (indexBufferSize > IndexBuffer!.BufferSize) { // index buffer should never be null. just smol
					GraphicsResourceProvider.EnqueueDestroy(IndexBuffer);

					IndexBuffer = GraphicsResourceProvider.CreateBuffer(IndexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
						VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, indexBufferSize);
				}

				TransferCommandPool.CopyToBuffer(IndexBuffer, CollectionsMarshal.AsSpan(allIndices));
			}

			// set chunk data
			if (chunkPositionIndicesPair.Length > (int)perChunkDataBuffer.Count) {
				perChunkDataBuffer = new((uint)chunkPositionIndicesPair.Length);

				ulong bufferSize = (ulong)sizeof(PerChunkData) * perChunkDataBuffer.Count;
				if (bufferSize > perChunkDataDescriptorBuffer.BufferSize) {
					GraphicsResourceProvider.EnqueueDestroy(perChunkDataDescriptorBuffer);

					perChunkDataDescriptorBuffer = GraphicsResourceProvider.CreateDescriptorBuffers(perChunkDataDescriptorBuffer.DebugName, bufferSize, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
						VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

					descriptorSets.UpdateDescriptorSet(1, perChunkDataDescriptorBuffer);
				}
			}

			// copy chunk data
			for (int i = 0; i < chunkPositionIndicesPair.Length; i++) { perChunkDataBuffer.Data[i] = new(chunkPositionIndicesPair[i].Key); }
			for (byte i = 0; i < MaxFramesInFlight; i++) { perChunkDataDescriptorBuffer.Copy(perChunkDataBuffer.Data, i); } // TODO what should i be doing? should i pass FrameIndex or copy all?

			// copy cmds
			if (drawCount != 0) {
				ulong cmdBufferSize = (ulong)(sizeof(VkDrawIndexedIndirectCommand) * drawCount);

				if (cmdBufferSize > IndirectCmdBuffer!.BufferSize) { // indirect buffer won't be null
					GraphicsResourceProvider.EnqueueDestroy(IndirectCmdBuffer);

					IndirectCmdBuffer = GraphicsResourceProvider.CreateBuffer(IndirectCmdBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageIndirectBufferBit,
						VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, cmdBufferSize);
				}

				IndirectCmdBuffer.Copy(CollectionsMarshal.AsSpan(cmds));
			}
		}

		internal void MarkAllChunksDirty() {
			foreach (ChunkPos position in chunkIndices.Keys) { EnqueueChunk(position); }
		}

		internal void EnqueueChunk(ChunkPos position) => chunkRenderQueue.Enqueue(position);

		internal bool EnqueueChunkIfCached(ChunkPos position) {
			if (chunkIndices.ContainsKey(position)) {
				EnqueueChunk(position);
				return true;
			}

			return false;
		}

		internal bool EnqueueChunkIfNotCached(ChunkPos position) {
			if (!chunkIndices.ContainsKey(position)) {
				EnqueueChunk(position);
				return true;
			}

			return false;
		}

		internal void ClearCache() {
			chunkIndices.Clear();
			drawCount = 0;
		}
	}
}