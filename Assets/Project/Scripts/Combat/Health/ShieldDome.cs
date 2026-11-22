using UnityEngine;

namespace CGD.Combat
{
    // A dome of force left where a barrier grenade lands: hostile shots and projectiles coming in
    // from outside stop on it, so whoever stands inside is covered from every side, while shots
    // fired from inside fly out (ShieldBarrier). What it stops wears its durability down; it
    // collapses when that is spent or its time runs out. Blasts, melee and beams ignore it.
    // Built at runtime like a Frontal Shield's wall, so it needs no prefab, only a transparent material.
    public sealed class ShieldDome : MonoBehaviour
    {
        private const string BarrierLayer = "Ignore Raycast";
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private float _durability;
        private float _endsAt;
        private float _stopped;

        // Durability 0 = it only ends with its time.
        public static ShieldDome Raise(Vector3 centre, float radius, float seconds, float durability, Material material, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name  = "ShieldDome";
            go.layer = LayerMask.NameToLayer(BarrierLayer);
            go.transform.position   = centre;
            go.transform.localScale = Vector3.one * (radius * 2f);

            // Shots still hit it, but nothing is physically pushed by it.
            go.GetComponent<Collider>().excludeLayers = ~0;

            var renderer = go.GetComponent<MeshRenderer>();
            if (material == null) renderer.enabled = false;
            else
            {
                renderer.sharedMaterial    = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var tint = new MaterialPropertyBlock();
                tint.SetColor(BaseColor, color);
                renderer.SetPropertyBlock(tint);
            }

            var barrier = go.AddComponent<ShieldBarrier>();
            barrier.MakeDome(radius);
            var dome = go.AddComponent<ShieldDome>();
            dome._durability = durability;
            dome._endsAt     = Time.time + seconds;
            barrier.Absorbed += dome.OnAbsorbed;
            return dome;
        }

        private void Update()
        {
            if (Time.time >= _endsAt) Destroy(gameObject);
        }

        private void OnAbsorbed(float damage)
        {
            _stopped += damage;
            if (_durability > 0f && _stopped >= _durability) Destroy(gameObject);
        }
    }
}
