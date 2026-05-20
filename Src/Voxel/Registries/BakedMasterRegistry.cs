using System.Collections.Frozen;
using System.Collections.Immutable;
using Engine3.Test.Voxel.Modding;

namespace Engine3.Test.Voxel.Registries;

public sealed class BakedMasterRegistry<T> where T : IRegistryObject {
	public FrozenSet<T> AllObjects { get; }
	public ImmutableArray<T> AllObjectsOrdered { get; }

	public FrozenDictionary<ModSource, BakedRegistry<T>> AllRegistries { get; }

	public BakedMasterRegistry(FrozenDictionary<ModSource, BakedRegistry<T>> allRegistries) {
		AllRegistries = allRegistries;
		AllObjects = allRegistries.Values.AsValueEnumerable().SelectMany(static s => s).ToFrozenSet();
		AllObjectsOrdered = AllObjects.ToImmutableArray();
	}

	public BakedRegistry<T> GetRegistry(ModSource modSource) => AllRegistries.TryGetValue(modSource, out BakedRegistry<T>? registry) ? registry : throw new ArgumentException($"No key: {modSource}");
}