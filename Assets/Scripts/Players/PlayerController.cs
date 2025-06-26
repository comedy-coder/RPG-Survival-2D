using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runMultiplier = 1.5f;
    
    [Header("Components")]
    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 mousePos;
    
    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        // Get input
        movement.x = Input.GetAxisRaw("Horizontal"); // A/D or Arrow Keys
        movement.y = Input.GetAxisRaw("Vertical");   // W/S or Arrow Keys
        
        // Get mouse position for rotation (optional)
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
    
    void FixedUpdate()
    {
        // Move player
        MovePlayer();
        
        // Rotate toward mouse (optional)
        RotatePlayer();
    }
    
    void MovePlayer()
    {
        // Calculate speed (run if holding Shift)
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed *= runMultiplier;
        }
        
        // Apply movement
        rb.MovePosition(rb.position + movement * currentSpeed * Time.fixedDeltaTime);
    }
    
    void RotatePlayer()
    {
        // Calculate rotation toward mouse
        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;
    }
}