using UnityEngine;

public class AimController : MonoBehaviour
{
    private Camera mainCam;
    private Plane mathGround; 

    void Start()
    {
        mainCam = Camera.main;

        if (mainCam == null) 
        {
            Debug.LogError("CRITICAL ERROR: 'Camera.main' is NULL! Go tag your camera as 'MainCamera' in the Inspector.");
        }
        else
        {
            Debug.Log("AimController: Camera found successfully.");
        }
    }

    void Update()
    {
        RotateSelf();
    }

    void RotateSelf()
    {
        if (mainCam == null) return;

        mathGround = new Plane(Vector3.up, transform.position);
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        float enter = 0.0f;
        if (mathGround.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            hitPoint.y = transform.position.y;

            transform.LookAt(hitPoint);

            Debug.DrawLine(transform.position, hitPoint, Color.green);
        }
        else
        {
            Debug.LogWarning("AimController: Math Plane Raycast failed (Mouse might be off screen).");
        }
    }
}