using System.Collections;
using System.Collections.Frozen;
using Engine3.Test.Voxel.Modding;

namespace Engine3.Test.Voxel.Registries;

public sealed class BakedRegistry<T> : IEnumerable<T> where T : IRegistryObject {
	public ModSource ModSource { get; }
	public uint Count { get; }

	private readonly FrozenSet<T> allRegistryObjects;

	public BakedRegistry(ModSource modSource, FrozenSet<T> allRegistryObjects) {
		ModSource = modSource;
		Count = (uint)allRegistryObjects.Count;
		this.allRegistryObjects = allRegistryObjects;
	}

	public IEnumerator<T> GetEnumerator() => allRegistryObjects.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}