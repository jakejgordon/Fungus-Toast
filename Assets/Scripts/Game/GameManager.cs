using UnityEngine;
using FungusToast.Core;
using FungusToast.Grid;
using FungusToast.Game;

public class GameManager : MonoBehaviour
{
    public int boardWidth = 20;
    public int boardHeight = 20;
    public int playerCount = 2;

    public GridVisualizer gridVisualizer;

    public static GameManager Instance { get; private set; }
    public GameBoard Board { get; private set; }
    public CameraCenterer cameraCenterer;
    [SerializeField] private MutationUIManager mutationUIManager;
    [SerializeField] private MutationManager mutationManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Board = new GameBoard(boardWidth, boardHeight, playerCount);
    }

    private void Start()
    {
        Board.PlaceInitialSpore(0, 2, 2);
        Board.PlaceInitialSpore(1, boardWidth - 3, boardHeight - 3);

        gridVisualizer.RenderBoard(Board);
    }

    public void InitializeGame(int count)
    {
        playerCount = count;
        Board = new GameBoard(boardWidth, boardHeight, playerCount);
        PlaceStartingSpores();
        gridVisualizer.RenderBoard(Board);

        mutationManager.ResetMutationPoints();
        mutationUIManager.SetSpendPointsButtonVisible(true);
    }


    //-- This places players roughly in a circle around the toast, spaced out evenly no matter the count (2, 3, 6, 8, etc.)
    public void PlaceStartingSpores()
    {
        float radius = Mathf.Min(boardWidth, boardHeight) * 0.35f;
        Vector2 center = new Vector2(boardWidth / 2f, boardHeight / 2f);

        for (int i = 0; i < playerCount; i++)
        {
            float angle = i * Mathf.PI * 2f / playerCount;
            float x = center.x + radius * Mathf.Cos(angle);
            float y = center.y + radius * Mathf.Sin(angle);

            int px = Mathf.Clamp(Mathf.RoundToInt(x), 0, boardWidth - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(y), 0, boardHeight - 1);

            Board.PlaceInitialSpore(i, px, py);
        }
    }

}
