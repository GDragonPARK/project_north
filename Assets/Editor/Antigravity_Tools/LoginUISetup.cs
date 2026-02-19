using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class LoginUISetup : EditorWindow
{
    [MenuItem("Antigravity/Setup Login UI")]
[MenuItem("Antigravity/Setup Login UI")]
    public static void SetupLoginUI()
    {
        var canvas = GameObject.Find("Canvas_Login");
        if (canvas == null)
        {
            Debug.LogError("[LoginUISetup] Canvas_Login not found! Create it first.");
            return;
        }

        // Clean existing children except we keep the canvas itself
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(canvas.transform.GetChild(i).gameObject);

        // ===== BG_Overlay =====
        var bgOverlayGO = CreateUIObject("BG_Overlay", canvas);
        var bgOverlayImg = bgOverlayGO.AddComponent<Image>();
        bgOverlayImg.color = new Color(0, 0, 0, 0.39f);
        SetStretchAll(bgOverlayGO.GetComponent<RectTransform>());

        // ===== TitleGroup =====
        var titleGroupGO = CreateUIObject("TitleGroup", canvas);
        var titleGroupRT = titleGroupGO.GetComponent<RectTransform>();
        titleGroupRT.anchorMin = new Vector2(0.5f, 1f);
        titleGroupRT.anchorMax = new Vector2(0.5f, 1f);
        titleGroupRT.pivot = new Vector2(0.5f, 1f);
        titleGroupRT.anchoredPosition = new Vector2(0, -80);
        titleGroupRT.sizeDelta = new Vector2(800, 120);
        var titleTextGO = CreateTMPText(titleGroupGO, "TitleText", "PROJECT NORTH",
            72, FontStyles.Bold, HexColor("#FFD700"), TextAlignmentOptions.Center);
        SetStretchAll(titleTextGO.GetComponent<RectTransform>());

        // ===== ConnectPanel =====
        var connectPanelGO = CreateUIObject("ConnectPanel", canvas);
        var cpImg = connectPanelGO.AddComponent<Image>();
        cpImg.color = new Color(0.1f, 0.1f, 0.1f, 0.78f);
        var cpRT = connectPanelGO.GetComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0.5f, 0f);
        cpRT.anchorMax = new Vector2(0.5f, 0f);
        cpRT.pivot = new Vector2(0.5f, 0f);
        cpRT.anchoredPosition = new Vector2(0, 60);
        cpRT.sizeDelta = new Vector2(600, 0);

        var vlg = connectPanelGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 25, 25);
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var csf = connectPanelGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Header
        var headerGO = CreateTMPText(connectPanelGO, "Header", "SERVER CONNECTION",
            24, FontStyles.Bold, HexColor("#FFA500"), TextAlignmentOptions.Center);
        var headerLE = headerGO.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 40;

        // InputArea
        var inputArea = CreateUIObject("InputArea", connectPanelGO);
        var inputHLG = inputArea.AddComponent<HorizontalLayoutGroup>();
        inputHLG.spacing = 10;
        inputHLG.childControlWidth = true;
        inputHLG.childControlHeight = true;
        inputHLG.childForceExpandWidth = true;
        inputHLG.childForceExpandHeight = false;
        var inputAreaLE = inputArea.AddComponent<LayoutElement>();
        inputAreaLE.preferredHeight = 50;

        var ipField = CreateTMPInputField(inputArea, "IP_InputField", "IP Address", "127.0.0.1");
        var ipLE = ipField.AddComponent<LayoutElement>();
        ipLE.flexibleWidth = 2;
        var portField = CreateTMPInputField(inputArea, "Port_InputField", "Port", "7777");
        var portLE = portField.AddComponent<LayoutElement>();
        portLE.flexibleWidth = 1;

        // ButtonArea
        var buttonArea = CreateUIObject("ButtonArea", connectPanelGO);
        var btnHLG = buttonArea.AddComponent<HorizontalLayoutGroup>();
        btnHLG.childAlignment = TextAnchor.MiddleCenter;
        btnHLG.childControlWidth = false;
        btnHLG.childControlHeight = true;
        btnHLG.childForceExpandWidth = false;
        btnHLG.childForceExpandHeight = false;
        var buttonAreaLE = buttonArea.AddComponent<LayoutElement>();
        buttonAreaLE.preferredHeight = 50;
        CreateButton(buttonArea, "ConnectButton", "CONNECT");

        // StatusText
        var statusGO = CreateTMPText(connectPanelGO, "StatusText", "Status: Disconnected",
            16, FontStyles.Normal, HexColor("#AAAAAA"), TextAlignmentOptions.Center);
        var statusLE = statusGO.AddComponent<LayoutElement>();
        statusLE.preferredHeight = 30;

        // LogArea
        CreateLogArea(connectPanelGO);

        // ===== Footer =====
        var footerGO = CreateUIObject("Footer", canvas);
        var footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(1f, 0f);
        footerRT.anchorMax = new Vector2(1f, 0f);
        footerRT.pivot = new Vector2(1f, 0f);
        footerRT.anchoredPosition = new Vector2(-20, 15);
        footerRT.sizeDelta = new Vector2(200, 30);
        var versionText = CreateTMPText(footerGO, "VersionText", "v0.1.0-alpha",
            12, FontStyles.Normal, HexColor("#666666"), TextAlignmentOptions.Right);
        SetStretchAll(versionText.GetComponent<RectTransform>());

        // ===== Add Controller Scripts =====
        if (canvas.GetComponent<LoginUIController>() == null)
            canvas.AddComponent<LoginUIController>();
        if (canvas.GetComponent<NetworkClientManager>() == null)
            canvas.AddComponent<NetworkClientManager>();

        WireReferences(canvas);

        EditorUtility.SetDirty(canvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[LoginUISetup] Login UI setup complete!");
    }

    static void WireReferences(GameObject canvas)
    {
        var controller = canvas.GetComponent<LoginUIController>();
        if (controller == null) return;

        var so = new SerializedObject(controller);

        var connectPanel = canvas.transform.Find("ConnectPanel");
        if (connectPanel == null) return;

        // IP InputField
        var ipField = connectPanel.Find("InputArea/IP_InputField");
        if (ipField != null)
        {
            var prop = so.FindProperty("ipInputField");
            if (prop != null) prop.objectReferenceValue = ipField.GetComponent<TMP_InputField>();
        }

        // Port InputField
        var portField = connectPanel.Find("InputArea/Port_InputField");
        if (portField != null)
        {
            var prop = so.FindProperty("portInputField");
            if (prop != null) prop.objectReferenceValue = portField.GetComponent<TMP_InputField>();
        }

        // Connect Button
        var connectBtn = connectPanel.Find("ButtonArea/ConnectButton");
        if (connectBtn != null)
        {
            var prop = so.FindProperty("connectButton");
            if (prop != null) prop.objectReferenceValue = connectBtn.GetComponent<Button>();
        }

        // Status Text
        var statusText = connectPanel.Find("StatusText");
        if (statusText != null)
        {
            var prop = so.FindProperty("statusText");
            if (prop != null) prop.objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
        }

        // Log Text
        var logArea = connectPanel.Find("LogArea");
        if (logArea != null)
        {
            var logText = logArea.transform.Find("Viewport/Content/LogText");
            if (logText != null)
            {
                var prop = so.FindProperty("logText");
                if (prop != null) prop.objectReferenceValue = logText.GetComponent<TextMeshProUGUI>();
            }

            var scrollRect = logArea.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                var prop = so.FindProperty("logScrollRect");
                if (prop != null) prop.objectReferenceValue = scrollRect;
            }
        }

        // Network Manager
        var networkManager = canvas.GetComponent<NetworkClientManager>();
        if (networkManager != null)
        {
            var prop = so.FindProperty("networkManager");
            if (prop != null) prop.objectReferenceValue = networkManager;
        }

        so.ApplyModifiedProperties();
    }

    // ===== Helper Methods =====

    static RectTransform EnsureRectTransform(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            // Need to destroy Transform first and add RectTransform
            // This is tricky in Unity - use AddComponent approach
            var parent = go.transform.parent;
            var siblingIndex = go.transform.GetSiblingIndex();
            
            // If it already has a Transform (non-Rect), we need to work with it
            // Actually, when parented under a Canvas, new GOs should auto-get RectTransform
            // Let's just try to get it or add components that force it
            if (rt == null)
            {
                // Adding a CanvasRenderer should force RectTransform
                if (go.GetComponent<CanvasRenderer>() == null)
                    go.AddComponent<CanvasRenderer>();
                rt = go.GetComponent<RectTransform>();
            }
        }
        return rt;
    }

    static void SetStretchAll(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    static GameObject CreateUIObject(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject CreateTMPText(GameObject parent, string name, string text,
        float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;

        return go;
    }

    static GameObject CreateTMPInputField(GameObject parent, string name, string placeholder, string defaultText)
    {
        // Create container
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent.transform, false);

        // Background Image
        var bgImage = go.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.12f, 1f); // #1F1F1F

        // Text Area
        var textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(go.transform, false);
        var textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(10, 5);
        textAreaRT.offsetMax = new Vector2(-10, -5);

        // Add RectMask2D for text clipping
        textArea.AddComponent<RectMask2D>();

        // Input Text
        var inputTextGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        inputTextGO.transform.SetParent(textArea.transform, false);
        var inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 18;
        inputText.color = HexColor("#FFD700");
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.richText = false;
        var inputTextRT = inputTextGO.GetComponent<RectTransform>();
        SetStretchAll(inputTextRT);

        // Placeholder Text
        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer));
        placeholderGO.transform.SetParent(textArea.transform, false);
        var placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 18;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        placeholderText.alignment = TextAlignmentOptions.Left;
        var placeholderRT = placeholderGO.GetComponent<RectTransform>();
        SetStretchAll(placeholderRT);

        // TMP InputField
        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRT;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;
        inputField.text = defaultText;
        inputField.fontAsset = inputText.font;
        inputField.pointSize = 18;

        // Navigation
        var nav = inputField.navigation;
        nav.mode = Navigation.Mode.Automatic;
        inputField.navigation = nav;

        // Caret
        inputField.caretColor = HexColor("#FFD700");
        inputField.customCaretColor = true;
        inputField.caretWidth = 2;

        // Selection Color
        inputField.selectionColor = new Color(1f, 0.84f, 0f, 0.3f);

        return go;
    }

    static GameObject CreateButton(GameObject parent, string name, string text)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260, 45);

        // Background Image
        var img = go.AddComponent<Image>();
        img.color = HexColor("#8B4513"); // SaddleBrown

        // Button Component
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = HexColor("#8B4513");
        colors.highlightedColor = HexColor("#A0522D");
        colors.pressedColor = HexColor("#6B3410");
        colors.selectedColor = HexColor("#8B4513");
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = colors;

        // Outline for border effect
        var outline = go.AddComponent<Outline>();
        outline.effectColor = HexColor("#D4A017");
        outline.effectDistance = new Vector2(2, 2);

        // Button Text
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = HexColor("#FFD700");
        tmp.alignment = TextAlignmentOptions.Center;
        var textRT = textGO.GetComponent<RectTransform>();
        SetStretchAll(textRT);

        return go;
    }

    static GameObject CreateLogArea(GameObject parent)
    {
        // LogArea container with ScrollRect
        var logArea = new GameObject("LogArea", typeof(RectTransform), typeof(CanvasRenderer));
        logArea.transform.SetParent(parent.transform, false);

        var logAreaLE = logArea.AddComponent<LayoutElement>();
        logAreaLE.preferredHeight = 150;
        logAreaLE.flexibleHeight = 0;

        var logAreaImg = logArea.AddComponent<Image>();
        logAreaImg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f); // Darker bg

        var scrollRect = logArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20;

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer));
        viewport.transform.SetParent(logArea.transform, false);
        var viewportRT = viewport.GetComponent<RectTransform>();
        SetStretchAll(viewportRT);
        viewportRT.offsetMin = new Vector2(5, 5);
        viewportRT.offsetMax = new Vector2(-5, -5);

        var viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.clear;
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect.viewport = viewportRT;

        // Content
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // LogText
        var logTextGO = new GameObject("LogText", typeof(RectTransform), typeof(CanvasRenderer));
        logTextGO.transform.SetParent(content.transform, false);
        var logText = logTextGO.AddComponent<TextMeshProUGUI>();
        logText.text = "";
        logText.fontSize = 14;
        logText.color = HexColor("#CCCCCC");
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.enableWordWrapping = true;
        logText.overflowMode = TextOverflowModes.Overflow;
        logText.richText = true;

        var logTextRT = logTextGO.GetComponent<RectTransform>();
        logTextRT.anchorMin = new Vector2(0, 1);
        logTextRT.anchorMax = new Vector2(1, 1);
        logTextRT.pivot = new Vector2(0.5f, 1);
        logTextRT.anchoredPosition = Vector2.zero;
        logTextRT.sizeDelta = new Vector2(0, 30);

        // Add LayoutElement to logText for auto sizing
        var logTextLE = logTextGO.AddComponent<LayoutElement>();
        logTextLE.preferredWidth = -1;

        // VerticalLayoutGroup for content
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return logArea;
    }
}
