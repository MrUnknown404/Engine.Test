namespace Engine4.Test;

public static class Entry {
	public static void Main(string[] args) {
		TestGame game = new();
		game.Start(args);
		Console.WriteLine("exit");
	}
}