using System.Numerics;
using Engine3.Test.LightCycle.Cycle.Controller;

namespace Engine3.Test.LightCycle.Cycle {
	public class Cycle {
		private const byte SlowdownModifier = 100;

		private static readonly uint TargetUpdateCount = Engine3.GameInstance.TargetUpdateCount;

		public Guid Uuid { get; }
		public CycleTransform Transform { get; } = CycleTransform.Zero;
		public Direction Direction { get; private set; }

		public bool IsDead { get; private set; }

		private readonly ICycleController controller;
		private readonly Map.CycleProperties properties;

		public Cycle(Guid uuid, ICycleController controller, Map.CycleProperties properties, Direction direction = Direction.Up) {
			Uuid = uuid;
			this.controller = controller;
			this.properties = properties;
			Direction = direction;
		}

		public void Update() {
			if (IsDead) { return; }

			bool isDead = ShouldBeDead();
			IsDead = isDead;
			if (IsDead) { return; }

			Direction = controller.CheckForDirectionChange(Direction);
			UpdateValues();

			return;

			bool ShouldBeDead() => false; // TODO impl later

			void UpdateValues() {
				Vector2 moveVector = Direction switch {
						Direction.Up => Vector2.UnitY, // why does Y need to be flipped?
						Direction.Down => -Vector2.UnitY,
						Direction.Left => -Vector2.UnitX,
						Direction.Right => Vector2.UnitX,
						_ => throw new ArgumentOutOfRangeException(),
				};

				Transform.Position += moveVector * properties.Speed / TargetUpdateCount / SlowdownModifier; // TODO impl acceleration
			}
		}
	}
}