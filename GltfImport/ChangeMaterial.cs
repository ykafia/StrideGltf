using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering;
using Stride.Core.Diagnostics;

namespace GltfImport
{
    public class ChangeMaterial : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        private MaterialInstance mat;
        private MaterialInstance oldmat;
        private Model model;
        public override void Start()
        {
            model = this.Entity.Get<ModelComponent>().Model;
            oldmat = model.Materials[0];
            var desc = new MaterialDescriptor
            {
                Attributes =
                    new MaterialAttributes
                    {
                        Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.AliceBlue)),
                        MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(3)),
                        Emissive = new MaterialEmissiveMapFeature(new ComputeFloat4(new Vector4(5, 0, 0, 1)))
                    }
            };
            mat = new MaterialInstance(Material.New(GraphicsDevice, desc));
            Log.ActivateLog(LogMessageType.Info);
        }

        public override void Update()
        {
            // Do stuff every new frame
            if (Input.IsKeyPressed(Keys.Y))
                model.Materials[0] = mat;
            if (Input.IsKeyPressed(Keys.U))
                model.Materials[0] = oldmat;

        }
    }
}
