using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Lets the player drag a floating card (onboarding coachmarks, the pinned player
    /// inspector) out of the way of whatever it is covering. The whole card is the drag
    /// surface: the body text is not interactive, and a big target beats a precise
    /// title-bar grab. Controls nested inside the card (the close button, icon tiles)
    /// still work because Unity only starts a drag after the pointer moves past its drag
    /// threshold, and <see cref="CursorManager"/> lets the nearest surface decide the
    /// cursor, so a nested <see cref="Selectable"/> shows the hand and a hover-only icon
    /// tile shows the arrow rather than the move cursor.
    ///
    /// Affordances, in the order a player meets them: a dot grip in the title row (built by
    /// <see cref="CoachmarkLayoutUtility.AddGrip"/>), the four-way move cursor while the
    /// pointer is over the card, and a lift (brighter outline, slight scale-up) while the
    /// drag is in progress.
    ///
    /// Once dragged, the card is the player's: <see cref="HasBeenMoved"/> stays true until
    /// the host calls <see cref="ResetMoved"/> on a fresh show, and hosts that re-anchor a
    /// card (the inspector follows its scoreboard icon) must stop while it is set. Nothing
    /// is persisted across sessions.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ICursorSurface
    {
        private const float LiftScale = 1.02f;

        private RectTransform cardRect;
        private RectTransform boundsRect;
        private CanvasGroup canvasGroup;
        private Outline outline;
        private Vector2 padding = CoachmarkLayoutUtility.DefaultScreenPadding;
        private Vector2 grabOffset;
        private Vector3 restingScale = Vector3.one;
        private Color restingOutlineColor;
        private bool isDragging;

        /// <summary>True from the end of the first drag until <see cref="ResetMoved"/>.</summary>
        public bool HasBeenMoved { get; private set; }

        /// <summary>Raised when a drag ends, so a host can stop re-anchoring the card.</summary>
        public event Action Moved;

        /// <summary>
        /// The move cursor is only offered while the card can actually take the drag: a
        /// card fading in with raycasts off, or one whose group is non-interactive, should
        /// not advertise it.
        /// </summary>
        public bool ShowsMoveCursor =>
            isActiveAndEnabled
            && (canvasGroup == null || (canvasGroup.interactable && canvasGroup.blocksRaycasts));

        public CursorKind? PreferredCursor => ShowsMoveCursor ? CursorKind.Move : null;

        /// <summary>
        /// Wires the card. <paramref name="bounds"/> is the rect the card is clamped inside
        /// (its parent when null); <paramref name="screenPadding"/> is the inset kept from
        /// that rect's edges, in the card's local units.
        /// </summary>
        public void Configure(RectTransform bounds = null, Vector2? screenPadding = null)
        {
            cardRect = (RectTransform)transform;
            boundsRect = bounds != null ? bounds : cardRect.parent as RectTransform;
            padding = screenPadding ?? CoachmarkLayoutUtility.DefaultScreenPadding;
            canvasGroup = GetComponent<CanvasGroup>();
            outline = GetComponent<Outline>();
        }

        /// <summary>Forgets the player's placement; call when the card is shown fresh.</summary>
        public void ResetMoved()
        {
            HasBeenMoved = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !ShowsMoveCursor)
            {
                return;
            }

            EnsureConfigured();
            if (!TryGetLocalPoint(eventData, out Vector2 localPoint))
            {
                return;
            }

            // The entrance/pulse animation also drives scale and outline; a drag ends it so
            // the two never fight over the same fields.
            GetComponent<CoachmarkAttentionEffect>()?.Settle();

            isDragging = true;
            grabOffset = localPoint - cardRect.anchoredPosition;
            restingScale = cardRect.localScale;
            cardRect.localScale = restingScale * LiftScale;
            if (outline != null)
            {
                restingOutlineColor = outline.effectColor;
                outline.effectColor = UIStyleTokens.State.Focus;
            }

            cardRect.SetAsLastSibling();
            CursorManager.Instance?.Push(CursorKind.Move, this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || !TryGetLocalPoint(eventData, out Vector2 localPoint))
            {
                return;
            }

            cardRect.anchoredPosition = CoachmarkLayoutUtility.ClampAnchoredPosition(
                cardRect, boundsRect, localPoint - grabOffset, padding);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
            {
                return;
            }

            FinishDrag();
            HasBeenMoved = true;
            Moved?.Invoke();
        }

        private void OnDisable()
        {
            if (isDragging)
            {
                FinishDrag();
            }
        }

        private void FinishDrag()
        {
            isDragging = false;
            cardRect.localScale = restingScale;
            if (outline != null)
            {
                outline.effectColor = restingOutlineColor;
            }

            CursorManager.Instance?.Pop(this);
        }

        private void EnsureConfigured()
        {
            if (cardRect == null)
            {
                Configure();
            }
        }

        /// <summary>
        /// The pointer in the bounds rect's local space, expressed as an anchored position
        /// for the card (so the card's anchor offset is already removed).
        /// </summary>
        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 anchoredPoint)
        {
            anchoredPoint = default;
            if (boundsRect == null)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    boundsRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return false;
            }

            anchoredPoint = CoachmarkLayoutUtility.LocalPointToAnchoredPosition(cardRect, boundsRect, localPoint);
            return true;
        }
    }
}
