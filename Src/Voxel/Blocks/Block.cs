using Engine3.Test.Voxel.Registries;

namespace Engine3.Test.Voxel.Blocks {
	public class Block : IRegistryObject {
		public RegistryKey RegistryKey { get; }

		public BlockProperties Properties { get; }

		public Block(RegistryKey registryKey, BlockProperties? properties = null) {
			RegistryKey = registryKey;
			Properties = properties ?? new();
		}
	}
}