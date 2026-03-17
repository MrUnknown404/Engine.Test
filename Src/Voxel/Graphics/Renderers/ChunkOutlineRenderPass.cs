using System.Reflection;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class ChunkOutlineRenderPass : VulkanRenderPass { // TODO add toggle
		private const string Name = "ChunkOutline";

		internal ChunkPos CameraChunkPos { set; private get; }

		private readonly DescriptorSets descriptorSet;

		private readonly VoxelTest game;
		private World.World? World => game.World;

		public override bool ShouldRender { get => field && World != null; set; } = true;

		private readonly uint indexCount;

		public ChunkOutlineRenderPass(VoxelTest game, VoxelRenderPassRenderer renderer, Assembly assembly, DescriptorBuffers cameraUniformBuffer) : base("Chunk Outline Render Pass", renderer,
			CreatePipeline(renderer.GraphicsResourceProvider, renderer.SwapChain, assembly, out DescriptorSetLayout descriptorSetLayout)) {
			this.game = game;

			const byte Radius = 1;

			ChunkOutlineVertex[] vertices = MakeVertices(Radius);
			uint[] indices = MakeIndices(Radius);
			indexCount = (uint)indices.Length;

			// buffers
			VertexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(ChunkOutlineVertex) * vertices.Length));

			IndexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(uint) * indices.Length));

			// copy
			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, vertices), TransferCommandPool.CopyDataToBufferInfo.Copy(IndexBuffer, indices), ]);

			// descriptors
			DescriptorPool descriptorPool = GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, ], 1, MaxFramesInFlight);
			descriptorSet = descriptorPool.AllocateDescriptorSets(descriptorSetLayout);
			descriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);

			return;

			ChunkOutlineVertex[] MakeVertices(byte radius) {
				const uint XColor = 0xFF000000; // can't get color blending working?
				const uint YColor = 0x00FF0000;
				const uint ZColor = 0x0000FF00;

				uint size = 1 + radius * 2u;
				uint halfLength = size * Chunk.Size / 2;
				int halfLength0 = (int)(-halfLength + Chunk.Size / 2);
				int halfLength1 = (int)(halfLength + Chunk.Size / 2);

				List<ChunkOutlineVertex> vertices = new();

				for (int z = 0; z <= size; z++) { // y
					int zo = (z - radius) * Chunk.Size;

					for (int x = 0; x <= size; x++) {
						int xo = (x - radius) * Chunk.Size;

						vertices.Add(new(xo, halfLength0, zo, YColor));
						vertices.Add(new(xo, halfLength1, zo, YColor));
					}
				}

				for (int z = 0; z <= size; z++) { // x
					int zo = (z - radius) * Chunk.Size;

					for (int y = 0; y <= size; y++) {
						int yo = (y - radius) * Chunk.Size;

						vertices.Add(new(halfLength0, yo, zo, XColor));
						vertices.Add(new(halfLength1, yo, zo, XColor));
					}
				}

				for (int x = 0; x <= size; x++) { // z
					int xo = (x - radius) * Chunk.Size;

					for (int y = 0; y <= size; y++) {
						int yo = (y - radius) * Chunk.Size;

						vertices.Add(new(xo, yo, halfLength0, ZColor));
						vertices.Add(new(xo, yo, halfLength1, ZColor));
					}
				}

				return vertices.ToArray();
			}

			uint[] MakeIndices(byte radius) {
				uint size = 1 + radius * 2u;
				uint count = 3 * (size + 1) * (size + 1) * 2;

				uint[] indices = new uint[count];
				for (uint i = 0; i < count; i++) { indices[i] = i; }

				return indices;
			}
		}

		protected override void CopyBuffers(float delta, byte frameIndex) { }

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			commandBuffer.CmdPushConstants(GraphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit, new ChunkOutlinePushConstants(CameraChunkPos));

			commandBuffer.CmdBindDescriptorSet(GraphicsPipeline.Layout, descriptorSet.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageVertexBit);
			commandBuffer.CmdDrawIndexed(indexCount);
		}

		private static GraphicsPipeline CreatePipeline(VulkanResourceProvider graphicsResourceProvider, SwapChain swapChain, Assembly assembly, out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = graphicsResourceProvider.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = graphicsResourceProvider.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			descriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), ]);

			GraphicsPipeline pipeline = graphicsResourceProvider.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", swapChain.ImageFormat, [ vertexShader, fragmentShader, ], ChunkOutlineVertex.GetAttributeDescriptions(), ChunkOutlineVertex.GetBindingDescriptions()) {
						Topology = VkPrimitiveTopology.PrimitiveTopologyLineList,
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, offset = 0, size = (uint)sizeof(ChunkOutlinePushConstants), }, ],
						CullMode = VkCullModeFlagBits.CullModeNone,
						EnableDepthTest = true,
						EnableDepthWrite = true,
				});

			graphicsResourceProvider.EnqueueDestroy(vertexShader);
			graphicsResourceProvider.EnqueueDestroy(fragmentShader);

			return pipeline;
		}
	}
}