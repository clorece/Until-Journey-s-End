using UnityEngine;
using System.Collections; // Required for Coroutines

[RequireComponent(typeof(EntityStats))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 1.5f;
    public float heightOffset = 0.5f; 

    [Header("Dependencies")]
    public Transform cameraTransform; 

    private bool isLunging = false;
    private Vector3 lungeDirection;
    private float lungeSpeed;

    private SpriteRenderer characterSpriteRenderer;
    private EntityStats myStats;
    private Vector2 inputVector;
    private Vector3 moveDirection;
    private Camera mainCam;

    public bool isMoving => inputVector.magnitude > 0.01f;

    void Start()
    {
        characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myStats = GetComponent<EntityStats>();
        mainCam = Camera.main;

        if (characterSpriteRenderer == null)
            Debug.LogError("PlayerMovement: No SpriteRenderer found in children!");

        if (cameraTransform == null && mainCam != null) 
            cameraTransform = mainCam.transform;
    }

    void Update()
    {
        if (isLunging) return;

        inputVector.x = Input.GetAxisRaw("Horizontal");
        inputVector.y = Input.GetAxisRaw("Vertical");

        FlipSpriteTowardsMouse();
    }

    void FixedUpdate()
    {
        if (isLunging)
        {
            HandleLunge();
        }
        else
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 targetMoveDir = (camForward * inputVector.y) + (camRight * inputVector.x);

        // we want to raycast from the center of our camera/screen to constantly clip the player on the ground layer
        // this way we cant prevent any changes to the state of the character from changing the y level that
        // the player is on.
        // this also helps take account elevation and angled ground

        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * heightOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer))
        {
            moveDirection = Vector3.ProjectOnPlane(targetMoveDir, hit.normal).normalized;
            
            float targetY = hit.point.y;
            if (Mathf.Abs(transform.position.y - targetY) < 0.5f) 
            {
                Vector3 newPos = transform.position;
                newPos.y = Mathf.MoveTowards(transform.position.y, targetY, 10f * Time.fixedDeltaTime);
                transform.position = newPos;
            }
        }
        else
        {
            moveDirection = targetMoveDir.normalized;
        }

        if (inputVector.magnitude > 0.01f)
        {
            float currentSpeed = myStats.GetStatValue(StatType.MoveSpeed);
            transform.Translate(moveDirection * currentSpeed * Time.fixedDeltaTime, Space.World);
        }
    }

    private void FlipSpriteTowardsMouse()
    {
        if (characterSpriteRenderer == null || mainCam == null) return;

        Plane playerPlane = new Plane(Vector3.up, transform.position);
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        float enter = 0.0f;

        if (playerPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            if (hitPoint.x < transform.position.x)
                characterSpriteRenderer.flipX = true; // face Left
            else
                characterSpriteRenderer.flipX = false; // face Right
        }
    }

    public void ApplyLunge(Vector3 direction, float speed, float duration)
    {
        if (isLunging) return; // prevent double-dashing
        StartCoroutine(LungeRoutine(direction, speed, duration));
    }

    private IEnumerator LungeRoutine(Vector3 direction, float speed, float duration)
    {
        isLunging = true;
        lungeDirection = direction;
        lungeSpeed = speed;

        yield return new WaitForSeconds(duration);

        isLunging = false;
    }

    private void HandleLunge()
    {
        // move strictly in the dash direction, ignoring ground snapping for speed
        transform.Translate(lungeDirection * lungeSpeed * Time.fixedDeltaTime, Space.World);
    }
}