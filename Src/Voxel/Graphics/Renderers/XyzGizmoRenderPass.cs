using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.DataStructs;
using Engine3.Client.Graphics.Vertex;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Voxel.Graphics.DataStructs;
using Engine3.Utility;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using SharpGLTF.Runtime;
using GLTFMaterial = SharpGLTF.Schema2.Material;
using Vector3 = System.Numerics.Vector3;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class XyzGizmoRenderPass : VulkanRenderPass { // TODO once this is done merge into engine
		private const string DebugName = "Xyz Gizmo";
		private const string FileName = "XyzGizmo";

		private static Matrix4x4 GizmoTransform { get; } = Matrix4x4.CreateScale(0.1f) * Matrix4x4.CreateTranslation(-Vector3.UnitZ); // TODO scale based on viewport size. edit: just gonna do lines
		private static readonly Material[] MaterialUniformData = [ new(Color4.Red), new(Color4.Green), new(Color4.Blue), new(Color4.White), ]; // TODO read from model & remove static

		private readonly DescriptorSets sceneDescriptorSets;
		private readonly DescriptorSet materialDescriptorSet;

		private readonly DescriptorBuffers sceneUniformBuffers;
		private readonly DescriptorBuffer materialStorageBuffer;

		private readonly Camera camera;

		private readonly Model xyzGizmoModel;
		private ProjectionModel sceneUniformData;

		private VkExtent2D swapChainExtent;

		private static int tempMaterialCounter;

		public XyzGizmoRenderPass(VoxelRenderPassRenderer renderer, Assembly assembly, Camera camera) : base(renderer,
			CreatePipeline(renderer.GraphicsResourceProvider, renderer.SwapChain, assembly, out DescriptorSetLayout sceneLayout, out DescriptorSetLayout materialLayout)) {
			this.camera = camera;
			swapChainExtent = renderer.SwapChain.Extent;

			// model
			xyzGizmoModel = AssetH.LoadModel(FileName, static meshPrimitiveDecoder => {
				RenderMesh renderMesh = GltfMeshToMesh_VertexXyz(meshPrimitiveDecoder);
				tempMaterialCounter++;
				return renderMesh;
			}, assembly, (byte)sizeof(VertexXyz));

			xyzGizmoModel.Collect(out byte[] vertices, out uint[] indices);

			// buffers
			VertexBuffer = GraphicsResourceProvider.CreateBuffer($"{DebugName} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)vertices.Length);

			IndexBuffer = GraphicsResourceProvider.CreateBuffer($"{DebugName} Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(uint) * indices.Length));

			// descriptors
			sceneUniformBuffers = GraphicsResourceProvider.CreateDescriptorBuffers($"{DebugName} Scene Uniform Buffer", (ulong)sizeof(ProjectionModel), renderer.MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			materialStorageBuffer = GraphicsResourceProvider.CreateDescriptorBuffer($"{DebugName} Material Storage Buffer", (ulong)(sizeof(Material) * MaterialUniformData.Length), VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);

			DescriptorPool descriptorPool = GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, ], 2, renderer.MaxFramesInFlight);

			sceneDescriptorSets = descriptorPool.AllocateDescriptorSets(sceneLayout);
			materialDescriptorSet = descriptorPool.AllocateDescriptorSet(materialLayout);

			sceneDescriptorSets.UpdateDescriptorSet(0, sceneUniformBuffers);
			materialDescriptorSet.UpdateDescriptorSet(0, materialStorageBuffer);

			// initial copy
			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(VertexBuffer, vertices), TransferCommandPool.CopyDataToBufferInfo.Copy(IndexBuffer, indices), ]);
			materialStorageBuffer.Copy(MaterialUniformData);

			return;

			[SuppressMessage("ReSharper", "InconsistentNaming")] // TODO move
			static RenderMesh GltfMeshToMesh_VertexXyz(IMeshPrimitiveDecoder<GLTFMaterial> meshPrimitiveDecoder) {
				List<VertexXyz> vertices = new();
				List<uint> indices = new();

				GLTFMaterial material = meshPrimitiveDecoder.Material; // TODO use

				for (int i = 0; i < meshPrimitiveDecoder.VertexCount; i++) {
					Vector3 pos = meshPrimitiveDecoder.GetPosition(i);
					vertices.Add(new(pos));
				}

				foreach ((int indexA, int indexB, int indexC) in meshPrimitiveDecoder.TriangleIndices) {
					indices.Add((uint)indexA);
					indices.Add((uint)indexB);
					indices.Add((uint)indexC);
				}

				return new(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(vertices)).ToArray(), indices.ToArray()) {
						Material = MaterialUniformData[tempMaterialCounter],
						// Material = new(Color4<Rgba>.FromVector4(material.Channels.First().Color)),
				};
			}
		}

		protected override void CopyBuffers(float delta, byte frameIndex) {
			sceneUniformData = new(camera.Projection, Matrix4x4.Transform(Matrix4x4.Identity, camera.Orientation) * GizmoTransform);
			sceneUniformBuffers.Copy(sceneUniformData, frameIndex);
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer, byte frameIndex) {
			commandBuffer.CmdBindDescriptorSets(GraphicsPipeline.Layout, [ sceneDescriptorSets.GetCurrent(frameIndex), materialDescriptorSet.VkDescriptorSet, ],
				VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);

			commandBuffer.CmdClearDepth(swapChainExtent);

			foreach (Model.RenderData data in xyzGizmoModel.RenderDataList) {
				if (data.Material is not null) {
					commandBuffer.CmdPushConstants(GraphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit, new MaterialPushConstants(MaterialToIndex(data.Material.Value) + 1 /* 0 is reserved for no texture */), 0);
				}

				commandBuffer.CmdDrawIndexed(data.IndexCount, 1, 0, data.VertexOffset, 0);
			}

			return;

			uint MaterialToIndex(Material material) { // TODO this is bad
				for (uint i = 0; i < MaterialUniformData.Length; i++) {
					if (MaterialUniformData[i] == material) { return i; }
				}

				return 0;
			}
		}

		private static GraphicsPipeline CreatePipeline(VulkanResourceProvider graphicsResourceProvider, SwapChain swapChain, Assembly assembly, out DescriptorSetLayout sceneDescriptorSetLayout,
			out DescriptorSetLayout materialDescriptorSetLayout) {
			VulkanShader vertexShader = graphicsResourceProvider.CreateShader($"{DebugName} Vertex Shader", FileName, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = graphicsResourceProvider.CreateShader($"{DebugName} Fragment Shader", FileName, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			sceneDescriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), ]);
			materialDescriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);

			GraphicsPipeline pipeline = graphicsResourceProvider.CreateGraphicsPipeline(
				new($"{DebugName} Graphics Pipeline", swapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyz.GetAttributeDescriptions(), VertexXyz.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ sceneDescriptorSetLayout.VkDescriptorSetLayout, materialDescriptorSetLayout.VkDescriptorSetLayout, ],
						PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, size = (byte)sizeof(MaterialPushConstants), }, ],
						EnableDepthTest = true,
						EnableDepthWrite = true,
				});

			graphicsResourceProvider.EnqueueDestroy(vertexShader);
			graphicsResourceProvider.EnqueueDestroy(fragmentShader);

			return pipeline;
		}

		protected override void OnSwapchainInvalid(SwapChain swapChain) => swapChainExtent = swapChain.Extent;
	}
}