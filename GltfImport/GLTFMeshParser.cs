
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Core.Diagnostics;

namespace GltfImport
{
    public enum VertexType
    {
        VertexPositionTexture,
        VertexPositionNormalTexture,
        VertexPositionNormalColor

    }
    public class GLTFMeshParser
    {
        public GraphicsDevice Device { get; set; }

        public SharpGLTF.Schema2.ModelRoot GltfRoot { get; set; }
        public SharpGLTF.Schema2.Mesh CurrentMesh { get; set; }
        public Stride.Core.Serialization.Contents.ContentManager Content { get; set; }

        public Logger Logger { get; set; }
        // public Dictionary<string, Texture> Textures { get; set; }

        public Model GetModel(int modelID)
        {
            var model = new Model();
            CurrentMesh = GltfRoot.LogicalMeshes[modelID];
            for (int i = 0; i < GltfRoot.LogicalMeshes[modelID].Primitives.Count; i++)
            {
                var matDesc = GetMaterial(i);
                // var matDesc = new MaterialDescriptor{
                //     Attributes = new MaterialAttributes {
                //         Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Stride.Core.Mathematics.Color.AliceBlue)),
                //         DiffuseModel = new MaterialDiffuseLambertModelFeature()
                //     }
                // };
                var material = Material.New(Device, matDesc);
                model.Add(material);
                var mesh = new Mesh { Draw = GetMeshDraw(i) };
                Logger.Info("Index of material is : " + model.Materials.IndexOf(material));
                mesh.MaterialIndex = model.Materials.IndexOf(material);
                model.Add(mesh);

            }
            return model;
        }
        public MaterialDescriptor GetMaterial(int primitiveID)
        {
            var material = new MaterialDescriptor
            {
                Attributes = new MaterialAttributes()
            };
            foreach (var chan in CurrentMesh.Primitives[primitiveID].Material.Channels)
            {
                if (chan.Texture != null)
                {
                    using var fs = new FileStream(chan.Texture.PrimaryImage.Content.SourcePath, FileMode.Open);
                    var image = Stride.Graphics.Image.Load(fs, true);
                    var texture = Texture.New(Device, image, TextureFlags.None);


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
                            Logger.Info("Added diffuse material");
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
            return material;
        }
        public MeshDraw GetMeshDraw(int primitiveID)
        {
            Stride.Graphics.Buffer vBuff;
            var vs = GetVertexBuffer(primitiveID);
            var vt = GetVertexType(primitiveID);
            if (vt == VertexType.VertexPositionNormalTexture)
            {
                vBuff = Stride.Graphics.Buffer.Vertex.New(
                    Device,
                    AsVPNT(vs),
                    GraphicsResourceUsage.Dynamic
                );
            }
            else
            {
                vBuff = Stride.Graphics.Buffer.Vertex.New(
                    Device,
                    AsVPT(vs),
                    GraphicsResourceUsage.Dynamic
                );
            }

            var iBuff = Stride.Graphics.Buffer.Index.New(
                Device,
                GetIndices(primitiveID)
            );
            var primitiveType = GetPrimitiveType(primitiveID);
            return new MeshDraw
            {
                PrimitiveType = primitiveType,
                DrawCount = iBuff.ElementCount,
                VertexBuffers = new[] { new VertexBufferBinding(vBuff, VertexPositionNormalTexture.Layout, vBuff.ElementCount) },
                IndexBuffer = new IndexBufferBinding(iBuff, true, iBuff.ElementCount),
            };

        }

        public VertexType GetVertexType(int primitiveID)
        {
            var keys = CurrentMesh.Primitives[primitiveID].VertexAccessors.Keys;
            if (keys.Contains("NORMAL"))
                return VertexType.VertexPositionNormalTexture;
            else
                return VertexType.VertexPositionTexture;
        }

        public IVertex[] GetVertexBuffer(int primitiveID)
        {
            var result = new List<IVertex>();
            if (GetVertexType(primitiveID) == VertexType.VertexPositionNormalTexture)
            {
                var positions = CurrentMesh.Primitives[primitiveID].VertexAccessors["POSITION"].AsVector3Array();
                var normals = CurrentMesh.Primitives[primitiveID].VertexAccessors["NORMAL"].AsVector3Array();

                var texAccessor = CurrentMesh.Primitives[primitiveID].VertexAccessors.Where(x => x.Key.Contains("TEXCOORD")).Select(x => x.Key).First();
                var texCoords = CurrentMesh.Primitives[primitiveID].VertexAccessors[texAccessor].AsVector2Array();

                for (int i = 0; i < positions.Count(); i++)
                {
                    result.Add(new VertexPositionNormalTexture(ToStrideVector3(positions[i]), ToStrideVector3(normals[i]), ToStrideVector2(texCoords[i])));
                }
                return result.ToArray();
            }
            else
            {
                var positions = CurrentMesh.Primitives[primitiveID].VertexAccessors["POSITION"].AsVector3Array();

                var texAccessor = CurrentMesh.Primitives[primitiveID].VertexAccessors.Where(x => x.Key.Contains("TEXCOORD")).Select(x => x.Key).First();
                var texCoords = CurrentMesh.Primitives[primitiveID].VertexAccessors[texAccessor].AsVector2Array();
                for (int i = 0; i < positions.Count(); i++)
                {
                    result.Add(new VertexPositionTexture(ToStrideVector3(positions[i]), ToStrideVector2(texCoords[i])));
                }
                return result.ToArray();
            }
        }
        public UInt32[] GetIndices(int primitiveID)
        {
            // return CurrentMesh.Primitives[primitiveID].GetIndices().ToArray();
            var prim = CurrentMesh.Primitives[primitiveID];
            // var result = new List<uint>();
            if (prim.GetIndices() != null)
                // return prim.GetIndices().ToArray();
            {
                var result = new List<uint>();
                prim.GetTriangleIndices().ToList().ForEach(x => {result.Add((uint)x.A);result.Add((uint)x.C);result.Add((uint)x.B);});
                return result.ToArray();
                // return Enumerable.Range(0, prim.GetVertices("POSITION").AsVector3Array().Count -1).Select(x=>(uint)x).ToArray();
            }
            else
            {
                var result = new List<uint>();
                prim.GetTriangleIndices().ToList().ForEach(x => {result.Add((uint)x.A);result.Add((uint)x.B);result.Add((uint)x.C);});
                return result.ToArray();
                // return Enumerable.Range(0, prim.GetVertices("POSITION").AsVector3Array().Count -1).Select(x=>(uint)x).ToArray();
            }
        }

        public int GetTriIndicesCount(int primitiveID)
        {
            return CurrentMesh.Primitives[primitiveID].GetTriangleIndices().Count();
        }
        public Stride.Graphics.PrimitiveType GetPrimitiveType(int primitiveID)
        {
            return CurrentMesh.Primitives[primitiveID].DrawPrimitiveType switch
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

        // public Object GetIndicesByType(int primitiveID)
        // {
        //     List<int> indices = new List<int>();
        //     switch(CurrentMesh.Primitives[primitiveID].DrawPrimitiveType)
        //     {
        //         case SharpGLTF.Schema2.PrimitiveType.TRIANGLES :
        //             CurrentMesh.Primitives[primitiveID].GetTriangleIndices().ToList().ForEach(i => {indices.Add(i.A);indices.Add(i.B);indices.Add(i.C);});
        //             return indices;
        //         case SharpGLTF.Schema2.PrimitiveType.TRIANGLE_STRIP :
        //             CurrentMesh.Primitives[primitiveID].GetTriangleIndices();
        //         case SharpGLTF.Schema2.PrimitiveType.LINES :
        //             Stride.Graphics.PrimitiveType.LineList;
        //         case SharpGLTF.Schema2.PrimitiveType.POINTS :
        //             Stride.Graphics.PrimitiveType.PointList;
        //         case SharpGLTF.Schema2.PrimitiveType.LINE_LOOP :
        //             Stride.Graphics.PrimitiveType.Undefined;
        //         case SharpGLTF.Schema2.PrimitiveType.LINE_STRIP :
        //             Stride.Graphics.PrimitiveType.Undefined;
        //         case SharpGLTF.Schema2.PrimitiveType.TRIANGLE_FAN :
        //             Stride.Graphics.PrimitiveType.Undefined;
        //         _ => Stride.Graphics.PrimitiveType.Undefined,
        //     };
        // }
        private Vector3 ToStrideVector3(System.Numerics.Vector3 a) => new Vector3(a.X, a.Y, a.Z);
        private Vector2 ToStrideVector2(System.Numerics.Vector2 a) => new Vector2(a.X, a.Y);
        private VertexPositionNormalTexture[] AsVPNT(IVertex[] v) => v.Select(x => (VertexPositionNormalTexture)x).ToArray();
        private VertexPositionTexture[] AsVPT(IVertex[] v) => v.Select(x => (VertexPositionTexture)x).ToArray();

        public void LoadTextures()
        {
            GltfRoot
                .LogicalImages
                .Select(x => x.Content.SourcePath)
                .Select(path => new FileStream(path, FileMode.Open))
                .Select(stream => Stride.Graphics.Image.Load(stream, true))
                .Select(image => Texture.New(Device, image, TextureFlags.DepthStencil));
        }
    }

}
