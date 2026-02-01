using Engine3.Client;
using OpenTK.Platform;

namespace Engine3.Test.LightCycle.Cycle.Controller {
	public class PlayerInputProvider : IPlayerInputProvider {
		private readonly InputManager inputManager;

		public PlayerInputProvider(InputManager inputManager) => this.inputManager = inputManager;

		public bool TurnLeft => inputManager.GetKey(Key.A) || inputManager.GetKey(Key.LeftArrow);
		public bool TurnRight => inputManager.GetKey(Key.D) || inputManager.GetKey(Key.RightArrow);
	}
}