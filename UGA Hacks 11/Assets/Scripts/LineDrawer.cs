using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private Transform cursorTransform;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Normalized input mapping")]
    [Tooltip("If true, incoming normalized Y is assumed TOP-left origin (y increases downward).")]
    [SerializeField] private bool invertNormalizedY = true;

    [Tooltip("Z distance used for ScreenToWorldPoint. For perspective cameras, must be in front of the camera.")]
    [SerializeField] private float screenToWorldZ = 10f;

    [Tooltip("Optional camera override. If null, uses Camera.main.")]
    [SerializeField] private Camera cameraOverride;

    private LineRenderer _currentLine;

    // Store normalized points (0..1) so behavior is resolution independent.
    private readonly List<Vector2> _pointsNorm = new List<Vector2>();

    private Camera Cam => cameraOverride != null ? cameraOverride : Camera.main;

    /// <summary>
    /// Update the cursor using normalized (0..1) coords.
    /// Call this on the MAIN THREAD only.
    /// </summary>
    public void UpdateCursorNormalized(Vector2 normalizedPosition)
    {
        var cam = Cam;
        if (cam == null || cursorTransform == null) return;

        Vector3 worldPos = NormalizedToWorld(normalizedPosition, cam);
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
        _pointsNorm.Clear();
    }

    /// <summary>
    /// Add a point using normalized (0..1) coords.
    /// Call this on the MAIN THREAD only.
    /// </summary>
    public void AddPointNormalized(Vector2 normalizedPosition)
    {
        if (_currentLine == null) return;

        var cam = Cam;
        if (cam == null) return;

        _pointsNorm.Add(normalizedPosition);

        Vector3 worldPos = NormalizedToWorld(normalizedPosition, cam);

        _currentLine.positionCount = _pointsNorm.Count;
        _currentLine.SetPosition(_pointsNorm.Count - 1, worldPos);
    }

    public void InitiateFade()
    {
        if (_currentLine != null)
        {
            StartCoroutine(FadeOutRoutine(_currentLine));
            _currentLine = null; // Detach reference so it's "frozen" while fading
        }
    }

    private Vector3 NormalizedToWorld(Vector2 norm, Camera cam)
    {
        // Clamp to sane bounds in case sender jitters slightly outside 0..1
        float xNorm = Mathf.Clamp01(norm.x);
        float yNorm = Mathf.Clamp01(norm.y);

        if (invertNormalizedY)
            yNorm = 1f - yNorm;

        float px = xNorm * Screen.width;
        float py = yNorm * Screen.height;

        return cam.ScreenToWorldPoint(new Vector3(px, py, screenToWorldZ));
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
                new GradientAlphaKey[] {
                    new GradientAlphaKey(alpha, 0f),
                    new GradientAlphaKey(alpha, 1f)
                }
            );
            line.colorGradient = gradient;

            yield return null;
        }

        Destroy(line.gameObject);
    }
}