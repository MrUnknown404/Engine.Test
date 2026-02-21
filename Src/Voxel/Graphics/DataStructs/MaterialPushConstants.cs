namespace Engine3.Test.Voxel.Graphics.DataStructs {
	public readonly record struct MaterialPushConstants {
		public uint MaterialIndex { get; init; }
		public MaterialPushConstants(uint materialIndex) => MaterialIndex = materialIndex;
	}
}