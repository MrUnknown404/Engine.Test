using System.Diagnostics.CodeAnalysis;
using Engine4.Client;
using Engine4.Client.Graphics.Rendering;
using Engine4.Client.Windowing;
using Engine4.IO;
using Engine4.Utility.Math;
using Engine4.Utility.Versions;
using NLog;

namespace Engine4.Test;

public class TestGame : GameClient {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Game);

	private Window? vulkanWindow0;
	private Window? vulkanWindow1;

	private Renderer? vulkanWindow0Renderer;
	private Renderer? vulkanWindow1Renderer;

	public Window? VulkanWindow0 => vulkanWindow0;
	public Window? VulkanWindow1 => vulkanWindow1;

	public Renderer? VulkanWindow0Renderer => vulkanWindow0Renderer;
	public Renderer? VulkanWindow1Renderer => vulkanWindow1Renderer;

	public TestRenderPass? TestRenderPass0 { get; private set; }
	public TestRenderPass? TestRenderPass1 { get; private set; }

	internal TestGame() : base("Test Game", new BuildVersion(0)) {
		PerformanceMonitor = new();
		TargetUps = 60;
		TargetFps = 0;

		OnSetupDoneEvent += OnSetupDone;
	}

	private void OnSetupDone() {
		VulkanWindow0?.Show();
		VulkanWindow1?.Show();
	}

	[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
	protected override void SetupGame() {
		const string Title = "title goes here";
		const bool CreateSecondWindow = false;

		Color4 clearColor = new(0.005f, 0.005f, 0.005f, 1);

		Logger.Debug(Title);

		Logger.Trace("Making windows & renderers...");
		CreateWindow($"vulkan 0. {Title}", 854, 480, clearColor, out vulkanWindow0, out vulkanWindow0Renderer);
		TestRenderPass0 = new(vulkanWindow0Renderer);
		vulkanWindow0Renderer.AddRenderPass(TestRenderPass0);

#pragma warning disable CS0162 // Unreachable code detected
		if (CreateSecondWindow) { // for testing
			CreateWindow($"vulkan 1. {Title}", 854, 480, clearColor, out vulkanWindow1, out vulkanWindow1Renderer);
			TestRenderPass1 = new(vulkanWindow1Renderer);
			vulkanWindow1Renderer.AddRenderPass(TestRenderPass1);
		}
#pragma warning restore CS0162 // Unreachable code detected

		// ConsoleRenderer = new TestConsoleRenderer();

		Logger.Trace("trace test");
		Logger.Debug("debug test");
		Logger.Info("Info test");
		Logger.Warn("warn test");
		Logger.Error("error test");
		Logger.Fatal("fatal test");

		return;

		// static void SetupRenderGraph(RenderGraph3 graph, VulkanRenderer renderer) {
		// 	const ulong BufferSize = sizeof(uint) * 10;
		// 	const ushort Width = 1920, Height = 1080;
		// 	Color4 clearColor = new(0.001f, 0.001f, 0.001f, 1);
		//
		// 	// TODO what if we want to resize later? auto resize behind the scenes?
		// 	RenderGraph3.BufferHandle vertexBuffer1 = graph.AddBuffer("vertex buffer 1", BufferSize, VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);
		// 	RenderGraph3.BufferHandle vertexBuffer2 = graph.AddBuffer("vertex buffer 2", BufferSize, VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);
		// 	RenderGraph3.BufferHandle indexBuffer = graph.AddBuffer("index buffer", BufferSize, VkBufferUsageFlagBits2.BufferUsage2IndexBufferBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);
		//
		// 	// TODO upload data somehow
		//
		// 	// swap chain?
		// 	// RenderGraph3.TextureHandle colorImage = graph.AddTexture("color image", Width, Height, renderer.GetSwapChainFormat(), VkImageUsageFlagBits.ImageUsageColorAttachmentBit);
		// 	// RenderGraph3.TextureHandle depthImage = graph.AddTexture("depth image", Width, Height, renderer.GetDepthFormat(), VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit);
		//
		// 	TestRenderPass3 renderPass1 = new(graph, renderer.GetSwapChainFormat(), vertexBuffer1, indexBuffer, clearColor);
		// 	TestRenderPass3 renderPass2 = new(graph, renderer.GetSwapChainFormat(), vertexBuffer2, indexBuffer, clearColor);
		// 	// renderPass1.SetDepthImage(depthImage, new(1, 0));
		// 	// renderPass2.SetDepthImage(depthImage, new(1, 0));
		//
		// 	renderPass1.AddInput(vertexBuffer1, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
		// 	renderPass1.AddInput(indexBuffer, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
		// 	// renderPass1.AddOutput(colorImage);
		// 	// renderPass1.AddOutput(depthImage);
		//
		// 	renderPass2.AddInput(vertexBuffer2, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
		// 	renderPass2.AddInput(indexBuffer, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
		// 	// renderPass2.AddOutput(colorImage);
		// 	// renderPass2.AddOutput(depthImage);
		//
		// 	RenderGraph3.RenderPassHandle graphicsPass1 = graph.AddPass("graphics pass 1", renderPass1);
		// 	RenderGraph3.RenderPassHandle graphicsPass2 = graph.AddPass("graphics pass 2", renderPass2);
		// }
	}

	protected override void Update() {
		if (!AnyWindowsExist) { // TODO should i just allow window visibility?
			RequestShutdown(true);
			return;
		}
	}

	protected override void Cleanup() { }
}