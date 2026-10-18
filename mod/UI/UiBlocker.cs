using UnityEngine;
using UnityEngine.UI;

namespace APKingdom2Crown;

// A top-most uGUI canvas behind the menu window that does two jobs:
//  - An invisible, raycast-catching panel exactly behind the window, because IMGUI windows don't stop clicks
//    reaching the game's own (uGUI) menus underneath, e.g. the pause menu.
//  - Showing the menu's last drawn picture. The menu is drawn into a render texture only when something can have
//    changed (mouse, keys, a few times a second otherwise) and this shows that picture on every other frame, so
//    an open menu costs next to nothing.
internal static class UiBlocker
{
    private static GameObject _root;
    private static RectTransform _panel;
    private static RawImage _display;
    private static RenderTexture _picture;
    private static bool _failed, _captureFailed;

    // False if render textures don't work here; the menu then draws itself every frame instead.
    public static bool CanCapture => !_captureFailed && Ensure();

    public static void Cover(Rect screenRectTopLeft)
    {
        if (!Ensure()) return;
        var position = new Vector2(screenRectTopLeft.x, -screenRectTopLeft.y);
        var size = new Vector2(screenRectTopLeft.width, screenRectTopLeft.height);
        if (_panel.anchoredPosition != position) _panel.anchoredPosition = position;
        if (_panel.sizeDelta != size) _panel.sizeDelta = size;
        if (!_root.activeSelf) _root.SetActive(true);
    }

    public static void Hide()
    {
        if (_display != null) _display.enabled = false; // no stale picture flashing up next time the menu opens
        if (_root != null && _root.activeSelf) _root.SetActive(false);
    }

    // Redirects IMGUI drawing into the picture. Returns the previous render target to hand back to EndCapture,
    // or false if capturing isn't possible.
    public static bool BeginCapture(out RenderTexture previous)
    {
        previous = null;
        if (!CanCapture) return false;
        try
        {
            int w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            if (_picture == null || _picture.width != w || _picture.height != h)
            {
                if (_picture != null) Object.Destroy(_picture);
                _picture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
                _picture.Create();
                _display.texture = _picture;
            }
            previous = RenderTexture.active;
            RenderTexture.active = _picture;
            GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
            return true;
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Menu picture unavailable, drawing every frame instead: {e.Message}");
            _captureFailed = true;
            if (_display != null) _display.enabled = false;
            return false;
        }
    }

    public static void EndCapture(RenderTexture previous)
    {
        RenderTexture.active = previous;
        if (_display != null && !_display.enabled) _display.enabled = true;
    }

    private static bool Ensure()
    {
        if (_root != null && _panel != null) return true;
        if (_failed) return false;
        try
        {
            _root = new GameObject("AP_MenuInputBlocker");
            Object.DontDestroyOnLoad(_root);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            _root.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(_root.transform, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0f, 1f);
            _panel.anchorMax = new Vector2(0f, 1f);
            _panel.pivot = new Vector2(0f, 1f);
            var image = panelGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            try
            {
                var displayGo = new GameObject("MenuPicture");
                displayGo.transform.SetParent(_root.transform, false);
                var rect = displayGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _display = displayGo.AddComponent<RawImage>();
                _display.raycastTarget = false;
                _display.enabled = false; // until the first picture is drawn
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"[AP] Menu picture unavailable, drawing every frame instead: {e.Message}");
                _captureFailed = true;
            }
            return true;
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Could not create the menu input blocker: {e.Message}");
            _root = null;
            _panel = null;
            _failed = true;
            _captureFailed = true;
            return false;
        }
    }
}
