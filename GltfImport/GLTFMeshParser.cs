using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Rendering;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Graphics;

namespace GltfImport
{
    public class GLTFMeshParser
    {
        public GraphicsDevice Device { get; set; }

        public SharpGLTF.Schema2.Mesh AssetMesh { get; set; }

        public Model GetModel()
        {
            var model = new Model();
            for (int i = 0; i < AssetMesh.Primitives.Count; i++)
            {
                model.Add(new Mesh { Draw = GetMeshDraw(i) });
            }
            return model;
        }
        public MeshDraw GetMeshDraw(int primitiveID)
        {

            var vBuff = Stride.Graphics.Buffer.Vertex.New(
                Device,
                GetVertexPositionTextureBuffer(primitiveID),
                GraphicsResourceUsage.Dynamic
            );
            var iBuff = Stride.Graphics.Buffer.Index.New(
                Device,
                GetIndices(primitiveID)
            );
            var primitiveType = GetPrimitiveType(primitiveID);
            var tricount = GetTriIndicesCount(primitiveID);
            return new MeshDraw
            {
                PrimitiveType = primitiveType,
                DrawCount = iBuff.ElementCount,
                VertexBuffers = new[] { new VertexBufferBinding(vBuff, VertexPositionNormalTexture.Layout, vBuff.ElementCount) },
                IndexBuffer = new IndexBufferBinding(iBuff, true, iBuff.ElementCount),
            };
        }
        public VertexPositionNormalTexture[] GetVertexPositionTextureBuffer(int primitiveID)
        {
            var positions = AssetMesh.Primitives[primitiveID].VertexAccessors["POSITION"].AsVector3Array();
            var normals = AssetMesh.Primitives[primitiveID].VertexAccessors["NORMAL"].AsVector3Array();
            // var tangents = AssetMesh.Primitives[primitiveID].VertexAccessors["TANGENTS"].AsVector3Array();
            var texAccessor = AssetMesh.Primitives[primitiveID].VertexAccessors.Where(x => x.Key.Contains("TEXCOORD")).Select(x => x.Key).First();
            var texCoords = AssetMesh.Primitives[primitiveID].VertexAccessors[texAccessor].AsVector2Array();
            var result = new List<VertexPositionNormalTexture>();
            for (int i = 0; i < positions.Count(); i++)
            {
                result.Add(new VertexPositionNormalTexture(ToStrideVector3(positions[i]),ToStrideVector3(normals[i]), ToStrideVector2(texCoords[i])));
            }
            return result.ToArray();
        }
        public UInt32[] GetIndices(int primitiveID)
        {
            var indices = new List<UInt32>();
            foreach(var (A, B, C) in AssetMesh.Primitives[primitiveID].GetTriangleIndices())
            {
                indices.Add((uint)A); indices.Add((uint)B); indices.Add((uint)C);
            }
            return indices.ToArray();
        }

        public int GetTriIndicesCount(int primitiveID)
        {
            return AssetMesh.Primitives[primitiveID].GetTriangleIndices().Count();
        }
        public Stride.Graphics.PrimitiveType GetPrimitiveType(int primitiveID)
        {
            return AssetMesh.Primitives[primitiveID].DrawPrimitiveType switch
            {
                SharpGLTF.Schema2.PrimitiveType.TRIANGLES => Stride.Graphics.PrimitiveType.TriangleList,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLE_STRIP => Stride.Graphics.PrimitiveType.TriangleStrip,
                SharpGLTF.Schema2.PrimitiveType.LINES => Stride.Graphics.PrimitiveType.LineList,
                SharpGLTF.Schema2.PrimitiveType.POINTS => Stride.Graphics.PrimitiveType.PointList,
                SharpGLTF.Schema2.PrimitiveType.LINE_LOOP => Stride.Graphics.PrimitiveType.Undefined,
                SharpGLTF.Schema2.PrimitiveType.LINE_STRIP => Stride.Graphics.PrimitiveType.Undefined,
                SharpGLTF.Schema2.PrimitiveType.TRIANGLE_FAN => Stride.Graphics.PrimitiveType.Undefined,
                _ => Stride.Graphics.PrimitiveType.Undefined,
            };
        }
        private Vector3 ToStrideVector3(System.Numerics.Vector3 a)
        {
            return new Vector3(a.X, a.Z, a.Y);
        }
        private Vector2 ToStrideVector2(System.Numerics.Vector2 a)
        {
            return new Vector2(a.X, a.Y);
        }

    }

}
