using System.Reflection;
using Engine4.Client.Graphics;
using Engine4.Client.Graphics._Test;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Utility.Extensions;
using Engine4.Utility.Math;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Test;

public class TestRenderPass3 : GraphicsRenderPass3 {
	private readonly RenderGraph3.BufferHandle vertexBufferHandle;
	private readonly RenderGraph3.BufferHandle indexBufferHandle;

	private readonly VulkanBuffer vertexBuffer; // this won't work if i set up memory aliasing
	private readonly VulkanBuffer indexBuffer;

	private readonly GraphicsPipeline graphicsPipeline;

	public TestRenderPass3(VkFormat swapChainImageFormat, RenderGraph3.BufferHandle vertexBufferHandle, RenderGraph3.BufferHandle indexBufferHandle, Color4 clearColor) {
		this.vertexBufferHandle = vertexBufferHandle;
		this.indexBufferHandle = indexBufferHandle;
		vertexBuffer = RenderGraph.GetBuffer(vertexBufferHandle);
		indexBuffer = RenderGraph.GetBuffer(indexBufferHandle);
		ClearColor = clearColor.ToVkClearColorValue();

		// graphics pipeline TODO can this be more automated? should this be created elsewhere and then passes into here?

		Assembly assembly = GetType().Assembly;
		VulkanShader vertexShader = ResourceManager.CreateShader($"{nameof(TestRenderPass3)} Vertex Shader", assembly, "TestVertex", ShaderLanguage.Glsl, ShaderType.Vertex);
		VulkanShader fragmentShader = ResourceManager.CreateShader($"{nameof(TestRenderPass3)} Fragment Shader", assembly, "TestFragment", ShaderLanguage.Glsl, ShaderType.Fragment);

		VkVertexInputAttributeDescription[] vertexAttributeDescriptions;
		VkVertexInputBindingDescription[] vertexBindingDescriptions;

		graphicsPipeline = ResourceManager.CreateGraphicsPipeline(new GraphicsPipeline.Settings($"{nameof(TestRenderPass3)} Graphics Pipeline", swapChainImageFormat, [ vertexShader, fragmentShader, ],
			vertexAttributeDescriptions, vertexBindingDescriptions) {
				EnableDepthTest = true, //
				EnableDepthWrite = true,
		});

		// TODO destroy shaders
	}

	protected override void Execute(GraphicsCommandBuffer commandBuffer) {
		// DRAW. bind(?)/push constants/draw indexed/etc

		commandBuffer.CmdBindGraphicsPipeline(graphicsPipeline);

		commandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
		commandBuffer.CmdBindIndexBuffer(indexBuffer, 0);
		commandBuffer.CmdDrawIndexed(3);
	}
}