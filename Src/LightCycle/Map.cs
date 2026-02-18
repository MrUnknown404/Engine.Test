using Engine3.Client;
using Engine3.Test.LightCycle.Cycle.Controller;

namespace Engine3.Test.LightCycle {
	public class Map {
		public string Name { get; }
		public uint Size { get; }

		private readonly List<Cycle.Cycle> cycles = new(); // TODO move to GameManager
		private readonly CycleProperties cycleProperties;

		public IEnumerable<Cycle.Cycle> Cycles => cycles;

		public Map(string name, uint size, CycleProperties cycleProperties) {
			Name = name;
			Size = size;
			this.cycleProperties = cycleProperties;
		}

		public void AddCycles(KeyManager keyManager) {
			cycles.Add(new(Guid.CreateVersion7(), new PlayerCycleController(new PlayerInputProvider(keyManager)), cycleProperties)); //
		}

		public void Update() {
			foreach (Cycle.Cycle cycle in cycles) { cycle.Update(); }
		}

		public class CycleProperties {
			public float Speed { get; }

			public CycleProperties(float speed) => Speed = speed;
		}
	}
}