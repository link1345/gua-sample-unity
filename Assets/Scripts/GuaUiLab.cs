using System;
using System.Collections;
using Gua.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GuaUiLab
{
    public static readonly Vector2 DesignSize = new(541f, 857f);
    private static readonly Color TextColor = new(0.96f, 0.97f, 1f, 1f);
    private static Font _font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Build()
    {
        if (Object.FindFirstObjectByType<GuaUiLabController>() != null)
            return;

        Debug.developerConsoleEnabled = false;
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        EnsureEventSystem();
        BuildCamera();

        var canvasObject = new GameObject(
            "GuaUiLab",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(GuaScreen),
            typeof(GuaUiLabController));
        canvasObject.GetComponent<GuaScreen>().Value = "page1";

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        var letterbox = Rect("LetterboxBackground", canvasObject.transform, Vector2.zero, Vector2.zero);
        Stretch(letterbox);
        var letterboxImage = letterbox.gameObject.AddComponent<Image>();
        letterboxImage.color = Color.black;
        letterboxImage.raycastTarget = false;

        var designRoot = Rect("DesignRoot", canvasObject.transform, Vector2.zero, DesignSize);
        designRoot.gameObject.AddComponent<GuaId>().Value = "root";
        designRoot.gameObject.AddComponent<AspectFitDesignRoot>();

        Artwork("Background", designRoot, "Art/01-background", Vector2.zero, DesignSize);
        Artwork("Title", designRoot, "Art/02-title-component-transparent", new Vector2(6f, 237f), new Vector2(433f, 153f));

        var pageOne = Rect("PageOne", designRoot, Vector2.zero, DesignSize);
        Stretch(pageOne);
        var start = ArtworkButton("StartButton", "start", pageOne, "Start", "Art/03-button-cyan-transparent", new Vector2(1.5f, -29.5f), new Vector2(431f, 129f), 58);
        var end = ArtworkButton("EndButton", "end", pageOne, "End", "Art/04-button-violet-transparent", new Vector2(0f, -199.5f), new Vector2(431f, 128f), 58);

        var pageTwo = Rect("PageTwo", designRoot, Vector2.zero, DesignSize);
        Stretch(pageTwo);
        var loading = Label("LoadingLabel", pageTwo, "Loading....", new Vector2(29f, 56.5f), new Vector2(270f, 81f), 50);
        loading.gameObject.AddComponent<GuaId>().Value = "loading";
        var back = ArtworkButton("BackButton", "back", pageTwo, "Back", "Art/04-button-violet-transparent", new Vector2(14.5f, -326f), new Vector2(431f, 128f), 58);
        pageTwo.gameObject.SetActive(false);

        var confirmation = BuildConfirmation(designRoot, out var cancel, out var confirm);
        confirmation.gameObject.SetActive(false);

        var controller = canvasObject.GetComponent<GuaUiLabController>();
        controller.Initialize(pageOne.gameObject, pageTwo.gameObject, confirmation.gameObject, loading.gameObject, back, canvasObject.GetComponent<GuaScreen>());
        start.onClick.AddListener(controller.ShowPageTwo);
        end.onClick.AddListener(controller.ShowExitConfirmation);
        back.onClick.AddListener(controller.ShowPageOne);
        cancel.onClick.AddListener(controller.ShowPageOne);
        confirm.onClick.AddListener(controller.ConfirmExit);
    }

    private static RectTransform BuildConfirmation(Transform designRoot, out Button cancel, out Button confirm)
    {
        var overlay = Rect("ExitConfirmation", designRoot, Vector2.zero, DesignSize);
        Stretch(overlay);

        var dimmer = Rect("Dimmer", overlay, Vector2.zero, Vector2.zero);
        Stretch(dimmer);
        var dimmerImage = dimmer.gameObject.AddComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.72f);

        var panel = Rect("DialogPanel", overlay, new Vector2(0f, 0.5f), new Vector2(455f, 274f));
        var panelGraphic = panel.gameObject.AddComponent<RoundedPanelGraphic>();
        panelGraphic.FillColor = new Color(0.015f, 0.035f, 0.11f, 0.98f);
        panelGraphic.BorderColor = new Color(0.08f, 0.78f, 1f, 1f);
        panelGraphic.BorderWidth = 3f;
        panelGraphic.Radius = 14f;
        panelGraphic.raycastTarget = false;

        var question = Label("Question", panel, "Exit the game?", new Vector2(0f, 60f), new Vector2(407f, 86f), 34);
        question.gameObject.AddComponent<GuaId>().Value = "exit_question";

        cancel = ArtworkButton("CancelButton", "cancel_exit", panel, "Cancel", "Art/04-button-violet-transparent", new Vector2(-107f, -50f), new Vector2(195f, 58f), 25);
        confirm = ArtworkButton("ConfirmButton", "confirm_exit", panel, "OK", "Art/03-button-cyan-transparent", new Vector2(107f, -50f), new Vector2(195f, 58f), 25);
        return overlay;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var events = new GameObject("EventSystem");
        events.AddComponent<EventSystem>();
        events.AddComponent<StandaloneInputModule>();
    }

    private static void BuildCamera()
    {
        var cameraObject = new GameObject("BackgroundCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.depth = -100;
    }

    private static Button ArtworkButton(string name, string id, Transform parent, string text, string resource, Vector2 position, Vector2 size, int fontSize)
    {
        var rect = Rect(name, parent, position, size);
        rect.gameObject.AddComponent<GuaId>().Value = id;
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = LoadSprite(resource);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.88f, 0.93f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.58f, 0.58f, 0.66f, 0.75f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var label = Label("Label", rect, text, Vector2.zero, size, fontSize);
        Stretch(label.rectTransform);
        label.raycastTarget = false;
        return button;
    }

    private static Image Artwork(string name, Transform parent, string resource, Vector2 position, Vector2 size)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = LoadSprite(resource);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadSprite(string resource)
    {
        var texture = Resources.Load<Texture2D>(resource);
        if (texture == null)
            throw new InvalidOperationException($"Missing UI texture in Resources: {resource}");
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
    {
        var rect = Rect(name, parent, position, size);
        var label = rect.gameObject.AddComponent<Text>();
        label.text = value;
        label.font = _font;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = TextColor;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        var rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public sealed class AspectFitDesignRoot : MonoBehaviour
{
    private int _width;
    private int _height;

    private void OnEnable() => Apply();

    private void Update()
    {
        if (_width != Screen.width || _height != Screen.height)
            Apply();
    }

    private void Apply()
    {
        _width = Screen.width;
        _height = Screen.height;
        var scale = Mathf.Min(_width / GuaUiLab.DesignSize.x, _height / GuaUiLab.DesignSize.y);
        transform.localScale = Vector3.one * scale;
        ((RectTransform)transform).anchoredPosition = Vector2.zero;
    }
}

public sealed class GuaUiLabController : MonoBehaviour
{
    private const float LoadingDurationSeconds = 6f;
    private GameObject _pageOne;
    private GameObject _pageTwo;
    private GameObject _confirmation;
    private GameObject _loading;
    private Button _back;
    private GuaScreen _screen;
    private Coroutine _loadingRoutine;

    public void Initialize(GameObject pageOne, GameObject pageTwo, GameObject confirmation, GameObject loading, Button back, GuaScreen screen)
    {
        _pageOne = pageOne;
        _pageTwo = pageTwo;
        _confirmation = confirmation;
        _loading = loading;
        _back = back;
        _screen = screen;
        ShowPageOne();
    }

    public void ShowPageOne()
    {
        CancelLoading();
        _screen.Value = "page1";
        _pageOne.SetActive(true);
        _pageTwo.SetActive(false);
        _confirmation.SetActive(false);
        Debug.Log("Returned to page1");
    }

    public void ShowPageTwo()
    {
        CancelLoading();
        _screen.Value = "page2";
        _pageOne.SetActive(false);
        _pageTwo.SetActive(true);
        _confirmation.SetActive(false);
        _loading.SetActive(true);
        _back.interactable = false;
        Debug.Log("Start pressed: opening page2");
        _loadingRoutine = StartCoroutine(FinishLoading());
    }

    public void ShowExitConfirmation()
    {
        _screen.Value = "exit_confirmation";
        _confirmation.SetActive(true);
        Debug.Log("End pressed: opening confirmation");
    }

    public void ConfirmExit()
    {
        Debug.Log("Exit confirmed: closing game");
        Application.Quit();
    }

    private IEnumerator FinishLoading()
    {
        yield return new WaitForSecondsRealtime(LoadingDurationSeconds);
        _loading.SetActive(false);
        _back.interactable = true;
        _loadingRoutine = null;
        Debug.Log("Loading finished: Back enabled");
    }

    private void CancelLoading()
    {
        if (_loadingRoutine == null)
            return;
        StopCoroutine(_loadingRoutine);
        _loadingRoutine = null;
    }
}

public sealed class RoundedPanelGraphic : MaskableGraphic
{
    public Color FillColor = Color.black;
    public Color BorderColor = Color.cyan;
    public float BorderWidth = 3f;
    public float Radius = 14f;

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        var rect = rectTransform.rect;
        const int cornerSegments = 6;
        var count = cornerSegments * 4;
        var innerRadius = Mathf.Max(0f, Radius - BorderWidth);

        for (var i = 0; i < count; i++)
        {
            var corner = i / cornerSegments;
            var step = i % cornerSegments;
            var angle = (-90f + corner * 90f + step * (90f / (cornerSegments - 1))) * Mathf.Deg2Rad;
            var center = corner switch
            {
                0 => new Vector2(rect.xMax - Radius, rect.yMin + Radius),
                1 => new Vector2(rect.xMax - Radius, rect.yMax - Radius),
                2 => new Vector2(rect.xMin + Radius, rect.yMax - Radius),
                _ => new Vector2(rect.xMin + Radius, rect.yMin + Radius),
            };
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            helper.AddVert(center + direction * Radius, BorderColor, Vector2.zero);
            helper.AddVert(center + direction * innerRadius, FillColor, Vector2.zero);
        }

        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            helper.AddTriangle(i * 2, next * 2, i * 2 + 1);
            helper.AddTriangle(next * 2, next * 2 + 1, i * 2 + 1);
        }

        var centerIndex = count * 2;
        helper.AddVert(rect.center, FillColor, Vector2.zero);
        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            helper.AddTriangle(centerIndex, i * 2 + 1, next * 2 + 1);
        }
    }
}
