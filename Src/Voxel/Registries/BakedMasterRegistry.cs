using System.Collections.Frozen;
using Engine3.Test.Voxel.Modding;

namespace Engine3.Test.Voxel.Registries {
	public sealed class BakedMasterRegistry<T> where T : IRegistryObject {
		private readonly FrozenSet<T> allObjects;
		private readonly FrozenDictionary<ModSource, BakedRegistry<T>> allRegistries;

		public IEnumerable<T> AllObjects => allObjects;
		public IEnumerable<BakedRegistry<T>> AllRegistries => allRegistries.Values;

		public uint AllObjectCount { get; }

		public BakedMasterRegistry(FrozenDictionary<ModSource, BakedRegistry<T>> allRegistries) {
			this.allRegistries = allRegistries;
			allObjects = allRegistries.Values.AsValueEnumerable().SelectMany(static s => s).ToFrozenSet();
			AllObjectCount = (uint)allObjects.Count;
		}

		public BakedRegistry<T> GetRegistry(ModSource modSource) => allRegistries.TryGetValue(modSource, out BakedRegistry<T>? registry) ? registry : throw new ArgumentException($"No key: {modSource}");
	}
}