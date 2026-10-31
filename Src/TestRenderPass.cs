using System.Reflection;
using Engine4.Client.Graphics;
using Engine4.Client.Graphics._Test;
using Engine4.Client.Graphics.Rendering;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Test;

public unsafe class TestRenderPass : RenderPass {
	protected override GraphicsPipeline GraphicsPipeline { get; }

	private readonly VulkanBuffer vertexBuffer;
	private readonly VulkanBuffer indexBuffer;

	private readonly VulkanTexture testTexture;

	private readonly SamplerDescriptors samplerDescriptors;
	private readonly ResourceDescriptors resourceDescriptors;

	private readonly Renderer renderer;
	private VkExtent2D SwapChainExtent => renderer.GetSwapChainExtent();

	public TestRenderPass(Renderer renderer) {
		this.renderer = renderer;

		TestVertex[] vertices = [ // xyz uv rgb
				new(-0.5f, -0.5f, 0, 0, 0, 1, 0, 0), // top left
				new(-0.5f, 0.5f, 0, 1, 0, 0, 1, 0), // bottom left
				new(0.5f, -0.5f, 0, 0, 1, 0, 0, 1), // top right
				new(0.5f, 0.5f, 0, 1, 1, 1, 1, 1), // bottom right
		];

		uint[] indices = [
				0, 1, 2, //
				2, 1, 3,
		];

		VulkanSettings vulkanSettings = renderer.VulkanManager.VulkanSettings;
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

		byte[] pixelData = new byte[64 * 64 * 4]; // test
		Array.Fill(pixelData, byte.MaxValue); // TODO Something isn't working here. image is still black

		renderer.TransferCommandPool.CopyToImage(testTexture, 64, 64, 4, pixelData);

		// TODO copy data to texture

		samplerDescriptors = new(resourceManager, physicalGpu, logicalGpu,
		[
				new() {
						minFilter = VkFilter.FilterLinear,
						magFilter = VkFilter.FilterLinear,
						addressModeU = VkSamplerAddressMode.SamplerAddressModeRepeat,
						addressModeV = VkSamplerAddressMode.SamplerAddressModeRepeat,
						addressModeW = VkSamplerAddressMode.SamplerAddressModeRepeat,
						anisotropyEnable = vulkanSettings.AllowEnableAnisotropy ? VkH.True : VkH.False,
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

		GraphicsPipeline = CreateGraphicsPipeline(renderer.ResourceManager, renderer.GetSwapChainFormat(), typeof(TestRenderPass).Assembly);
	}

	[MustUseReturnValue]
	private static GraphicsPipeline CreateGraphicsPipeline(VulkanResourceManager resourceManager, VkFormat swapChainImageFormat, Assembly assembly) {
		Shader vertexShader = resourceManager.CreateShader($"{nameof(TestRenderPass3)} Vertex Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Vertex);
		Shader fragmentShader = resourceManager.CreateShader($"{nameof(TestRenderPass3)} Fragment Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Fragment);

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

		commandBuffer.CmdDrawIndexed(6);
	}
}