using Engine3.Client;
using OpenTK.Platform;

namespace Engine3.Test.LightCycle.Cycle.Controller {
	public class PlayerInputProvider : IPlayerInputProvider {
		private readonly KeyManager keyManager;

		public PlayerInputProvider(KeyManager keyManager) => this.keyManager = keyManager;

		public bool TurnLeft => keyManager.IsKey(Key.A) || keyManager.IsKey(Key.LeftArrow);
		public bool TurnRight => keyManager.IsKey(Key.D) || keyManager.IsKey(Key.RightArrow);
	}
}