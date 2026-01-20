using Engine3.Graphics;

namespace Engine3.Test {
	public class ConsoleTest : GameClient {
		public ConsoleTest() : base("Console Test", new(0, 0, 0), GraphicsApi.Console) { }

		protected override void Update() { }
		protected override void Cleanup() { }
	}
}