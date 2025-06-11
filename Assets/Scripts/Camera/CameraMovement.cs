using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float sensX = 400f;
    public float sensY = 400f;
    public Transform player;

    float rotationY;
    float rotationX;

    public static float baseSensX;
    public static float baseSensY;

    public static bool isSwinging = false; // Static flag set externally (e.g., from WeaponSwingController)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        baseSensX = sensX;
        baseSensY = sensY;

        //sensX = baseSensX;
        //sensY = baseSensY;

    }

    void Update()
    {
        if (!PlayerMovement.dialogueActive)
        {
            float sensitivityMultiplier = isSwinging ? 0.3f : 1.0f; // Reduce camera sensitivity while swinging

            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * baseSensX * sensitivityMultiplier;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * baseSensY * sensitivityMultiplier;

            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -80f, 80f);

            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
            player.rotation = Quaternion.Euler(0, rotationY, 0);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
