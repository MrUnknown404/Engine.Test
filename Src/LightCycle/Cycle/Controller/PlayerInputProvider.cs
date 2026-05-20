using Engine3.Client;
using OpenTK.Platform;

namespace Engine3.Test.LightCycle.Cycle.Controller;

public class PlayerInputProvider : IPlayerInputProvider {
	private readonly KeyboardManager keyboardManager;

	public PlayerInputProvider(KeyboardManager keyboardManager) => this.keyboardManager = keyboardManager;

	public bool TurnLeft => keyboardManager.IsKey(Key.A) || keyboardManager.IsKey(Key.LeftArrow);
	public bool TurnRight => keyboardManager.IsKey(Key.D) || keyboardManager.IsKey(Key.RightArrow);
}