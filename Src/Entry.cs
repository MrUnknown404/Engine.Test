using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics;
using NLog;

namespace Engine3.Test {
	public static class Entry {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const GraphicsBackend TestGraphicsBackend = GraphicsBackend.Vulkan;

		[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
		private static void Main() { // TODO args to change api
#pragma warning disable CS0162 // Unreachable code detected
			GameClient gameClient = TestGraphicsBackend switch {
					GraphicsBackend.Console => new ConsoleTest(),
					GraphicsBackend.OpenGL => new OpenGLTest(),
					GraphicsBackend.Vulkan => new VulkanTest(),
					_ => throw new ArgumentOutOfRangeException(),
			};
#pragma warning restore CS0162 // Unreachable code detected

			gameClient.Start(gameClient, new());
			Logger.Info("Entry Exit");
		}
	}
}