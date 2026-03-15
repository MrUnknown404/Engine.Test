using Engine3.Client.Graphics.Console;
using Engine3.Utility.Versions;
using NLog;

namespace Engine3.Test.Test {
	// TODO make some utility methods? methods for drawing from left/right side. & drawing left/right?

	// https://en.wikipedia.org/wiki/Box-drawing_characters
	public class ConsoleTest : GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const string InputPrefix = "> ";
		public const byte InputPrefixLength = 2;
		public const char InputBlinkingCharacter = ':';
		public const byte InputHistoryWidth = 40;
		public const byte InputYOffset = 2;

		private readonly Thread inputThread;
		private readonly List<string> commandHistory = new();
		private int commandHistoryIndex;

		private char[] inputBuffer;
		private bool shouldRedrawInput;

		private ushort inputCursorPosition;
		private ushort inputLength;
		private bool cursorBlink;

		private readonly ConsoleGraphicsBackend testGraphics;

		internal ConsoleTest() : base("Console Test", new BuildVersion(0), new ConsoleGraphicsBackend()) { // TODO how do i hook into an external console's window to check for closing?
			TargetFps = 60;

			testGraphics = (ConsoleGraphicsBackend)GraphicsBackend;
			inputBuffer = new char[testGraphics.Width];

			inputThread = new(RunInputThread) { Name = "Input", };

			Console.CursorVisible = false;

			OnSetupFinishedEvent += OnSetupFinished;
		}

		private void OnSetupFinished() => inputThread.Start();

		protected override void Update() {
			if (testGraphics.TryResizeBuffer()) { // TODO on window resize? is that possible?
				inputBuffer = new char[testGraphics.Width];
				ClearInput();
				DrawHistory(InputYOffset, InputHistoryWidth, (ushort)(testGraphics.Height - 2));

				Logger.Info($"Buffer Size: ({testGraphics.Width}, {testGraphics.Height})");
			}

			const byte InputHistoryBorderPadding = 1;

			testGraphics.Blit('\u2500', 0, (ushort)(testGraphics.Height - InputYOffset), testGraphics.Width, 1); // input divider
			testGraphics.Blit('\u2502', InputHistoryWidth + InputHistoryBorderPadding, 0, 1, (ushort)(testGraphics.Height - InputYOffset)); // left border

			DrawRightSide();

			// test
			testGraphics.Blit('#', 20, 1, 3, 3);
			testGraphics.SetCharAt('#', 24, 1);

			// blink cursor
			if (inputCursorPosition + InputPrefixLength < testGraphics.Width - 1) {
				if (UpdateIndex % TargetUps == 0) {
					cursorBlink = !cursorBlink;
					shouldRedrawInput = true;
				}
			} else { cursorBlink = false; }

			TryUpdateInputBuffer(testGraphics.Width);

			return;

			void DrawRightSide() {
				const byte BeginXOffset = 4;
				const byte CellPadding = 2;
				const byte CellSize = 16;
				const byte BorderWidth = 1;
				const byte DividerWidth = 1;

				const byte StartX = BeginXOffset + BorderWidth;
				const byte RightCell = StartX + CellPadding;
				const byte Divider = RightCell + CellPadding + CellSize + DividerWidth;
				const byte LeftCell = Divider + CellPadding;
				const byte EndX = LeftCell + CellSize + CellPadding + DividerWidth;

				testGraphics.Blit('\u2502', (ushort)(testGraphics.Width - EndX), 0, 1, (ushort)(testGraphics.Height - InputYOffset)); // left line
				testGraphics.SetCharAt('\u2534', (ushort)(testGraphics.Width - EndX), (ushort)(testGraphics.Height - InputYOffset)); // left corner

				testGraphics.Blit('\u2502', (ushort)(testGraphics.Width - StartX), 0, 1, (ushort)(testGraphics.Height - InputYOffset)); // right line
				testGraphics.SetCharAt('\u2534', (ushort)(testGraphics.Width - StartX), (ushort)(testGraphics.Height - InputYOffset)); // right corner

				// right cell
				for (int i = 0; i < CellSize; i++) { testGraphics.SetCharAt(IntToHex(i), (ushort)(testGraphics.Width - RightCell - (CellSize - i)), (ushort)(testGraphics.Height - InputYOffset - 1)); }

				testGraphics.Blit(':', (ushort)(testGraphics.Width - Divider), 0, DividerWidth, (ushort)(testGraphics.Height - InputYOffset)); // divider

				// left cell
				for (int i = 0; i < CellSize; i++) { testGraphics.SetCharAt(IntToHex(i), (ushort)(testGraphics.Width - LeftCell - (CellSize - i)), (ushort)(testGraphics.Height - InputYOffset - 2)); }

				return;

				static char IntToHex(int value) =>
						value switch {
								< 10 => (char)(value + 48),
								< 16 => (char)(value + 55),
								_ => throw new ArgumentException(),
						};
			}

			void TryUpdateInputBuffer(ushort width) {
				if (!shouldRedrawInput) { return; }

				for (ushort i = 0; i < width; i++) {
					char value;

					if (i < InputPrefixLength) {
						value = InputPrefix[i]; //
					} else if (i - InputPrefixLength < inputLength) {
						value = inputBuffer[i - InputPrefixLength]; //
					} else if (i - InputPrefixLength == inputLength) {
						value = cursorBlink ? InputBlinkingCharacter : ' '; //
					} else {
						value = commandHistoryIndex > 0 && (i - InputPrefixLength) % 4 == 0 ? '-' : ' '; //
					}

					testGraphics.SetCharAt(value, i, (ushort)(testGraphics.Height - 1));
				}

				shouldRedrawInput = false;
			}
		}

		protected override void Cleanup() { }

