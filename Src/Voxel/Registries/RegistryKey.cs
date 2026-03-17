using Engine3.Test.Voxel.Exceptions;

namespace Engine3.Test.Voxel.Registries {
	public sealed class RegistryKey : IEquatable<RegistryKey> {
		public Modding.ModSource Source { get; }
		public string Key { get; }

		public RegistryKey(Modding.ModSource source, string key) {
			if (!Modding.ModSource.ValidNameRegex().IsMatch(key)) { throw new RegistryException($"Key must follow the following regex: {Modding.ModSource.ValidNameRegexString}"); }

			Source = source;
			Key = key;
		}

		public bool Equals(RegistryKey? other) => other is not null && other.Source == Source && other.Key == Key;
		public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is RegistryKey other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(Source, Key);

		public static bool operator ==(RegistryKey? left, RegistryKey? right) => Equals(left, right);
		public static bool operator !=(RegistryKey? left, RegistryKey? right) => !Equals(left, right);

		public override string ToString() => $"{Source.Name}:{Key}";
	}
}