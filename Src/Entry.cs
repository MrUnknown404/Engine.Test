using Engine4.Graphics;
using Engine4.IO;

namespace Engine4.Test;

public static class Entry {
	public const GraphicsApi GraphicsApi = Engine4.Graphics.GraphicsApi.Vulkan;

	public static void Main(string[] args) {
		using (Engine engine = new(args, GraphicsApi, new OpenTKEventHandler())) {
			TestGame game = new(engine);
			engine.Start(game);

			// loop exit
			Console.WriteLine("exit");
		}
	}
}