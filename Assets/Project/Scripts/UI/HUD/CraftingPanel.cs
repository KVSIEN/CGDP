using System.Text;
using UnityEngine;
using CGD.Crafting;
using CGD.Feedback;
using CGD.Input;
using CGD.Items;
using CGD.Player;

namespace CGD.UI
{
    // Opens when the player uses a CraftingStation and lists its recipes: what each needs,
    // how much of it the player has, and a Craft button that's only live when they can
    // afford it. Closes with the close button or the Interact key.
    public class CraftingPanel : ModalPanel
    {
        private readonly StringBuilder _line = new();

        private UIButtonList    _recipes;
        private CraftingStation _station;
        private PlayerInventory _inventory;

        protected override string  Title      => "Crafting";
        protected override Vector2 WindowSize => new(560f, 460f);

        protected override void Build(RectTransform content) => _recipes = new UIButtonList(content, 30f);

        private void OnEnable()  => CraftingStation.Opened += OnStationOpened;

        protected override void OnDisable()
        {
            CraftingStation.Opened -= OnStationOpened;
            base.OnDisable();
        }

        private void Update()
        {
            if (IsVisible && !OpenedThisFrame && _input != null && _input.WasPressedRaw(GameAction.Interact))
                Hide();
        }

        private void OnStationOpened(CraftingStation station, GameObject interactor)
        {
            if (IsBlocked(this) || !interactor.TryGetComponent(out PlayerInventory inventory)) return;

            _station   = station;
            _inventory = inventory;
            Show();
        }

        protected override void OnOpened()
        {
            SetTitle(_station.Label);
            _inventory.Inventory.Changed += Refresh;
            Refresh();
        }

        protected override void OnClosed()
        {
            if (_inventory != null) _inventory.Inventory.Changed -= Refresh;
            _station = null;
        }

        public override void Refresh()
        {
            if (!IsVisible || _station == null || _inventory == null) return;

            Inventory inventory = _inventory.Inventory;
            _recipes.Begin();
            if (_station.Recipes.Length == 0) _recipes.Label("Nothing to craft here.");

            foreach (RecipeDefinition recipe in _station.Recipes)
            {
                if (recipe == null) continue;
                _recipes.Add(Describe(recipe, inventory), () => Craft(recipe), Crafter.CanCraft(recipe, inventory));
            }
            _recipes.End();
        }

        private void Craft(RecipeDefinition recipe)
        {
            if (Crafter.TryCraft(recipe, _inventory.Inventory))
                FeedbackBus.Notify($"Crafted {recipe.DisplayName}", NotificationStyle.Reward);
        }

        // "Extended Magazine   Scrap Metal 7/12"
        private string Describe(RecipeDefinition recipe, Inventory inventory)
        {
            _line.Clear();
            _line.Append(recipe.DisplayName);
            if (recipe.Output.SafeCount > 1) _line.Append(" ×").Append(recipe.Output.SafeCount);
            _line.Append("   <color=#AAAAAA>");

            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                ItemAmount ingredient = recipe.Ingredients[i];
                if (ingredient.Item == null) continue;
                if (i > 0) _line.Append(",  ");

                int have = inventory.CountOf(ingredient.Item);
                _line.Append(ingredient.Item.DisplayName).Append(' ').Append(have).Append('/').Append(ingredient.SafeCount);
            }

            return _line.Append("</color>").ToString();
        }
    }
}
