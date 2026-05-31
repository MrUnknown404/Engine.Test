using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Engine3.Client;
using Engine3.Client.Client.Graphics;
using Engine3.Client.Client.Graphics.Console;
using Engine3.Client.Client.Graphics.OpenGL;
using Engine3.Client.Client.Graphics.Vulkan;
using Engine3.Core;
using Engine3.Core.Debug;
using Engine3.Core.Utility;
using Engine3.Test.Core.Test;
using Engine3.Test.LightCycle;
using Engine3.Test.Test;
using Engine3.Test.Tests;
using Engine3.Test.Voxel;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.World;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test;

public static class Entry {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const TestType TestType = Core.Test.TestType.Voxel;

#pragma warning disable CS0162 // Unreachable code detected
	[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
	private static void Main() { // TODO args to change api
#if DEBUG
		LoggerH.ConsoleLogLevel = LogLevel.Trace;
#endif
		LoggerH.PrintToConsole = TestType != TestType.ConsoleGraphicsTest;

		if (TestType == TestType.Automated) {
			AutomatedTests.AutomatedEntryTests();
			Logger.Info("Entry Exit");
			return;
		}

#if DEBUG
		StructLayoutDumper.AddStructs += static () => {
			StructLayoutDumper.AddStruct<XyzGizmoPushConstants>();
			StructLayoutDumper.AddStruct<PerChunkData>();
			StructLayoutDumper.AddStruct<ChunkPos>();
			StructLayoutDumper.AddStruct<LocalBlockPos>();
			StructLayoutDumper.AddStruct<GlobalBlockPos>();
		};
#endif

		// {
		// 		Settings = new() { EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt, },
		// }

		PerformanceMonitor performanceMonitor = new() { CalculateMinMaxAverage = true, StoreTimesForGraph = true, LastFrameTimeSize = 1000, };
		GraphicsBackend graphicsBackend = TestType switch {
				TestType.VulkanGraphicsTest => new VulkanBackend(new()) {
						Settings = new() {
								EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
						},
						IsPhysicalDeviceSuitable = static (settings, physicalDeviceProperties, physicalDeviceFeatures) =>
								VulkanBackend.DefaultIsPhysicalDeviceSuitable(settings, physicalDeviceProperties, physicalDeviceFeatures) && physicalDeviceFeatures.multiDrawIndirect == VkH.True,
				},
				TestType.OpenGLGraphicsTest => new OpenGLBackend(new()),
				TestType.ConsoleGraphicsTest => new ConsoleGraphicsBackend(),
				TestType.LightCycleOpenGL => new OpenGLBackend(new()),
				TestType.LightCycleVulkan => new VulkanBackend(new()) {
						Settings = new() {
								EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
						},
				},
				TestType.Voxel => new VulkanBackend(new()) {
						Settings = new() {
								EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
						},
				},
				TestType.Automated => new VulkanBackend(new()) {
						Settings = new() {
								EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
						},
				},
				_ => throw new ArgumentOutOfRangeException(),
		};

		using Engine3Client engine = new(graphicsBackend) { PerformanceMonitor = performanceMonitor, };

		EngineGame game = TestType switch {
				// tests
				TestType.VulkanGraphicsTest => new VulkanTest(),
				TestType.OpenGLGraphicsTest => new OpenGLTest(),
				TestType.ConsoleGraphicsTest => new ConsoleTest(),
				TestType.Automated => throw new UnreachableException(),

				// games
				TestType.LightCycleOpenGL => new LightCycleTest(false),
				TestType.LightCycleVulkan => new LightCycleTest(true),
				TestType.Voxel => new VoxelTest(true),

				_ => throw new ArgumentOutOfRangeException(),
		};

		engine.Start(game);

		Logger.Info("Entry Exit");
	}
#pragma warning restore CS0162 // Unreachable code detected
}