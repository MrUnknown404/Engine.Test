using Engine3.Client;

namespace Engine3.Test.LightCycle {
	public class GameManager {
		public Map? Map { get; private set; }

		public void Setup(KeyboardManager keyboardManager) {
			Map = new("Test Map", 10, new(1));
			Map.AddCycles(keyboardManager);
		}

		public void Update() {
			if (Map == null) { return; }

			Map.Update();
		}
	}
}