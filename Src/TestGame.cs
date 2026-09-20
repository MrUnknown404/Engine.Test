using Engine4.Client;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Rendering;
using Engine4.IO;
using Engine4.Utility.Math;
using Engine4.Utility.Versions;
using NLog;

namespace Engine4.Test;

public class TestGame : GameClient {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Game);

	public Window? VulkanWindow0 { get; private set; }
	public Window? VulkanWindow1 { get; private set; }

	public VulkanRenderer VulkanWindow0Renderer { get; private set; } = null!;
	public VulkanRenderer VulkanWindow1Renderer { get; private set; } = null!;

	public RenderPass TestRenderPass { get; private set; } = null!;

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

// 		Action<RenderGraph> setupRenderGraph = static graph => {
// 			const uint Size = 0; // TODO
// 			const ushort Width = 1920, Height = 1080;
//
// 			RenderGraph.BufferHandle vertexBuffer = graph.AddBuffer("vertex buffer", Size, VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit); // TODO what if we want to resize later? auto resize behind the scenes?
// 			RenderGraph.BufferHandle indexBuffer = graph.AddBuffer("index buffer", Size, VkBufferUsageFlagBits2.BufferUsage2IndexBufferBit);
// 			// RenderGraph.ImageHandle swapchainImage = graph.AddTexture("swapchain image", Width, Height, format, VkImageUsageFlagBits.ImageUsageColorAttachmentBit, VkImageLayout.ImageLayoutPresentSrcKhr);
// 			// RenderGraph.ImageHandle depthImage = graph.AddTexture("depth image", Width, Height, format, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit, VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal);
//
// 			// TODO how do i set data?
//
// 			// RenderPassBuilder copyPassBuilder = new("test copy pass", RenderPassStage.Transfer, Exec); // compute/graphics/transfer
// 			// copyPassBuilder.AddOutput(vertexBuffer);
// 			// copyPassBuilder.AddOutput(indexBuffer);
// 			// RenderGraph.RenderPassHandle testCopyPass = graph.AddPass(copyPassBuilder);
//
// 			RenderPassBuilder drawPassBuilder = new("test draw pass", RenderPassStage.Graphics, Exec); // compute/graphics/transfer
// 			drawPassBuilder.AddInput(vertexBuffer);
// 			drawPassBuilder.AddInput(indexBuffer);
// 			// drawPassBuilder.AddOutput(swapchainImage);
// 			// drawPassBuilder.AddOutput(depthImage);
// 			RenderGraph.RenderPassHandle testDrawPass = graph.AddPass(drawPassBuilder);
//
// 			return;
//
// 			static void Exec(GraphicsCommandBuffer graphicsCommandBuffer) {
// 				graphicsCommandBuffer.CmdBeginRendering(extent, colorView, clearColor, depthView, depthStencil);
// 				// DRAW
// 				graphicsCommandBuffer.CmdEndRendering();
// 			}
//
// 			/*
// 			private static void Test(VulkanResourceManager resourceManager) { // test example
// 				RenderGraph renderGraph = new(resourceManager);
//
// 				BufferHandle vertexBuffer = renderGraph.AddBuffer("vertex buffer"); // TODO what if we want to resize later? auto resize behind the scenes?
// 				BufferHandle indexBuffer = renderGraph.AddBuffer("index buffer");
// 				// ImageHandle testImage = renderGraph.AddTexture("test image");
//
// 				RenderPassBuilder passBuilder = new("testPass", RenderPassStage.Graphics, null); // compute/graphics/transfer
// 				passBuilder.AddInput(vertexBuffer);
// 				passBuilder.AddInput(indexBuffer);
// 				// passBuilder.AddInput(testImage);
// 				RenderPassHandle testPass = renderGraph.AddPass(passBuilder);
//
// 				renderGraph.DisablePass(testPass);
// 				renderGraph.EnablePass(testPass);
//
// 				renderGraph.RemovePass(testPass);
// 			   }
// 			 */
// 		};

		Logger.Trace("Making renderers");
		Color4 clearColor = new(0.005f, 0.005f, 0.005f, 1);
		VulkanWindow0Renderer = CreateRenderer(nameof(VulkanWindow0Renderer), VulkanWindow0, clearColor, TestRenderPass);
		VulkanWindow1Renderer = CreateRenderer(nameof(VulkanWindow1Renderer), VulkanWindow1, clearColor, TestRenderPass);
		// ConsoleRenderer = new TestConsoleRenderer();

		Logger.Trace("trace test");
		Logger.Debug("debug test");
		Logger.Info("Info test");
		Logger.Warn("warn test");
		Logger.Error("error test");
		Logger.Fatal("fatal test");
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