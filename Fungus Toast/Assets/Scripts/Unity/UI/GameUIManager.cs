using UnityEngine;
using FungusToast.Unity.UI.MutationTree;

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

        [Header("End-game")]
        [SerializeField] private UI_EndGamePanel endGamePanel;

        [Header("Phase Transitions")]
        [SerializeField] private UI_PhaseBanner phaseBanner;

        public UI_MutationManager MutationUIManager => mutationUIManager;
        public UI_MoldProfilePanel MoldProfilePanel => moldProfilePanel;
        public UI_PlayerBinder PlayerUIBinder => playerUIBinder;
        public GameObject LeftSidebar => leftSidebar;
        public UI_RightSidebar RightSidebar => rightSidebar;
        public UI_EndGamePanel EndGamePanel => endGamePanel;
        public UI_PhaseBanner PhaseBanner => phaseBanner;
    }
}
