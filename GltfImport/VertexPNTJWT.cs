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
    public struct VertexPNTJWT : IEquatable<VertexPNTJWT>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionTexture"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="textureCoordinate">UV texture coordinates.</param>
        public VertexPNTJWT(Vector3 position, Vector3 normal, Vector2 textureCoordinate, int joint, Vector4 weight)
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
        public int BlendIndices;

        /// <summary>
        /// BlendWeight vector.
        /// </summary>
        public Vector4 BlendWeight;

        /// <summary>
        /// BlendWeight vector.
        /// </summary>
        public Vector4 Tangent;



        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 12+12+8+4+16+16;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = 
            new VertexDeclaration(
                VertexElement.Position<Vector3>(0,0),
                VertexElement.Normal<Vector3>(0,12),
                VertexElement.TextureCoordinate<Vector2>(0,24),
                new VertexElement(VertexElementUsage.BlendIndices,0,PixelFormat.R8G8B8A8_UInt,32),
                new VertexElement(VertexElementUsage.BlendWeight,0, PixelFormat.R32G32B32A32_Float,36),
                VertexElement.Tangent<Vector4>(0,-1)
            );

        public bool Equals(VertexPNTJWT other)
        {
            return 
                Position.Equals(other.Position) && 
                Normal.Equals(other.Normal) &&
                TextureCoordinate.Equals(other.TextureCoordinate)&&
                BlendIndices.Equals(other.BlendIndices) &&
                BlendWeight.Equals(other.BlendWeight) &&
                Tangent.Equals(other.Tangent);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is VertexPNTJWT && this.Equals(obj);
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

        public static bool operator ==(VertexPNTJWT left, VertexPNTJWT right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPNTJWT left, VertexPNTJWT right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Position: {Position},Normal: {Normal}, Texcoord: {TextureCoordinate}, BlendIndices: {BlendIndices}, BlendWeight: {BlendWeight}, Tangent: {Tangent}";
        }
    }
}