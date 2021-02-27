using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace GltfImport
{
    class GltfParser
    {

        public static Model LoadFirstModel(GraphicsDevice device, SharpGLTF.Schema2.ModelRoot root)
        {
            var result = new Model
            {
                Meshes =
                    root.LogicalMeshes[0].Primitives
                    .Select(x => LoadMesh(x, device))
                    .ToList()
            };
            result.Add(RedMaterial(device));
            result.Meshes.ForEach(x => x.MaterialIndex = 0);
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
            return Material.New(device,descriptor);
        }

        public static Mesh LoadMesh(SharpGLTF.Schema2.MeshPrimitive mesh, GraphicsDevice device)
        {

            var draw = new MeshDraw
            {
                PrimitiveType = ConvertPrimitiveType(mesh.DrawPrimitiveType),
                IndexBuffer = ConvertIndexBufferBinding(mesh, device),
                VertexBuffers = ConvertVertexBufferBinding(mesh, device),
                DrawCount = mesh.IndexAccessor.Count
            };


            
            var result = new Mesh(
                draw,
                new ParameterCollection()
            );

            //TODO : Add parameter collection only after checking if it has
            result.Parameters.Set(MaterialKeys.HasSkinningPosition, true);
            result.Parameters.Set(MaterialKeys.HasSkinningNormal, true);
            return result;
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

            var binding = new VertexBufferBinding(buffer,declaration,size);

            return new List<VertexBufferBinding>() { binding }.ToArray();
        }


        
        public static (VertexElement,int) ConvertVertexElement(KeyValuePair<string,SharpGLTF.Schema2.Accessor> accessor, int offset)
        {
            return accessor.Key switch
            {
                "POSITION" => (VertexElement.Position<Vector3>(0,offset),Vector3.SizeInBytes),
                "NORMAL" => (VertexElement.Normal<Vector3>(0, offset), Vector3.SizeInBytes),
                "TANGENT" => (VertexElement.Tangent<Vector3>(0, offset),Vector3.SizeInBytes),
                "COLOR" => (VertexElement.Color<Vector4>(0,offset), Vector4.SizeInBytes),
                "TEXCOORD_0" => (VertexElement.TextureCoordinate<Vector2>(0, offset), Vector2.SizeInBytes),
                "TEXCOORD_1" => (VertexElement.TextureCoordinate<Vector2>(1, offset), Vector2.SizeInBytes),
                "TEXCOORD_2" => (VertexElement.TextureCoordinate<Vector2>(2, offset), Vector2.SizeInBytes),
                "TEXCOORD_3" => (VertexElement.TextureCoordinate<Vector2>(3, offset), Vector2.SizeInBytes),
                "TEXCOORD_4" => (VertexElement.TextureCoordinate<Vector2>(4, offset), Vector2.SizeInBytes),
                "TEXCOORD_5" => (VertexElement.TextureCoordinate<Vector2>(5, offset), Vector2.SizeInBytes),
                "TEXCOORD_6" => (VertexElement.TextureCoordinate<Vector2>(6, offset), Vector2.SizeInBytes),
                "TEXCOORD_7" => (VertexElement.TextureCoordinate<Vector2>(7, offset), Vector2.SizeInBytes),
                "TEXCOORD_8" => (VertexElement.TextureCoordinate<Vector2>(8, offset), Vector2.SizeInBytes),
                "TEXCOORD_9" => (VertexElement.TextureCoordinate<Vector2>(9, offset), Vector2.SizeInBytes),
                "JOINTS_0" => (new VertexElement(VertexElementUsage.BlendIndices,0,PixelFormat.R16G16B16A16_UInt,offset), 8),
                "WEIGHTS_0" => (new VertexElement(VertexElementUsage.BlendWeight, 0, PixelFormat.R32G32B32A32_Float, offset), Vector4.SizeInBytes),
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
