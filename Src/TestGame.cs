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

		if (!AnyWindowsExist) {
			RequestShutdown(true);
			return;
		}

		if (UpdateCount % TargetUps == 0) { Logger.Debug($"hello world {UpdateCount / TargetUps}"); }

		// if (UpdateCount == TargetUps * 3) { VulkanWindow1?.RequestClose(false); }
		// if (UpdateCount == TargetUps * 10) { RequestShutdown(false); }
	}

	protected override void Cleanup() { }
}