using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private Transform cursorTransform;
    [SerializeField] private float fadeDuration = 0.5f;
    
    private LineRenderer _currentLine;
    private List<Vector2> _points = new List<Vector2>();

    public void UpdateCursor(Vector2 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        cursorTransform.position = worldPos;
    }

    public void StartNewLine()
    {
        // Immediate tactical reset: If a line exists, destroy it before starting fresh
        if (_currentLine != null) 
        {
            Destroy(_currentLine.gameObject);
        }

        GameObject lineObj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        _currentLine = lineObj.GetComponent<LineRenderer>();
        _points.Clear();
    }

    public void AddPoint(Vector2 screenPosition)
    {
        if (_currentLine == null) return;

        _points.Add(screenPosition);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        
        _currentLine.positionCount = _points.Count;
        _currentLine.SetPosition(_points.Count - 1, worldPos);
    }

    public void InitiateFade()
    {
        if (_currentLine != null)
        {
            StartCoroutine(FadeOutRoutine(_currentLine));
            _currentLine = null; // Detach reference so it's "frozen" while fading
        }
    }

    private IEnumerator FadeOutRoutine(LineRenderer line)
    {
        float elapsed = 0f;
        Gradient originalGradient = line.colorGradient;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            
            // Apply alpha to the gradient keys
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                originalGradient.colorKeys,
                new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha, 1f) }
            );
            line.colorGradient = gradient;
            
            yield return null;
        }

        Destroy(line.gameObject);
    }
}