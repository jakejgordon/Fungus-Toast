using FungusToast.Core.Board;
using FungusToast.Core.Config;
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
            // Mark the mycovariant as triggered before resolving effects
            if (playerMyco != null)
            {
                playerMyco.MarkTriggered();
            }

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
            else if (mycovariant.Id == MycovariantIds.PlasmidBountyId ||
                     mycovariant.Id == MycovariantIds.PlasmidBountyIIId ||
                     mycovariant.Id == MycovariantIds.PlasmidBountyIIIId)
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
            // Apply the mutation point award based on which Plasmid Bounty was selected
            var playerMyco = player.PlayerMycovariants
                .FirstOrDefault(pm => pm.MycovariantId == MycovariantIds.PlasmidBountyId ||
                                     pm.MycovariantId == MycovariantIds.PlasmidBountyIIId ||
                                     pm.MycovariantId == MycovariantIds.PlasmidBountyIIIId);
            
            if (playerMyco != null)
            {
                int pointsToAdd = playerMyco.MycovariantId switch
                {
                    MycovariantIds.PlasmidBountyId => MycovariantGameBalance.PlasmidBountyMutationPointAward,
                    MycovariantIds.PlasmidBountyIIId => MycovariantGameBalance.PlasmidBountyIIMutationPointAward,
                    MycovariantIds.PlasmidBountyIIIId => MycovariantGameBalance.PlasmidBountyIIIMutationPointAward,
                    _ => 0
                };
                
                if (pointsToAdd > 0)
                {
                    player.AddMutationPoints(pointsToAdd);
                }
            }
            
            // Only pulse if the panel is active and enabled
            var panel = GameManager.Instance.GameUI.MoldProfilePanel;
            if (panel != null && panel.gameObject.activeInHierarchy && panel.enabled)
            {
                panel.PulseMutationPoints();
            }
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
