using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Graphics;

namespace GltfImport
{
    public class DisplayBuffer : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        public int Number;

        public override void Start()
        {
            // Initialization of the script.
        }

        public override void Update()
        {
            var model = this.Entity.Get<ModelComponent>();
            if (model!= null)
            {
                var declaration = model.Model.Meshes[0].Draw.VertexBuffers.First().Declaration;
                if (declaration.Equals(VertexSkinned.Layout))
                {
                    var biw = 
                        model.Model.Meshes[0]
                        .Draw.VertexBuffers.First()
                        .Buffer
                        .GetData<VertexSkinned>(Game.GraphicsContext.CommandList)
                        .Select(x => (x.BlendIndices, x.BlendWeight))
                        .ToArray();
                    for (int i = 0; i < biw.Count(); i++)
                    {
                        DebugText.Print(biw[i].ToString(), new Int2(200 * Number, 20 * i + 10));
                    }
                }
                else //if (declaration.Equals(VertexPNTJWT.Layout))
                {
                    var biw =
                        model.Model.Meshes[0]
                        .Draw.VertexBuffers.First()
                        .Buffer
                        .GetData<VertexPNTJWT>(Game.GraphicsContext.CommandList)
                        .Select(x => (x.BlendIndices, x.BlendWeight))
                        .ToArray();
                    for (int i = 0; i < biw.Count(); i++)
                    {
                        DebugText.Print(ByteString(biw[i].BlendIndices), new Int2(200 * Number, 20 * i + 10));
                    }
                }
                //else
                //{
                //    for (int i = 0; i < declaration.VertexElements.Count(); i++)
                //    {
                //        DebugText.Print(declaration.VertexElements[i].ToString(), new Int2(200 * Number, 20*i+10));
                //    }
                //    for (int i = 0; i < VertexPNTJWT.Layout.VertexElements.Count(); i++)
                //    {
                //        DebugText.Print(VertexPNTJWT.Layout.VertexElements[i].ToString(), new Int2(200 * Number, 20 * i + 30 + 20* declaration.VertexElements.Count()));
                //    }

                //}
            }
            
        }

        private String ByteString(int i)
        {
            var array = BitConverter.GetBytes(i);
            return $"{{x : {array[0]},y : {array[1]},z : {array[2]},w : {array[3]}}}";
        }
    }
}
