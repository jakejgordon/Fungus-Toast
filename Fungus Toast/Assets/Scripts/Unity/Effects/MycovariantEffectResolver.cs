using FungusToast.Core.Board;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.MycovariantDraft;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.Effects
{
    public class MycovariantEffectResolver : MonoBehaviour
    {
        public static MycovariantEffectResolver Instance { get; private set; }

        [SerializeField] private GameObject draftPanel;
        [SerializeField] private GridVisualizer gridVisualizer; // Add this in inspector if not already present

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this.gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Returns true if the given mycovariant ID is any Jetting Mycelium (all directions).
        /// </summary>
        public static bool IsJettingMycelium(int id) =>
            id == MycovariantIds.JettingMyceliumNorthId ||
            id == MycovariantIds.JettingMyceliumEastId ||
            id == MycovariantIds.JettingMyceliumSouthId ||
            id == MycovariantIds.JettingMyceliumWestId;

        public IEnumerator ResolveEffect(
            Player player,
            Mycovariant mycovariant,
            PlayerMycovariant playerMyco,
            Action onComplete)
        {
            if (IsJettingMycelium(mycovariant.Id))
            {
                yield return StartCoroutine(
                    MycovariantEffectHelpers.HandleJettingMycelium(
                        player,
                        mycovariant,
                        onComplete,
                        draftPanel,
                        gridVisualizer
                    )
                );
            }
            else if (mycovariant.Id == MycovariantIds.PlasmidBountyId)
            {
                HandlePlasmidBounty(player);
                onComplete?.Invoke();
            }
            else if (mycovariant.Id == MycovariantIds.MycelialBastionId)
            {
                yield return StartCoroutine(
                    MycovariantEffectHelpers.HandleMycelialBastion(
                        player,
                        mycovariant,
                        onComplete,
                        draftPanel,
                        gridVisualizer
                    )
                );
            }
            else if (mycovariant.Id == MycovariantIds.SurgicalInoculationId)
            {
                yield return StartCoroutine(
                    MycovariantEffectHelpers.HandleSurgicalInoculation(
                        player,
                        mycovariant,
                        onComplete,
                        draftPanel,
                        gridVisualizer
                    )
                );
            }
            // Add more cases as needed
            else
            {
                onComplete?.Invoke();
            }
        }

        private void HandlePlasmidBounty(Player player)
        {
            // Core logic already handles the mutation point award via MycovariantFactory
            // No need to duplicate it here
            
            // Only pulse if the panel is active and enabled
            var panel = GameManager.Instance.GameUI.MoldProfilePanel;
            if (panel != null && panel.gameObject.activeInHierarchy && panel.enabled)
            {
                panel.PulseMutationPoints();
            }
            
            /*
            GameManager.Instance.GameUI.RightSidebar?.AddLogEntry(
                $"{player.PlayerName} gained 15 mutation points from Plasmid Bounty!"
            );
            */
        }

        /// <summary>
        /// Returns the correct CardinalDirection for a Jetting Mycelium mycovariant ID.
        /// </summary>
        public static CardinalDirection DirectionFromMycovariantId(int id)
        {
            if (id == MycovariantIds.JettingMyceliumNorthId) return CardinalDirection.North;
            if (id == MycovariantIds.JettingMyceliumEastId) return CardinalDirection.East;
            if (id == MycovariantIds.JettingMyceliumSouthId) return CardinalDirection.South;
            if (id == MycovariantIds.JettingMyceliumWestId) return CardinalDirection.West;
            throw new ArgumentException("Invalid Jetting Mycelium ID");
        }
    }
}
