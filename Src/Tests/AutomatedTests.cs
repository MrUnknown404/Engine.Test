using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Utility.Versions;
using NLog;

namespace Engine3.Test.Tests;

// TODO automate some tests
public class AutomatedTests : GameClient {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private AutomatedTests(EngineGraphicsBackend graphicsBackend) : base(nameof(AutomatedTests), new BuildVersion(0), graphicsBackend) { }

	internal static void AutomatedEntryTests() {
		using Engine3 engine = new(Client.Graphics.GraphicsBackend.Vulkan);
		engine.Initialize(new());

		Logger.Info("Beginning first test");
		AutomatedTests tests = new(new VulkanBackend(new()));

		Logger.Debug("Starting first test");
		engine.StartGame(tests);

		Logger.Debug("Shutting down first test");
		tests.RequestShutdown();

		Logger.Info("Beginning second test");
		tests = new(new VulkanBackend(new()));

		Logger.Debug("Starting second test");
		engine.StartGame(tests);

		Logger.Debug("Shutting down second test");
		tests.RequestShutdown();

		Logger.Info("Done!");
	}

	protected override void Update() { }
	protected override void Cleanup() { }
}