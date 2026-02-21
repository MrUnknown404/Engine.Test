using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.DataStructs;
using Engine3.Client.Graphics.Vertex;
using Engine3.Debug;
using Engine3.Test.Core.Test;
using Engine3.Test.LightCycle;
using Engine3.Test.Test;
using Engine3.Test.Voxel;
using Engine3.Utility;
using NLog;

namespace Engine3.Test {
	public static class Entry {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const GraphicsBackend TestGraphicsBackend = GraphicsBackend.Vulkan;
		private const TestType TestType = global::Engine3.Test.Core.Test.TestType.Voxel;

		[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
		private static void Main() { // TODO args to change api
			LoggerH.ConsoleLogLevel = LogLevel.Trace;

#if DEBUG
			StructLayoutDumper.AddStructs += static () => {
				StructLayoutDumper.AddStruct<VertexXyzRgb>();
				StructLayoutDumper.AddStruct<VertexXyzUvRgb>();
				StructLayoutDumper.AddStruct<ProjectionView>();
			};
#endif

			GameClient gameClient = TestType switch {
					TestType.GraphicsTest => TestGraphicsBackend switch {
							GraphicsBackend.Console => new ConsoleTest(),
							GraphicsBackend.OpenGL => new OpenGLTest(),
							GraphicsBackend.Vulkan => new VulkanTest(),
							_ => throw new ArgumentOutOfRangeException(),
					},
					TestType.LightCycle => new LightCycleTest(TestGraphicsBackend == GraphicsBackend.Vulkan),
					TestType.Voxel => new VoxelTest(TestGraphicsBackend == GraphicsBackend.Vulkan),
					_ => throw new ArgumentOutOfRangeException(),
			};

			gameClient.Start(gameClient, new());
			Logger.Info("Entry Exit");
		}
	}
}