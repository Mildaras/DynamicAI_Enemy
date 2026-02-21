using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CooldownDisplay : MonoBehaviour
{
    [System.Serializable]
    public struct AbilityUI
    {
        public KeyCode   key;          // e.g. KeyCode.F for Blink
        public float     cooldown;     // e.g. blinkCooldown
        public Image     maskImage;    // the CooldownMask Image
        [HideInInspector] public bool  onCooldown;
        [HideInInspector] public float lastUsedTime;
    }

    public AbilityUI[] abilities;

    void Update()
    {
        float now = Time.time;
        for (int i = 0; i < abilities.Length; i++)
        {
            // work directly on abilities[i]
            if (abilities[i].onCooldown)
            {
                float elapsed = now - abilities[i].lastUsedTime;
                if (elapsed >= abilities[i].cooldown)
                {
                    abilities[i].onCooldown = false;
                    abilities[i].maskImage.fillAmount = 0f;
                }
                else
                {
                    // animate fill from 1→0 over the cooldown
                    abilities[i].maskImage.fillAmount = 1f - (elapsed / abilities[i].cooldown);
                }
            }
        }
    }

    // call this when you start an ability
    public void TriggerCooldown(KeyCode key)
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i].key == key)
            {
                abilities[i].onCooldown = true;
                abilities[i].lastUsedTime = Time.time;
                abilities[i].maskImage.fillAmount = 1f;
                return;
            }
        }
    }
}
