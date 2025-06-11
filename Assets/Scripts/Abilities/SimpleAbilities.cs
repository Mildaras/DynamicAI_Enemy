using UnityEngine;

public class SimpleAbilities : MonoBehaviour
{
    public GameObject traits;
    public GameObject buttons;

    public void Update()
    {
        
    }

    public void GiveExtraJump()
    {
        if (PlayerData.purchaseFromNPC(1000f))
        {
            PlayerData.hasExtraJump = true;
            Transform shopTrait = buttons.transform.Find("Double Jump");
            shopTrait.gameObject.SetActive(false);
        }
        else
        {
            print("Not enough money");
        }
    }


    public void GiveBlink()
    {
        if (PlayerData.purchaseFromNPC(1000f))
        {
            PlayerData.hasBlink = true;
            Transform blinkTrait = traits.transform.Find("Blink");
            Transform shopTrait = buttons.transform.Find("Blink");
            if (blinkTrait != null)
            {
                blinkTrait.gameObject.SetActive(true);
                shopTrait.gameObject.SetActive(false);
            }
        }
        else
        {
            print("Not enough money");
        }
    }

    public void GiveStun()
    {
        if (PlayerData.purchaseFromNPC(2000f))
        {
            PlayerData.hasStunPulse = true;
            Transform stunTrait = traits.transform.Find("Stun");
            Transform shopTrait = buttons.transform.Find("Stun Pulse");
            if (stunTrait != null)
            {
                stunTrait.gameObject.SetActive(true);
                shopTrait.gameObject.SetActive(false);
            }
        }
        else
        {
            print("Not enough money");
        }
    }

    public void GiveReflect()
    {
        if (PlayerData.purchaseFromNPC(5000f))
        {
            PlayerData.hasReflect = true;
            Transform reflectTrait = traits.transform.Find("Reflect");
            Transform shopTrait = buttons.transform.Find("Reflect");
            if (reflectTrait != null)
            {
                reflectTrait.gameObject.SetActive(true);
                shopTrait.gameObject.SetActive(false);
            }
        }
        else
        {
            print("Not enough money");
        }
    }

    public void GiveSmite()
    {
        if (PlayerData.purchaseFromNPC(1000f))
        {
            PlayerData.hasSmite = true;
            Transform smiteTrait = traits.transform.Find("Smite");
            Transform shopTrait = buttons.transform.Find("Smite");
            if (smiteTrait != null)
            {
                smiteTrait.gameObject.SetActive(true);
                shopTrait.gameObject.SetActive(false);
            }
        }
        else
        {
            print("Not enough money");
        }
    }
}
