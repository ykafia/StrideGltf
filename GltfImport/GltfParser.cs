using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;
using Stride.Rendering.Materials;

namespace GltfImport
{
    class GltfParser
    {

        public static Model LoadFirstModel(GraphicsDevice device, SharpGLTF.Schema2.ModelRoot root)
        {
            var result = new Model();
            foreach(var e in root.LogicalMeshes[0].Primitives)
            {
                result.Meshes.Add(LoadMesh(e,device));
            }
            return result;
        }

        public static Mesh LoadMesh(SharpGLTF.Schema2.MeshPrimitive mesh, GraphicsDevice device)
        {
            
            var draw = new MeshDraw
            {
                PrimitiveType = ConvertPrimitiveType(mesh.DrawPrimitiveType),

                IndexBuffer = ConvertIndexBufferBinding(mesh, device),

                VertexBuffers = ConvertVertexBufferBinding(mesh, device)
            };
            
            var result =
                new Mesh(
                draw,
                new ParameterCollection()
            );

            result.Parameters.Set<bool>(MaterialKeys.HasSkinningPosition, true);            
            return result;
        }

        
        public static IndexBufferBinding ConvertIndexBufferBinding(SharpGLTF.Schema2.MeshPrimitive mesh, GraphicsDevice device)
        {
            var indices = mesh.GetIndices().Select(x => (uint)x).ToArray();
            var buf = Stride.Graphics.Buffer.Index.New(device, indices);
            return new IndexBufferBinding(buf, true, indices.Length);
        }

        public static VertexBufferBinding[] ConvertVertexBufferBinding(SharpGLTF.Schema2.MeshPrimitive mesh, GraphicsDevice device)
        {
                        
            var declaration = 
                new VertexDeclaration(
                    mesh.VertexAccessors.Select(ConvertVertexElement).ToArray()
            );

            var size = mesh.VertexAccessors.First().Value.Count;

            byte[] byteBuffer = new byte[(declaration.VertexStride * size)];

            for (int i = 0; i < size; i += declaration.VertexStride)
            {
                foreach(var ve in declaration.EnumerateWithOffsets())
                {
                    var tmp = mesh.GetVertexAccessor(ve.VertexElement.SemanticName).TryGetVertexBytes(i / declaration.VertexStride);
                    Array.Copy(tmp.Array,byteBuffer,i + ve.Offset);
                }
            }
            var buffer = 
                Stride.Graphics.Buffer.Vertex.New(
                    device, 
                    new DataPointer(
                        GCHandle.Alloc(byteBuffer).AddrOfPinnedObject(),
                        byteBuffer.Length
                    )
                );

            var binding = new VertexBufferBinding(buffer,declaration,0);

            return new List<VertexBufferBinding>() { binding }.ToArray();
        }


        
        public static VertexElement ConvertVertexElement(KeyValuePair<string,SharpGLTF.Schema2.Accessor> accessor)
        {
            return accessor.Key switch
            {
                "POSITION" => VertexElement.Position<Vector3>(0,accessor.Value.ByteOffset),
                "NORMAL" => VertexElement.Position<Vector3>(0, accessor.Value.ByteOffset),
                "TANGENT" => VertexElement.Tangent<Vector3>(0, accessor.Value.ByteOffset),
                "COLOR" => VertexElement.Color<Vector4>(0,accessor.Value.ByteOffset),
                "TEXCOORD_0" => VertexElement.Position<Vector2>(0, accessor.Value.ByteOffset),
                "TEXCOORD_1" => VertexElement.Position<Vector2>(1, accessor.Value.ByteOffset),
                "TEXCOORD_2" => VertexElement.Position<Vector2>(2, accessor.Value.ByteOffset),
                "TEXCOORD_3" => VertexElement.Position<Vector2>(3, accessor.Value.ByteOffset),
                "TEXCOORD_4" => VertexElement.Position<Vector2>(4, accessor.Value.ByteOffset),
                "TEXCOORD_5" => VertexElement.Position<Vector2>(5, accessor.Value.ByteOffset),
                "TEXCOORD_6" => VertexElement.Position<Vector2>(6, accessor.Value.ByteOffset),
                "TEXCOORD_7" => VertexElement.Position<Vector2>(7, accessor.Value.ByteOffset),
                "TEXCOORD_8" => VertexElement.Position<Vector2>(8, accessor.Value.ByteOffset),
                "TEXCOORD_9" => VertexElement.Position<Vector2>(9, accessor.Value.ByteOffset),
                "JOINTS_0" => new VertexElement(VertexElementUsage.BlendIndices,0,PixelFormat.R32G32B32A32_UInt,accessor.Value.ByteOffset),
                "WEIGHTS_0" => new VertexElement(VertexElementUsage.BlendIndices, 0, PixelFormat.R32G32B32A32_UInt, accessor.Value.ByteOffset),
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
