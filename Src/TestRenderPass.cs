using System.Reflection;
using Engine4.Client.Graphics;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Rendering;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Test;

public unsafe class TestRenderPass : RenderPass {
	private readonly VulkanBuffer vertexBuffer;
	private readonly VulkanBuffer indexBuffer;

	private readonly VulkanBuffer descriptorSamplerHeapBuffer;
	private readonly VulkanBuffer descriptorResourceHeapBuffer;

	private readonly Texture testTexture;
	private readonly TextureSampler testSampler;

	private readonly ulong bufferHeapOffset;
	private readonly ulong bufferDescriptorSize;
	private readonly ulong imageHeapOffset;
	private readonly ulong imageDescriptorSize;
	private readonly ulong samplerHeapOffset;
	private readonly ulong samplerDescriptorSize;

	private readonly VulkanRenderer renderer;
	private VkExtent2D SwapChainExtent => renderer.GetSwapChainExtent();

	public TestRenderPass(VulkanRenderer renderer) : base(CreateGraphicsPipeline(renderer.ResourceManager, renderer.GetSwapChainFormat(), typeof(TestRenderPass).Assembly)) {
		this.renderer = renderer;

		TestVertex[] vertices = [ // xyz rgb
				new(0, -0.5f, 0, 1, 0, 0), //
				new(-0.5f, 0.5f, 0, 0, 1, 0), //
				new(0.5f, 0.5f, 0, 0, 0, 1), //
		];

		uint[] indices = [ 0, 1, 2, ];

		VulkanManager vulkanManager = renderer.VulkanManager;
		VulkanResourceManager resourceManager = renderer.ResourceManager;

		vertexBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Vertex Buffer", (ulong)(vertices.Length * sizeof(TestVertex)), VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		indexBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Index Buffer", (ulong)(indices.Length * sizeof(uint)), VkBufferUsageFlagBits2.BufferUsage2IndexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		const byte TempSize = sizeof(uint); // TODO

		// https://docs.vulkan.org/guide/latest/descriptor_heap.html
		// https://github.com/SaschaWillems/Vulkan/blob/master/examples/descriptorheap/descriptorheap.cpp

		descriptorSamplerHeapBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Descriptor Sampler Heap Buffer", TempSize,
			VkBufferUsageFlagBits2.BufferUsage2SamplerDescriptorBufferBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		descriptorResourceHeapBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Descriptor Resource Heap Buffer", TempSize,
			VkBufferUsageFlagBits2.BufferUsage2SamplerDescriptorBufferBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		vertexBuffer.Copy(vertices);
		indexBuffer.Copy(indices);

		testTexture = resourceManager.CreateTexture($"{nameof(TestRenderPass)} Test Texture", 64, 64, VkFormat.FormatR8g8b8a8Srgb);
		testSampler = new(vulkanManager, new TextureSampler.Settings(VkFilter.FilterLinear, VkFilter.FilterLinear, renderer.PhysicalGpu));

		LogicalGpu logicalDevice = renderer.LogicalGpu;
		SurfaceReadyPhysicalGpu physicalGpu = renderer.PhysicalGpu;
		VkPhysicalDeviceDescriptorHeapPropertiesEXT descriptorHeapProperties = physicalGpu.DescriptorHeapProperties;

		// VkDevice logicalDevice = renderer.LogicalGpu.VkLogicalDevice;
		// VkImageViewCreateInfo imageViewCreateInfo = testTexture.ViewCreateInfo; // copy
		//
		// VkImageDescriptorInfoEXT imageDescriptorInfo = new() {
		// 		pView = &imageViewCreateInfo, //
		// 		layout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal,
		// };
		//
		// VkResourceDescriptorInfoEXT[] resourceDescriptorInfo = [
		// 		new() { type = VkDescriptorType.DescriptorTypeSampledImage, data = new() { pImage = &imageDescriptorInfo, }, }, //
		// ];
		//
		// VkHostAddressRangeEXT[] resourceHostAddressRange = [
		// 		new() { address = , size = , }, //
		// ];
		//
		// // resourceDescriptorInfo.length must equal hostAddressRange.length
		//
		// fixed (VkResourceDescriptorInfoEXT* pResourceDescriptorInfo = resourceDescriptorInfo) {
		// 	fixed (VkHostAddressRangeEXT* pResourceHostAddressRange = resourceHostAddressRange) {
		// 		Vk.WriteResourceDescriptorsEXT(logicalDevice, (uint)resourceDescriptorInfo.Length, pResourceDescriptorInfo, pResourceHostAddressRange);
		// 	}
		// }
		//
		// VkSamplerCreateInfo[] samplerCreateInfos = [
		// 		new() { }, // todo
		// ];
		//
		// VkHostAddressRangeEXT[] samplerHostAddressRange = [
		// 		new() { address = , size = , }, //
		// ];
		//
		// fixed (VkSamplerCreateInfo* pSamplerCreateInfos = samplerCreateInfos) {
		// 	fixed (VkHostAddressRangeEXT* pSamplerHostAddressRange = samplerHostAddressRange) {
		// 		Vk.WriteSamplerDescriptorsEXT(logicalDevice, (uint)samplerCreateInfos.Length, pSamplerCreateInfos, pSamplerHostAddressRange);
		// 	}
		// }
	}

	[MustUseReturnValue]
	private static GraphicsPipeline CreateGraphicsPipeline(VulkanResourceManager resourceManager, VkFormat swapChainImageFormat, Assembly assembly) {
		VulkanShader vertexShader = resourceManager.CreateShader($"{nameof(TestRenderPass3)} Vertex Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Vertex);
		VulkanShader fragmentShader = resourceManager.CreateShader($"{nameof(TestRenderPass3)} Fragment Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Fragment);

		GraphicsPipeline graphicsPipeline = resourceManager.CreateGraphicsPipeline(new GraphicsPipeline.Settings($"{nameof(TestRenderPass3)} Graphics Pipeline", swapChainImageFormat, [ vertexShader, fragmentShader, ],
			TestVertex.GetAttributeDescriptions(0), TestVertex.GetBindingDescriptions(0)) {
				EnableDepthTest = true, //
				EnableDepthWrite = true,
		});

		resourceManager.EnqueueDestroy(vertexShader);
		resourceManager.EnqueueDestroy(fragmentShader);

		return graphicsPipeline;
	}

	protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer) {
		commandBuffer.CmdSetViewport(0, 0, SwapChainExtent.width, SwapChainExtent.height, 0, 1); // TODO configurable? right now i can't change the depth
		commandBuffer.CmdSetScissor(new(), SwapChainExtent); // TODO configurable?

		commandBuffer.CmdBindGraphicsPipeline(GraphicsPipeline);

		commandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
		commandBuffer.CmdBindIndexBuffer(indexBuffer);

		commandBuffer.CmdDrawIndexed(3);
	}
}