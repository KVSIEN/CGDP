using CGD.Items;

namespace CGD.Crafting
{
    // The crafting rule: a recipe can be made when the inventory holds every ingredient;
    // making it takes them all and adds the output. All-or-nothing — a failed craft
    // leaves the inventory untouched.
    public static class Crafter
    {
        public static bool CanCraft(RecipeDefinition recipe, Inventory inventory)
        {
            if (recipe == null || inventory == null || recipe.Output.Item == null) return false;

            foreach (ItemAmount ingredient in recipe.Ingredients)
                if (ingredient.Item != null && !inventory.Has(ingredient.Item, Needed(recipe, ingredient.Item)))
                    return false;
            return true;
        }

        // Total of one item across the recipe, in case it's listed more than once.
        public static int Needed(RecipeDefinition recipe, ItemDefinition item)
        {
            int total = 0;
            foreach (ItemAmount ingredient in recipe.Ingredients)
                if (ingredient.Item == item) total += ingredient.SafeCount;
            return total;
        }

        public static bool TryCraft(RecipeDefinition recipe, Inventory inventory)
        {
            if (!CanCraft(recipe, inventory)) return false;

            foreach (ItemAmount ingredient in recipe.Ingredients)
                if (ingredient.Item != null)
                    inventory.Remove(ingredient.Item, ingredient.SafeCount);

            AddOutput(recipe.Output, inventory);
            return true;
        }

        private static void AddOutput(ItemAmount output, Inventory inventory)
        {
            if (output.Item is not GearDefinition gear)
            {
                inventory.Add(output.Item, output.SafeCount);
                return;
            }

            for (int i = 0; i < output.SafeCount; i++)
                inventory.Add(gear.CreateInstance());
        }
    }
}
