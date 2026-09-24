using Engine4.Client.Graphics._Test;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Utility.Extensions;
using Engine4.Utility.Math;

namespace Engine4.Test;

public class TestRenderPass3 : GraphicsRenderPass3 {
	private readonly RenderGraph3.BufferHandle vertexBufferHandle;
	private readonly RenderGraph3.BufferHandle indexBufferHandle;

	public TestRenderPass3(RenderGraph3.BufferHandle vertexBufferHandle, RenderGraph3.BufferHandle indexBufferHandle, Color4 clearColor) {
		this.vertexBufferHandle = vertexBufferHandle;
		this.indexBufferHandle = indexBufferHandle;
		ClearColor = clearColor.ToVkClearColorValue();
	}

	protected override void Execute(GraphicsCommandBuffer commandBuffer) {
		// DRAW. bind(?)/push constants/draw indexed/etc

		// TODO pipeline/draw

		// commandBuffer.CmdBindGraphicsPipeline();
		commandBuffer.CmdBindVertexBuffer(RenderGraph.GetBuffer(vertexBufferHandle), 0);
		commandBuffer.CmdBindIndexBuffer(RenderGraph.GetBuffer(indexBufferHandle), 0);
		// commandBuffer.CmdDrawIndexed(0);
	}
}