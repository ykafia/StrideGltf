using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Animations;
using Stride.Rendering.Lights;
using Stride.Rendering.Colors;

namespace GltfImport
{
    public class AnimationLight : SyncScript
    {
        // Declared public member fields and properties will show in the game studio

        public override void Start()
        {
            var lightC = Entity.Get<LightComponent>();
            var clip = new AnimationClip { Duration = TimeSpan.FromSeconds(1) };
            var colorLightBaseName = typeof(ColorLightBase).AssemblyQualifiedName;
            var colorRgbProviderName = typeof(ColorRgbProvider).AssemblyQualifiedName;

            clip.AddCurve($"[LightComponent.Key].Type.({colorLightBaseName})Color.({colorRgbProviderName})Value", CreateLightColorCurve());
            clip.RepeatMode = AnimationRepeatMode.LoopInfinite;
            var animC = Entity.GetOrCreate<AnimationComponent>();
            animC.Animations.Add("LightCurve",clip);
            animC.Play("LightCurve");
        }
        private AnimationCurve CreateLightColorCurve()
        {
            return new AnimationCurve<Vector3>
            {
                InterpolationType = AnimationCurveInterpolationType.Linear,
                KeyFrames =
                {
                    CreateKeyFrame(0.00f, Vector3.UnitX), // red

                    CreateKeyFrame(0.50f, Vector3.UnitZ), // blue

                    CreateKeyFrame(1.00f, Vector3.UnitX), // red
                }
            };
        }

        private AnimationCurve CreateLightRotationCurve()
        {
            return new AnimationCurve<Quaternion>
            {
                InterpolationType = AnimationCurveInterpolationType.Linear,
                KeyFrames =
                {
                    CreateKeyFrame(0.00f, Quaternion.RotationX(0)),
                    CreateKeyFrame(0.25f, Quaternion.RotationX(MathUtil.PiOverTwo)),
                    CreateKeyFrame(0.50f, Quaternion.RotationX(MathUtil.Pi)),
                    CreateKeyFrame(0.75f, Quaternion.RotationX(-MathUtil.PiOverTwo)),
                    CreateKeyFrame(1.00f, Quaternion.RotationX(MathUtil.TwoPi))
                }
            };
        }

        private static KeyFrameData<T> CreateKeyFrame<T>(float keyTime, T value)
        {
            return new KeyFrameData<T>((CompressedTimeSpan)TimeSpan.FromSeconds(keyTime), value);
        }

        public override void Update()
        {
            var x = 0;
        }
    }
}
