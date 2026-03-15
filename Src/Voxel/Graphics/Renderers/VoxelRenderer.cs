using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.DataStructs;
using Engine3.Client.Graphics.ImGui;
using Engine3.Client.Graphics.ImGui.Makers;
using Engine3.Client.Graphics.ImGui.Providers;
using Engine3.Client.Graphics.VertexLayouts;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Core.Graphics;
using Engine3.Test.Test.Graphics.Vulkan;
using Engine3.Test.Voxel.World;
using Engine3.Utility;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;
using Vector3 = System.Numerics.Vector3;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class VoxelRenderer : VulkanNodeRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private WorldRecorder? worldRecorderNode;
		private XyzGizmoRecorderNode? xyzGizmoRecorderNode;

		private GraphicsPipeline cubeGraphicsPipeline = null!;

		private DescriptorBuffers cameraUniformBuffer = null!;

		private VulkanBuffer cubeVertexBuffer = null!; // TODO for testing. remove
		private VulkanBuffer cubeIndexBuffer = null!;
		private DescriptorBuffers cubeInstanceBuffers = null!;
		private DescriptorSets cubeDescriptorSet = null!;

		private VulkanImage image = null!;
		private TextureSampler textureSampler = null!;

		private readonly Camera camera;

		private readonly VertexXyzUvRgb[] cubeVertices;
		private readonly uint[] cubeIndices;

		private readonly Vector3 cubePosition = new(0, 0, -5);
		private readonly ModelsBuffer cubeUniformBufferValue = new(1);

		private readonly Assembly gameAssembly;

		public static float PrevCubeRotation { get; private set; }
		public static float CubeRotation { get; private set; }

		public VoxelRenderer(GameClient game, VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly gameAssembly) : base(graphicsBackend, window, true) {
			this.camera = camera;
			this.gameAssembly = gameAssembly;

			CreateImGui(out ImGuiBackend backend, out ImGuiRenderer renderer);
			ImGuiBackend = backend;
			ImGuiRenderer = renderer;
			UseImGui = true;

			ImGuiBackend.ShowDebugUI = true;
			ImGuiBackend.DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, };

			CubeBuilder.BuildCube(BlockFaceMask.All, 1, 0, 0, 0, out VertexXyzUv[] cubeVertices, out cubeIndices);
			this.cubeVertices = cubeVertices.Select(static v => new VertexXyzUvRgb(v.X, v.Y, v.Z, v.U, v.V, 1, 1, 1)).ToArray();

			CreateCubeGraphicsPipeline(out DescriptorSetLayout cubeDescriptorSetLayout);

			CreateBuffers();

			CreateSamplerAndTextures();

			CreateDescriptorSets(cubeDescriptorSetLayout);
			UpdateDescriptorSets();

			worldRecorderNode = new(this, gameAssembly, cameraUniformBuffer, image, textureSampler);
			xyzGizmoRecorderNode = new(this, gameAssembly, camera);

			AddNode(worldRecorderNode);
			AddNode(xyzGizmoRecorderNode);
		}

		private void AddExtraDebugUI(float indentAmount) {
			ImGuiH.IndentedCollapsingHeader("Camera", indentAmount, DrawCamera);

			if (xyzGizmoRecorderNode != null) {
				bool showXyzGizmo = xyzGizmoRecorderNode.ShouldDraw;
				if (ImGuiNet.Checkbox("Show XYZ Gizmo", ref showXyzGizmo)) { xyzGizmoRecorderNode.ShouldDraw = showXyzGizmo; }
			}

			ImGuiNet.Text("test");

			return;

			void DrawCamera() => CameraImGuiMaker.ShowImGui(camera); // this should be faster than a lambda?
		}

		private void CreateCubeGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			const string Name = "Test";

			VulkanShader vertexShader = GraphicsResourceProvider.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = GraphicsResourceProvider.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			descriptorSetLayout = GraphicsResourceProvider.CreateDescriptorSetLayout([
					new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), //
					new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 1), //
					new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 2), //
			]);

			// ew
			cubeGraphicsPipeline = GraphicsResourceProvider.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzUvRgb.GetAttributeDescriptions(), VertexXyzUvRgb.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ], EnableDepthTest = true, EnableDepthWrite = true,
				});

			Logger.Debug("Created cube graphics pipeline");

			GraphicsResourceProvider.EnqueueDestroy(vertexShader);
			GraphicsResourceProvider.EnqueueDestroy(fragmentShader);
		}

		private void CreateBuffers() {
			// cube
			cubeVertexBuffer = GraphicsResourceProvider.CreateBuffer("Cube Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(VertexXyzUvRgb) * cubeVertices.Length));

			cubeIndexBuffer = GraphicsResourceProvider.CreateBuffer("Cube Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(uint) * cubeIndices.Length));

			// copy
			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(cubeVertexBuffer, cubeVertices), TransferCommandPool.CopyDataToBufferInfo.Copy(cubeIndexBuffer, cubeIndices), ]);

			Logger.Debug("Created & copied vertex/index buffers");

			// descriptor buffers
			cameraUniformBuffer = GraphicsResourceProvider.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(ProjectionView), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			cubeInstanceBuffers = GraphicsResourceProvider.CreateDescriptorBuffers("Cube Instance Storage Buffers", cubeUniformBufferValue.Size, MaxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			Logger.Debug("Created uniform buffers");
		}

		private void CreateSamplerAndTextures() {
			textureSampler = GraphicsResourceProvider.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, Window.SelectedGpu.PhysicalDeviceProperties2.properties.limits));
			Logger.Debug("Created texture sampler");

			using (StbiImage stbiImage = AssetH.LoadImage("Test.64x64", "png", 4, gameAssembly)) {
				image = GraphicsResourceProvider.CreateImage("Test 64x64 Image", (uint)stbiImage.Width, (uint)stbiImage.Height, VkFormat.FormatR8g8b8a8Srgb);
				TransferCommandPool.CopyToImage(image, PhysicalGpu.QueueFamilyIndices, LogicalGpu.TransferQueue, stbiImage);
			}

			Logger.Debug("Created image");
		}

		private void CreateDescriptorSets(DescriptorSetLayout cubeLayout) {
			DescriptorPool descriptorPool =
					GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1,
						MaxFramesInFlight);

			cubeDescriptorSet = descriptorPool.AllocateDescriptorSets(cubeLayout);
			Logger.Debug("Created descriptor sets");
		}

		private void UpdateDescriptorSets() {
			if (cubeInstanceBuffers == null || cameraUniformBuffer == null || image == null || textureSampler == null || cubeDescriptorSet == null) { throw new NullReferenceException(); }

			cubeDescriptorSet.UpdateDescriptorSet(0, cameraUniformBuffer);
			cubeDescriptorSet.UpdateDescriptorSet(1, cubeInstanceBuffers);
			cubeDescriptorSet.UpdateDescriptorSet(2, image.ImageView, textureSampler.Sampler);

			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer) {
			if (cubeGraphicsPipeline == null || cubeVertexBuffer == null || cubeIndexBuffer == null || cubeDescriptorSet == null) { throw new NullReferenceException(); }

			commandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			commandBuffer.CmdSetScissor(0, 0, SwapChain.Extent);

			// Cube
			commandBuffer.CmdBindGraphicsPipeline(cubeGraphicsPipeline.Pipeline); // TODO integrate graphics pipeline binding into VulkanRenderer?

			commandBuffer.CmdBindDescriptorSet(cubeGraphicsPipeline.Layout, cubeDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);
			commandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			commandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			commandBuffer.CmdDrawIndexed((uint)cubeIndices.Length, 1, 0, 0, 0);

			// nodes
			base.RecordCommandBuffer(commandBuffer);
		}

		protected override void CopyBuffers(float delta) {
			if (cubeInstanceBuffers == null || cameraUniformBuffer == null) { throw new NullReferenceException(); }

			cameraUniformBuffer.Copy(new ProjectionView(camera.Projection, camera.View), FrameIndex); // TODO lerp camera position & rotation

			cubeUniformBufferValue.Models[0] = Matrix4x4.CreateRotationY(float.Lerp(PrevCubeRotation, CubeRotation, delta) * float.DegreesToRadians(90f)) * Matrix4x4.CreateTranslation(cubePosition);
			cubeInstanceBuffers.Copy(MemoryMarshal.AsBytes(cubeUniformBufferValue.Models), FrameIndex);

			// nodes
			base.CopyBuffers(delta);
		}

		protected override void Update() {
			const float Rotation = float.Pi / 3f / 60f;

			PrevCubeRotation = CubeRotation;
			CubeRotation += Rotation;
			CubeRotation %= 360;
		}

		public void MarkChunkDirty() => worldRecorderNode?.MarkChunkDirty();
		public void SetWorld(World.World world) => worldRecorderNode?.World = world;

		protected override void Cleanup() {
			//

			base.Cleanup();
		}
	}
}