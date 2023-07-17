using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FrostWind.Utils;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridManager : SingletonMonoBehaviour<GridManager>
{
    [SerializeField, Min(0)] private int width = 10;
    [SerializeField, Min(0)] private int height = 10;

    [SerializeField] private GameObject highlightPrefab;

    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite yellowSprite;
    [SerializeField] private Sprite purpleSprite;
    [SerializeField] private Sprite bombSprite;
    [SerializeField] private Sprite spannerSprite;
    [SerializeField] private Sprite timerSprite;

    [Header("Sound")] [SerializeField] private bool soundEnabled = true;
    [SerializeField] private GameObject muteIcon;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource bossMusicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip swapSound;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip spannerSound;
    [SerializeField] private AudioClip timerSound;
    [SerializeField] private float volume = 0.6f;


    private Grid _grid;
    private int _combo = 0;
    private List<CellAdapter> _adapters = new();
    private Queue<GridPos> _powersToConsume = new();

    [Header("Debug")] [SerializeField] private bool debugMode;

    protected override void Awake()
    {
        base.Awake();
        InitGrid();
    }

    private void Update()
    {
        if (ProgressBar.Instance.IsGameOver) return;
        _adapters.ForEach(a => a.Step());
        if (_adapters.Any(a => a.IsBusy)) return;

        if (_grid.FallGems(ProgressBar.Instance.IsOverheating)) return;

        while (_powersToConsume.TryDequeue(out var pos))
        {
            if (_grid[pos].Color == GemColor.Spanner)
            {
                sfxSource.PlayOneShot(spannerSound);
            }
            else if(_grid[pos].Color == GemColor.Timer)
            {
                sfxSource.PlayOneShot(timerSound);
            }
            _grid[pos].Empty();
        }

        var destroyed = _grid.DestoryGems(_upgradePositions);
        _upgradePositions.Clear();
        if (destroyed > 0)
        {
            sfxSource.PlayOneShot(destroySound);
            _combo++;
            ProgressBar.Instance.AddScore(destroyed, _combo);
            return;
        }
    }

    private void InitGrid()
    {
        _grid = new Grid(width, height);

        foreach (var cell in _grid)
        {
            var go = new GameObject($"Cell {cell.Position}");
            go.transform.parent = transform;
            var adapter = go.AddComponent<CellAdapter>();
            adapter.BlueSprite    = blueSprite;
            adapter.RedSprite     = redSprite;
            adapter.GreenSprite   = greenSprite;
            adapter.YellowSprite  = yellowSprite;
            adapter.PurpleSprite  = purpleSprite;
            adapter.BombSprite    = bombSprite;
            adapter.SpannerSprite = spannerSprite;
            adapter.TimerSprite   = timerSprite;
            adapter.SetCell(cell);
            _adapters.Add(adapter);
            go.transform.position = GridToWorld(cell.Position);
            var hl = Instantiate(highlightPrefab, go.transform);
            hl.SetActive(false);
            adapter.Highlight = hl;
        }
    }

    public GridPos WorldToGrid(Vector3 worldPos) => Vector3Int.FloorToInt((worldPos - transform.position));
    public Vector2 GridToWorld(GridPos gridPos) => transform.position + (Vector3)gridPos;

#if UNITY_EDITOR
    private void OnValidate()
    {
        _grid = new Grid(width, height);
    }

    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        var size = new Vector3(1.0f * .9f, 1.0f * .9f, 0f);
        foreach (var cell in _grid)
        {
            Handles.color = Color.white;
            Handles.DrawWireCube(GridToWorld(cell.Position) + new Vector2(0.5F, 0.5F), size);
        }
    }
#endif

    private CellAdapter _clickedCell = null;
    private List<GridPos> _upgradePositions = new();

    public void FadeBossMusic()
    {
        bossMusicSource.DOKill();
        musicSource.DOKill();
        bossMusicSource.DOFade(volume, 0.5f);
        musicSource.DOFade(0, 0.5f);
    }

    public void FadeNormalMusic()
    {
        bossMusicSource.DOKill();
        musicSource.DOKill();
        musicSource.DOFade(volume, 0.5f);
        bossMusicSource.DOFade(0, 0.5f);
    }

    public void ClickCell(CellAdapter cellAdapter)
    {
        if (ProgressBar.Instance.IsGameOver) return;
        var cell = cellAdapter.Cell;
        _upgradePositions.Clear();
        if (_clickedCell == null)
        {
            cellAdapter.SetHighlight();
            _clickedCell = cellAdapter;
            return;
        }

        var clickedCell = _clickedCell.Cell;

        if (clickedCell.Position.X == cell.Position.X && clickedCell.Position.Y != cell.Position.Y &&
            Mathf.Abs(clickedCell.Position.Y - cell.Position.Y) == 1)
        {
            if (_grid.SwapGems(cell.Position, clickedCell.Position))
            {
                // _grid.DestoryGems();
                sfxSource.PlayOneShot(swapSound);
                _upgradePositions.Add(cell.Position);
                _upgradePositions.Add(clickedCell.Position);
                _clickedCell.ClearHighlight();
                _clickedCell = null;
                _combo       = 0;
            }
            else
            {
                _clickedCell.ClearHighlight();
                _clickedCell = cellAdapter;
                _clickedCell.SetHighlight();
            }
        }
        else if (clickedCell.Position.Y == cell.Position.Y && clickedCell.Position.X != cell.Position.X &&
                 Mathf.Abs(clickedCell.Position.X - cell.Position.X) == 1)
        {
            if (_grid.SwapGems(cell.Position, clickedCell.Position))
            {
                // _grid.DestoryGems();
                sfxSource.PlayOneShot(swapSound);
                _upgradePositions.Add(cell.Position);
                _upgradePositions.Add(clickedCell.Position);
                _clickedCell.ClearHighlight();
                _clickedCell = null;
                _combo       = 0;
            }
            else
            {
                _clickedCell.ClearHighlight();
                _clickedCell = cellAdapter;
                _clickedCell.SetHighlight();
            }
        }
        else
        {
            _clickedCell.ClearHighlight();
            _clickedCell = cellAdapter;
            _clickedCell.SetHighlight();
        }
    }

    public void ConsumePower(GridPos position)
    {
        _powersToConsume.Enqueue(position);
    }

    private bool _isMuted = false;
    public void ToggleSound()
    {
        _isMuted             = !_isMuted;
        bossMusicSource.mute = _isMuted;
        musicSource.mute     = _isMuted;
        sfxSource.mute       = _isMuted;
        muteIcon.SetActive(_isMuted);
    }
}