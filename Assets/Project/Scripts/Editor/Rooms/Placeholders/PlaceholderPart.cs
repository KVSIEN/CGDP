using System;
using UnityEngine;

namespace CGD.Editor
{
    // One primitive of a placeholder prop, in metres from the prop's foot: x right, y up,
    // z out from the wall it backs onto (its front). Size is the part's full width, height
    // and depth, whatever its shape (a cylinder's height too).
    [Serializable]
    public class PlaceholderPart
    {
        public string Name = "Body";
        public PlaceholderShape Shape;
        public Vector3 Centre = new(0f, 0.5f, 0f);
        public Vector3 Size = Vector3.one;
        [Tooltip("Degrees about the prop's x axis (a ramp, a chute, a tank on its side)")]
        public float Tilt;
        public int Swatch;
        [Tooltip("Blocks movement and shots (has a collider). Ground cover is not solid")]
        public bool Solid = true;

        public PlaceholderPart Clone() => (PlaceholderPart)MemberwiseClone();

        // Unity's cylinder is two units tall; the others one.
        public Vector3 Scale => Shape == PlaceholderShape.Cylinder ? new Vector3(Size.x, Size.y / 2f, Size.z) : Size;

        public Bounds Bounds
        {
            get
            {
                float cos = Mathf.Abs(Mathf.Cos(Tilt * Mathf.Deg2Rad)), sin = Mathf.Abs(Mathf.Sin(Tilt * Mathf.Deg2Rad));
                return new Bounds(Centre, new Vector3(Size.x, cos * Size.y + sin * Size.z, sin * Size.y + cos * Size.z));
            }
        }

        // The part a prefab's child is, when it is a plain primitive.
        public static bool TryRead(Transform child, PlaceholderPalette palette, out PlaceholderPart part)
        {
            part = null;
            if (!child.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) return false;
            if (!TryShapeOf(filter.sharedMesh.name, out PlaceholderShape shape)) return false;

            Vector3 scale = child.localScale;
            part = new PlaceholderPart
            {
                Name   = child.name,
                Shape  = shape,
                Centre = child.localPosition,
                Size   = shape == PlaceholderShape.Cylinder ? new Vector3(scale.x, scale.y * 2f, scale.z) : scale,
                Tilt   = Mathf.DeltaAngle(0f, child.localEulerAngles.x),
                Swatch = child.TryGetComponent(out MeshRenderer renderer) && palette != null ? Mathf.Max(0, palette.IndexOf(renderer.sharedMaterial)) : 0,
                Solid  = child.TryGetComponent(out Collider _),
            };
            return true;
        }

        private static bool TryShapeOf(string meshName, out PlaceholderShape shape)
        {
            shape = PlaceholderShape.Box;
            if (meshName == "Cube") return true;
            if (meshName == "Cylinder") { shape = PlaceholderShape.Cylinder; return true; }
            if (meshName == "Sphere") { shape = PlaceholderShape.Sphere; return true; }
            return false;
        }

        public PrimitiveType Primitive => Shape switch
        {
            PlaceholderShape.Cylinder => PrimitiveType.Cylinder,
            PlaceholderShape.Sphere   => PrimitiveType.Sphere,
            _                         => PrimitiveType.Cube,
        };
    }
}
