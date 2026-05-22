using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics;
using Engine3.Test.Core.Test;
using Engine3.Test.LightCycle;
using Engine3.Test.Test;
using Engine3.Test.Tests;
using Engine3.Test.Voxel;
using NLog;

#if DEBUG
using Engine3.Debug;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.World;
using Engine3.Utility;
#endif

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

		using Engine3 engine = new(TestType switch {
				TestType.VulkanGraphicsTest => GraphicsBackend.Vulkan,
				TestType.OpenGLGraphicsTest => GraphicsBackend.OpenGL,
				TestType.ConsoleGraphicsTest => GraphicsBackend.Console,
				TestType.LightCycleOpenGL => GraphicsBackend.OpenGL,
				TestType.LightCycleVulkan => GraphicsBackend.Vulkan,
				TestType.Voxel => GraphicsBackend.Vulkan,
				TestType.Automated => GraphicsBackend.Vulkan,
				_ => throw new ArgumentOutOfRangeException(),
		});

		engine.Initialize(new() { PrintToConsole = TestType != TestType.ConsoleGraphicsTest, });

		engine.StartGame<GameClient>(TestType switch {
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
		});

		// gameClient.Start(new());
		Logger.Info("Entry Exit");
	}
#pragma warning restore CS0162 // Unreachable code detected
}