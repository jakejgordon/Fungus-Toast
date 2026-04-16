using UnityEngine;
using UnityEngine.InputSystem;

namespace FungusToast.Unity.Input
{
    internal static class UnityInputAdapter
    {
        private const float LegacyScrollStep = 120f;

        public static Vector2 GetPointerScreenPosition()
        {
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }

            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            return Vector2.zero;
        }

        public static Vector2 GetPointerDelta()
        {
            if (Pointer.current != null)
            {
                return Pointer.current.delta.ReadValue();
            }

            if (Mouse.current != null)
            {
                return Mouse.current.delta.ReadValue();
            }

            return Vector2.zero;
        }

        public static float GetMouseScrollDelta()
        {
            float rawScroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (Mathf.Approximately(rawScroll, 0f))
            {
                return 0f;
            }

            return rawScroll / LegacyScrollStep;
        }

        public static Vector2 GetKeyboardMoveVector()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            int horizontal = 0;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal--;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal++;
            }

            int vertical = 0;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical--;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical++;
            }

            return new Vector2(horizontal, vertical);
        }

        public static bool IsPrimaryPointerPressed()
        {
            return Mouse.current?.leftButton.isPressed ?? false;
        }

        public static bool WasPrimaryPointerPressedThisFrame()
        {
            return Mouse.current?.leftButton.wasPressedThisFrame ?? false;
        }

        public static bool IsSecondaryPointerPressed()
        {
            return Mouse.current?.rightButton.isPressed ?? false;
        }

        public static bool WasSecondaryPointerPressedThisFrame()
        {
            return Mouse.current?.rightButton.wasPressedThisFrame ?? false;
        }

        public static bool WasEscapePressedThisFrame()
        {
            return Keyboard.current?.escapeKey.wasPressedThisFrame ?? false;
        }

        public static bool IsTouchSupportedOnCurrentPlatform()
        {
            return Application.isMobilePlatform && Touchscreen.current != null;
        }
    }
}