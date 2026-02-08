using UnityEngine;
using UnityEngine.InputSystem;

public class KeyedInputProvider : MonoBehaviour
{
    [SerializeField] private LineDrawer lineDrawer;
    private bool _isDrawing = false;

    void Update()
    {
        Vector2 currentPos = Mouse.current.position.ReadValue();
        lineDrawer.UpdateCursor(currentPos);

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            _isDrawing = true;
            lineDrawer.StartNewLine();
        }

        if (Keyboard.current.mKey.wasReleasedThisFrame)
        {
            _isDrawing = false;
            lineDrawer.InitiateFade(); // Order the fade-out
        }

        if (_isDrawing)
        {
            lineDrawer.AddPoint(currentPos);
        }
    }
}