using System;
using System.Runtime.InteropServices;
using SharpDX.Mathematics.Interop;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position and color information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexSkinned : IEquatable<VertexSkinned>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionTexture"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="textureCoordinate">UV texture coordinates.</param>
        public VertexSkinned(Vector3 position, Vector3 normal, Vector2 textureCoordinate, Vector4 joint, Vector4 weight)
            : this()
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            BlendIndices = joint;
            BlendWeight = weight;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Normal vector.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// UV texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate;

        /// <summary>
        /// BlendIndices vector.
        /// </summary>
        public Vector4 BlendIndices;

        /// <summary>
        /// BlendWeight vector.
        /// </summary>
        public Vector4 BlendWeight;



        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 12+12+8+16+16;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = 
            new VertexDeclaration(
                    VertexElement.Position<Vector3>(),
                    VertexElement.Normal<Vector3>(),
                    VertexElement.TextureCoordinate<Vector2>(),
                    new VertexElement(VertexElementUsage.BlendIndices,3,PixelFormat.R32G32B32A32_Float,VertexElement.AppendAligned),
                    new VertexElement(VertexElementUsage.BlendWeight,4, PixelFormat.R32G32B32A32_Float,VertexElement.AppendAligned)
            );

        public bool Equals(VertexSkinned other)
        {
            return 
                Position.Equals(other.Position) && 
                Normal.Equals(other.Normal) &&
                TextureCoordinate.Equals(other.TextureCoordinate)&&
                BlendIndices.Equals(other.BlendIndices) &&
                BlendWeight.Equals(other.BlendWeight);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is VertexSkinned && this.Equals(obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                hashCode = (hashCode * 397) ^ BlendIndices.GetHashCode();
                hashCode = (hashCode * 397) ^ BlendWeight.GetHashCode();

                return hashCode;
            }
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
            TextureCoordinate.X = (1.0f - TextureCoordinate.X);
        }

        public static bool operator ==(VertexSkinned left, VertexSkinned right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexSkinned left, VertexSkinned right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Position: {Position},Normal: {Normal}, Texcoord: {TextureCoordinate}, BlendIndices: {BlendIndices}, BlendWeight: {BlendWeight}";
        }
    }
}