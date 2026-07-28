using Engine4.Graphics;

namespace Engine4.Test;

public static class Entry {
	public static readonly GraphicsApi GraphicsApi = GraphicsApi.Vulkan;

	public static void Main(string[] args) {
		using (Engine engine = new(args, GraphicsApi)) {
			TestGame game = new(engine);
			engine.Start(game);

			// loop exit
			Console.WriteLine("exit");
		}
	}
}