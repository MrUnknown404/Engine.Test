using Engine4.Graphics.IO;

namespace Engine4.Test;

public static class Entry {
	public static void Main(string[] args) {
		using (Engine engine = new(args, new OpenTKEventHandler())) {
			TestGame game = new(engine);
			engine.Start(game);

			// loop exit
			Console.WriteLine("exit");
		}
	}
}