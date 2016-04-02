using System;
using UnityEngine;

public static class AnimatorHelper
{
    /// <summary>
    /// Avoids the issue where a trigger won't reset if the animations it triggers are already playing.
    /// Documented here: http://answers.unity3d.com/questions/801875/mecanim-trigger-getting-stuck-in-true-state.html
    /// </summary>
    /// <param name="_animator">Animator.</param>
    /// <param name="animationName">Animation name.</param>
    /// <param name="triggerName">Trigger name.</param>
    /// <param name="layer">Layer.</param>
    public static void SetTriggerSafe(this Animator _animator, string animationName, string triggerName, int layer)
    {
        //if (_animator.GetCurrentAnimatorStateInfo(layer).IsName(animationName))
        //if (!_animator.GetCurrentAnimatorStateInfo(layer).IsName(animationName))
        {
            _animator.SetTrigger(triggerName);       
            //_animator.Play (animationName);
            //Debug.Log ("Trigger succeeded");

            foreach (string trigger in TargetingSystem.triggers)
            {
                if (!trigger.Equals(triggerName))
                {
                    _animator.ResetTrigger(trigger);
                }
            }
        }
    }
}
