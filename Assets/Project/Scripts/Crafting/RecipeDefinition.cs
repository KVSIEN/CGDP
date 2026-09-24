using UnityEngine;

namespace CGD.Crafting
{
    // Turns ingredients into an item. Gear outputs (armor, weapon categories) are rolled
    // fresh at their own tier each time; stackable outputs are added as a stack.
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "CGD/Crafting/Recipe")]
    public class RecipeDefinition : ScriptableObject
    {
        [SerializeField] private ItemAmount[] _ingredients = System.Array.Empty<ItemAmount>();
        [SerializeField] private ItemAmount   _output;

        public ItemAmount[] Ingredients => _ingredients;
        public ItemAmount   Output      => _output;

        public string DisplayName => _output.Item != null ? _output.Item.DisplayName : name;
    }
}
