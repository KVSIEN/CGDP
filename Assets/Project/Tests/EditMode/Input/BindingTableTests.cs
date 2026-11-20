using NUnit.Framework;
using UnityEngine;
using CGD.DevTools;
using CGD.Input;

namespace CGD.Tests
{
    // The behaviour BindingTable gives both binding tables: shared-control checks, slot writes
    // and which rows may be rebound. Saving is left out: it writes the real PlayerPrefs.
    public class BindingTableTests
    {
        private InputBindingSettings   _player;
        private SpectatorInputSettings _spectator;

        [SetUp]
        public void SetUp()
        {
            _player = ScriptableObject.CreateInstance<InputBindingSettings>();
            _player.ResetToDefaults();
            _spectator = ScriptableObject.CreateInstance<SpectatorInputSettings>();
            _spectator.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_spectator);
        }

        [Test]
        public void AControlIsNotSharedWithItsOwnSlot() =>
            Assert.AreEqual(string.Empty, _player.SharedWith(GameAction.Jump, true, "<Keyboard>/space"));

        [Test]
        public void AControlUsedElsewhereNamesTheOtherAction() =>
            Assert.AreEqual("Interact", _player.SharedWith(GameAction.Jump, true, "<Keyboard>/e"));

        [Test]
        public void TheOtherSlotOfTheSameActionCountsAsShared() =>
            Assert.AreEqual("Jump", _player.SharedWith(GameAction.Jump, false, "<Keyboard>/space"));

        [Test]
        public void SpectatorSharingUsesReadableNames()
        {
            string shared = _spectator.SharedWith(SpectatorAction.Pause, true, "<Keyboard>/w");
            Assert.AreEqual(SpectatorActionLabels.Of(SpectatorAction.MoveForward), shared);
        }

        [Test]
        public void SetPathWritesOnlyThatSlot()
        {
            int index = _player.IndexOf(GameAction.Reload);
            _player.SetPath(index, false, "<Keyboard>/g");

            Assert.AreEqual("<Keyboard>/r", _player.PathAt(index, true));
            Assert.AreEqual("<Keyboard>/g", _player.PathAt(index, false));
        }

        [Test]
        public void SetModeKeepsThePaths()
        {
            int index = _player.IndexOf(GameAction.Crouch);
            _player.SetMode(index, InputActionMode.Held);

            Assert.IsTrue(_player.TryGet(GameAction.Crouch, out ActionBinding binding));
            Assert.AreEqual(InputActionMode.Held, binding.Mode);
            Assert.AreEqual("<Keyboard>/leftCtrl", binding.PrimaryPath);
        }

        [Test]
        public void OnlyTheSpectatorSettingsRowIsFixed()
        {
            Assert.IsFalse(_spectator.IsRebindable(SpectatorAction.Settings));
            Assert.IsTrue(_spectator.IsRebindable(SpectatorAction.Pause));
            Assert.IsTrue(_player.IsRebindable(GameAction.Pause));
        }
    }
}
