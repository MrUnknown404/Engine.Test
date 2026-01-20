using System.Diagnostics.CodeAnalysis;
using Engine3.Graphics;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test {
	public static class Entry {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const GraphicsApi TestGraphicsApi = GraphicsApi.Vulkan;

		[SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
		private static void Main() { // TODO args to change api
#pragma warning disable CS0162 // Unreachable code detected
			GameClient gameClient = TestGraphicsApi switch {
					GraphicsApi.Console => new ConsoleTest(),
					GraphicsApi.OpenGL => new OpenGLTest(),
					GraphicsApi.Vulkan => new VulkanTest {
							EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
					},
					_ => throw new ArgumentOutOfRangeException(),
			};
#pragma warning restore CS0162 // Unreachable code detected

			gameClient.Start(gameClient, new());
			Logger.Info("Entry Exit");
		}
	}
}