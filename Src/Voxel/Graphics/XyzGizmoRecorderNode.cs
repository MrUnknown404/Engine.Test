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

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class XyzGizmoRecorderNode : VulkanRecorderNode { // TODO once this is done merge into engine
		private static Matrix4x4 GizmoTransform { get; } = Matrix4x4.CreateScale(0.1f) * Matrix4x4.CreateTranslation(-Vector3.UnitZ); // TODO scale based on viewport size. edit: just gonna do lines

		private readonly GraphicsPipeline gizmoGraphicsPipeline;
		private readonly DescriptorSets sceneDescriptorSets;
		private readonly DescriptorSets materialDescriptorSets;
		private readonly DescriptorBuffers sceneUniformBuffers;
		private readonly DescriptorBuffers materialUniformBuffers; // TODO DescriptorBuffer class

		private readonly VulkanBuffer xyzGizmoVertexBuffer;
		private readonly VulkanBuffer xyzGizmoIndexBuffer;

		private readonly Model xyzGizmoModel;

		private ProjectionModel sceneUniformData;
		private static readonly Material[] MaterialUniformData = [ new(Color4.Red), new(Color4.Green), new(Color4.Blue), new(Color4.White), ]; // TODO read from model & remove static

		private readonly Camera camera;
		private VkExtent2D swapChainExtent;

		private static int tempMaterialCounter;

		public XyzGizmoRecorderNode(LogicalGpu logicalGpu, TransferCommandPool transferCommandPool, SwapChain swapChain, Assembly gameAssembly, byte maxFramesInFlight, Camera camera) {
			swapChainExtent = swapChain.Extent;
			this.camera = camera;

			gizmoGraphicsPipeline = CreatePipeline(logicalGpu, swapChain.ImageFormat, gameAssembly, out DescriptorSetLayout sceneLayout, out DescriptorSetLayout materialLayout);

			xyzGizmoModel = AssetH.LoadModel("XYZGizmo", static meshPrimitiveDecoder => {
				RenderMesh renderMesh = GltfMeshToMesh_VertexXyz(meshPrimitiveDecoder);
				tempMaterialCounter++;
				return renderMesh;
			}, gameAssembly, (byte)sizeof(VertexXyz));

			xyzGizmoModel.Collect(out byte[] vertices, out uint[] indices);

			CreateBuffers(logicalGpu, maxFramesInFlight, vertices, indices, (byte)MaterialUniformData.Length, out xyzGizmoVertexBuffer, out xyzGizmoIndexBuffer, out sceneUniformBuffers, out materialUniformBuffers);

			CreateDescriptorSets(logicalGpu, sceneLayout, materialLayout, maxFramesInFlight, sceneUniformBuffers, materialUniformBuffers, out sceneDescriptorSets, out materialDescriptorSets);

			// initial copy
			transferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(xyzGizmoVertexBuffer, vertices), TransferCommandPool.CopyDataToBufferInfo.Copy(xyzGizmoIndexBuffer, indices), ]);
			materialUniformBuffers.Copy(MaterialUniformData, 0);

			return;

			// [SuppressMessage("ReSharper", "InconsistentNaming")]
			// static RenderMesh GltfMeshesToMesh_VertexXyz(IReadOnlyList<SharpGLTF.Schema2.Mesh> inMeshes) {
			// 	List<VertexXyz> vertices = new();
			// 	List<uint> indices = new();
			// 	uint offset = 0;
			//
			// 	IMeshDecoder<GLTFMaterial>[] meshDecoders = inMeshes.Decode(new RuntimeOptions());
			// 	foreach (IMeshDecoder<GLTFMaterial> meshDecoder in meshDecoders) {
			// 		foreach (IMeshPrimitiveDecoder<GLTFMaterial> meshPrimitiveDecoder in meshDecoder.Primitives) {
			// 			for (int i = 0; i < meshPrimitiveDecoder.VertexCount; i++) {
			// 				Vector3 pos = meshPrimitiveDecoder.GetPosition(i);
			// 				vertices.Add(new(pos));
			// 			}
			//
			// 			foreach ((int indexA, int indexB, int indexC) in meshPrimitiveDecoder.TriangleIndices) {
			// 				indices.Add((uint)indexA + offset);
			// 				indices.Add((uint)indexB + offset);
			// 				indices.Add((uint)indexC + offset);
			// 			}
			//
			// 			offset += (uint)meshPrimitiveDecoder.VertexCount;
			// 		}
			// 	}
			//
			// 	return new(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(vertices)).ToArray(), indices.ToArray());
			// }

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

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, byte frameIndex) { // TODO see if i can draw with lines instead
			if (!ShouldDraw) { return; }

			graphicsCommandBuffer.CmdBindGraphicsPipeline(gizmoGraphicsPipeline.Pipeline);
			graphicsCommandBuffer.CmdBindDescriptorSets(gizmoGraphicsPipeline.Layout, [ sceneDescriptorSets.GetCurrent(frameIndex), materialDescriptorSets.GetCurrent(0), ],
				VkShaderStageFlagBits.ShaderStageVertexBit | VkShaderStageFlagBits.ShaderStageFragmentBit);

			graphicsCommandBuffer.CmdBindVertexBuffer(xyzGizmoVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(xyzGizmoIndexBuffer, xyzGizmoIndexBuffer.BufferSize);

			graphicsCommandBuffer.CmdClearDepth(swapChainExtent);

			foreach (Model.RenderData data in xyzGizmoModel.RenderDataList) {
				if (data.Material is not null) {
					graphicsCommandBuffer.CmdPushConstants(gizmoGraphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit,
						new MaterialPushConstants(MaterialToIndex(data.Material.Value) + 1 /* 0 is reserved for no texture */), 0);
				}

				graphicsCommandBuffer.CmdDrawIndexed(data.IndexCount, 1, 0, data.VertexOffset, 0);
			}

			return;

			uint MaterialToIndex(Material material) { // TODO cache
				for (uint i = 0; i < MaterialUniformData.Length; i++) {
					if (MaterialUniformData[i] == material) { return i; }
				}

				return 0;
			}
		}

		protected override void CopyBuffers(float delta, byte frameIndex) {
			sceneUniformData = new(camera.Projection, Matrix4x4.Transform(Matrix4x4.Identity, camera.Orientation) * GizmoTransform);
			sceneUniformBuffers.Copy(sceneUniformData, frameIndex);
		}

		protected override void OnSwapChainChange(SwapChain newSwapChain) => swapChainExtent = newSwapChain.Extent;

		private static GraphicsPipeline CreatePipeline(LogicalGpu logicalGpu, VkFormat swapChainImageFormat, Assembly gameAssembly, out DescriptorSetLayout sceneDescriptorSetLayout,
			out DescriptorSetLayout materialDescriptorSetLayout) {
			const string Name = "Gizmo";

			VulkanShader vertexShader = logicalGpu.CreateShader($"{Name} Vertex Shader", Name, ShaderLanguage.Glsl, ShaderType.Vertex, gameAssembly);
			VulkanShader fragmentShader = logicalGpu.CreateShader($"{Name} Fragment Shader", Name, ShaderLanguage.Glsl, ShaderType.Fragment, gameAssembly);

			sceneDescriptorSetLayout = logicalGpu.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), ]);
			materialDescriptorSetLayout = logicalGpu.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeStorageBuffer, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);

			GraphicsPipeline pipeline = logicalGpu.CreateGraphicsPipeline(
				new($"{Name} Graphics Pipeline", swapChainImageFormat, [ vertexShader, fragmentShader, ], VertexXyz.GetAttributeDescriptions(), VertexXyz.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ sceneDescriptorSetLayout.VkDescriptorSetLayout, materialDescriptorSetLayout.VkDescriptorSetLayout, ],
						PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, size = (byte)sizeof(MaterialPushConstants), }, ],
						EnableDepthTest = true,
						EnableDepthWrite = true,
				});

			logicalGpu.EnqueueDestroy(vertexShader);
			logicalGpu.EnqueueDestroy(fragmentShader);

			return pipeline;
		}

		private static void CreateBuffers(LogicalGpu logicalGpu, byte maxFramesInFlight, byte[] vertices, uint[] indices, byte materialCount, out VulkanBuffer vertexBuffer, out VulkanBuffer indexBuffer,
			out DescriptorBuffers sceneDescriptorBuffers, out DescriptorBuffers materialDescriptorBuffers) {
			vertexBuffer = logicalGpu.CreateBuffer("Gizmo Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)vertices.Length);

			indexBuffer = logicalGpu.CreateBuffer("Gizmo Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * indices.Length));

			sceneDescriptorBuffers = logicalGpu.CreateDescriptorBuffers("Gizmo Scene Uniform Buffer", (ulong)sizeof(ProjectionModel), maxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			materialDescriptorBuffers = logicalGpu.CreateDescriptorBuffers("Gizmo Material Storage Buffer", (ulong)(sizeof(Material) * materialCount), maxFramesInFlight, VkDescriptorType.DescriptorTypeStorageBuffer,
				VkBufferUsageFlagBits.BufferUsageStorageBufferBit);
		}

		private static void CreateDescriptorSets(LogicalGpu logicalGpu, DescriptorSetLayout sceneDescriptorSetLayout, DescriptorSetLayout materialDescriptorSetLayout, byte maxFramesInFlight,
			DescriptorBuffers sceneDescriptorBuffers, DescriptorBuffers materialDescriptorBuffers, out DescriptorSets sceneDescriptorSets, out DescriptorSets materialDescriptorSets) {
			DescriptorPool descriptorPool = logicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeStorageBuffer, ], 2, maxFramesInFlight);

			sceneDescriptorSets = descriptorPool.AllocateDescriptorSet(sceneDescriptorSetLayout);
			materialDescriptorSets = descriptorPool.AllocateDescriptorSet(materialDescriptorSetLayout);

			sceneDescriptorSets.UpdateDescriptorSet(0, sceneDescriptorBuffers);
			materialDescriptorSets.UpdateDescriptorSet(0, materialDescriptorBuffers);
		}
	}
}