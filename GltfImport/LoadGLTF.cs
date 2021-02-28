using System;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Core.Diagnostics;
using Stride.Shaders.Parser.Mixins;
using Stride.Graphics;
using System.Linq;
using System.IO;

namespace GltfImport
{
    public class LoadGLTF : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        private bool loaded = false;

        public string Path { get; set; }

        public SharpGLTF.Schema2.ModelRoot fox_glb;
        public override void Start()
        {
            //Entity.Transform.Position = Vector3.UnitY;
            Log.ActivateLog(LogMessageType.Info);
            // var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/cube/AnimatedCube.gltf");
            //fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/fox/Fox.gltf");
            // var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/icosphere/icosphere.gltf");
            // var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/torus/torus.gltf");
            //fox_glb = SharpGLTF.Schema2.ModelRoot.Load("C:/Users/kafia/Documents/blender try/SimpleCube.gltf");
            //fox_glb = SharpGLTF.Schema2.ModelRoot.ReadGLB(File.OpenRead("D:/codeProj/xenproj/GltfImport/GltfImport/Resources/Fox.glb"));
            fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/codeProj/xenproj/GltfImport/GltfImport/Resources/FoxEmbedded.gltf");

        }

        public override void Update()
        {
            
            if (!loaded) 
            {                
                Entity.Add(new ModelComponent(GltfParser.LoadFirstModel(GraphicsDevice, fox_glb)));
                Log.Info("Model Loaded");
                loaded = true;
            }
            DebugText.Print(Entity.Transform.Position.ToString(),new Int2(10,10));
            //var model = Entity.Get<ModelComponent>();

        }

    }
}
