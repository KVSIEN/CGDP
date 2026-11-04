using UnityEngine;

namespace CGD.FiringRange
{
    // Keeps a texture at a fixed real-world size however the object is scaled, so a wall
    // stretched to 110 m shows the same bricks as one stretched to 11 m. A shared material
    // can't do this on its own: its tiling is one number, while each wall has its own size.
    // The two largest dimensions of the mesh are the ones that carry the texture
    // (walls: width × height, floors and roofs: width × depth).
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TiledSurface : MonoBehaviour
    {
        private static readonly int BaseMapScaleOffsetId = Shader.PropertyToID("_BaseMap_ST");

        [Tooltip("Metres covered by one repeat of the texture")]
        [Min(0.01f)]
        [SerializeField] private float _metersPerTile = 1f;

        private MaterialPropertyBlock _block;

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        private void Apply()
        {
            Vector3 size = Vector3.Scale(GetComponent<MeshFilter>().sharedMesh.bounds.size, transform.lossyScale);

            // The thinnest axis is the edge of the slab; the other two get the texture.
            Vector2 tiles = size.z <= size.x && size.z <= size.y ? new Vector2(size.x, size.y)
                          : size.y <= size.x                     ? new Vector2(size.x, size.z)
                                                                 : new Vector2(size.z, size.y);
            tiles /= _metersPerTile;

            _block ??= new MaterialPropertyBlock();
            var target = GetComponent<MeshRenderer>();
            target.GetPropertyBlock(_block);
            _block.SetVector(BaseMapScaleOffsetId, new Vector4(tiles.x, tiles.y, 0f, 0f));
            target.SetPropertyBlock(_block);
        }
    }
}
