using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HighlightManager : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        [Tooltip("Elemen yang disorot. Ini satu-satunya yang wajib diisi.")]
        public RectTransform target;

        [Tooltip("Opsional: kalau elemennya dibikin script (misal kartu), isi wadahnya di sini dan Child Index-nya.")]
        public RectTransform container;
        public int childIndex;

        [Tooltip("Opsional: Button yang ditunggu kliknya, kalau beda dari Button di target.")]
        public Button clickButton;

        [Tooltip("Opsional, cuma buat step hasil: yang disorot kalau GAGAL (Output). Target = Status, disorot kalau berhasil.")]
        public RectTransform errorTarget;
    }

    [Header("Refs")]
    [SerializeField] CanvasGroup dimmer;

    [Header("Setting")]
    [SerializeField] float dimAlpha = 0.7f;
    [SerializeField] float fadeDuration = 0.25f;
    [SerializeField] float moveDuration = 0.3f;
    [SerializeField] float padding = 12f;

    [Header("Tutorial")]
    [SerializeField] Step[] steps;
    [SerializeField] bool showOnStart = true;
    [SerializeField] float showDelay = 2f;
    [SerializeField] float afterClickDelay = 2f;  // jeda setelah user klik
    [SerializeField] float holdSeconds = 4f;      // lama nyorot kalau step nggak punya Button (misal "baca pesan")
    [SerializeField] float resultWait = 1f;       // tunggu hasil run keluar dulu
    [SerializeField] string successKeyword = "berhasil"; // kata di Status kalau sukses
    public UnityEvent onFinished;

    RectTransform dimmerRT;
    RectTransform top, bottom, left, right;
    Rect currentHole;
    Coroutine fade, move, delayRoutine;
    bool visible;

    void Awake()
    {
        dimmer.gameObject.SetActive(true);
        dimmerRT = (RectTransform)dimmer.transform;

        // Image hitam bawaan Dimmer dimatiin, diganti 4 strip
        var own = dimmer.GetComponent<Image>();
        if (own != null) own.enabled = false;

        top = MakeStrip("Top");
        bottom = MakeStrip("Bottom");
        left = MakeStrip("Left");
        right = MakeStrip("Right");

        SetAlpha(0f);
    }

    void Start()
    {
        if (showOnStart && steps != null && steps.Length > 0)
            delayRoutine = StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        yield return new WaitForSecondsRealtime(showDelay);

        for (int i = 0; i < steps.Length; i++)
        {
            Step s = steps[i];

            if (s.target == null && s.container == null)
            {
                Debug.LogWarning("HighlightManager: step " + i + " belum diisi Target, dilewati.");
                continue;
            }

            RectTransform t = null;
            yield return ResolveTarget(s, r => t = r);

            // step hasil: Target = Status (berhasil), Error Target = Output (gagal)
            if (s.errorTarget != null)
            {
                yield return RunResultStep(s, t);
                continue;
            }

            Show(t);
            yield return WaitForClick(s, t);
            yield return new WaitForSecondsRealtime(afterClickDelay);
        }

        delayRoutine = null;
        Clear();
        onFinished?.Invoke();
    }

    // Tunggu target aktif (window-nya kebuka), atau anak wadah udah kebikin
    IEnumerator ResolveTarget(Step s, Action<RectTransform> done)
    {
        if (s.container != null)
        {
            while (!s.container.gameObject.activeInHierarchy || s.container.childCount <= s.childIndex)
                yield return null;

            yield return null; // 1 frame biar layout selesai
            LayoutRebuilder.ForceRebuildLayoutImmediate(s.container);
            done((RectTransform)s.container.GetChild(s.childIndex));
        }
        else
        {
            while (!s.target.gameObject.activeInHierarchy)
                yield return null;

            yield return null;
            done(s.target);
        }
    }

    IEnumerator RunResultStep(Step s, RectTransform status)
    {
        yield return new WaitForSecondsRealtime(resultWait);

        // teks Status dibaca langsung dari objek yang di-drag ke Target
        var txt = status.GetComponentInChildren<TMP_Text>();
        bool ok = txt != null &&
                  txt.text.IndexOf(successKeyword, StringComparison.OrdinalIgnoreCase) >= 0;

        RectTransform chosen = ok ? status : s.errorTarget;
        while (!chosen.gameObject.activeInHierarchy) yield return null;

        Show(chosen);
        yield return new WaitForSecondsRealtime(holdSeconds);
    }

    // Nunggu user klik Button. Kalau nggak ada Button, sorot beberapa detik aja
    IEnumerator WaitForClick(Step s, RectTransform target)
    {
        Button btn = s.clickButton;
        if (btn == null) btn = target.GetComponent<Button>();
        // elemen hasil bikinan script (kartu): cari Button di dalamnya
        if (btn == null && s.container != null) btn = target.GetComponentInChildren<Button>();

        if (btn == null)
        {
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield break;
        }

        bool clicked = false;
        UnityAction onClick = () => clicked = true;
        btn.onClick.AddListener(onClick);

        try
        {
            while (!clicked) yield return null;
        }
        finally
        {
            // dilepas walau coroutine dibatalin, dan aman kalau button-nya udah ke-destroy
            if (btn != null) btn.onClick.RemoveListener(onClick);
        }
    }

    public void Show(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();
        Rect hole = GetHole(target);

        if (!visible)
        {
            currentHole = hole;
            ApplyHole(hole);
        }
        else
        {
            if (move != null) StopCoroutine(move);
            move = StartCoroutine(MoveRoutine(hole));
        }

        visible = true;
        FadeTo(dimAlpha);
    }

    public void Clear()
    {
        // batalin kalau masih nunggu
        if (delayRoutine != null)
        {
            StopCoroutine(delayRoutine);
            delayRoutine = null;
        }

        visible = false;
        FadeTo(0f);
    }

    RectTransform MakeStrip(string stripName)
    {
        var go = new GameObject(stripName, typeof(RectTransform), typeof(Image));
        go.layer = dimmer.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(dimmerRT, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true; // strip ini yang nahan klik, lubangnya tembus
        return rt;
    }

    // Hitung lubang dalam koordinat lokal Dimmer, dari pojok kiri-bawah
    Rect GetHole(RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (var c in corners)
        {
            Vector2 p = dimmerRT.InverseTransformPoint(c);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        Rect r = dimmerRT.rect;
        min -= r.min;
        max -= r.min;

        min -= Vector2.one * padding;
        max += Vector2.one * padding;

        min = Vector2.Max(min, Vector2.zero);
        max = Vector2.Min(max, r.size);

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    void ApplyHole(Rect h)
    {
        float w = dimmerRT.rect.width;
        float ht = dimmerRT.rect.height;

        Place(bottom, 0f, 0f, w, h.yMin);
        Place(top, 0f, h.yMax, w, ht - h.yMax);
        Place(left, 0f, h.yMin, h.xMin, h.height);
        Place(right, h.xMax, h.yMin, w - h.xMax, h.height);
    }

    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
    }

    IEnumerator MoveRoutine(Rect to)
    {
        Rect from = currentHole;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / moveDuration;
            float e = Mathf.SmoothStep(0f, 1f, t);
            currentHole = Rect.MinMaxRect(
                Mathf.Lerp(from.xMin, to.xMin, e),
                Mathf.Lerp(from.yMin, to.yMin, e),
                Mathf.Lerp(from.xMax, to.xMax, e),
                Mathf.Lerp(from.yMax, to.yMax, e));
            ApplyHole(currentHole);
            yield return null;
        }
        currentHole = to;
        ApplyHole(to);
    }

    void FadeTo(float target)
    {
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        float start = dimmer.alpha;
        float t = 0f;
        dimmer.blocksRaycasts = true;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeDuration;
            dimmer.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }
        SetAlpha(target);
    }

    void SetAlpha(float a)
    {
        dimmer.alpha = a;
        dimmer.blocksRaycasts = a > 0.01f;
    }
}