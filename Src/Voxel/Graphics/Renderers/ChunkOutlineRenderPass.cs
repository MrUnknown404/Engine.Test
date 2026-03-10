using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class ChunkOutlineRenderPass : VulkanRenderPass {
		private const string Name = "ChunkOutline";

		internal ChunkPos CameraChunkPos { set; private get; }

		private readonly DescriptorSets descriptorSet;
		private readonly DescriptorBuffers instanceBuffer;

		private readonly ChunkOutlineBuffer instanceBufferValue = new(2);

		private readonly uint indexCount;

		public ChunkOutlineRenderPass(VoxelRenderPassRenderer renderer, Assembly assembly, DescriptorBuffers cameraUniformBuffer) : base(renderer,
			CreatePipeline(renderer.GraphicsResourceProvider, renderer.SwapChain, assembly, out DescriptorSetLayout descriptorSetLayout)) {
			ChunkOutlineVertex[] vertices = [ // TODO maybe make NxN and move around player?
					new(0, 0, 0), //
					new(0, 0, Chunk.Size), //
					new(Chunk.Size, 0, Chunk.Size), //
					new(Chunk.Size, 0, 0), //
					new(0, Chunk.Size, 0), //
					new(0, Chunk.Size, Chunk.Size), //
					new(Chunk.Size, Chunk.Size, Chunk.Size), //
					new(Chunk.Size, Chunk.Size, 0), //
			];

			uint[] indices = [
					0, 1, 1, 2, 2, 3, 3, 0, //
					4, 5, 5, 6, 6, 7, 7, 4, //
					0, 4, 1, 5, 2, 6, 3, 7, //
			];

			indexCount = (uint)indices.Length;

			// buffers
			VertexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(ChunkOutlineVertex) * vertices.Length));

			IndexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(uint) * indices.Length));

			instanceBuffer = GraphicsResourceProvider.CreateDescriptorBuffers($"{Name} Instance Storage Buffers", instanceBufferValue.Size, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			// copy
			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, vertices), TransferCommandPool.CopyDataToBufferInfo.Copy(IndexBuffer, indices), ]);

			instanceBufferValue.Positions[0] = new(1, 0, 0);
			instanceBufferValue.Positions[1] = new(0, 0, 1);

			for (byte i = 0; i < MaxFramesInFlight; i++) { instanceBuffer.Copy(MemoryMarshal.AsBytes(instanceBufferValue.Positions), i); }

			// descriptors
			DescriptorPool descriptorPool =
					GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
						MaxFramesInFlight);

			descriptorSet = descriptorPool.AllocateDescriptorSets(descriptorSetLayout);

			descriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			descriptorSet.UpdateDescriptorSet(1, instanceBuffer);
		}

		protected override void CopyBuffers(float delta, byte frameIndex) {
			// instanceBufferValue.Positions[0] = new();
			//
			// instanceBuffer.Copy(MemoryMarshal.AsBytes(instanceBufferValue.Positions), frameIndex);
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			commandBuffer.CmdPushConstants(GraphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit, new ChunkPositionPushConstants(CameraChunkPos));

			commandBuffer.CmdBindDescriptorSet(GraphicsPipeline.Layout, descriptorSet.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			commandBuffer.CmdDrawIndexed(indexCount, instanceBufferValue.Count, 0, 0, 0);
		}

		private static GraphicsPipeline CreatePipeline(VulkanResourceProvider graphicsResourceProvider, SwapChain swapChain, Assembly assembly, out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = graphicsResourceProvider.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = graphicsResourceProvider.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			descriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
			]);

			GraphicsPipeline pipeline = graphicsResourceProvider.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", swapChain.ImageFormat, [ vertexShader, fragmentShader, ], ChunkOutlineVertex.GetAttributeDescriptions(), ChunkOutlineVertex.GetBindingDescriptions()) {
						Topology = VkPrimitiveTopology.PrimitiveTopologyLineList,
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, offset = 0, size = (uint)sizeof(ChunkPositionPushConstants), }, ],
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