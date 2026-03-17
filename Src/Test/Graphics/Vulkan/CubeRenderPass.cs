using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.VertexLayouts;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel;
using Engine3.Test.Voxel.World;
using Engine3.Utility;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Test.Test.Graphics.Vulkan {
	public unsafe class CubeRenderPass : VulkanRenderPass { // TODO make unit textures and use those
		private const string Name = "Cube";

		// private readonly DescriptorSetLayout descriptorSetLayout;
		private readonly DescriptorSets descriptorSet;

		// private readonly DescriptorBuffers cameraUniformBuffer;
		private readonly DescriptorBuffers instanceBuffer;

		// private readonly VulkanImage image;
		// private readonly TextureSampler textureSampler;

		private readonly StructBuffer<Matrix4x4> instanceBufferValue = new(1);

		private readonly uint indexCount;

		private float prevCubeRotation;
		private float cubeRotation;

		public CubeRenderPass(VulkanRenderPassRenderer renderer, Assembly assembly, DescriptorBuffers cameraUniformBuffer) : base($"{Name} Render Pass", renderer,
			CreatePipeline(renderer.GraphicsResourceProvider, renderer.SwapChain, assembly, out DescriptorSetLayout descriptorSetLayout)) {
			ShouldUpdate = true;

			CubeBuilder.BuildCube(BlockFaceMask.All, 1, 0, 0, 0, out VertexXyzUv[] cubeVertices, out uint[] cubeIndices);
			indexCount = (uint)cubeIndices.Length;

			// buffers
			VertexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(VertexXyzUv) * cubeVertices.Length));

			IndexBuffer = GraphicsResourceProvider.CreateBuffer($"{Name} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(uint) * cubeIndices.Length));

			instanceBuffer = GraphicsResourceProvider.CreateDescriptorBuffers($"{Name} Instance Storage Buffers", instanceBufferValue.Size, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			// copy
			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, cubeVertices), TransferCommandPool.CopyDataToBufferInfo.Copy(IndexBuffer, cubeIndices), ]);
			for (byte i = 0; i < MaxFramesInFlight; i++) { instanceBuffer.Copy(MemoryMarshal.AsBytes(instanceBufferValue.Data), i); }

			// textures
			TextureSampler textureSampler = GraphicsResourceProvider.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, PhysicalGpu.PhysicalDeviceProperties2.properties.limits));
			VulkanImage image;

			using (StbiImage stbiImage = AssetH.LoadImage("Test.64x64", "png", 4, assembly)) {
				image = GraphicsResourceProvider.CreateImage($"{Name} Test 64x64 Image", (uint)stbiImage.Width, (uint)stbiImage.Height, VkFormat.FormatR8g8b8a8Srgb);
				TransferCommandPool.CopyToImage(image, PhysicalGpu.QueueFamilyIndices, LogicalGpu.TransferQueue, stbiImage);
			}

			// descriptors
			DescriptorPool descriptorPool =
					GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
						MaxFramesInFlight);

			descriptorSet = descriptorPool.AllocateDescriptorSets(descriptorSetLayout);

			descriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			descriptorSet.UpdateDescriptorSet(1, instanceBuffer);
			descriptorSet.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);

			instanceBufferValue.Data[0] = Matrix4x4.CreateTranslation(0, 0, -5f);
		}

		protected override void Update() {
			const float Rotation = 360f / 3f / 60f;

			prevCubeRotation = cubeRotation;
			cubeRotation += Rotation;

			// we can't mod cubeRotation unless prevCubeRotation is also offset
			if ((prevCubeRotation > 360 && cubeRotation > 360) || (prevCubeRotation < 360 && cubeRotation < 360)) {
				prevCubeRotation %= 360;
				cubeRotation %= 360;
			}
		}

		protected override void CopyBuffers(float delta, byte frameIndex) {
			instanceBufferValue.Data[0] = Matrix4x4.CreateRotationY(float.DegreesToRadians(float.Lerp(prevCubeRotation, cubeRotation, delta)));

			instanceBuffer.Copy(MemoryMarshal.AsBytes(instanceBufferValue.Data), frameIndex);
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			commandBuffer.CmdBindDescriptorSet(GraphicsPipeline.Layout, descriptorSet.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			commandBuffer.CmdDrawIndexed(indexCount);
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
	}
}