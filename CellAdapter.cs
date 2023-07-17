using System;
using DG.Tweening;
using FrostWind.Utils;
using UnityEngine;

public class CellAdapter : MonoBehaviour
{
    private SpriteRenderer sr;

    public bool IsBusy { get; private set; }

    public Sprite BlueSprite { get; set; }
    public Sprite RedSprite { get; set; }
    public Sprite GreenSprite { get; set; }
    public Sprite YellowSprite { get; set; }
    public Sprite PurpleSprite { get; set; }
    public Sprite BombSprite { get; set; }
    public Sprite SpannerSprite { get; set; }
    public Sprite TimerSprite { get; set; }

    public Cell Cell { get; private set; }
    public GameObject Highlight { get; set; }

    public void SetCell(Cell c) => Cell = c;

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        var col = gameObject.AddComponent<BoxCollider2D>();
        col.size   = Vector2.one;
        col.offset = Vector2.one / 2f;
    }

    public void Step()
    {
        if (IsBusy) return;

        if (Cell.Events.Count > 0)
        {
            if (Cell.Events.TryDequeue(out var cellEvent))
            {
                switch (cellEvent.EventType)
                {
                    case CellEventType.Destroy:
                        //Debug.Log($"Animate dest {Cell.Position}");
                        IsBusy = true;
                        transform.DOScale(Vector3.zero, .5f).OnComplete(() => IsBusy = false);
                        break;
                    case CellEventType.Slide:
                        //Debug.Log($"Animate slide {Cell.Position} {cellEvent.Target}");
                        IsBusy = true;
                        transform.DOLocalMove(cellEvent.Target, .5f).OnComplete(() =>
                        {
                            IsBusy               = false;
                            transform.localScale = Vector3.one;
                        });
                        break;
                    case CellEventType.Fall:
                        //Debug.Log($"Animate slide {Cell.Position} {cellEvent.Target}");
                        IsBusy = true;
                        SetSprite();
                        transform.DOLocalMove(Cell.Position, .3f).From(cellEvent.Target).OnComplete(() =>
                        {
                            IsBusy               = false;
                            transform.localScale = Vector3.one;
                        });
                        break;
                    case CellEventType.ExecPowerSpanner:
                        GridManager.Instance.ConsumePower(Cell.Position);
                        ProgressBar.Instance.IncreaseCapacity();
                        break;
                    case CellEventType.ExecPowerTimer:
                        GridManager.Instance.ConsumePower(Cell.Position);
                        ProgressBar.Instance.ReduceSpeed();
                        break;
                }
            }
        }

        if (IsBusy) return;

        var t = transform;
        t.localPosition = Cell.Position;
        t.localScale    = Vector3.one;

        SetSprite();
    }

    private void SetSprite()
    {
        sr.sprite = Cell.Color switch
        {
            GemColor.Empty => null,
            GemColor.Blue => BlueSprite,
            GemColor.Green => GreenSprite,
            GemColor.Yellow => YellowSprite,
            GemColor.Red => RedSprite,
            GemColor.Purple => PurpleSprite,
            GemColor.Bomb => BombSprite,
            GemColor.Spanner => SpannerSprite,
            GemColor.Timer => TimerSprite,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void OnMouseDown()
    {
        GridManager.Instance.ClickCell(this);
    }

    public void SetHighlight() => Highlight.SetActive(true);
    public void ClearHighlight() => Highlight.SetActive(false);
}