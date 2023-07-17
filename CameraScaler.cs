using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DefaultNamespace
{
    public class CameraScaler : MonoBehaviour
    {
        [SerializeField] private Camera camera;
        [SerializeField] private Collider2D collider;
        [SerializeField] private float buffer = 1f;

        private void Update()
        {
            var (center, size)        = CalculateOrthoSize();
            camera.transform.position = center;
            camera.orthographicSize   = size;
        }

        private (Vector3 center, float size) CalculateOrthoSize()
        {
            var bounds = collider.bounds;
            bounds.Expand(buffer);

            var vertical   = bounds.size.y;
            var horizontal = bounds.size.x * camera.pixelHeight / camera.pixelWidth;

            var size   = Mathf.Max(horizontal, vertical) * .5f;
            var center = bounds.center + new Vector3(0, 0, -10);

            return (center, size);
        }
    }
}