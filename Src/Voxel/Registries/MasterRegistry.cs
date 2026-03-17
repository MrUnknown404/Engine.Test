using System.Collections.Frozen;
using Engine3.Test.Voxel.Modding;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.Registries {
	[MustDisposeResource]
	public class MasterRegistry<T> : IDisposable where T : IRegistryObject {
		private readonly Dictionary<ModSource, BakedRegistry<T>> allObjectsBySource = new();

		private bool wasDisposed;

		public void AddRegistry(BakedRegistry<T> registry) {
			if (!allObjectsBySource.TryAdd(registry.ModSource, registry)) { throw new ArgumentException($"Registry already contains mod source: {registry.ModSource}"); }
		}

		public BakedMasterRegistry<T> Bake() => new(allObjectsBySource.ToFrozenDictionary());

		public void Dispose() { // not really used for disposing. used to help avoid keeping this object around. use BakedMasterRegistry
			if (wasDisposed) { return; }
			GC.SuppressFinalize(this);
			wasDisposed = true;
		}
	}
}