using System.Reflection;
using System.Text.RegularExpressions;
using Engine3.Test.Voxel.Exceptions;

namespace Engine3.Test.Voxel.Modding {
	public sealed partial class ModSource {
		public const string ValidNameRegexString = "^([a-z_])+$";

		public string Name { get; }
		public Assembly Assembly { get; }

		public ModSource(string name, Assembly assembly) {
			if (!ValidNameRegex().IsMatch(name)) { throw new RegistryException($"Name must follow the following regex: {ValidNameRegexString}"); }

			Name = name;
			Assembly = assembly;
		}

		[GeneratedRegex(ValidNameRegexString, RegexOptions.Compiled)]
		public static partial Regex ValidNameRegex();
	}
}