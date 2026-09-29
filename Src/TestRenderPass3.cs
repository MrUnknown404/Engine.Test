using Engine4.Client.Graphics._Test;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Utility.Extensions;
using Engine4.Utility.Math;

namespace Engine4.Test;

public class TestRenderPass3 : GraphicsRenderPass3 {
	private readonly RenderGraph3.BufferHandle vertexBufferHandle;
	private readonly RenderGraph3.BufferHandle indexBufferHandle;

	private readonly VulkanBuffer vertexBuffer; // this won't work if i set up memory aliasing
	private readonly VulkanBuffer indexBuffer;

	public TestRenderPass3(RenderGraph3.BufferHandle vertexBufferHandle, RenderGraph3.BufferHandle indexBufferHandle, Color4 clearColor) {
		this.vertexBufferHandle = vertexBufferHandle;
		this.indexBufferHandle = indexBufferHandle;
		vertexBuffer = RenderGraph.GetBuffer(vertexBufferHandle);
		indexBuffer = RenderGraph.GetBuffer(indexBufferHandle);
		ClearColor = clearColor.ToVkClearColorValue();

		// TODO create shaders & pipeline
	}

	protected override void Execute(GraphicsCommandBuffer commandBuffer) {
		// DRAW. bind(?)/push constants/draw indexed/etc

		commandBuffer.CmdBindGraphicsPipeline();

		commandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
		commandBuffer.CmdBindIndexBuffer(indexBuffer, 0);
		commandBuffer.CmdDrawIndexed(3);
	}
}