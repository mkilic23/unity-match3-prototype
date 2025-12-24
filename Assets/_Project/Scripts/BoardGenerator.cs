using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class BoardGenerator : MonoBehaviour
{
    [Header("Board Size")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float swapDuration = 0.12f;
    [SerializeField] private float fallDuration = 0.10f;
    [SerializeField] private TMP_Text scoreText;
    private int score;


    [Header("References")]
    [SerializeField] private Tile tilePrefab;

    [Header("Kind Colors (must be 6)")]
    [SerializeField] private Color[] kindColors = new Color[6];

    [Header("Layout")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 origin = Vector2.zero;

    [Header("Timings")]
    [SerializeField] private float stepDelay = 0.06f;

    private Tile[,] grid;
    private Tile selected;
    private bool busy;
    private bool scoringEnabled;


    private void Start()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile Prefab is not assigned! Assign it in Inspector.");
            return;
        }

        if (kindColors == null || kindColors.Length != 6)
        {
            Debug.LogError("Kind Colors array must have exactly 6 elements.");
            return;
        }

        Generate();
        StartCoroutine(ResolveBoard(initialClean: true));
        UpdateScoreUI();

    }
    private void UpdateScoreUI()
{
    Debug.Log($"UpdateScoreUI called. score={score}, scoreText={(scoreText ? scoreText.name : "NULL")}");
    if (scoreText != null)
        scoreText.text = $"Score: {score}";
}



    private void Update()
    {
        if (busy) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 world2 = new Vector2(world.x, world.y);

        RaycastHit2D hit = Physics2D.Raycast(world2, Vector2.zero);
        if (hit.collider == null) return;

        Tile t = hit.collider.GetComponent<Tile>();
        if (t == null) return;

        OnTileClicked(t);
    }

    private void OnTileClicked(Tile t)
    {
        if (selected == null)
        {
            selected = t;
            selected.SetSelected(true);
            return;
        }

        if (selected == t)
        {
            selected.SetSelected(false);
            selected = null;
            return;
        }

        if (!TryFindTilePos(selected, out Vector2Int a) || !TryFindTilePos(t, out Vector2Int b))
{
    RebuildGridFromScene();

    if (!TryFindTilePos(selected, out a) || !TryFindTilePos(t, out b))
    {
        Debug.LogWarning("Clicked tile position could not be resolved in grid.");
        selected.SetSelected(false);
        selected = null;
        return;
    }
}

        int manhattan = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        if (manhattan == 1)
        {
            // swap dene
            SwapTiles(a, b);

            // match var mı?
            var matches = FindAllMatches();
            if (matches.Count > 0)
            {
                // match varsa çöz
                selected.SetSelected(false);
                selected = null;
                StartCoroutine(ResolveBoard());
            }
            else
            {
                // yoksa swap geri
                SwapTiles(a, b);
                selected.SetSelected(false);
                selected = null;
            }
        }
        else
        {
            // komşu değilse seçimi değiştir
            selected.SetSelected(false);
            selected = t;
            selected.SetSelected(true);
        }
    }

    private void Generate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        grid = new Tile[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SpawnAt(x, y);
            }
        }
    }

    private void SpawnAt(int x, int y)
    {
        int k = Random.Range(0, 6);
        var kind = (TileKind)k;

        Vector3 target = ToWorldPos(x, y);
        Vector3 spawn = target + Vector3.up * (cellSize * 2f); // yukarıdan gelsin

        Tile t = Instantiate(tilePrefab, spawn, Quaternion.identity, transform);
        t.Init(kind, kindColors[k]);

        grid[x, y] = t;

        t.MoveTo(target, fallDuration);
    }


    private Vector3 ToWorldPos(int x, int y)
    {
        return new Vector3(origin.x + x * cellSize, origin.y + y * cellSize, 0f);
    }

    private Vector2Int GetGridPos(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - origin.y) / cellSize);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return new Vector2Int(x, y);
    }

    private void SwapTiles(Vector2Int a, Vector2Int b)
{
    Tile ta = grid[a.x, a.y];
    Tile tb = grid[b.x, b.y];

    if (ta == null || tb == null)
    {
        Debug.LogWarning($"SwapTiles aborted: ta={ta} tb={tb} at a={a} b={b}. Rebuilding grid.");
        RebuildGridFromScene();
        ta = grid[a.x, a.y];
        tb = grid[b.x, b.y];
        if (ta == null || tb == null) return;
    }

    // grid swap
    grid[a.x, a.y] = tb;
    grid[b.x, b.y] = ta;

    Vector3 posA = ToWorldPos(a.x, a.y);
    Vector3 posB = ToWorldPos(b.x, b.y);

    // animasyon
    ta.MoveTo(posB, swapDuration);
    tb.MoveTo(posA, swapDuration);

}


    // ---------------- MATCH / CLEAR / COLLAPSE / REFILL ----------------

    private IEnumerator ResolveBoard(bool initialClean = false)

    {
        busy = true;
        scoringEnabled = !initialClean;

        while (true)
        {
            HashSet<Vector2Int> matches = FindAllMatches();
            if (matches.Count == 0) break;

            yield return new WaitForSeconds(stepDelay);

            ClearMatches(matches);

            yield return new WaitForSeconds(stepDelay);

            CollapseColumns();

            yield return new WaitForSeconds(stepDelay);

            RefillBoard();

            yield return new WaitForSeconds(stepDelay);
        }
                if (initialClean)
        {
            score = 0;
            UpdateScoreUI();
            scoringEnabled = true;
        }

        busy = false;
    }

    private HashSet<Vector2Int> FindAllMatches()
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();

        // yatay tarama
        for (int y = 0; y < height; y++)
        {
            int runStart = 0;
            while (runStart < width)
            {
                if (grid[runStart, y] == null) { runStart++; continue; }

                TileKind kind = grid[runStart, y].Kind;
                int runEnd = runStart + 1;

                while (runEnd < width && grid[runEnd, y] != null && grid[runEnd, y].Kind == kind)
                    runEnd++;

                int runLen = runEnd - runStart;
                if (runLen >= 3)
                {
                    for (int x = runStart; x < runEnd; x++)
                        result.Add(new Vector2Int(x, y));
                }

                runStart = runEnd;
            }
        }

        // dikey tarama
        for (int x = 0; x < width; x++)
        {
            int runStart = 0;
            while (runStart < height)
            {
                if (grid[x, runStart] == null) { runStart++; continue; }

                TileKind kind = grid[x, runStart].Kind;
                int runEnd = runStart + 1;

                while (runEnd < height && grid[x, runEnd] != null && grid[x, runEnd].Kind == kind)
                    runEnd++;

                int runLen = runEnd - runStart;
                if (runLen >= 3)
                {
                    for (int y = runStart; y < runEnd; y++)
                        result.Add(new Vector2Int(x, y));
                }

                runStart = runEnd;
            }
        }

        return result;
    }

