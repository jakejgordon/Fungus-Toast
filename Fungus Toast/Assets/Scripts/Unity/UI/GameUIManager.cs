using UnityEngine;
using FungusToast.Unity.UI.MutationTree;
using FungusToast.Unity.UI.GameLog;

namespace FungusToast.Unity.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("Core UI")]
        [SerializeField] private UI_MutationManager mutationUIManager;
        [SerializeField] private UI_MoldProfilePanel moldProfilePanel;
        [SerializeField] private UI_PlayerBinder playerUIBinder;

        [Header("Sidebars")]
        [SerializeField] private GameObject leftSidebar;
        [SerializeField] private UI_RightSidebar rightSidebar;
        
        [Header("Game Log")]
        [SerializeField] private UI_GameLogPanel gameLogPanel;
        [SerializeField] private GameLogManager gameLogManager;

        [Header("End-game")]
        [SerializeField] private UI_EndGamePanel endGamePanel;

        [Header("Phase Transitions")]
        [SerializeField] private UI_PhaseBanner phaseBanner;

        [Header("Phase Tracker")]
        [SerializeField] private UI_PhaseProgressTracker phaseProgressTracker;

        public UI_PhaseProgressTracker PhaseProgressTracker => phaseProgressTracker;

        public UI_MutationManager MutationUIManager => mutationUIManager;
        public UI_MoldProfilePanel MoldProfilePanel => moldProfilePanel;
        public UI_PlayerBinder PlayerUIBinder => playerUIBinder;
        public GameObject LeftSidebar => leftSidebar;
        public UI_RightSidebar RightSidebar => rightSidebar;
        public UI_EndGamePanel EndGamePanel => endGamePanel;
        public UI_PhaseBanner PhaseBanner => phaseBanner;
        public UI_GameLogPanel GameLogPanel => gameLogPanel;
        public GameLogManager GameLogManager => gameLogManager;
    }
}
