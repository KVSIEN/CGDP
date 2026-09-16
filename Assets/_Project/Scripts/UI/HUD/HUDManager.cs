using UnityEngine;

namespace CGD.UI
{
    // Shows and hides every HUDElement under this object together (death screen,
    // settings menu). New HUD elements only need to be children — no wiring here.
    public class HUDManager : MonoBehaviour
    {
        private HUDElement[] _elements;

        private void Awake()
        {
            _elements = GetComponentsInChildren<HUDElement>(true);
        }

        public void ShowAll()
        {
            foreach (HUDElement element in _elements)
            {
                if (element.ShowWithHud) element.Show();
            }
        }

        public void HideAll()
        {
            foreach (HUDElement element in _elements)
                element.Hide();
        }
    }
}
