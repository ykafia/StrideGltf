﻿using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using System.IO;
using Stride.Animations;
using Stride.Core.Collections;

using static GltfImport.GltfParser.GltfUtils;
using static GltfImport.GltfParser.AnimationParser;

namespace GltfImport.GltfParser
{
    class GltfMeshParser
    {

        public static Model LoadFirstModel(GraphicsDevice device, SharpGLTF.Schema2.ModelRoot root)
        {
            var result = new Model
            {
                Meshes =
                    root.LogicalMeshes.First().Primitives
                    .Select(x => LoadMesh(x, device))
                    .ToList()
            };
            LoadMaterials(root, device).ForEach(x => result.Add(x));
            //result.Add(RedMaterial(device));
            //result.Meshes.ForEach(x => x.MaterialIndex = 0);
            result.Skeleton = ConvertSkeleton(root);
            return result;
        }

        
        public static MeshSkinningDefinition ConvertInverseBindMatrices(SharpGLTF.Schema2.ModelRoot root)
        {
            var skin = root.LogicalNodes.First(x => x.Mesh == root.LogicalMeshes.First()).Skin;
            var jointList = Enumerable.Range(0, skin.JointsCount).Select(skin.GetJoint);
            var mnt =
                new MeshSkinningDefinition
                {
                    Bones =
                        jointList
                            .Select((x, i) =>
                                new MeshBoneDefinition
                                {
                                    NodeIndex = i + 1,
                                    LinkToMeshMatrix = ConvertNumerics(x.InverseBindMatrix)
                                }
                            )
                            .ToArray()
                };
            return mnt;
        }

        public static Dictionary<string,AnimationClip> ConvertAnimations(SharpGLTF.Schema2.ModelRoot root)
        {
            var animations = root.LogicalAnimations;
            
            var clips =
                animations
                .Select(x =>
                   {
                       //Create animation clip with 
                       var clip = new AnimationClip { Duration = TimeSpan.FromSeconds(x.Duration)};
                       clip.RepeatMode = AnimationRepeatMode.LoopInfinite;
                       // Add Curve
                       ConvertCurves(x.Channels,root).ToList().ForEach(x => clip.AddCurve(x.Key, x.Value));

                       return (x.Name,clip);
                   }
                )
                .ToList()
                .ToDictionary(x => x.Name, x => x.clip);
            return clips;
        }

