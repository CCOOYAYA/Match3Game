using Cysharp.Threading.Tasks;
using Spine.Unity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class GameBoardHintEntity : MonoBehaviour
{
    [Header("Hint Options")]
    [SerializeField] private float _inactivityBeforeHint;
    [SerializeField] private float _hintDuration;
                     private bool _hintOn;

    [Header("Hint Animations")]
    [SerializeField] private float hintAnimationDuration = 2f;
    [SerializeField] private AnimationCurve hintPiecePosition;

    // Top Priority
    private readonly int flyBombCombineWeight = 15;
    private readonly int bombCombineWeight = 20;
    private readonly int rocketCombineWeight = 15;
    private readonly int rainbowRainbowWeight = 10000;

    private readonly int flyBombSpawnWeight = 10;
    private readonly int bombSpawnWeight = 15;
    private readonly int rocketSpawnWeight = 10;
    private readonly int rainbowSpawnWeight = 40;

    private CancellationTokenSource _tokenSource;
    private SkeletonAnimation _skeletonAnimation;
    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    

    public bool HintOn 
    {
        get => _hintOn;
        set
        {
            _hintOn = value;
            if (!_hintOn)
            {
                ShutDownHint();
            }
        }
    }
    public bool IsHinting { get; private set; }
    public float HintingTime { get; private set; }
    public float InactivityBeforeHint => _inactivityBeforeHint;
    public float HintDuration => _hintDuration;


    public int CalculatePriority(int pieceId1, int pieceId2)
    {
        if (pieceId1 == Constants.PieceRainbowId && pieceId2 == Constants.PieceRainbowId)
            return rainbowRainbowWeight;

        bool useMultiply = (pieceId1 == Constants.PieceRainbowId && (pieceId2 == Constants.PieceFlyBombId || pieceId2 == Constants.PieceBombId || pieceId2 == Constants.PieceHRocketId || pieceId2 == Constants.PieceVRocketId)) || 
                           (pieceId2 == Constants.PieceRainbowId && (pieceId1 == Constants.PieceFlyBombId || pieceId1 == Constants.PieceBombId || pieceId1 == Constants.PieceHRocketId || pieceId1 == Constants.PieceVRocketId));

        var p1 = pieceId1 switch
        {
            var x when x == Constants.PieceFlyBombId                                    => flyBombCombineWeight,
            var x when x == Constants.PieceBombId                                       => bombCombineWeight,
            var x when x == Constants.PieceHRocketId || x == Constants.PieceVRocketId   => rocketCombineWeight,
            var x when x == Constants.PieceRainbowId                                    => GameBoardManager.instance.GetMostFreeBasicPieceCount(),
            _                                                                           => 0
        };

        var p2 = pieceId2 switch
        {
            var x when x == Constants.PieceFlyBombId                                    => flyBombCombineWeight,
            var x when x == Constants.PieceBombId                                       => bombCombineWeight,
            var x when x == Constants.PieceHRocketId || x == Constants.PieceVRocketId   => rocketCombineWeight,
            var x when x == Constants.PieceRainbowId                                    => GameBoardManager.instance.GetMostFreeBasicPieceCount(),
            _                                                                           => 0
        };

        return useMultiply ? p1 * p2 : p1 + p2;
    }

    public int CalculatePriority(int pieceId) =>
        pieceId switch
        {
            var x when x == Constants.PieceFlyBombId                                    => flyBombSpawnWeight,
            var x when x == Constants.PieceBombId                                       => bombSpawnWeight,
            var x when x == Constants.PieceHRocketId || x == Constants.PieceVRocketId   => rocketSpawnWeight,
            var x when x == Constants.PieceRainbowId                                    => rainbowSpawnWeight,
            _                                                                           => 0
        };


    /// <summary>
    /// 显示Hint
    /// </summary>
    public void DisplayHint(PossibleSwap possibleSwap)
    {
        IsHinting = true;
        HintingTime = 0f;
        _tokenSource = new();

        if (gameObject.activeInHierarchy == false)
            gameObject.SetActive(true);
        transform.SetPositionAndRotation(possibleSwap.AnimationWorldPosition, Quaternion.Euler(0, 0, possibleSwap.AnimationRotation));

        PlayHintAnimation(possibleSwap).Forget();
    }


    /// <summary>
    /// 关闭Hint
    /// </summary>
    public void ShutDownHint()
    {
        IsHinting = false;
        HintingTime = 0f;
        _tokenSource?.Cancel();
        _tokenSource?.Dispose();
        _tokenSource = null;

        if (gameObject != null &&
            gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }


    private async UniTask PlayHintAnimation(PossibleSwap possibleSwap)
    {
        // 播放提示动画
        _skeletonAnimation ??= GetComponent<SkeletonAnimation>();
        SkeletonAnimation.Initialize(true);
        if (possibleSwap.HintAnimation != null)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, possibleSwap.HintAnimation, true);
        }

        // 播放棋子高亮动效
        List<Piece> highlightPieces = new();
        var standardPiece = GameBoardManager.instance.slotGrid[possibleSwap.StartPosition].piece;
        if (standardPiece.CanUse == false)
            highlightPieces.Add(standardPiece);
        possibleSwap.ContainPosition.ForEach(pos =>
        {
            var piece = GameBoardManager.instance.slotGrid[pos].piece;
            if (piece.CanUse == false && piece.Id == standardPiece.Id)
                highlightPieces.Add(piece);
        });
        _tokenSource.Token.Register(() => highlightPieces.ForEach(piece => piece?.PlayIdleAnimation()));     // Register cancel callback
        highlightPieces.ForEach(piece => piece.PlayHintAnimation(hintAnimationDuration));

        // 播放棋子缓动动效
        Transform piece1Transform = GameBoardManager.instance.slotGrid[possibleSwap.StartPosition].piece.Transform;
        Transform piece2Transform = possibleSwap.IsSynthesize ? GameBoardManager.instance.slotGrid[possibleSwap.ContainPosition.FirstOrDefault(x => x.Equals(possibleSwap.StartPosition) == false)].piece.Transform : null;
        var piece1OriginalPosition = piece1Transform.position;
        var piece2OriginalPosition = piece2Transform == null ? Vector3.zero : piece2Transform.position;
        bool moveX = possibleSwap.SwapDirection.Equals(GridPosition.Left) || possibleSwap.SwapDirection.Equals(GridPosition.Right);
        int positive = possibleSwap.SwapDirection.Equals(GridPosition.Right) || possibleSwap.SwapDirection.Equals(GridPosition.Up) ? 1 : -1;
        _tokenSource.Token.Register(() =>
        {
            if (piece1Transform != null)
                piece1Transform.position = piece1OriginalPosition;
            if (piece2Transform != null)
                piece2Transform.position = piece2OriginalPosition;
        });

        while (HintingTime < hintAnimationDuration && 
               _tokenSource?.Token.IsCancellationRequested == false)
        {
            HintingTime += Time.deltaTime;
            if (piece1Transform != null)
                piece1Transform.position = piece1OriginalPosition + (moveX ? new Vector3(positive * hintPiecePosition.Evaluate(HintingTime), 0f, 0f) : new Vector3(0f, positive * hintPiecePosition.Evaluate(HintingTime), 0f));
            if (piece2Transform != null)
                piece2Transform.position = piece2OriginalPosition + (moveX ? new Vector3(-positive * hintPiecePosition.Evaluate(HintingTime), 0f, 0f) : new Vector3(0f, -positive * hintPiecePosition.Evaluate(HintingTime), 0f));
            await UniTask.NextFrame();
        }

        // 完成后关闭
        if (_tokenSource != null &&
            _tokenSource?.Token.IsCancellationRequested == false)
            ShutDownHint();
    }

    private void OnDestroy()
    {
        ShutDownHint();
    }
}

public class PossibleSwap
{
    public bool IsSynthesize;
    public GridPosition StartPosition;
    public GridPosition SwapDirection;
    public List<GridPosition> ContainPosition;

    public AnimationReferenceAsset HintAnimation;
    public Vector3 AnimationWorldPosition;
    public int AnimationRotation;
}
