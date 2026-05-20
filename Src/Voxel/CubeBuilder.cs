using Engine3.Client.Graphics.VertexLayouts;
using Engine3.Test.Voxel.World;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel;

public static class CubeBuilder {
	private static readonly uint[] FaceIndices = [ 0, 1, 2, 2, 3, 0, ];

	[MustUseReturnValue]
	public static VertexXyzUv[] BuildFace(BlockFace face, float size, float x = 0, float y = 0, float z = 0) {
		float h = size / 2f;
		float x0 = x - h, x1 = x + h;
		float y0 = y - h, y1 = y + h;
		float z0 = z - h, z1 = z + h;

		const float U0 = 0, U1 = 1;
		const float V0 = 0, V1 = 1;

		return face switch { // (U shape)
				BlockFace.North => [
						new(x1, y1, z0, U0, V1), // Z-
						new(x1, y0, z0, U0, V0), //
						new(x0, y0, z0, U1, V0), //
						new(x0, y1, z0, U1, V1), //
				],
				BlockFace.East => [
						new(x1, y1, z1, U0, V1), // X+
						new(x1, y0, z1, U0, V0), //
						new(x1, y0, z0, U1, V0), //
						new(x1, y1, z0, U1, V1), //
				],
				BlockFace.South => [
						new(x0, y1, z1, U0, V1), // Z+
						new(x0, y0, z1, U0, V0), //
						new(x1, y0, z1, U1, V0), //
						new(x1, y1, z1, U1, V1), //
				],
				BlockFace.West => [
						new(x0, y1, z0, U0, V1), // X-
						new(x0, y0, z0, U0, V0), //
						new(x0, y0, z1, U1, V0), //
						new(x0, y1, z1, U1, V1), //
				],
				BlockFace.Up => [
						new(x0, y1, z0, U0, V1), // Y+
						new(x0, y1, z1, U0, V0), //
						new(x1, y1, z1, U1, V0), //
						new(x1, y1, z0, U1, V1), //
				],
				BlockFace.Down => [
						new(x1, y0, z0, U0, V1), // Y-
						new(x1, y0, z1, U0, V0), //
						new(x0, y0, z1, U1, V0), //
						new(x0, y0, z0, U1, V1), //
				],
				_ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
		};
	}

	public static void BuildFace(ref VertexXyzUv[] array, uint index, BlockFace face, float size, float x = 0, float y = 0, float z = 0) {
		VertexXyzUv[] vertices = BuildFace(face, size, x, y, z);
		for (int i = 0; i < vertices.Length; i++) { array[index + i] = vertices[i]; }
	}

	public static void BuildCube(BlockFaceMask faceMask, float size, float x, float y, float z, out VertexXyzUv[] vertices, out uint[] indices) {
		if (faceMask == BlockFaceMask.None) { throw new ArgumentException(); }

		const byte VerticesPerFace = 4;
		const byte IndicesPerFace = 6;

		bool hasNorth = faceMask.HasFlagFast(BlockFaceMask.North);
		bool hasEast = faceMask.HasFlagFast(BlockFaceMask.East);
		bool hasSouth = faceMask.HasFlagFast(BlockFaceMask.South);
		bool hasWest = faceMask.HasFlagFast(BlockFaceMask.West);
		bool hasUp = faceMask.HasFlagFast(BlockFaceMask.Up);
		bool hasDown = faceMask.HasFlagFast(BlockFaceMask.Down);

		byte count = (byte)new[] { hasNorth, hasEast, hasSouth, hasWest, hasUp, hasDown, }.AsValueEnumerable().Count(static b => b); // it would probably be faster to just check

		vertices = new VertexXyzUv[count * VerticesPerFace]; // should i just use a list?
		indices = new uint[count * IndicesPerFace];

		uint indexOffset = 0;
		uint vertexOffset = 0;

		if (hasNorth) {
			BuildFace(ref vertices, vertexOffset, BlockFace.North, size, x, y, z);
			BuildIndices(ref indices, indexOffset++);

			vertexOffset += VerticesPerFace;
		}

		if (hasEast) {
			BuildFace(ref vertices, vertexOffset, BlockFace.East, size, x, y, z);
			BuildIndices(ref indices, indexOffset++);

			vertexOffset += VerticesPerFace;
		}

		if (hasSouth) {
			BuildFace(ref vertices, vertexOffset, BlockFace.South, size, x, y, z);
			BuildIndices(ref indices, indexOffset++);

			vertexOffset += VerticesPerFace;
		}

		if (hasWest) {
			BuildFace(ref vertices, vertexOffset, BlockFace.West, size, x, y, z);
			BuildIndices(ref indices, indexOffset++);

			vertexOffset += VerticesPerFace;
		}

		if (hasUp) {
			BuildFace(ref vertices, vertexOffset, BlockFace.Up, size, x, y, z);
			BuildIndices(ref indices, indexOffset++);

			vertexOffset += VerticesPerFace;
		}

		if (hasDown) {
			BuildFace(ref vertices, vertexOffset, BlockFace.Down, size, x, y, z);
			BuildIndices(ref indices, indexOffset);
		}

		return;

		void BuildIndices(ref uint[] indices, uint offset) {
			for (int j = 0; j < IndicesPerFace; j++) { indices[offset * IndicesPerFace + j] = offset * VerticesPerFace + FaceIndices[j]; }
		}
	}
}