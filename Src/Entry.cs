using Engine4.Client.Utility;
using NLog.Time;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Test;

public static class Entry {
	private static void Main(string[] args) {
		TestGame game = new();
		game.Start(args,
			new ClientStartupSettings {
					LoggingSettings = new() { TimeSource = new AccurateUtcTimeSource(), },
					LoadGlfw = true,
					VulkanSettings = new() {
							EnabledDebugMessageSeverities = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt,
					},
			});

		Console.WriteLine("exit");
	}
}