
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Core.Diagnostics;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Shaders.Ast;

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

        public GeometricMeshData<VertexPNTJW> GeometricData;

        public Logger Logger { get; set; }
        // public Dictionary<string, Texture> Textures { get; set; }

        public Model GetModel(int modelID)
        {
            var model = new Model();
            CurrentMesh = GltfRoot.LogicalMeshes[modelID];
            for (int i = 0; i < GltfRoot.LogicalMeshes[modelID].Primitives.Count; i++)
            {
                //var matDesc = GetMaterial(i);
                var matDesc = new MaterialDescriptor
                {
                    Attributes = new MaterialAttributes
                    {
                        Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Stride.Core.Mathematics.Color.AliceBlue)),
                        DiffuseModel = new MaterialDiffuseLambertModelFeature()
                    }
                };
                var material = Material.New(Device, matDesc);
                model.Add(material);
                //var pc = new ParameterCollection();
                //var p1 = new ParameterKeyInfo(new ObjectParameterKey<bool>("Material.HasSkinningPosition",1,Array.Empty<PropertyKeyMetadata>()),0,
                //pc.ParameterKeyInfos.Add(p);




                var mesh = new Mesh { Draw = GetMeshDraw(i) };

                mesh.Parameters.Set<bool>(MaterialKeys.HasSkinningPosition, true);
                mesh.Parameters.Set<bool>(MaterialKeys.HasSkinningNormal, true);

                //Logger.Info("Index of material is : " + model.Materials.IndexOf(material));
                mesh.MaterialIndex = model.Materials.IndexOf(material);
                model.Add(mesh);
                model.Skeleton = GetSkeleton(i, mesh);
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
                            //material.Attributes.DiffuseModel = new MaterialDiffuseCelShadingModelFeature();
                            //Logger.Info("Added diffuse material");
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
            //if (vt == VertexType.VertexPositionNormalTexture)
            //{
            //    vBuff = Stride.Graphics.Buffer.Vertex.New(
            //        Device,
            //        AsVPNTJW(vs),
            //        GraphicsResourceUsage.Default
            //    );
            //}
            //else
            //{
            //    vBuff = Stride.Graphics.Buffer.Vertex.New(
            //        Device,
            //        AsVPT(vs),
            //        GraphicsResourceUsage.Dynamic
            //    );
            //}
            vBuff = Stride.Graphics.Buffer.Vertex.New(
                    Device,
                    AsVPNTJW(vs),
                    GraphicsResourceUsage.Default
                );

            var iBuff = Stride.Graphics.Buffer.Index.New(
                Device,
                GetIndices(primitiveID)
            );
            var primitiveType = GetPrimitiveType(primitiveID);

            return new MeshDraw
            {
                PrimitiveType = primitiveType,
                DrawCount = iBuff.ElementCount,
                VertexBuffers = new[] { new VertexBufferBinding(vBuff, VertexPNTJW.Layout, vBuff.ElementCount) },
                IndexBuffer = new IndexBufferBinding(iBuff, true, iBuff.ElementCount)
            };


        }
        public bool IsSkinnedMesh() => CurrentMesh.AllPrimitivesHaveJoints;

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
            var elems = new List<VertexElement>();
            if (GetVertexType(primitiveID) == VertexType.VertexPositionNormalTexture)
            {
                var positions = CurrentMesh.Primitives[primitiveID].VertexAccessors["POSITION"].AsVector3Array();
                var normals = CurrentMesh.Primitives[primitiveID].VertexAccessors["NORMAL"].AsVector3Array();
                var joints = CurrentMesh.Primitives[primitiveID].VertexAccessors["JOINTS_0"].AsVector4Array();
                var weights = CurrentMesh.Primitives[primitiveID].VertexAccessors["WEIGHTS_0"].AsVector4Array();
                var texAccessor = CurrentMesh.Primitives[primitiveID].VertexAccessors.Where(x => x.Key.Contains("TEXCOORD")).Select(x => x.Key).First();
                var texCoords = CurrentMesh.Primitives[primitiveID].VertexAccessors[texAccessor].AsVector2Array();

                for (int i = 0; i < positions.Count(); i++)
                {
                    result.Add(
                        new VertexPNTJW(
                            ToStrideVector3(positions[i]),
                            ToStrideVector3(normals[i]),
                            ToStrideVector2(texCoords[i]),
                            ToStrideInt4(joints[i]),
                            ToStrideVector4(weights[i])
                        )
                    );
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



        public uint[] GetIndices(int primitiveID)
        {
            var prim = CurrentMesh.Primitives[primitiveID];

            var result = new List<uint>();
            prim.GetTriangleIndices().ToList().ForEach(x => { result.Add((uint)x.A); result.Add((uint)x.C); result.Add((uint)x.B); });
            return result.ToArray();
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

        
        // TODO : Check each joints *FOR EACH MESHES*
        public Skeleton GetSkeleton(int primitiveID, Mesh mesh)
        {
            var foxSkin = GltfRoot.LogicalNodes.First(x => x.Mesh == CurrentMesh).Skin;
            List<int> BoneInfluencing = new List<int>();
            var glSK = foxSkin.Skeleton;
            CurrentMesh
                .Primitives[primitiveID]
                .VertexAccessors["JOINTS_0"]
                .AsVector4Array()
                .ToList()
                .ForEach(
                    x=> { BoneInfluencing.Add((int)x.X); BoneInfluencing.Add((int)x.Y); BoneInfluencing.Add((int)x.Z); BoneInfluencing.Add((int)x.W); }
            );
            BoneInfluencing = BoneInfluencing.Distinct().ToList();
            var sk = new Skeleton();
            var nodes = new List<ModelNodeDefinition>();
            var glnodes = new List<SharpGLTF.Schema2.Node>();
            var matNodes = new List<System.Numerics.Matrix4x4>();
            //var glJoints = new List<(SharpGLTF.Schema2.Node, System.Numerics.Matrix4x4)>();
            for (int i = 0; i < foxSkin.JointsCount; i++)
            {
                glnodes.Add(foxSkin.GetJoint(i).Joint);
                matNodes.Add(foxSkin.GetJoint(i).InverseBindMatrix);
            }
            glnodes.ForEach(
                x =>
                {
                    var pn = GetParentNodeIndex(glnodes, x);
                    nodes.Add(
                        new ModelNodeDefinition
                        {
                            Flags = pn == -1 ? ModelNodeFlags.EnableRender : ModelNodeFlags.Default,
                            ParentIndex = pn,
                            Transform =
                                    new TransformTRS
                                    {
                                        Position = ToStrideVector3(x.LocalTransform.Translation),
                                        Rotation = ToStrideQuaternion(x.LocalTransform.Rotation),
                                        Scale = ToStrideVector3(x.LocalTransform.Scale)
                                    },
                            Name = x.Name
                        }
                    );
                }
            );

            var rootTransform = ToStrideMatrix2(glnodes[0].WorldMatrix);
            var rootTransformInverse = rootTransform;
            rootTransformInverse.Invert();

            
            var mbd = 
                BoneInfluencing
                .Select(x => (x, ToStrideMatrix1(matNodes[x])))
                .Select(
                    v => new MeshBoneDefinition 
                    { 
                        NodeIndex = v.x, 
                        LinkToMeshMatrix = v.Item2//rootTransformInverse*v.Item2*rootTransform 
                    })
                .ToList();

            
            sk.Nodes = nodes.ToArray();
            mesh.Skinning = new MeshSkinningDefinition
            {
                Bones = mbd.ToArray()
            };

            return sk;
        }
        private int GetParentNodeIndex(List<SharpGLTF.Schema2.Node> glnodes, SharpGLTF.Schema2.Node current)
        {
            try
            {
                return glnodes.IndexOf(glnodes.First(y => y.Name == current.VisualParent.Name));
            }
            catch (Exception)
            {
                return -1;
            }
        }
        private Vector4 ToStrideVector4(System.Numerics.Vector4 a) => new Vector4(a.X, a.Y, a.Z, a.W);
        private Int4 ToStrideInt4(System.Numerics.Vector4 a) => new Int4((int)a.X, (int)a.Y, (int)a.Z, (int)a.W);

        private Vector3 ToStrideVector3(System.Numerics.Vector3 a) => new Vector3(a.X, a.Y, a.Z);
        private Vector2 ToStrideVector2(System.Numerics.Vector2 a) => new Vector2(a.X, a.Y);
        private VertexPNTJW[] AsVPNTJW(IVertex[] v) => v.Select(x => (VertexPNTJW)x).ToArray();
        private VertexPositionNormalTexture[] AsVPNT(IVertex[] v) => v.Select(x => (VertexPositionNormalTexture)x).ToArray();
        private VertexPositionTexture[] AsVPT(IVertex[] v) => v.Select(x => (VertexPositionTexture)x).ToArray();
        public Vector3 ToStridePosition(SharpGLTF.Transforms.AffineTransform tr) => new Vector3(tr.Translation.X, tr.Translation.Y, tr.Translation.Z);
        public Quaternion ToStrideQuaternion(System.Numerics.Quaternion rot) => new Quaternion(rot.X, rot.Y, rot.Z, rot.W);
        public Matrix ToStrideMatrix1(System.Numerics.Matrix4x4 mat)
        {
            return new Matrix(
                    mat.M11, mat.M21, mat.M31, mat.M41,
                    mat.M12, mat.M22, mat.M32, mat.M42,
                    mat.M13, mat.M23, mat.M33, mat.M43,
                    mat.M14, mat.M24, mat.M34, mat.M44
                );
        }
        public Matrix ToStrideMatrix2(System.Numerics.Matrix4x4 mat)
        {
            return new Matrix(
                    mat.M11, mat.M12, mat.M13, mat.M14,
                    mat.M21, mat.M22, mat.M23, mat.M24,
                    mat.M31, mat.M32, mat.M33, mat.M34,
                    mat.M41, mat.M42, mat.M43, mat.M44
                );
        }
    }
}
