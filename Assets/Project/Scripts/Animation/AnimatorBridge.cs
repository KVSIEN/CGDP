using System.Collections.Generic;
using UnityEngine;

namespace CGD.Animation
{
    // Writes gameplay state into an Animator without the gameplay side knowing what the
    // controller contains: parameters the controller doesn't define are skipped (instead of
    // Unity logging a warning every frame), and a missing Animator makes every call a no-op,
    // so characters work the same with or without a rig.
    public class AnimatorBridge
    {
        private readonly Animator _animator;
        private readonly HashSet<int> _parameters = new();

        public AnimatorBridge(Animator animator)
        {
            _animator = animator;
            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (var parameter in animator.parameters)
                _parameters.Add(parameter.nameHash);
        }

        public bool IsActive => _animator != null && _animator.isActiveAndEnabled && _parameters.Count > 0;

        // dampTime smooths blend-tree inputs so a sudden stop doesn't snap the pose.
        public void SetFloat(int id, float value, float dampTime, float deltaTime)
        {
            if (Has(id)) _animator.SetFloat(id, value, dampTime, deltaTime);
        }

        public void SetFloat(int id, float value)
        {
            if (Has(id)) _animator.SetFloat(id, value);
        }

        public void SetBool(int id, bool value)
        {
            if (Has(id)) _animator.SetBool(id, value);
        }

        public void SetInteger(int id, int value)
        {
            if (Has(id)) _animator.SetInteger(id, value);
        }

        public void SetTrigger(int id)
        {
            if (Has(id)) _animator.SetTrigger(id);
        }

        public void ResetTrigger(int id)
        {
            if (Has(id)) _animator.ResetTrigger(id);
        }

        // Back to the entry state with fresh parameters — used when a pooled or dead character
        // comes back so it doesn't resume mid-death-animation.
        public void Rebind()
        {
            if (_animator == null) return;
            _animator.Rebind();
            _animator.Update(0f);
        }

        private bool Has(int id) => IsActive && _parameters.Contains(id);
    }
}
