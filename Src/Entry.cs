using System.Diagnostics.CodeAnalysis;
using Engine3.Test.Core.Test;
using Engine3.Test.LightCycle;
using Engine3.Test.Test;
using Engine3.Test.Voxel;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Test.Voxel.World;
using Engine3.Utility;
using NLog;

#if DEBUG
using Engine3.Debug;
#endif

namespace Engine3.Test;

public static class Entry {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const TestType TestType = Core.Test.TestType.Voxel;

	[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
	private static void Main() { // TODO args to change api
#if DEBUG
		LoggerH.ConsoleLogLevel = LogLevel.Trace;

		StructLayoutDumper.AddStructs += static () => {
			StructLayoutDumper.AddStruct<XyzGizmoPushConstants>();
			StructLayoutDumper.AddStruct<PerChunkData>();
			StructLayoutDumper.AddStruct<ChunkPos>();
			StructLayoutDumper.AddStruct<LocalBlockPos>();
			StructLayoutDumper.AddStruct<GlobalBlockPos>();
		};
#endif

		GameClient gameClient = TestType switch {
				// tests
				TestType.VulkanGraphicsTest => new VulkanTest(),
				TestType.OpenGLGraphicsTest => new OpenGLTest(),
				TestType.ConsoleGraphicsTest => new ConsoleTest(),

				// games
				TestType.LightCycle => new LightCycleTest(true),
				TestType.Voxel => new VoxelTest(true),
				_ => throw new ArgumentOutOfRangeException(),
		};

		gameClient.Start(new());
		Logger.Info("Entry Exit");
	}
}