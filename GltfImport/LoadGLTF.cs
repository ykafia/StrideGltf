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
using StrideBuffer = Stride.Graphics.Buffer;

namespace GltfImport
{
    public class LoadGLTF : StartupScript
    {
        // Declared public member fields and properties will show in the game studio
        public string Path{get;set;}
        public override void Start()
        {
            var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/cube/AnimatedCube.gltf");
            var modelLoader = new GLTFMeshParser
            {
                Device = this.GraphicsDevice,
                AssetMesh = fox_glb.LogicalMeshes[0]
            };
            Entity.Add(new ModelComponent(modelLoader.GetModel()));
        }

    }
}
