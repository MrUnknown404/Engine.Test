using System.Numerics;
using Engine3.Client.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel {
	public static class PlaneBuilder {
		[MustUseReturnValue]
		public static Vector3[] BuildPlane(Vector3 position, Vector2 scale, Vector3 normal) {
			float hx = scale.X / 2f, hy = scale.Y / 2f;

			//
			throw new NotImplementedException();
		}
	}

	public static class CubeBuilder {
		[MustUseReturnValue]
		public static VertexXyzUvRgb[] BuildFace(BlockFace face, float size, float x = 0, float y = 0, float z = 0) {
			float h = size / 2f;
			float x0 = x - h, x1 = x + h;
			float y0 = y - h, y1 = y + h;
			float z0 = z - h, z1 = z + h;

			const float U0 = 0, U1 = 1;
			const float V0 = 0, V1 = 1;
			const byte R = 1, G = 1, B = 1;

			// return face switch { // (U shape)
			// 		// BlockFace.North => expr,
			// 		// BlockFace.East => expr,
			// 		// BlockFace.South => expr,
			// 		// BlockFace.West => expr,
			// 		BlockFace.Up => [
			// 				new(x0, y, z0, U0, V1, R, G, B), // Y+ (Top north)
			// 				new(x0, y, z1, U0, V0, R, G, B), //
			// 				new(x1, y, z1, U1, V0, R, G, B), //
			// 				new(x1, y, z0, U1, V1, R, G, B), //
			// 		],
			// 		// BlockFace.Down => [
			// 		// 		new(x0, y, z0, U0, V1, R, G, B), // Y- (Top north)
			// 		// 		new(x0, y, z1, U0, V0, R, G, B), //
			// 		// 		new(x1, y, z1, U1, V0, R, G, B), //
			// 		// 		new(x1, y, z0, U1, V1, R, G, B), //
			// 		// ],
			// 		_ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
			// };

			throw new NotImplementedException();
		}

		public static void BuildFace(ref VertexXyzUvRgb[] array, uint index, BlockFace face, float size, float x = 0, float y = 0, float z = 0) {
			VertexXyzUvRgb[] a = BuildFace(face, size, x, y, z);
			for (int i = 0; i < a.Length; i++) { array[index + i] = a[i]; }
		}

		[MustUseReturnValue]
		public static VertexXyzUvRgb[] BuildCube(BlockFaceMask faceMask, float size) {
			if (faceMask == BlockFaceMask.None) { throw new ArgumentException(); }

			const byte VertexPerFace = 4;

			bool hasNorth = faceMask.HasFlagFast(BlockFaceMask.North);
			bool hasEast = faceMask.HasFlagFast(BlockFaceMask.East);
			bool hasSouth = faceMask.HasFlagFast(BlockFaceMask.South);
			bool hasWest = faceMask.HasFlagFast(BlockFaceMask.West);
			bool hasUp = faceMask.HasFlagFast(BlockFaceMask.Up);
			bool hasDown = faceMask.HasFlagFast(BlockFaceMask.Down);

			byte count = (byte)new[] { hasNorth, hasEast, hasSouth, hasWest, hasUp, hasDown, }.AsValueEnumerable().Count(static b => b);

			VertexXyzUvRgb[] array = new VertexXyzUvRgb[count * VertexPerFace];
			uint offset = 0;

			if (hasNorth) {
				BuildFace(ref array, offset, BlockFace.North, size);
				offset += VertexPerFace;
			}

			if (hasEast) {
				BuildFace(ref array, offset, BlockFace.East, size);
				offset += VertexPerFace;
			}

			if (hasSouth) {
				BuildFace(ref array, offset, BlockFace.South, size);
				offset += VertexPerFace;
			}

			if (hasWest) {
				BuildFace(ref array, offset, BlockFace.West, size);
				offset += VertexPerFace;
			}

			if (hasUp) {
				BuildFace(ref array, offset, BlockFace.Up, size, y: size / 2f);
				offset += VertexPerFace;
			}

			if (hasDown) { BuildFace(ref array, offset, BlockFace.Down, size, y: -(size / 2f)); }

			return array;
		}
	}
}
