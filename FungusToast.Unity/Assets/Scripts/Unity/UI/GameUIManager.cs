using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core.Board;
using FungusToast.Unity.UI.MutationTree;
using FungusToast.Unity.UI.GameLog;

namespace FungusToast.Unity.UI
{
    public class GameUIManager : MonoBehaviour
    {
            private const float FlexibleLogMinHeight = 180f;

        [Header("Core UI")]
        [SerializeField] private UI_MutationManager mutationUIManager;
        [SerializeField] private UI_PlayerBinder playerUIBinder;

        [Header("Sidebars")]
        [SerializeField] private GameObject leftSidebar;
        [SerializeField] private UI_RightSidebar rightSidebar;
        [SerializeField] private UI_MoldProfileRoot moldProfileRoot; // Added mold profile root reference
        
        [Header("Player Activity Log (Left Sidebar)")]
        [SerializeField] private UI_GameLogPanel playerActivityLogPanel;
        [SerializeField] private GameLogManager playerActivityLogManager;
        
        [Header("Global Events Log (Right Sidebar)")]
        [SerializeField] private UI_GameLogPanel globalEventsLogPanel;
        [SerializeField] private GlobalGameLogManager globalEventsLogManager;

        [Header("Loading / Transitions")]
        [SerializeField] private UI_LoadingScreen loadingScreen;

        [Header("End-game")]
        [SerializeField] private UI_EndGamePanel endGamePanel;

        [Header("Pause Menu")]
        [SerializeField] private UI_PauseMenuPanel pauseMenuPanel;
        [SerializeField] private Sprite pauseMenuButtonIcon;
        [SerializeField] private Sprite nextTrackButtonIcon;

        [Header("Phase Transitions")]
        [SerializeField] private UI_PhaseBanner phaseBanner;

        [Header("Phase Tracker")]
        [SerializeField] private UI_PhaseProgressTracker phaseProgressTracker;

            private void Awake()
            {
                ApplySidebarLogLayoutBehavior();
            }

        public UI_PhaseProgressTracker PhaseProgressTracker => phaseProgressTracker;

            private void ApplySidebarLogLayoutBehavior()
            {
                if (leftSidebar != null)
                {
                    var sidebarLayout = leftSidebar.GetComponent<VerticalLayoutGroup>();
                    if (sidebarLayout != null)
                    {
                        sidebarLayout.childControlHeight = true;
                        sidebarLayout.childForceExpandHeight = false;
                    }
                }

                if (playerActivityLogPanel == null)
                {
                    return;
                }

                var layoutElement = playerActivityLogPanel.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = playerActivityLogPanel.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.minHeight = FlexibleLogMinHeight;
                layoutElement.preferredHeight = -1f;
                layoutElement.flexibleHeight = 1f;
            }

        // ── Core accessors ──
        public UI_MutationManager MutationUIManager => mutationUIManager;
        public UI_PlayerBinder PlayerUIBinder => playerUIBinder;
        public GameObject LeftSidebar => leftSidebar;
        public UI_RightSidebar RightSidebar => rightSidebar;
        public UI_LoadingScreen LoadingScreen => loadingScreen;
        public UI_EndGamePanel EndGamePanel => endGamePanel;
        public UI_PauseMenuPanel PauseMenuPanel => pauseMenuPanel;
        public Sprite PauseMenuButtonIcon => pauseMenuButtonIcon;
        public Sprite NextTrackButtonIcon => nextTrackButtonIcon;
        public UI_PhaseBanner PhaseBanner => phaseBanner;
        public UI_MoldProfileRoot MoldProfileRoot => moldProfileRoot;

        // ── Board accessor ──
        // Set by GameManager after board creation so UI components can access it
        // without reaching into GameManager.Instance.
        public GameBoard Board { get; private set; }

        /// <summary>
        /// Called by GameManager after board creation. Allows UI components to
        /// reference the board via GameUIManager instead of GameManager.Instance.
        /// </summary>
        public void SetBoard(GameBoard board) => Board = board;
        public void ClearBoard() => Board = null;
        
        // Player Activity Log (Left Sidebar)
        public UI_GameLogPanel PlayerActivityLogPanel => playerActivityLogPanel;
        public GameLogManager PlayerActivityLogManager => playerActivityLogManager;
        
        // Global Events Log (Right Sidebar)  
        public UI_GameLogPanel GlobalEventsLogPanel => globalEventsLogPanel;
        public GlobalGameLogManager GlobalEventsLogManager => globalEventsLogManager;

        // Unified logging interface
        public GameLogRouter GameLogRouter 
        {
            get
            {
                if (gameLogRouter == null)
                {
                    gameLogRouter = new GameLogRouter(playerActivityLogManager, globalEventsLogManager, MutationTreeToastPresenter);
                    
                    // Set the router reference on the player activity log manager for silent mode awareness
                    playerActivityLogManager?.SetGameLogRouter(gameLogRouter);
                }
                return gameLogRouter;
            }
        }
        // Legacy properties for backwards compatibility
        public UI_GameLogPanel GameLogPanel => playerActivityLogPanel;
        public GameLogManager GameLogManager => playerActivityLogManager;
        public UI_GameLogPanel GlobalGameLogPanel => globalEventsLogPanel;
        public GlobalGameLogManager GlobalGameLogManager => globalEventsLogManager;
        public UI_MutationTreeToastPresenter MutationTreeToastPresenter
        {
            get
            {
                if (mutationTreeToastPresenter == null)
                {
                    mutationTreeToastPresenter = GetComponent<UI_MutationTreeToastPresenter>();
                    if (mutationTreeToastPresenter == null)
                    {
                        mutationTreeToastPresenter = gameObject.AddComponent<UI_MutationTreeToastPresenter>();
                    }

                    mutationTreeToastPresenter.Initialize(mutationUIManager);
                }

                return mutationTreeToastPresenter;
            }
        }

        public void RegisterPauseMenuPanel(UI_PauseMenuPanel panel) => pauseMenuPanel = panel;

        // Routing observer for unified event handling
        private GameLogRouter gameLogRouter;
        private UI_MutationTreeToastPresenter mutationTreeToastPresenter;
    }
}
