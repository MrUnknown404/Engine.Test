using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine3.Test.Voxel.Graphics.DataStructs;

[StructLayout(LayoutKind.Explicit)]
public readonly record struct WorldLightingPushConstants {
	[field: FieldOffset(0)] public uint PackedColor { get; init; }
	[field: FieldOffset(4)] public Vector3 LightDirection { get; init; }

	public WorldLightingPushConstants(uint packedColor, Vector3 lightDirection) {
		PackedColor = packedColor;
		LightDirection = lightDirection;
	}
}