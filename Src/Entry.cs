namespace Engine4.Test;

public static class Entry {
	public static void Main(string[] args) {
		TestGame game = new(args);
		game.Start();
		Console.WriteLine("exit");
	}
}