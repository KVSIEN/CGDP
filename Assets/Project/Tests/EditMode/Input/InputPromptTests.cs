using NUnit.Framework;
using UnityEngine;
using CGD.Input;

namespace CGD.Tests
{
    public class InputPromptTests
    {
        private InputBindingSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<InputBindingSettings>();
            _settings.ResetToDefaults();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_settings);

        [TestCase("Left Button", "LMB")]
        [TestCase("Right Button", "RMB")]
        [TestCase("Left Shift", "Shift")]
        [TestCase("Right Ctrl", "Ctrl")]
        [TestCase("Escape", "Esc")]
        [TestCase("Scroll/Down", "Wheel Down")]
        [TestCase("Scroll/Up", "Wheel Up")]
        [TestCase("E", "E")]
        [TestCase("", InputPromptLabels.Unbound)]
        public void AbbreviatesLongControlNames(string readable, string expected) =>
            Assert.AreEqual(expected, InputPromptLabels.Abbreviate(readable));

        [Test]
        public void UnboundPathIsTheUnboundMark() =>
            Assert.AreEqual(InputPromptLabels.Unbound, InputPromptLabels.Short(""));

        [Test]
        public void DefaultBindingsShowTheirKeys()
        {
            Assert.AreEqual("E", _settings.PromptFor(GameAction.Interact));
            Assert.AreEqual("C", _settings.PromptFor(GameAction.Dodge));
            Assert.AreEqual("RMB", _settings.PromptFor(GameAction.AimDownSights));
        }

        [Test]
        public void AnActionWithNoBindingShowsTheUnboundMark() =>
            Assert.AreEqual(InputPromptLabels.Unbound, _settings.PromptFor(GameAction.LastWeapon));

        [Test]
        public void TheSecondaryBindingShowsWhenThePrimaryIsEmpty()
        {
            for (int i = 0; i < _settings.Bindings.Count; i++)
            {
                if (_settings.Bindings[i].Action != GameAction.Reload) continue;
                ActionBinding binding = _settings.Bindings[i];
                binding.PrimaryPath   = "";
                binding.SecondaryPath = "<Keyboard>/g";
                _settings.Bindings[i] = binding;
            }

            Assert.AreEqual("G", _settings.PromptFor(GameAction.Reload));
        }

        [Test]
        public void ChangedIsRaisedByNotifyChanged()
        {
            int raised = 0;
            _settings.Changed += () => raised++;
            _settings.NotifyChanged();
            Assert.AreEqual(1, raised);
        }
    }
}
