using System.Diagnostics;
using Engine3.Client.Graphics.Console;
using Engine3.Utility.Versions;
using NLog;

namespace Engine3.Test.Test {
	public class ConsoleTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly Stopwatch stopwatch = new();
		private readonly Random random = new();

		private int width = Console.BufferWidth;
		private int height = Console.BufferHeight;
		private char[,] buffer;

		internal ConsoleTest() : base("Console Test", new Version4Interweaved(0, 0, 0), new ConsoleGraphicsBackend()) {
			OnSetupFinishedEvent += OnOnSetupFinishedEvent;
			Console.CursorVisible = false;
			buffer = NewBuffer(Console.BufferWidth, Console.BufferHeight);
		}

		private static void OnOnSetupFinishedEvent() { }

		protected override void Update() { // this only works in rider's console. i'm getting junk with external console
			if (Console.KeyAvailable) { Console.ReadKey(true); } // destroy input?

			if (width != Console.BufferWidth || height != Console.BufferHeight) {
				width = Console.BufferWidth;
				height = Console.BufferHeight;
				buffer = NewBuffer(width, height);
			}

			char[] row = new char[width];

			UpdateBuffer();

			stopwatch.Start();
			TimeSpan startTime = stopwatch.Elapsed;

			for (int y = 0; y < height; y++) {
				Buffer.BlockCopy(buffer, y * width, row, 0, width);
				Console.SetCursorPosition(0, y);
				Console.Write(row);
			}

			TimeSpan endTime = stopwatch.Elapsed;
			double difference = (endTime - startTime).TotalMilliseconds;

			Logger.Debug($"Setting console buffer took {difference:F}ms");
		}

		private void UpdateBuffer() {
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					buffer[y, x] = random.Next(200) switch {
							0 => '#',
							1 => 'o',
							2 => 'x',
							3 => '*',
							4 => '.',
							_ => ' ',
					};
				}
			}
		}

		private static char[,] NewBuffer(int width, int height) {
			char[,] buffer = new char[height, width];

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) { buffer[y, x] = ' '; }
			}

			Logger.Debug($"Buffer resize: {width}x{height}");
			return buffer;
		}

		protected override void Cleanup() { }
	}
}