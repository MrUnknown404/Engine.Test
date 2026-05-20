using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Graphics.DataStructs;

[StructLayout(LayoutKind.Explicit)]
public readonly record struct PerChunkData {
	[field: FieldOffset(0)] public required ChunkPos ChunkPos { get; init; }

	[SetsRequiredMembers] public PerChunkData(ChunkPos chunkPos) => ChunkPos = chunkPos;
}