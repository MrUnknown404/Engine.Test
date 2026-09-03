namespace Engine4.Test;

public static class Entry {
	public static void Main(string[] args) {
		TestGame2 game = new(args);
		game.Start();
		Console.WriteLine("exit");

		// using (Engine engine = new(args)) {
		// 	TestGame game = new(engine);
		// 	engine.Start(game);
		//
		// 	// loop exit
		// 	Console.WriteLine("exit");
		// }
	}
}