using UnityEngine;
using UnityEngine.UI;

namespace Ecclesia
{
    // All scenery shares one pixel grid; UI text retains the display resolution.
    public sealed class PixelViewport : MonoBehaviour
    {
        const int Width = 288, Height = 512;
        RenderTexture texture;
        RectTransform imageRect;
        Camera worldCamera;
        Rect screenRect;
        GameObject canvasObject;
        GameObject displayCameraObject;

        public void Initialize(Camera camera)
        {
            worldCamera = camera;
            texture = new RenderTexture(Width, Height, 24) { filterMode = FilterMode.Point, antiAliasing = 1, name = "Ecclesia pixel surface" };
            texture.Create(); camera.targetTexture = texture;
            camera.orthographicSize = 16f; camera.aspect = (float)Width / Height;
            camera.allowHDR = false; camera.allowMSAA = false;
            // The world camera renders offscreen. Keep a separate camera targeting
            // Display 1 so Game View has a display output beneath the overlay UI.
            displayCameraObject = new GameObject("Ecclesia Display Camera");
            displayCameraObject.transform.SetParent(transform, false);
            var displayCamera = displayCameraObject.AddComponent<Camera>();
            displayCamera.targetTexture = null;
            displayCamera.targetDisplay = 0;
            displayCamera.cullingMask = 0;
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = ChapelWorld.Hex("#282D25");
            displayCamera.depth = -100;
            displayCamera.allowHDR = false;
            displayCamera.allowMSAA = false;
            displayCamera.useOcclusionCulling = false;
            canvasObject = new GameObject("Pixel viewport", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -100;
            var background = new GameObject("Letterbox", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            var bg = background.GetComponent<RectTransform>(); bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.sizeDelta = Vector2.zero;
            background.GetComponent<Image>().color = ChapelWorld.Hex("#282D25"); background.GetComponent<Image>().raycastTarget = false;
            var surface = new GameObject("288 x 512", typeof(RectTransform), typeof(RawImage));
            surface.transform.SetParent(canvasObject.transform, false);
            imageRect = surface.GetComponent<RectTransform>();
            var image = surface.GetComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
            Update();
        }
        void Update()
        {
            if (!imageRect) return;
            // Reserve the same safe-area header/footer space used by the portrait HUD.
            var safe = Screen.safeArea;
            float uiScale = Screen.width / 720f;
            var available = new Rect(safe.x, safe.y + 150 * uiScale,
                safe.width, Mathf.Max(1, safe.height - 250 * uiScale));
            // Fill the complete content area, retaining the entire map and doorway.
            // Integer-only fitting left large borders on portrait device aspect ratios.
            imageRect.sizeDelta = available.size;
            imageRect.anchoredPosition = available.center - new Vector2(Screen.width, Screen.height) / 2;
            // Input uses the same stretched rectangle as the displayed world texture.
            screenRect = available;
        }
        public bool TryWorldPoint(Vector2 screen, out Vector3 world)
        {
            world = default;
            if (!screenRect.Contains(screen)) return false;
            world = worldCamera.ViewportToWorldPoint(new Vector3((screen.x-screenRect.x)/screenRect.width, (screen.y-screenRect.y)/screenRect.height, 10));
            return true;
        }
        void OnDestroy()
        {
            if (worldCamera) worldCamera.targetTexture = null;
            if (texture) { texture.Release(); Destroy(texture); }
            if (canvasObject) Destroy(canvasObject);
            if (displayCameraObject) Destroy(displayCameraObject);
        }
    }
}
