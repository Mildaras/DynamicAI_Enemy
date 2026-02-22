using UnityEngine;
using System;
using System.Collections;
using TMPro;
using UnityEngine.UI;


public class PlayerData : MonoBehaviour
{
    public static event Action OnPlayerDeath;
    public static bool hasExtraJump = false;
    public static bool hasInvunerability = false;
    public static bool hasBlink = false;
    public static bool hasStunPulse = false;
    public static bool hasReflect = false;
    public static bool hasSmite = false;

    static float health = 100f;
    static float maxHealth = 100f;
    static float extraRegen = 15f;
    static float shield = 0f;
    static float maxShield = 200f;
    static float gold = 0f;

    public static float playerHealth = health;
    public static float playerShield = shield;
    public static float playerGold = gold;

    public TextMeshProUGUI healthText;
    public TextMeshProUGUI shieldText;
    public TextMeshProUGUI goldText;

    private Coroutine regenCoroutine;

    public void Update()
    {
        playerHealth = health;
        playerShield = shield;
        playerGold = gold;

        healthText.text = Mathf.RoundToInt(health).ToString();
        shieldText.text = Mathf.RoundToInt(shield).ToString();
        goldText.text = Mathf.RoundToInt(gold).ToString();


        if (Input.GetKeyDown(KeyCode.C))
        {
            addGold(100000);
            print("Gold added");
        }

        if ((health < maxHealth || shield < maxShield) && regenCoroutine == null)
        {
            regenCoroutine = StartCoroutine(HPRegeneration());
        }
    }

    private IEnumerator HPRegeneration()
    {
        while (health < maxHealth || shield < maxShield)
        {
            float regenThisFrame = extraRegen * Time.deltaTime;

            if (health < maxHealth)
            {
                health = Mathf.Min(health + regenThisFrame, maxHealth);
            }
            else if (shield < maxShield)
            {
                shield = Mathf.Min(shield + regenThisFrame, maxShield);
            }

            yield return null;
        }
        regenCoroutine = null;
    }

    public static void takeDamage(float damage)
    {
        if(shield > 0)
        {
            shield -= damage;
            if (shield < 0)
            {
                health += shield;
                shield = 0;
            }
            else if (shield == 0)
            {
                health -= damage;
            }
        }
        else
        {
            health -= damage;
        }
        
        // Trigger damage indicator (red flash + screen shake)
        if (DamageIndicator.Instance != null)
        {
            DamageIndicator.Instance.OnDamageTaken(damage);
        }

        // trigger death exactly once
        if (health <= 0f)
        {
            health = 0f;
            Debug.Log("Player is dead");
            OnPlayerDeath?.Invoke();
        }
    }

    public static bool purchaseFromNPC(float cost)
    {
        if (gold >= cost)
        {
            removeGold(cost);
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void addGold(float amount)
    {
        gold += amount;
    }

    static void removeGold(float amount)
    {
        gold -= amount;
    }
}