        public static List<Material> LoadMaterials(SharpGLTF.Schema2.ModelRoot root, GraphicsDevice device)
        {

            var result = new List<Material>();
            foreach (var mat in root.LogicalMaterials)
            {
                var material = new MaterialDescriptor
                {
                    Attributes = new MaterialAttributes()
                };
                foreach (var chan in mat.Channels)
                {
                    if (chan.Texture != null)
                    {
                        var imgBuf = chan.Texture.PrimaryImage.Content.Content.ToArray();
                        var imgPtr = new DataPointer(GCHandle.Alloc(imgBuf, GCHandleType.Pinned).AddrOfPinnedObject(), imgBuf.Length);

                        var image = Stride.Graphics.Image.Load(imgPtr);
                        var texture = Texture.New(device, image, TextureFlags.None);


                        switch (chan.Key)
                        {
                            case "BaseColor":
                                var vt = new ComputeTextureColor(texture)
                                {
                                    AddressModeU = TextureAddressMode.Wrap,
                                    AddressModeV = TextureAddressMode.Wrap,
                                    TexcoordIndex = TextureCoordinate.Texcoord0
                                };

                                material.Attributes.Diffuse = new MaterialDiffuseMapFeature(vt);

                                material.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
                                break;
                            case "MetallicRoughness":
                                material.Attributes.MicroSurface = new MaterialGlossinessMapFeature(new ComputeTextureScalar(texture, TextureCoordinate.Texcoord0, Vector2.One, Vector2.Zero));
                                break;
                            case "Normal":
                                material.Attributes.Surface = new MaterialNormalMapFeature(new ComputeTextureColor(texture));
                                break;
                            case "Occlusion":
                                material.Attributes.Occlusion = new MaterialOcclusionMapFeature();
                                break;
                            case "Emissive":
                                material.Attributes.Emissive = new MaterialEmissiveMapFeature(new ComputeTextureColor(texture));
                                break;
                        }

                    }

                }
                material.Attributes.CullMode = CullMode.Back;
                result.Add(Material.New(device, material));
            }
            return result;
        }
        public static Material RedMaterial(GraphicsDevice device)
        {
            var descriptor = new MaterialDescriptor
            {
                Attributes = new MaterialAttributes
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Red)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    CullMode = CullMode.Back
                }
            };
            return Material.New(device, descriptor);
        }

        public static Mesh LoadMesh(SharpGLTF.Schema2.MeshPrimitive mesh, GraphicsDevice device)
        {

            var draw = new MeshDraw
            {
                PrimitiveType = ConvertPrimitiveType(mesh.DrawPrimitiveType),
                IndexBuffer = ConvertIndexBufferBinding(mesh, device),
                VertexBuffers = ConvertVertexBufferBinding(mesh, device),
                DrawCount = GetDrawCount(mesh)
            };



            var result = new Mesh(draw, new ParameterCollection())
            {
                Skinning = ConvertInverseBindMatrices(mesh.LogicalParent.LogicalParent),
                Name = mesh.LogicalParent.Name,
                MaterialIndex = mesh.LogicalParent.LogicalParent.LogicalMaterials.ToList().IndexOf(mesh.Material)
            };


            //TODO : Add parameter collection only after checking if it has
            result.Parameters.Set(MaterialKeys.HasSkinningPosition, true);
            result.Parameters.Set(MaterialKeys.HasSkinningNormal, true);
            return result;
        }

        private static int GetDrawCount(SharpGLTF.Schema2.MeshPrimitive mesh)
        {
            var tmp = mesh.GetTriangleIndices().Select(x => new int[] { x.A, x.C, x.B }).SelectMany(x => x).Select(x => (uint)x).ToArray();
            return mesh.GetTriangleIndices().Select(x => new int[] { x.A, x.C, x.B }).SelectMany(x => x).Select(x => (uint)x).ToArray().Length;
        }

        public static IndexBufferBinding ConvertIndexBufferBinding(SharpGLTF.Schema2.MeshPrimitive mesh, GraphicsDevice device)
        {
            var indices = mesh.GetTriangleIndices().Select(x => new int[] { x.A, x.C, x.B }).SelectMany(x => x).Select(x => (uint)x).ToArray();
            var buf = Stride.Graphics.Buffer.Index.New(device, indices);
            return new IndexBufferBinding(buf, true, indices.Length);
        }

        public static VertexBufferBinding[] ConvertVertexBufferBinding(SharpGLTF.Schema2.MeshPrimitive mesh, GraphicsDevice device)
        {
            var offset = 0;
            var vertElem =
                mesh.VertexAccessors
                .Select(
                    x =>
                    {
                        var y = ConvertVertexElement(x, offset);
                        offset += y.Item2;
                        return y.Item1;
                    })
                .ToList();

            var declaration =
                new VertexDeclaration(
                    vertElem.ToArray()
            );

            var size = mesh.VertexAccessors.First().Value.Count;
            var byteBuffer = Enumerable.Range(0, size)
                .Select(
                    x =>
                        declaration.EnumerateWithOffsets()
                            .Select(y => y.VertexElement.SemanticName
                                .Replace("ORD", "ORD_" + y.VertexElement.SemanticIndex)
                                .Replace("BLENDINDICES", "JOINTS_0")
                                .Replace("BLENDWEIGHT", "WEIGHTS_0")
                            )
                            .Select(y => mesh.GetVertexAccessor(y).TryGetVertexBytes(x).ToArray())
                )
                .SelectMany(x => x)
                .SelectMany(x => x)
                .ToArray();

            var buffer =
                Stride.Graphics.Buffer.Vertex.New(
                    device,
                    new DataPointer(
                        GCHandle.Alloc(byteBuffer, GCHandleType.Pinned).AddrOfPinnedObject(),
                        byteBuffer.Length
                    )
                );

            var binding = new VertexBufferBinding(buffer, declaration, size);

            return new List<VertexBufferBinding>() { binding }.ToArray();
        }



        public static (VertexElement, int) ConvertVertexElement(KeyValuePair<string, SharpGLTF.Schema2.Accessor> accessor, int offset)
        {

            return (accessor.Key, accessor.Value.Format.ByteSize) switch
            {
                ("POSITION", 12) => (VertexElement.Position<Vector3>(0, offset), Vector3.SizeInBytes),
                ("NORMAL", 12) => (VertexElement.Normal<Vector3>(0, offset), Vector3.SizeInBytes),
                ("TANGENT", 12) => (VertexElement.Tangent<Vector3>(0, offset), Vector3.SizeInBytes),
                ("COLOR", 16) => (VertexElement.Color<Vector4>(0, offset), Vector4.SizeInBytes),
                ("TEXCOORD_0", 8) => (VertexElement.TextureCoordinate<Vector2>(0, offset), Vector2.SizeInBytes),
                ("TEXCOORD_1", 8) => (VertexElement.TextureCoordinate<Vector2>(1, offset), Vector2.SizeInBytes),
                ("TEXCOORD_2", 8) => (VertexElement.TextureCoordinate<Vector2>(2, offset), Vector2.SizeInBytes),
                ("TEXCOORD_3", 8) => (VertexElement.TextureCoordinate<Vector2>(3, offset), Vector2.SizeInBytes),
                ("TEXCOORD_4", 8) => (VertexElement.TextureCoordinate<Vector2>(4, offset), Vector2.SizeInBytes),
                ("TEXCOORD_5", 8) => (VertexElement.TextureCoordinate<Vector2>(5, offset), Vector2.SizeInBytes),
                ("TEXCOORD_6", 8) => (VertexElement.TextureCoordinate<Vector2>(6, offset), Vector2.SizeInBytes),
                ("TEXCOORD_7", 8) => (VertexElement.TextureCoordinate<Vector2>(7, offset), Vector2.SizeInBytes),
                ("TEXCOORD_8", 8) => (VertexElement.TextureCoordinate<Vector2>(8, offset), Vector2.SizeInBytes),
                ("TEXCOORD_9", 8) => (VertexElement.TextureCoordinate<Vector2>(9, offset), Vector2.SizeInBytes),
                ("JOINTS_0", 8) => (new VertexElement(VertexElementUsage.BlendIndices, 0, PixelFormat.R16G16B16A16_UInt, offset), 8),
                ("JOINTS_0", 4) => (new VertexElement(VertexElementUsage.BlendIndices, 0, PixelFormat.R8G8B8A8_UInt, offset), 4),
                ("WEIGHTS_0", 16) => (new VertexElement(VertexElementUsage.BlendWeight, 0, PixelFormat.R32G32B32A32_Float, offset), Vector4.SizeInBytes),
                _ => throw new NotImplementedException(),
            };
        }

        public static PrimitiveType ConvertPrimitiveType(SharpGLTF.Schema2.PrimitiveType gltfType)
        {
            return gltfType switch
            {
                SharpGLTF.Schema2.PrimitiveType.LINES => PrimitiveType.LineList,
                SharpGLTF.Schema2.PrimitiveType.POINTS => PrimitiveType.PointList,
                SharpGLTF.Schema2.PrimitiveType.LINE_LOOP => PrimitiveType.Undefined,
                SharpGLTF.Schema2.PrimitiveType.LINE_STRIP => PrimitiveType.LineStrip,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLES => PrimitiveType.TriangleList,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLE_STRIP => PrimitiveType.TriangleStrip,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLE_FAN => PrimitiveType.Undefined,
                _ => throw new NotImplementedException()
            };
        }
        

    }
}
