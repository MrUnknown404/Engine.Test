using Vector3 = System.Numerics.Vector3;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public readonly record struct WorldFragmentPushConstants {
		public uint PackedColor { get; init; }
		public Vector3 LightDirection { get; init; }

		public WorldFragmentPushConstants(uint packedColor, Vector3 lightDirection) {
			PackedColor = packedColor;
			LightDirection = lightDirection;
		}
	}
}