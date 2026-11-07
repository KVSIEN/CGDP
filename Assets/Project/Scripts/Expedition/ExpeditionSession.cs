using System.IO;
using UnityEngine;
using CGD.Flow;
using CGD.Items;
using CGD.Saving;

namespace CGD.Expedition
{
    // Keeps the ExpeditionLedger (the ship's hold, the packed kit, the last report) alive across
    // scene loads. Sits on the GameFlow object, which survives every load; reached through
    // Instance like GameFlow. With a catalog the hold and kit are also saved to disk
    // (ShipPersistence) and loaded when the game starts.
    [RequireComponent(typeof(GameFlow))]
    public class ExpeditionSession : MonoBehaviour
    {
        [Tooltip("Names items in the save. Without one nothing is saved")]
        [SerializeField] private ItemCatalog _catalog;
        [Tooltip("Off: every play session in the editor starts with an empty ship. Builds always save")]
        [SerializeField] private bool _saveInEditor;
        [Tooltip("File in the persistent data folder. The editor uses its own copy (\"-editor\"), so it never touches a build's save")]
        [SerializeField] private string _fileName = "ship.json";

        public static ExpeditionSession Instance { get; private set; }

        public ExpeditionLedger Ledger { get; } = new();

        // Null when saving is off (no catalog, or in the editor without _saveInEditor).
        public ShipPersistence Persistence { get; private set; }

        private bool SavingEnabled => _catalog != null && (!Application.isEditor || _saveInEditor);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // GameFlow destroys a duplicate's whole object; this just doesn't claim Instance first.
        // Awake, so the save is in the ledger before any ExpeditionRunner deploys in Start.
        private void Awake()
        {
            if (Instance != null && Instance != this) return;
            Instance = this;

            if (!SavingEnabled) return;
            Persistence = new ShipPersistence(Ledger, _catalog, new SaveFile(SavePath()));
            Persistence.Load();
        }

        private void LateUpdate() => Persistence?.SaveIfDirty();

        private void OnApplicationQuit() => Persistence?.SaveIfDirty();

        private void OnDestroy()
        {
            if (Instance != this) return;

            Persistence?.SaveIfDirty();
            Persistence?.Dispose();
            Persistence = null;
            Instance = null;
        }

        private string SavePath()
        {
            string fileName = Application.isEditor
                ? Path.GetFileNameWithoutExtension(_fileName) + "-editor" + Path.GetExtension(_fileName)
                : _fileName;
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
