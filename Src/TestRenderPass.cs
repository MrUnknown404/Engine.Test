using System.Reflection;
using Engine4.Client.Graphics;
using Engine4.Client.Graphics._Test;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Rendering;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Test;

public unsafe class TestRenderPass : RenderPass {
	private readonly VulkanBuffer vertexBuffer;
	private readonly VulkanBuffer indexBuffer;

	private readonly Texture testTexture;

	private readonly SamplerDescriptors samplerDescriptors;
	private readonly ResourceDescriptors resourceDescriptors;

	private readonly VulkanRenderer renderer;
	private VkExtent2D SwapChainExtent => renderer.GetSwapChainExtent();

	public TestRenderPass(VulkanRenderer renderer) : base(CreateGraphicsPipeline(renderer.ResourceManager, renderer.GetSwapChainFormat(), typeof(TestRenderPass).Assembly)) {
		this.renderer = renderer;

		TestVertex[] vertices = [ // xyz uv rgb
				new(-0.5f, -0.5f, 0, 0, 0, 0, 0, 0), // top left
				new(-0.5f, 0.5f, 0, 0, 0, 0, 0, 0), // bottom left
				new(0.5f, -0.5f, 0, 0, 0, 0, 0, 0), // top right
				new(0.5f, 0.5f, 0, 0, 0, 0, 0, 0), // bottom right
		];

		uint[] indices = [
				0, 1, 2, //
				2, 1, 3,
		];

		VulkanManager vulkanManager = renderer.VulkanManager;
		VulkanResourceManager resourceManager = renderer.ResourceManager;
		SurfaceReadyPhysicalGpu physicalGpu = renderer.PhysicalGpu;
		LogicalGpu logicalGpu = renderer.LogicalGpu;

		vertexBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Vertex Buffer", (ulong)(vertices.Length * sizeof(TestVertex)), VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		indexBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Index Buffer", (ulong)(indices.Length * sizeof(uint)), VkBufferUsageFlagBits2.BufferUsage2IndexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		vertexBuffer.Copy(vertices);
		indexBuffer.Copy(indices);

		testTexture = resourceManager.CreateTexture($"{nameof(TestRenderPass)} Test Texture", 64, 64, VkFormat.FormatR8g8b8a8Srgb);
		// TODO copy data to texture

		samplerDescriptors = new(resourceManager, physicalGpu, logicalGpu,
		[
				new() {
						minFilter = VkFilter.FilterLinear,
						magFilter = VkFilter.FilterLinear,
						addressModeU = VkSamplerAddressMode.SamplerAddressModeRepeat,
						addressModeV = VkSamplerAddressMode.SamplerAddressModeRepeat,
						addressModeW = VkSamplerAddressMode.SamplerAddressModeRepeat,
						anisotropyEnable = vulkanManager.AllowEnableAnisotropy ? VkH.True : VkH.False,
						maxAnisotropy = physicalGpu.Properties2.properties.limits.maxSamplerAnisotropy,
						borderColor = VkBorderColor.BorderColorIntOpaqueBlack,
						unnormalizedCoordinates = VkH.False,
						compareEnable = VkH.False,
						compareOp = VkCompareOp.CompareOpAlways,
						mipmapMode = VkSamplerMipmapMode.SamplerMipmapModeLinear,
						mipLodBias = 0,
						minLod = 0,
						maxLod = 0,
				},
		]);

		resourceDescriptors = new(resourceManager, physicalGpu, logicalGpu, [ testTexture, ], [ ]);
	}

	[MustUseReturnValue]
	private static GraphicsPipeline CreateGraphicsPipeline(VulkanResourceManager resourceManager, VkFormat swapChainImageFormat, Assembly assembly) {
		VulkanShader vertexShader = resourceManager.CreateShader($"{nameof(TestRenderPass3)} Vertex Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Vertex);
		VulkanShader fragmentShader = resourceManager.CreateShader($"{nameof(TestRenderPass3)} Fragment Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Fragment);

		GraphicsPipeline graphicsPipeline = resourceManager.CreateGraphicsPipeline(new GraphicsPipeline.Settings($"{nameof(TestRenderPass3)} Graphics Pipeline", swapChainImageFormat, [ vertexShader, fragmentShader, ],
			TestVertex.GetAttributeDescriptions(0), TestVertex.GetBindingDescriptions(0)) {
				EnableDepthTest = true, //
				EnableDepthWrite = true,
				CullMode = VkCullModeFlagBits.CullModeNone,
		});

		resourceManager.EnqueueDestroy(vertexShader);
		resourceManager.EnqueueDestroy(fragmentShader);

		return graphicsPipeline;
	}

	protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer) {
		commandBuffer.CmdSetViewport(0, 0, SwapChainExtent.width, SwapChainExtent.height, 0, 1); // TODO configurable? right now i can't change the depth
		commandBuffer.CmdSetScissor(new(), SwapChainExtent); // TODO configurable?

		commandBuffer.CmdBindSamplerHeap(samplerDescriptors);
		commandBuffer.CmdBindResourceHeap(resourceDescriptors);

		commandBuffer.CmdBindGraphicsPipeline(GraphicsPipeline);

		commandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
		commandBuffer.CmdBindIndexBuffer(indexBuffer);

		commandBuffer.CmdDrawIndexed(3);
	}
}