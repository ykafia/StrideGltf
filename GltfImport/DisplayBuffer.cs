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
            //model.Skeleton.NodeTransformations[1].Transform.Rotation *= Quaternion.RotationY(5.0f * (float)Game.UpdateTime.Elapsed.TotalSeconds * 4.0f);
            //if (model!= null)
            //{
            //    var declaration = model.Model.Meshes[0].Draw.VertexBuffers.First().Declaration;
            //    var buffer = model.Model.Meshes.First().Draw.VertexBuffers.First().Buffer.GetData<byte>(Game.GraphicsContext.CommandList);
            //    for (int i = 0; i < buffer.Length; i+=64)
            //    {
            //        var position = new Vector3(
            //            BitConverter.ToSingle(new ArraySegment<byte>(buffer, i,4).Array, 0),
            //            BitConverter.ToSingle(new ArraySegment<byte>(buffer, i+4, 4).Array, 0),
            //            BitConverter.ToSingle(new ArraySegment<byte>(buffer, i+8, 4).Array, 0)
            //        );
            //        DebugText.Print(position.ToString(), new Int2(Number * 200, i / 2));
            //    }
            //}

        }

    }
}
