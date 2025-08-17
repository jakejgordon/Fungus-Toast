using System;
using UnityEngine;

namespace FungusToast.Unity.UI.Tooltips
{
    public struct TooltipRequest
    {
        public string StaticText;
        public Func<string> DynamicTextFunc;
        public RectTransform Anchor;
        public bool UsePointerPosition;
        public Vector2 PivotPreference;
        public int? MaxWidth;
        public bool FollowPointer;

        public string ResolveText() => DynamicTextFunc != null ? DynamicTextFunc() : StaticText;
    }
}
