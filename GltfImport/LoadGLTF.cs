using System;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Core.Diagnostics;
using Stride.Shaders.Parser.Mixins;

namespace GltfImport
{
    public class LoadGLTF : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        public string Path { get; set; }
        public override void Start()
        {
            Log.ActivateLog(LogMessageType.Info);
            // var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/cube/AnimatedCube.gltf");
            var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/fox/Fox.gltf");
            // var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/icosphere/icosphere.gltf");
            // var fox_glb = SharpGLTF.Schema2.ModelRoot.Load("D:/Downloads/glTF/torus/torus.gltf");

            var modelLoader = new GLTFMeshParser
            {
                Device = this.GraphicsDevice,
                GltfRoot = fox_glb,
                Logger = Log
            };
            Entity.Add(new ModelComponent(modelLoader.GetModel(0)));
            Log.Info("Model Loaded");

        }

        public override void Update()
        {
            var model = Entity.Get<ModelComponent>();
            //model.Skeleton.NodeTransformations[6].Transform.Rotation *= Quaternion.RotationX(0.000001f * (float)Game.UpdateTime.Elapsed.TotalSeconds);
        }
    }
}
