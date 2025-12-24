using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour
{
    [SerializeField] private TileKind kind;
    private SpriteRenderer sr;

    private Vector3 baseScale;
    private bool selected;

    private Coroutine moveCo;

    public TileKind Kind => kind;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    public void Init(TileKind k, Color c)
    {
        kind = k;
        sr.color = new Color(c.r, c.g, c.b, 1f);
        name = $"Tile_{kind}";
    }

    public void SetSelected(bool value)
    {
        selected = value;
        transform.localScale = selected ? baseScale * 1.12f : baseScale;
    }

    public void MoveTo(Vector3 target, float duration)
    {
        if (moveCo != null) StopCoroutine(moveCo);
        moveCo = StartCoroutine(MoveToRoutine(target, duration));
    }

    private IEnumerator MoveToRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            if (!this) yield break; // tile destroy edildiyse çık
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        if (!this) yield break;
        transform.position = target;
    }

    private void OnDisable()
    {
        if (moveCo != null) StopCoroutine(moveCo);
        moveCo = null;
    }
}



