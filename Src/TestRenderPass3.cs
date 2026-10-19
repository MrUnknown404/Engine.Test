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

	private readonly GraphicsPipeline graphicsPipeline;

	public TestRenderPass3(RenderGraph3 renderGraph, VkFormat swapChainImageFormat, RenderGraph3.BufferHandle vertexBufferHandle, RenderGraph3.BufferHandle indexBufferHandle, Color4 clearColor) : base(renderGraph) {
		this.vertexBufferHandle = vertexBufferHandle;
		this.indexBufferHandle = indexBufferHandle;
		ClearColor = clearColor.ToVkClearColorValue();

		// graphics pipeline TODO can this be more automated? should this be created elsewhere and then passes into here?

		Assembly assembly = GetType().Assembly;
		VulkanShader vertexShader = ResourceManager.CreateShader($"{nameof(TestRenderPass3)} Vertex Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Vertex);
		VulkanShader fragmentShader = ResourceManager.CreateShader($"{nameof(TestRenderPass3)} Fragment Shader", assembly, "Test", ShaderLanguage.Glsl, ShaderType.Fragment);

		graphicsPipeline = ResourceManager.CreateGraphicsPipeline(new GraphicsPipeline.Settings($"{nameof(TestRenderPass3)} Graphics Pipeline", swapChainImageFormat, [ vertexShader, fragmentShader, ],
			TestVertex.GetAttributeDescriptions(0), TestVertex.GetBindingDescriptions(0)) {
				EnableDepthTest = true, //
				EnableDepthWrite = true,
		});

		// TODO destroy shaders
	}

	protected override void Execute(GraphicsCommandBuffer commandBuffer) {
		// DRAW. bind(?)/push constants/draw indexed/etc

		commandBuffer.CmdBindGraphicsPipeline(graphicsPipeline);

		commandBuffer.CmdBindVertexBuffer(RenderGraph.GetBuffer(vertexBufferHandle), 0);
		commandBuffer.CmdBindIndexBuffer(RenderGraph.GetBuffer(indexBufferHandle), 0);

		commandBuffer.CmdDrawIndexed(3);
	}
}