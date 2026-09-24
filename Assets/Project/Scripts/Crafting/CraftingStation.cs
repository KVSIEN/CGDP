using System;
using UnityEngine;
using CGD.Interaction;

namespace CGD.Crafting
{
    // A workbench in the world. Interacting opens the crafting panel with this station's
    // recipes. The panel lives on the HUD, so the station announces itself through
    // Opened rather than holding a scene reference — stations can be prefabs.
    [RequireComponent(typeof(Collider))]
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _label = "Workbench";
        [SerializeField] private RecipeDefinition[] _recipes = Array.Empty<RecipeDefinition>();

        // (station, the interacting player)
        public static event Action<CraftingStation, GameObject> Opened;

        public string             Label   => _label;
        public RecipeDefinition[] Recipes => _recipes;

        public string GetInteractLabel(GameObject interactor) => $"Use  {_label}";

        public void Interact(GameObject interactor) => Opened?.Invoke(this, interactor);
    }
}
