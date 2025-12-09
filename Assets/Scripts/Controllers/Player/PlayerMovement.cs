using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 1.5f;
    // offset to ensure the ray starts inside the player but checks below feet
    public float heightOffset = 0.5f; 

    [Header("Dependencies")]
    public Transform cameraTransform; 

    private SpriteRenderer characterSpriteRenderer;
    private Vector2 inputVector;
    private Vector3 moveDirection;

    public bool isMoving => inputVector.magnitude > 0.01f;

    void Start()
    {
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

        // Flip logic - Only flip if we are actually pressing a direction
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
        // 1. Calculate the intended move direction (even if it is zero)
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 targetMoveDir = (camForward * inputVector.y) + (camRight * inputVector.x);

        // instead of moving the player through raycasts to clip to the ground
        // we want to just make sure that the player is constantly clipping to the ground
        // in case some sprite actions want to change the y location of the object in space
        // to the y location the action was called on

        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * heightOffset;

        Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.red); 

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer))
        {
            // tilt the movement vector to match the slope
            moveDirection = Vector3.ProjectOnPlane(targetMoveDir, hit.normal).normalized;

            // if we are close to the ground, snap the Y position to the hit point
            float targetY = hit.point.y;
            
            // we only snap if we are very close to avoid snapping down from a jump/cliff too hard
            if (Mathf.Abs(transform.position.y - targetY) < 0.5f) 
            {
                Vector3 newPos = transform.position;
                
                // move position to the ground hit point
                newPos.y = Mathf.MoveTowards(transform.position.y, targetY, 10f * Time.fixedDeltaTime);
                transform.position = newPos;
            }
        }
        else
        {
            moveDirection = targetMoveDir.normalized;
        }

        // only move if there is input, otherwise we just stand there
        if (inputVector.magnitude > 0.01f)
        {
            transform.Translate(moveDirection * moveSpeed * Time.fixedDeltaTime, Space.World);
        }
    }

    public bool IsMoving()
    {
        return inputVector.magnitude > 0.01f;
    }
}