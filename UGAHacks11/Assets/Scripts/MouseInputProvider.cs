using UnityEngine;

public class MouseInputProvider : MonoBehaviour
{
    [SerializeField] private LineDrawer lineDrawer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lineDrawer.StartNewLine();
        }

        if (Input.GetMouseButton(0))
        {
            lineDrawer.AddPoint(Input.mousePosition);
        }
    }
}