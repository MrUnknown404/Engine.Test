using System.Runtime.InteropServices;

namespace Engine3.Test.Voxel.Graphics.DataStructs {
	[StructLayout(LayoutKind.Explicit)]
	public readonly record struct XyzGizmoPushConstants {
		[field: FieldOffset(0)] public uint MaterialIndex { get; init; }

		public XyzGizmoPushConstants(uint materialIndex) => MaterialIndex = materialIndex;
	}
}