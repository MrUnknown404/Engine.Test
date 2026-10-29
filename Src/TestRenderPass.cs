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

	private readonly VulkanRenderer renderer;
	private VkExtent2D SwapChainExtent => renderer.GetSwapChainExtent();

	public TestRenderPass(VulkanRenderer renderer) : base(CreateGraphicsPipeline(renderer.ResourceManager, renderer.GetSwapChainFormat(), typeof(TestRenderPass).Assembly)) {
		this.renderer = renderer;
		VulkanResourceManager resourceManager = renderer.ResourceManager;

		TestVertex[] vertices = [ // xyz rgb
				new(0, -0.5f, 0, 1, 0, 0), //
				new(-0.5f, 0.5f, 0, 0, 1, 0), //
				new(0.5f, 0.5f, 0, 0, 0, 1), //
		];

		uint[] indices = [ 0, 1, 2, ];

		vertexBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Vertex Buffer", (ulong)(vertices.Length * sizeof(TestVertex)), VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		indexBuffer = resourceManager.CreateBuffer($"{nameof(TestRenderPass)} Index Buffer", (ulong)(indices.Length * sizeof(uint)), VkBufferUsageFlagBits2.BufferUsage2IndexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		vertexBuffer.Copy(vertices);
		indexBuffer.Copy(indices);
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