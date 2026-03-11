using System.Numerics;
using USharpLibs.Common.Math;

namespace Engine3.Test.Voxel.World {
	public readonly record struct GlobalBlockPos {
		public int X { get; init; }
		public int Y { get; init; }
		public int Z { get; init; }

		public GlobalBlockPos(int x, int y, int z) {
			X = x;
			Y = y;
			Z = z;
		}

		public GlobalBlockPos(Vector3 vector3) {
			X = MathH.Round(vector3.X, MidpointRounding.ToNegativeInfinity);
			Y = MathH.Round(vector3.Y, MidpointRounding.ToNegativeInfinity);
			Z = MathH.Round(vector3.Z, MidpointRounding.ToNegativeInfinity);
		}
	}
}