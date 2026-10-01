#nullable enable

using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.PlayerInspector
{
    /// <summary>
    /// A <see cref="ScrollRect"/> that scrolls by mouse wheel and scrollbar only, handing every
    /// drag up to its parent instead. The inspector is a <see cref="DraggableCard"/> whose whole
    /// surface moves the panel; a stock ScrollRect is the nearer drag handler for anything in
    /// its viewport, so it would swallow those drags and the panel could only be moved by its
    /// header.
    /// </summary>
    public sealed class WheelOnlyScrollRect : ScrollRect
    {
        public override void OnInitializePotentialDrag(PointerEventData eventData) =>
            ForwardToParent(eventData, ExecuteEvents.initializePotentialDrag);

        public override void OnBeginDrag(PointerEventData eventData) =>
            ForwardToParent(eventData, ExecuteEvents.beginDragHandler);

        public override void OnDrag(PointerEventData eventData) =>
            ForwardToParent(eventData, ExecuteEvents.dragHandler);

        public override void OnEndDrag(PointerEventData eventData) =>
            ForwardToParent(eventData, ExecuteEvents.endDragHandler);

        private void ForwardToParent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
            where T : IEventSystemHandler
        {
            if (transform.parent != null)
            {
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, handler);
            }
        }
    }
}
