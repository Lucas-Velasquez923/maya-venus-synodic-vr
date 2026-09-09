using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR-compatible birthday scroll wheel input using custom PNG images.
///
/// Uses your custom background panel image and confirm button images.
/// The three dark boxes in your background are used as tap zones for
/// scrolling month, day, and year values.
///
/// SETUP:
///   1. Create a World Space canvas (Render Mode = World Space)
///   2. Add Tracked Device Graphic Raycaster (NOT Graphic Raycaster)
///   3. Add this script to the canvas (or a child)
///   4. Assign your 3 PNG sprites in the Inspector:
///      - backgroundSprite: the "ENTER YOUR BIRTH DATE" panel
///      - confirmNormalSprite: the Confirm button (default)
///      - confirmPressedSprite: the Confirm button (pressed/hover)
///   5. Make sure EventSystem has XR UI Input Module
///   6. BirthdaySceneManager will wire up the OnBirthdaySubmitted callback
///
/// INTERACTION:
///   Player points ray at the top/bottom half of each dark box to scroll
///   values up or down. Center of each box shows the current value.
///   Confirm button sends the birthday to BirthdaySceneManager.
/// </summary>
public class BirthdayScrollInput : MonoBehaviour
{
    [Header("Custom PNG Sprites")]
    [Tooltip("The background panel image (ENTER YOUR BIRTH DATE)")]
    public Sprite backgroundSprite;

    [Tooltip("The Confirm button image (normal state)")]
    public Sprite confirmNormalSprite;

    [Tooltip("The Confirm button image (pressed/hover state)")]
    public Sprite confirmPressedSprite;

    [Header("Panel Size")]
    [Tooltip("Width of the background panel in UI units")]
    public float panelWidth = 700f;

    [Tooltip("Height of the background panel in UI units")]
    public float panelHeight = 400f;

    [Tooltip("Width of the confirm button in UI units")]
    public float confirmWidth = 250f;

    [Tooltip("Height of the confirm button in UI units")]
    public float confirmHeight = 70f;

    [Tooltip("Gap between the bottom of the panel and the confirm button")]
    public float confirmGap = 15f;

    [Header("Input Box Layout")]
    [Tooltip("Width of each dark input box area")]
    public float boxWidth = 150f;

    [Tooltip("Height of each dark input box area")]
    public float boxHeight = 120f;

    [Tooltip("Horizontal spacing between box centers")]
    public float boxSpacing = 200f;

    [Tooltip("Vertical offset of box centers from panel center (negative = lower)")]
    public float boxYOffset = -45f;

    [Header("Text Style")]
    [Tooltip("Font size for the main value display")]
    public int mainFontSize = 52;

    [Tooltip("Font size for arrow symbols")]
    public int arrowFontSize = 28;

    [Tooltip("Main text color")]
    public Color textColor = Color.white;

    [Tooltip("Arrow color")]
    public Color arrowColor = new Color(1f, 1f, 1f, 0.6f);

    // ── Callback ─────────────────────────────────────────────────────────
    /// <summary>Set by BirthdaySceneManager before this canvas is shown.</summary>
    [HideInInspector]
    public Action<int, int, int> OnBirthdaySubmitted;

    // ── State ────────────────────────────────────────────────────────────
    int _month = 1;
    int _day = 1;
    int _year = 2000;

    const int MIN_YEAR = 1900;
    const int MAX_YEAR = 2026;

    // UI references (created at runtime)
    TextMeshProUGUI _monthMain;
    TextMeshProUGUI _dayMain;
    TextMeshProUGUI _yearMain;

    // ── Lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        BuildUI();
        UpdateAllDisplays();
    }

    // ── UI Construction ──────────────────────────────────────────────────

    void BuildUI()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        // ── Background panel (your custom PNG) ──────────────────────────
        GameObject panelGO = new GameObject("BackgroundPanel");
        panelGO.transform.SetParent(root, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.sprite = backgroundSprite;
        panelImg.type = Image.Type.Sliced;
        panelImg.preserveAspect = false;
        panelImg.raycastTarget = false;

        // ── Three scroll columns over the dark boxes ────────────────────
        // Columns are centered on the panel, spaced by boxSpacing
        float col1X = -boxSpacing;  // Month (left box)
        float col2X = 0f;           // Day (center box)
        float col3X = boxSpacing;   // Year (right box)

        BuildColumn(panelRT, "Month", col1X, boxYOffset,
            ref _monthMain, () => ChangeMonth(1), () => ChangeMonth(-1));

        BuildColumn(panelRT, "Day", col2X, boxYOffset,
            ref _dayMain, () => ChangeDay(1), () => ChangeDay(-1));

        BuildColumn(panelRT, "Year", col3X, boxYOffset,
            ref _yearMain, () => ChangeYear(1), () => ChangeYear(-1));

        // ── Confirm button (your custom PNG) ────────────────────────────
        GameObject btnGO = new GameObject("ConfirmButton");
        btnGO.transform.SetParent(root, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 1f);
        // Position just below the panel
        float btnY = -(panelHeight / 2f) - confirmGap;
        btnRT.anchoredPosition = new Vector2(0f, btnY);
        btnRT.sizeDelta = new Vector2(confirmWidth, confirmHeight);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = confirmNormalSprite;
        btnImg.type = Image.Type.Sliced;
        btnImg.preserveAspect = false;
        btnImg.raycastTarget = true;

        Button btn = btnGO.AddComponent<Button>();
        // Use SpriteSwap transition so your two PNGs are used
        btn.transition = Selectable.Transition.SpriteSwap;
        SpriteState spriteState = new SpriteState();
        spriteState.highlightedSprite = confirmPressedSprite;
        spriteState.pressedSprite = confirmPressedSprite;
        spriteState.selectedSprite = confirmNormalSprite;
        btn.spriteState = spriteState;
        btn.onClick.AddListener(OnSubmitClicked);
    }

    void BuildColumn(RectTransform parent, string label, float xPos, float yPos,
        ref TextMeshProUGUI mainTMP,
        UnityEngine.Events.UnityAction onUp, UnityEngine.Events.UnityAction onDown)
    {
        // Column container — centered over the dark box
        GameObject col = new GameObject(label + "Column");
        col.transform.SetParent(parent, false);
        RectTransform colRT = col.AddComponent<RectTransform>();
        colRT.anchorMin = new Vector2(0.5f, 0.5f);
        colRT.anchorMax = new Vector2(0.5f, 0.5f);
        colRT.pivot = new Vector2(0.5f, 0.5f);
        colRT.anchoredPosition = new Vector2(xPos, yPos);
        colRT.sizeDelta = new Vector2(boxWidth, boxHeight);

        // Up arrow tap zone (top region)
        GameObject upGO = new GameObject(label + "Up");
        upGO.transform.SetParent(colRT, false);
        RectTransform upRT = upGO.AddComponent<RectTransform>();
        upRT.anchorMin = new Vector2(0f, 0.75f);
        upRT.anchorMax = new Vector2(1f, 1.05f);
        upRT.offsetMin = Vector2.zero;
        upRT.offsetMax = Vector2.zero;

        Image upImg = upGO.AddComponent<Image>();
        upImg.color = new Color(0f, 0f, 0f, 0f); // invisible but raycastable
        upImg.raycastTarget = true;

        Button upBtn = upGO.AddComponent<Button>();
        upBtn.transition = Selectable.Transition.None;
        upBtn.onClick.AddListener(onUp);

        // Up arrow text
        CreateArrowText(upGO.transform, label + "UpArrow", "\u25B2");

        // Main value — centered in the box
        GameObject mainGO = CreateTextElement(colRT, label + "Main", "", textColor, mainFontSize,
            new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));
        mainTMP = mainGO.GetComponent<TextMeshProUGUI>();

        // Down arrow tap zone (bottom region)
        GameObject downGO = new GameObject(label + "Down");
        downGO.transform.SetParent(colRT, false);
        RectTransform downRT = downGO.AddComponent<RectTransform>();
        downRT.anchorMin = new Vector2(0f, -0.05f);
        downRT.anchorMax = new Vector2(1f, 0.25f);
        downRT.offsetMin = Vector2.zero;
        downRT.offsetMax = Vector2.zero;

        Image downImg = downGO.AddComponent<Image>();
        downImg.color = new Color(0f, 0f, 0f, 0f);
        downImg.raycastTarget = true;

        Button downBtn = downGO.AddComponent<Button>();
        downBtn.transition = Selectable.Transition.None;
        downBtn.onClick.AddListener(onDown);

        // Down arrow text
        CreateArrowText(downGO.transform, label + "DownArrow", "\u25BC");
    }

    void CreateArrowText(Transform parent, string name, string symbol)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = symbol;
        tmp.color = arrowColor;
        tmp.fontSize = arrowFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    GameObject CreateTextElement(RectTransform parent, string name, string text, Color color, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        return go;
    }

    // ── Value Changes ────────────────────────────────────────────────────

    void ChangeMonth(int dir)
    {
        _month += dir;
        if (_month > 12) _month = 1;
        if (_month < 1) _month = 12;
        ClampDay();
        UpdateAllDisplays();
    }

    void ChangeDay(int dir)
    {
        _day += dir;
        int maxDay = DaysInMonth(_month, _year);
        if (_day > maxDay) _day = 1;
        if (_day < 1) _day = maxDay;
        UpdateAllDisplays();
    }

    void ChangeYear(int dir)
    {
        _year += dir;
        if (_year > MAX_YEAR) _year = MIN_YEAR;
        if (_year < MIN_YEAR) _year = MAX_YEAR;
        ClampDay();
        UpdateAllDisplays();
    }

    void ClampDay()
    {
        int maxDay = DaysInMonth(_month, _year);
        if (_day > maxDay) _day = maxDay;
    }

    int DaysInMonth(int month, int year)
    {
        return DateTime.DaysInMonth(year, month);
    }

    // ── Display Updates ──────────────────────────────────────────────────

    void UpdateAllDisplays()
    {
        if (_monthMain) _monthMain.text = MonthName(_month);
        if (_dayMain) _dayMain.text = _day.ToString();
        if (_yearMain) _yearMain.text = _year.ToString();
    }

    string MonthName(int m)
    {
        return new DateTime(2000, m, 1).ToString("MMM");
    }

    // ── Submit ───────────────────────────────────────────────────────────

    void OnSubmitClicked()
    {
        try
        {
            var _ = new DateTime(_year, _month, _day);
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning($"[BirthdayInput] Invalid date: {_month}/{_day}/{_year} — clamping day.");
            _day = DaysInMonth(_month, _year);
            UpdateAllDisplays();
            return;
        }

        Debug.Log($"[BirthdayInput] Submitted: {_month}/{_day}/{_year}");

        if (OnBirthdaySubmitted != null)
            OnBirthdaySubmitted.Invoke(_year, _month, _day);
        else
            Debug.LogWarning("[BirthdayInput] No callback wired — BirthdaySceneManager not connected.");
    }
}