private void ClearMatches(HashSet<Vector2Int> matches)
{
    foreach (var p in matches)
    {
        Tile t = grid[p.x, p.y];
        if (t != null)
        {
            Destroy(t.gameObject);
            grid[p.x, p.y] = null;
        if (scoringEnabled){
         score += 10;        }
    }
    UpdateScoreUI();
}

}
    private void CollapseColumns()
    {
        // y=0 en alt kabulüyle aşağı doğru “sıkıştır”
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null) continue;

                if (writeY != y)
                {
                    grid[x, writeY] = grid[x, y];
                    grid[x, y] = null;

                grid[x, writeY].MoveTo(ToWorldPos(x, writeY), fallDuration);
                }

                writeY++;
            }
        }
    }

    private void RefillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                    SpawnAt(x, y);
            }
        }
    }
    private bool TryFindTilePos(Tile tile, out Vector2Int pos)
{
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            if (grid[x, y] == tile)
            {
                pos = new Vector2Int(x, y);
                return true;
            }
        }
    }

    pos = default;
    return false;
}

private void RebuildGridFromScene()
{
    grid = new Tile[width, height];

    foreach (Transform child in transform)
    {
        Tile t = child.GetComponent<Tile>();
        if (t == null) continue;

        Vector2Int p = GetGridPos(t.transform.position);

        // Çakışma olursa logla (debug için)
        if (grid[p.x, p.y] != null && grid[p.x, p.y] != t)
            Debug.LogWarning($"Grid collision at {p} between {grid[p.x, p.y].name} and {t.name}");

        grid[p.x, p.y] = t;
        t.transform.position = ToWorldPos(p.x, p.y); // hizalama
    }
}

}