		private void RunInputThread() {
			while (ShouldRunGameLoop) {
				TryAddInput();

				Thread.Sleep(1);
			}

			Logger.Debug("Input thread exit");

			return;

			void TryAddInput() { // TODO don't draw here. enqueue
				if (Console.KeyAvailable) {
					ConsoleKeyInfo key = Console.ReadKey(true);

					switch (key.Key) {
						case ConsoleKey.Backspace: RemoveKey(); break;
						case ConsoleKey.Enter: ProcessInput(); break;
						case ConsoleKey.Escape: ClearInput(); break;
						case ConsoleKey.LeftArrow: MoveCursorLeft(); break;
						case ConsoleKey.RightArrow: MoveCursorRight(); break;
						case ConsoleKey.UpArrow: HistoryUp(); break;
						case ConsoleKey.DownArrow: HistoryDown(); break;
						case >= ConsoleKey.A and <= ConsoleKey.Z or >= ConsoleKey.D0 and <= ConsoleKey.D9 or ConsoleKey.Spacebar: AddKey(key); break;
					}
				}
			}

			void AddKey(ConsoleKeyInfo key) {
				if (inputCursorPosition + InputPrefixLength >= testGraphics.Width) { return; }

				inputBuffer[inputCursorPosition] = key.KeyChar;

				inputCursorPosition = (ushort)Math.Min(inputCursorPosition + 1, testGraphics.Width - 1 - InputPrefixLength);
				inputLength++;

				if (commandHistoryIndex > 0) {
					commandHistoryIndex = -1;
					DrawHistory(InputYOffset, InputHistoryWidth, (ushort)(testGraphics.Height - InputYOffset));
				}

				shouldRedrawInput = true;
			}

			void RemoveKey() {
				if (inputCursorPosition == 0) { return; }

				inputCursorPosition = (ushort)Math.Min(inputCursorPosition - 1, testGraphics.Width - 1 - InputPrefixLength);
				inputLength--;

				inputBuffer[inputCursorPosition] = ' ';

				if (commandHistoryIndex > 0) {
					commandHistoryIndex = -1;
					DrawHistory(InputYOffset, InputHistoryWidth, (ushort)(testGraphics.Height - InputYOffset));
				}

				shouldRedrawInput = true;
			}

			void ProcessInput() {
				if (inputLength == 0) { return; }

				string cmd = new(inputBuffer[..Math.Min(inputLength, InputHistoryWidth)]);
				Logger.Trace($"Got: {cmd}");

				commandHistory.Add(cmd);
				commandHistoryIndex = 0;

				DrawHistory(InputYOffset, InputHistoryWidth, (ushort)(testGraphics.Height - InputYOffset));

				ClearInput();
			}

			void MoveCursorLeft() { throw new NotImplementedException(); }
			void MoveCursorRight() { throw new NotImplementedException(); }

			void HistoryUp() {
				if (commandHistory.Count == 0) { return; }

				if (commandHistoryIndex == -1) { commandHistoryIndex = 1; } else {
					commandHistoryIndex++;
					if (commandHistoryIndex - 1 >= commandHistory.Count) { commandHistoryIndex = -1; }
				}

				if (commandHistoryIndex > 0) { SetInput(commandHistory[^commandHistoryIndex]); } else { ClearInput(); }

				DrawHistory(InputYOffset, InputHistoryWidth, (ushort)(testGraphics.Height - InputYOffset));
			}

			void HistoryDown() {
				if (commandHistory.Count == 0) { return; }

				if (commandHistoryIndex == -1) { commandHistoryIndex = commandHistory.Count; } else {
					commandHistoryIndex--;
					if (commandHistoryIndex < 0) { commandHistoryIndex = commandHistory.Count; }
				}

				if (commandHistoryIndex > 0) { SetInput(commandHistory[^commandHistoryIndex]); } else { ClearInput(); }

				DrawHistory(InputYOffset, InputHistoryWidth, (ushort)(testGraphics.Height - InputYOffset));
			}
		}

		public void SetInput(string value) {
			if (value.Length > testGraphics.Width - InputPrefixLength) { throw new ArgumentOutOfRangeException(); }

			for (int i = 0; i < value.Length; i++) { inputBuffer[i] = value[i]; }
			for (int i = value.Length; i < testGraphics.Width - InputPrefixLength; i++) { inputBuffer[i] = ' '; }

			inputCursorPosition = (ushort)value.Length;
			inputLength = (ushort)value.Length;
			shouldRedrawInput = true;
		}

		private void ClearInput() {
			for (int i = 0; i < Math.Min(inputBuffer.Length, testGraphics.Width - InputPrefixLength); i++) { inputBuffer[i] = ' '; }

			inputCursorPosition = 0;
			inputLength = 0;
			shouldRedrawInput = true;
		}

		private void DrawHistory(ushort y, ushort width, ushort height) {
			int cmdCount = Math.Min(commandHistory.Count, height);

			if (cmdCount <= 0) { return; }

			for (int yi = 0; yi < cmdCount; yi++) {
				string str = commandHistory[commandHistory.Count - 1 - yi];

				ushort startX = 0;
				ushort endX = (ushort)str.Length;

				if (commandHistoryIndex != -1 && commandHistoryIndex <= cmdCount && commandHistoryIndex - 1 == yi) {
					testGraphics.SetCharAt('|', 0, (ushort)(height + 1 - y - yi));
					startX = 1;
					endX += 1;
				}

				for (ushort xi = startX; xi < endX; xi++) { testGraphics.SetCharAt(str[xi - startX], xi, (ushort)(height + 1 - y - yi)); }
				for (ushort xi = endX; xi < width; xi++) { testGraphics.SetCharAt(' ', xi, (ushort)(height + 1 - y - yi)); }
			}
		}
	}
}