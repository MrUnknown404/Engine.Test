using Engine4.Client;
using Engine4.Client.Graphics._Test;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Rendering;
using Engine4.IO;
using Engine4.Utility.Math;
using Engine4.Utility.Versions;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Test;

public class TestGame : GameClient {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Game);

	public Window? VulkanWindow0 { get; private set; }
	public Window? VulkanWindow1 { get; private set; }

	public VulkanRenderer? VulkanWindow0Renderer { get; private set; }
	public VulkanRenderer? VulkanWindow1Renderer { get; private set; }

	public RenderPass? TestRenderPass { get; private set; }

	public TestGame() : base("Test Game", new BuildVersion(0)) {
		PerformanceMonitor = new();
		TargetUps = 60;
		TargetFps = 0;

		OnSetupDoneEvent += OnSetupDone;
	}

	private void OnSetupDone() {
		VulkanWindow0?.Show();
		VulkanWindow1?.Show();
	}

	protected override void SetupGame() {
		const string Title = "title goes here";
		Logger.Debug(Title);

		Logger.Trace("Making windows");
		VulkanWindow0 = CreateWindow($"vulkan 0. {Title}", 854, 480);
		VulkanWindow1 = CreateWindow($"vulkan 1. {Title}", 854, 480);

		Logger.Trace("Making render passes");
		TestRenderPass = new TestRenderPass();

		Logger.Trace("Making renderers");
		Color4 clearColor = new(0.005f, 0.005f, 0.005f, 1);

		VulkanWindow0Renderer = CreateRenderer(nameof(VulkanWindow0Renderer), VulkanWindow0, clearColor, SetupRenderGraph, TestRenderPass);
		VulkanWindow1Renderer = CreateRenderer(nameof(VulkanWindow1Renderer), VulkanWindow1, clearColor, SetupRenderGraph, TestRenderPass);
		// ConsoleRenderer = new TestConsoleRenderer();

		Logger.Trace("trace test");
		Logger.Debug("debug test");
		Logger.Info("Info test");
		Logger.Warn("warn test");
		Logger.Error("error test");
		Logger.Fatal("fatal test");

		return;

		static void SetupRenderGraph(RenderGraph3 graph, VulkanRenderer renderer) {
			const ulong BufferSize = sizeof(uint) * 10;
			const ushort Width = 1920, Height = 1080;
			Color4 clearColor = new(0.001f, 0.001f, 0.001f, 1);

			// TODO what if we want to resize later? auto resize behind the scenes?
			RenderGraph3.BufferHandle vertexBuffer1 = graph.AddBuffer("vertex buffer 1", BufferSize, VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);
			RenderGraph3.BufferHandle vertexBuffer2 = graph.AddBuffer("vertex buffer 2", BufferSize, VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);
			RenderGraph3.BufferHandle indexBuffer = graph.AddBuffer("index buffer", BufferSize, VkBufferUsageFlagBits2.BufferUsage2IndexBufferBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

			// TODO upload data somehow

			// swap chain?
			// RenderGraph3.TextureHandle colorImage = graph.AddTexture("color image", Width, Height, renderer.GetSwapChainFormat(), VkImageUsageFlagBits.ImageUsageColorAttachmentBit);
			// RenderGraph3.TextureHandle depthImage = graph.AddTexture("depth image", Width, Height, renderer.GetDepthFormat(), VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit);

			TestRenderPass3 renderPass1 = new(vertexBuffer1, indexBuffer, clearColor);
			TestRenderPass3 renderPass2 = new(vertexBuffer2, indexBuffer, clearColor);
			// renderPass1.SetDepthImage(depthImage, new(1, 0));
			// renderPass2.SetDepthImage(depthImage, new(1, 0));

			renderPass1.AddInput(vertexBuffer1, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
			renderPass1.AddInput(indexBuffer, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
			// renderPass1.AddOutput(colorImage);
			// renderPass1.AddOutput(depthImage);

			renderPass2.AddInput(vertexBuffer2, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
			renderPass2.AddInput(indexBuffer, VkPipelineStageFlagBits2.PipelineStage2AllGraphicsBit);
			// renderPass2.AddOutput(colorImage);
			// renderPass2.AddOutput(depthImage);

			RenderGraph3.RenderPassHandle graphicsPass1 = graph.AddPass("graphics pass 1", renderPass1);
			RenderGraph3.RenderPassHandle graphicsPass2 = graph.AddPass("graphics pass 2", renderPass2);
		}
	}

	protected override void Update() {
		// Logger.Trace($"Update Count: {UpdateCount}, Ups: {PerformanceMonitor?.Ups.ToString() ?? "null"}");
		// Logger.Trace($"Frame Count: {FrameCount}, Fps: {PerformanceMonitor?.Fps.ToString() ?? "null"}");

		// Thread.Sleep(1); // simulating lag

		if (!AnyWindowsExist) { // TODO should i just allow window visibility?
			RequestShutdown(true);
			return;
		}

		if (UpdateCount % TargetUps == 0) { Logger.Debug($"hello world {UpdateCount / TargetUps}"); }

		// if (UpdateCount == TargetUps * 3) { VulkanWindow1?.RequestClose(false); }
		// if (UpdateCount == TargetUps * 10) { RequestShutdown(false); }
	}

	protected override void Cleanup() { }
}