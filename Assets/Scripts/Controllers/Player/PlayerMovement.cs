using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 1.5f; // not sure if i really need this, 
                                    // since the camera adjusts to the players position, 
                                    // even in the y axis. so the raymarch will hit the ground anyways
                                    // i think...

    [Header("Dependencies")]
    public Transform cameraTransform; 

    private SpriteRenderer characterSpriteRenderer;
    private Vector2 inputVector;
    private Vector3 moveDirection;

    void Start()
    {
        // render sprite on start from our animation controller script
        characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (characterSpriteRenderer == null)
            Debug.LogError("PlayerMovement: No SpriteRenderer found in children!");

        if (cameraTransform == null)
        {
            if (Camera.main != null) cameraTransform = Camera.main.transform;
            else Debug.LogError("PlayerMovement: No Camera assigned!");
        }
    }

    void Update()
    {
        inputVector.x = Input.GetAxisRaw("Horizontal");
        inputVector.y = Input.GetAxisRaw("Vertical");

        // flip sprite conditions to flip the sprite in the direction it is facing or walking
        if (characterSpriteRenderer != null && inputVector.magnitude > 0)
        {
            if (inputVector.x < 0) characterSpriteRenderer.flipX = true;
            else if (inputVector.x > 0) characterSpriteRenderer.flipX = false;
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (inputVector.magnitude <= 0) return;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 targetMoveDir = (camForward * inputVector.y) + (camRight * inputVector.x);
        
        // we want to do movement using raycasts, so we can move the target along the ground layer we set
        // so it can properly account for different y levels in terrain
        // as well as preventing the object from moving along the literal y axis when pressing 'up' or 'w'
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.red); // debug ray view

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer))
        {
            // project our flat movement vector onto the slope of the ground
            // this tilts the vector up or down based on the ground angle
            moveDirection = Vector3.ProjectOnPlane(targetMoveDir, hit.normal).normalized;
        }
        else
        {
            // if we are in the air, just move flat, but we shouldnt unless we move off the map
            moveDirection = targetMoveDir.normalized;
        }

        transform.Translate(moveDirection * moveSpeed * Time.fixedDeltaTime, Space.World);
    }

    public bool IsMoving()
    {
        return inputVector.magnitude > 0.01f;
    }
}