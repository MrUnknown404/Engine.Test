namespace Engine3.Test.LightCycle.Cycle.Controller;

public class PlayerCycleController : ICycleController {
	public IPlayerInputProvider InputProvider { get; }

	private bool leftHoldCheck;
	private bool rightHoldCheck;

	public PlayerCycleController(IPlayerInputProvider inputProvider) => InputProvider = inputProvider;

	public Direction CheckForDirectionChange(Direction currentDirection) {
		if (InputProvider.TurnLeft) {
			if (!leftHoldCheck) {
				leftHoldCheck = true;

				return currentDirection switch {
						Direction.Up => Direction.Left,
						Direction.Down => Direction.Right,
						Direction.Left => Direction.Down,
						Direction.Right => Direction.Up,
						_ => throw new ArgumentOutOfRangeException(nameof(currentDirection), currentDirection, null),
				};
			}
		} else { leftHoldCheck = false; }

		if (InputProvider.TurnRight) {
			if (!rightHoldCheck) {
				rightHoldCheck = true;

				return currentDirection switch {
						Direction.Up => Direction.Right,
						Direction.Down => Direction.Left,
						Direction.Left => Direction.Up,
						Direction.Right => Direction.Down,
						_ => throw new ArgumentOutOfRangeException(nameof(currentDirection), currentDirection, null),
				};
			}
		} else { rightHoldCheck = false; }

		return currentDirection;
	}
}