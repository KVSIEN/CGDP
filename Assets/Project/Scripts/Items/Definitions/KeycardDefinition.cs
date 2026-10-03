using UnityEngine;

namespace CGD.Items
{
    // Opens a locked door (Door._key). No other use, so it never shows up as crafting
    // material or ammo; generated levels place one per locked door.
    [CreateAssetMenu(fileName = "NewKeycard", menuName = "CGD/Items/Keycard")]
    public class KeycardDefinition : ItemDefinition
    {
        [SerializeField, Min(1)] private int _maxStack = 99;

        public override int MaxStack => Mathf.Max(1, _maxStack);
    }
}
