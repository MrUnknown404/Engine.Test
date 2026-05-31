using Engine3.Client;
using Engine3.Client.Client.Graphics.Vulkan;
using Engine3.Core;
using Engine3.Core.Utility.Versions;
using NLog;

namespace Engine3.Test.Tests;

// TODO automate some tests
public class AutomatedTests : EngineGame {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private AutomatedTests() : base(nameof(AutomatedTests), new BuildVersion(0)) { }

	internal static void AutomatedEntryTests() {
		using Engine3Client engine = new(new VulkanBackend(new()));

		Logger.Info("Creating first test");
		AutomatedTests tests = new();

		Logger.Debug("Starting first test");
		engine.Start(tests);

		Logger.Debug("Shutting down first test");
		tests.RequestShutdown();

		Logger.Info("Creating second test");
		tests = new();

		Logger.Debug("Starting second test");
		engine.Start(tests);

		Logger.Debug("Shutting down second test");
		tests.RequestShutdown();

		Logger.Info("Done!");
	}

	protected override void Update() { }
	protected override void Cleanup() { }
}