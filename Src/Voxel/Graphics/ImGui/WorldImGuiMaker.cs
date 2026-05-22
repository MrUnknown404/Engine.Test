using System.Numerics;
using Engine3.Client;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.World;
using ImGuiNET;

namespace Engine3.Test.Voxel.Graphics.ImGui;

public static unsafe class WorldImGuiMaker {
	public static void ShowImGui(World.World world) {
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

		ChunkPos cameraChunkPos = world.CameraChunkPos;
		GlobalBlockPos cameraGlobalBlockPos = world.CameraGlobalBlockPos;
		LocalBlockPos cameraLocalBlockPos = world.CameraLocalBlockPos;
		ChunkPos? lookAtChunkPos = world.LookAtChunkPos;
		GlobalBlockPos? lookAtGlobalBlockPos = world.LookAtGlobalBlockPos;
		LocalBlockPos? lookAtLocalBlockPos = world.LookAtLocalBlockPos;
		Block? lookAtBlock = world.LookAtBlock;

		int* cameraChunkPosPtr = stackalloc int[] { cameraChunkPos.X, cameraChunkPos.Y, cameraChunkPos.Z, };
		int* cameraGlobalBlockPosPtr = stackalloc int[] { cameraGlobalBlockPos.X, cameraGlobalBlockPos.Y, cameraGlobalBlockPos.Z, };
		int* cameraLocalBlockPosPtr = stackalloc int[] { cameraLocalBlockPos.X, cameraLocalBlockPos.Y, cameraLocalBlockPos.Z, };

		ImGuiNet.SeparatorText("Camera");
		ImGuiNet.DragInt3("ChunkPos", ref cameraChunkPosPtr[0], 0, 0, 0, null, ImGuiSliderFlags.NoInput);
		ImGuiNet.DragInt3("G. BlockPos", ref cameraGlobalBlockPosPtr[0], 0, 0, 0, null, ImGuiSliderFlags.NoInput);
		ImGuiNet.DragInt3("L. BlockPos", ref cameraLocalBlockPosPtr[0], 0, 0, 0, null, ImGuiSliderFlags.NoInput);

		Vector3 nan = Vector3.NaN;

		if (lookAtChunkPos != null) {
			int* lookAtChunkPosPtr = stackalloc int[] { lookAtChunkPos.Value.X, lookAtChunkPos.Value.Y, lookAtChunkPos.Value.Z, };
			ImGuiNet.DragInt3("@ ChunkPos", ref lookAtChunkPosPtr[0], 0, 0, 0, null, ImGuiSliderFlags.NoInput);
		} else {
			ImGuiNet.DragFloat3("@ ChunkPos", ref nan, 0, 0, 0, null, ImGuiSliderFlags.NoInput); //
		}

		if (lookAtGlobalBlockPos != null) {
			int* lookAtGlobalBlockPosPtr = stackalloc int[] { lookAtGlobalBlockPos.Value.X, lookAtGlobalBlockPos.Value.Y, lookAtGlobalBlockPos.Value.Z, };
			ImGuiNet.DragInt3("@ G. BlockPos", ref lookAtGlobalBlockPosPtr[0], 0, 0, 0, null, ImGuiSliderFlags.NoInput);
		} else {
			ImGuiNet.DragFloat3("@ G. BlockPos", ref nan, 0, 0, 0, null, ImGuiSliderFlags.NoInput); //
		}

		if (lookAtLocalBlockPos != null) {
			int* lookAtLocalBlockPosPtr = stackalloc int[] { lookAtLocalBlockPos.Value.X, lookAtLocalBlockPos.Value.Y, lookAtLocalBlockPos.Value.Z, };
			ImGuiNet.DragInt3("@ L. BlockPos", ref lookAtLocalBlockPosPtr[0], 0, 0, 0, null, ImGuiSliderFlags.NoInput);
		} else {
			ImGuiNet.DragFloat3("@ L. BlockPos", ref nan, 0, 0, 0, null, ImGuiSliderFlags.NoInput); //
		}

		ImGuiNet.Text($"Block: {(lookAtBlock != null ? lookAtBlock.RegistryKey : "null")}");

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