using UnityEngine;
using UnityEngine.UI;

namespace APKingdom2Crown;

// IMGUI windows don't stop clicks reaching the game's own (uGUI) menus underneath, e.g. the pause menu.
// This puts an invisible, raycast-catching panel on a top-most canvas exactly behind the menu window.
internal static class UiBlocker
{
    private static GameObject _root;
    private static RectTransform _panel;
    private static bool _failed;

    public static void Cover(Rect screenRectTopLeft)
    {
        if (!Ensure()) return;
        _panel.anchoredPosition = new Vector2(screenRectTopLeft.x, -screenRectTopLeft.y);
        _panel.sizeDelta = new Vector2(screenRectTopLeft.width, screenRectTopLeft.height);
        if (!_root.activeSelf) _root.SetActive(true);
    }

    public static void Hide()
    {
        if (_root != null && _root.activeSelf) _root.SetActive(false);
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
            return true;
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Could not create the menu input blocker: {e.Message}");
            _root = null;
            _panel = null;
            _failed = true;
            return false;
        }
    }
}
