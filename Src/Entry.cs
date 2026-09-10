using Engine4.Client;
using NLog.Time;

namespace Engine4.Test;

public static class Entry {
	public static void Main(string[] args) {
		TestGame game = new();
		game.Start(args, new ClientStartupSettings { LoggingSettings = new() { TimeSource = new AccurateUtcTimeSource(), }, LoadVulkan = true, LoadGlfw = true, });
		Console.WriteLine("exit");
	}
}