using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.VertexLayouts;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Test.Graphics.Vulkan;

public unsafe class VulkanRenderer2 : VulkanRendererBase {
	private const string TestShaderName = "Test";

	private GraphicsPipeline? graphicsPipeline;

	private VulkanBuffer? vertexBuffer;

	private readonly VertexXyzRgb[] vertices = [ new(0, 0.5f, 0, 1, 0, 0), new(-0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, -0.5f, 0, 0, 0, 1), ];
	private readonly Assembly gameAssembly;

	public VulkanRenderer2(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Assembly gameAssembly) : base(graphicsBackend, window, false) {
		this.gameAssembly = gameAssembly;

		VulkanShader vertexShader = GraphicsResourceProvider.CreateShader("Test Vertex Shader", TestShaderName, ShaderLanguage.Hlsl, ShaderType.Vertex, gameAssembly);
		VulkanShader fragmentShader = GraphicsResourceProvider.CreateShader("Test Fragment Shader", TestShaderName, ShaderLanguage.Hlsl, ShaderType.Fragment, gameAssembly);

		graphicsPipeline = GraphicsResourceProvider.CreateGraphicsPipeline(new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzRgb.GetAttributeDescriptions(),
			VertexXyzRgb.GetBindingDescriptions()) { FrontFace = VkFrontFace.FrontFaceClockwise, });

		GraphicsResourceProvider.EnqueueDestroy(vertexShader);
		GraphicsResourceProvider.EnqueueDestroy(fragmentShader);

		vertexBuffer = GraphicsResourceProvider.CreateBuffer("Test Vertex Buffer", VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, (ulong)(sizeof(VertexXyzRgb) * vertices.Length));

		vertexBuffer.Copy(vertices);
	}

	protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer) {
		if (vertexBuffer == null || graphicsPipeline == null) { return; }

		commandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

		commandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
		commandBuffer.CmdSetScissor(0, 0, SwapChain.Extent);

		commandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);

		commandBuffer.CmdDraw((uint)vertices.Length);
	}

	protected override void CopyBuffers(float delta) { }
}