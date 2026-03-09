using Engine3.Client;
using Engine3.Client.Graphics.ImGui;
using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Graphics.ImGui {
	public class WorldImGuiMaker : IImGuiMaker<World.World> {
		private WorldImGuiMaker() { }

		public static void ShowImGui(World.World world) {
			ImGuiNet.SeparatorText("World");

			if (ImGuiNet.BeginTable("Values", 2)) {
				DrawProperty("Chunk Count", world.ChunkCount.ToString());
				DrawProperty("Dirty Chunk Count", world.DirtyChunkCount.ToString());
				DrawProperty("Renderer Chunk Count", world.RendererChunkCount.ToString());

				ImGuiNet.EndTable();
			}

			ImGuiNet.Separator();

			ImGuiH.IndentedCollapsingHeader("Properties", 6, DrawProperties); // TODO get indent from somewhere

			if (ImGuiNet.Button("Mark All Chunks Dirty")) { world.MarkAllChunksDirty(); }

			ImGuiNet.SeparatorText("Client Side Stuff");
			if (ImGuiNet.Button("Mark All Rendering Chunks Dirty")) { world.MarkAllRenderingChunksDirty(); }
			if (ImGuiNet.Button("Clear Renderer Cache")) { world.ClearRenderCache(); }

			return;

			void DrawProperties() {
				WorldProperties properties = world.WorldProperties;

				if (ImGuiNet.BeginTable("Values", 2)) {
					DrawProperty("Seed", properties.Seed.ToString());

					DrawProperty("Width", properties.Width.ToString());
					DrawProperty("Depth", properties.Depth.ToString());
					DrawProperty("Height", properties.Height.ToString());

					DrawProperty("SeaLevel", properties.SeaLevel.ToString());

					ImGuiNet.EndTable();
				}
			}

			void DrawProperty(string name, string value) {
				ImGuiNet.TableNextRow();

				ImGuiNet.TableSetColumnIndex(0);
				ImGuiNet.Text(name);
				ImGuiNet.TableSetColumnIndex(1);
				ImGuiNet.Text(value);
			}
		}
	}
}