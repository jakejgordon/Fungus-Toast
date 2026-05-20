using FungusToast.Core.Board;
using FungusToast.Unity.Input;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.Onboarding;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FungusToast.Unity.Cameras
{
    public class CameraControls : MonoBehaviour
    {
        private const float CameraPanCoachmarkWidth = 380f;
        private const float CameraPanCoachmarkMinHeight = 150f;
        private const float CameraPanCoachmarkBottomOffset = 22f;
        private const float CameraPanCoachmarkBodyHorizontalPadding = 14f;
        private const float CameraPanCoachmarkBodyBottomPadding = 14f;
        private const float CameraPanCoachmarkBodyTopReservedHeight = 46f;
        private const float CameraPanDragThreshold = 0.01f;

        public float zoomSpeed = 12.5f;
        public float moveSpeed = 7.5f;
        public float minZoom = 5f;
        public float maxZoom = 100f;

        [Header("Zoom Scaling")]
        [Tooltip("Smallest board dimension that uses the minimum zoom sensitivity scale.")]
        [SerializeField] private float minBoardDimensionForZoomScaling = 10f;
        [Tooltip("Board dimension at which zoom reaches full sensitivity.")]
        [SerializeField] private float maxBoardDimensionForZoomScaling = 160f;
        [Tooltip("Multiplier applied to zoom sensitivity on the smallest supported boards.")]
        [SerializeField] private float minZoomSensitivityScale = 0.25f;

        [Header("Input Stability")]
        [Tooltip("Caps the timestep used for pan input so animation hitches do not fling the camera across the board.")]
        [SerializeField] private float maxPanInputDeltaTime = 1f / 45f;
        [Tooltip("Caps how far camera panning can move in a single frame at minimum zoom. The cap scales up with camera zoom so normal movement stays responsive while hitch spikes remain bounded.")]
        [SerializeField] private float maxPanDistancePerFrame = 0.75f;

        [Header("Camera Bounds")]
        [Tooltip("Maximum distance camera can move from board center (in world units). For 100x100 boards, try 75-100.")]
        public float maxDistanceFromCenter = 75f;
        [Tooltip("Reference to GameManager to get board dimensions")]
        public GameManager gameManager;
        [Tooltip("Reference to CameraCenterer to get board center")]
        public CameraCenterer cameraCenterer;
        [Tooltip("Auto-calculate bounds based on board size (recommended)")]
        public bool autoCalculateBounds = true;
        [Tooltip("Extra padding beyond board edges when auto-calculating (in world units)")]
        public float autoBoundsPadding = 25f;

        [Header("Small Board Safeguards")]
        [Tooltip("Maximum zoom-out multiplier relative to the initial board framing. Keeps tiny boards from shrinking to a speck.")]
        [SerializeField] private float maxZoomOutRelativeToInitialFraming = 1.12f;
        [Tooltip("For boards smaller than the viewport, keep at least this fraction of the board visible on each axis while panning.")]
        [SerializeField] [Range(0.5f, 1f)] private float minVisibleSmallBoardFraction = 0.85f;

        [Header("Onboarding")]
        [Tooltip("Delay before the first-game camera movement coachmark appears if the player has not panned yet.")]
        [SerializeField] private float cameraPanCoachmarkDelaySeconds = 3f;

        private RectTransform cameraPanCoachmarkRoot;
        private CanvasGroup cameraPanCoachmarkCanvasGroup;
        private TextMeshProUGUI cameraPanCoachmarkTitleTextLabel;
        private TextMeshProUGUI cameraPanCoachmarkBodyTextLabel;
        private Button cameraPanCoachmarkCloseButton;
        private float cameraPanCoachmarkElapsed;
        private bool hasDismissedCameraPanCoachmarkThisGame;
        private GameBoard trackedOnboardingBoard;

        void Update()
        {
            GameManager gameManager = GameManager.Instance;
            RefreshCameraPanOnboardingState(gameManager);

            if (gameManager != null && gameManager.IsPauseMenuOpen)
            {
                return;
            }

            if (gameManager?.GameUI?.EndGamePanel != null
                && gameManager.GameUI.EndGamePanel.BlocksGameplayCameraInput)
            {
                return;
            }

            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // Zoom with scroll wheel
            float scroll = pointerOverUi ? 0f : UnityInputAdapter.GetMouseScrollDelta();
            if (Camera.main != null)
            {
                Camera mainCamera = Camera.main;
                float panDeltaTime = GetPanDeltaTime();
                float maxPanDistance = GetMaxPanDistancePerFrame(mainCamera);
                float size = mainCamera.orthographicSize;
                bool cameraNavigatedThisFrame = false;

                // --- Zoom to mouse cursor logic ---
                if (Mathf.Abs(scroll) > 0.0001f)
                {
                    Camera cam = mainCamera;
                    // 1. Get world position under mouse before zoom
                    Vector2 pointerScreen = UnityInputAdapter.GetPointerScreenPosition();
                    Vector3 mouseScreenPos = new Vector3(pointerScreen.x, pointerScreen.y, 0f);
                    Vector3 worldBefore = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, cam.nearClipPlane));

                    // 2. Apply zoom
                    size -= scroll * GetZoomSpeedForCurrentBoard();
                    size = Mathf.Clamp(size, GetDynamicMinZoom(), GetDynamicMaxZoom());
                    cam.orthographicSize = size;

                    // 3. Get world position under mouse after zoom
                    Vector3 worldAfter = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, cam.nearClipPlane));

                    // 4. Offset camera position so the world point under the cursor stays fixed
                    Vector3 delta = worldBefore - worldAfter;
                    Vector3 newPosition = cam.transform.position + new Vector3(delta.x, delta.y, 0);

                    // 5. Apply bounds checking after zoom-to-cursor movement
                    cam.transform.position = ClampCameraPosition(newPosition);
                    cameraNavigatedThisFrame = true;
                }
                else
                {
                    // If no zoom, just clamp orthographic size
                    size = Mathf.Clamp(size, GetDynamicMinZoom(), GetDynamicMaxZoom());
                    mainCamera.orthographicSize = size;
                }

                // --- Panning with WASD/Arrow Keys ---
                Vector2 moveInput = UnityInputAdapter.GetKeyboardMoveVector();
                Vector3 move = new Vector3(moveInput.x, moveInput.y, 0f);
                if (move != Vector3.zero)
                {
                    // Scale movement by camera size for consistent feel
                    float scaledSpeed = moveSpeed * mainCamera.orthographicSize;
                    Vector3 frameDelta = Vector3.ClampMagnitude(move * scaledSpeed * panDeltaTime, maxPanDistance);
                    Vector3 newPosition = mainCamera.transform.position + frameDelta;
                    mainCamera.transform.position = ClampCameraPosition(newPosition);
                    cameraNavigatedThisFrame = true;
                }

                // --- Right-click drag pan ---
                if (!pointerOverUi && UnityInputAdapter.IsSecondaryPointerPressed())
                {
                    // Pointer delta is already frame-relative, so convert pixels directly into world-space movement.
                    float unitsPerPixel = (2f * mainCamera.orthographicSize) / Mathf.Max(1f, mainCamera.pixelHeight);
                    Vector2 pointerDelta = UnityInputAdapter.GetPointerDelta();
                    if (pointerDelta.sqrMagnitude > CameraPanDragThreshold)
                    {
                        Vector3 frameDelta = Vector3.ClampMagnitude(
                            new Vector3(-pointerDelta.x * unitsPerPixel, -pointerDelta.y * unitsPerPixel, 0f),
                            maxPanDistance);
                        Vector3 newPosition = mainCamera.transform.position + frameDelta;
                        mainCamera.transform.position = ClampCameraPosition(newPosition);
                        cameraNavigatedThisFrame = true;
                    }
                }

                if (cameraNavigatedThisFrame)
                {
                    ResolveCameraPanOnboarding(gameManager);
                }
                else
                {
                    TryShowCameraPanCoachmark(gameManager);
                }
            }
        }

        private void RefreshCameraPanOnboardingState(GameManager gameManager)
        {
            GameBoard currentBoard = gameManager != null ? gameManager.Board : null;
            if (ReferenceEquals(trackedOnboardingBoard, currentBoard))
            {
                return;
            }

            HideCameraPanCoachmarkImmediate();
            trackedOnboardingBoard = currentBoard;
            cameraPanCoachmarkElapsed = 0f;
            hasDismissedCameraPanCoachmarkThisGame = false;
        }

        private void TryShowCameraPanCoachmark(GameManager gameManager)
        {
            if (gameManager == null || gameManager.Board == null || IsCameraPanCoachmarkVisible())
            {
                return;
            }

            if (!NewPlayerTooltipRules.ShouldShowCameraPanIntro(
                    gameManager.ShouldForceFirstGameExperience,
                    gameManager.Board.CurrentRound,
                    GetHumanPlayerCountForOnboarding(gameManager),
                    hasDismissedCameraPanCoachmarkThisGame,
                    gameManager.IsFastForwarding))
            {
                return;
            }

            cameraPanCoachmarkElapsed += Time.unscaledDeltaTime;
            if (cameraPanCoachmarkElapsed < cameraPanCoachmarkDelaySeconds)
            {
                return;
            }

            EnsureCameraPanCoachmarkUi(gameManager);
            if (cameraPanCoachmarkRoot == null || cameraPanCoachmarkCanvasGroup == null)
            {
                return;
            }

            NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.CameraPanIntro);
            cameraPanCoachmarkTitleTextLabel.text = definition.Title;
            cameraPanCoachmarkBodyTextLabel.text = definition.Body;
            RefreshCameraPanCoachmarkLayout();
            cameraPanCoachmarkRoot.gameObject.SetActive(true);
            cameraPanCoachmarkRoot.SetAsLastSibling();
            cameraPanCoachmarkCanvasGroup.alpha = 1f;
            cameraPanCoachmarkCanvasGroup.blocksRaycasts = true;
            cameraPanCoachmarkCanvasGroup.interactable = true;
        }

        private void ResolveCameraPanOnboarding(GameManager gameManager)
        {
            hasDismissedCameraPanCoachmarkThisGame = true;

            if (gameManager != null && !gameManager.ShouldForceFirstGameExperience)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.CameraPanIntro);
            }

            HideCameraPanCoachmarkImmediate();
        }

        private bool IsCameraPanCoachmarkVisible()
        {
            return cameraPanCoachmarkRoot != null && cameraPanCoachmarkRoot.gameObject.activeSelf;
        }

        private int GetHumanPlayerCountForOnboarding(GameManager gameManager)
        {
            if (gameManager.CurrentGameMode == GameMode.Campaign)
            {
                return 1;
            }

            return Mathf.Max(0, gameManager.ConfiguredHumanPlayerCount);
        }

        private void EnsureCameraPanCoachmarkUi(GameManager gameManager)
        {
            if (cameraPanCoachmarkRoot != null)
            {
                return;
            }

            Canvas rootCanvas = ResolveRootCanvas(gameManager);
            if (rootCanvas == null)
            {
                return;
            }

            var rootObject = new GameObject("UI_CameraPanCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(rootCanvas.transform, false);

            cameraPanCoachmarkRoot = rootObject.GetComponent<RectTransform>();
            cameraPanCoachmarkRoot.anchorMin = new Vector2(0.5f, 0f);
            cameraPanCoachmarkRoot.anchorMax = new Vector2(0.5f, 0f);
            cameraPanCoachmarkRoot.pivot = new Vector2(0.5f, 0f);
            cameraPanCoachmarkRoot.anchoredPosition = new Vector2(0f, CameraPanCoachmarkBottomOffset);
            cameraPanCoachmarkRoot.sizeDelta = new Vector2(CameraPanCoachmarkWidth, CameraPanCoachmarkMinHeight);

            cameraPanCoachmarkCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            cameraPanCoachmarkCanvasGroup.alpha = 0f;
            cameraPanCoachmarkCanvasGroup.blocksRaycasts = false;
            cameraPanCoachmarkCanvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.State.Info, 0.14f);
            backgroundColor.a = 0.97f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.FocusOutline);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(14f, -42f);
            titleRect.offsetMax = new Vector2(-52f, -10f);

            cameraPanCoachmarkTitleTextLabel = titleObject.GetComponent<TextMeshProUGUI>();
            cameraPanCoachmarkTitleTextLabel.text = string.Empty;
            cameraPanCoachmarkTitleTextLabel.color = UIStyleTokens.Text.Primary;
            cameraPanCoachmarkTitleTextLabel.fontStyle = FontStyles.Bold;
            cameraPanCoachmarkTitleTextLabel.fontSize = 22f;
            cameraPanCoachmarkTitleTextLabel.alignment = TextAlignmentOptions.Left;
            cameraPanCoachmarkTitleTextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            cameraPanCoachmarkTitleTextLabel.overflowMode = TextOverflowModes.Ellipsis;
            cameraPanCoachmarkTitleTextLabel.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);

            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(CameraPanCoachmarkBodyHorizontalPadding, CameraPanCoachmarkBodyBottomPadding);
            bodyRect.offsetMax = new Vector2(-CameraPanCoachmarkBodyHorizontalPadding, -CameraPanCoachmarkBodyTopReservedHeight);

            cameraPanCoachmarkBodyTextLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            cameraPanCoachmarkBodyTextLabel.color = UIStyleTokens.Text.Primary;
            cameraPanCoachmarkBodyTextLabel.fontSize = 17f;
            cameraPanCoachmarkBodyTextLabel.alignment = TextAlignmentOptions.TopLeft;
            cameraPanCoachmarkBodyTextLabel.textWrappingMode = TextWrappingModes.Normal;
            cameraPanCoachmarkBodyTextLabel.overflowMode = TextOverflowModes.Overflow;
            cameraPanCoachmarkBodyTextLabel.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);

            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(34f, 34f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);

            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = UIStyleTokens.Surface.PanelElevated;

            cameraPanCoachmarkCloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(cameraPanCoachmarkCloseButton);
            cameraPanCoachmarkCloseButton.onClick.RemoveAllListeners();
            cameraPanCoachmarkCloseButton.onClick.AddListener(OnCameraPanCoachmarkDismissed);

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeObject.transform, false);

            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = 20f;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                cameraPanCoachmarkTitleTextLabel.font = TMP_Settings.defaultFontAsset;
                cameraPanCoachmarkBodyTextLabel.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void RefreshCameraPanCoachmarkLayout()
        {
            if (cameraPanCoachmarkRoot == null || cameraPanCoachmarkBodyTextLabel == null)
            {
                return;
            }

            float availableBodyWidth = Mathf.Max(
                1f,
                CameraPanCoachmarkWidth - (2f * CameraPanCoachmarkBodyHorizontalPadding));
            Vector2 bodyPreferredSize = cameraPanCoachmarkBodyTextLabel.GetPreferredValues(
                cameraPanCoachmarkBodyTextLabel.text,
                availableBodyWidth,
                0f);
            float requiredHeight = CameraPanCoachmarkBodyTopReservedHeight
                + bodyPreferredSize.y
                + CameraPanCoachmarkBodyBottomPadding;

            cameraPanCoachmarkRoot.sizeDelta = new Vector2(
                CameraPanCoachmarkWidth,
                Mathf.Max(CameraPanCoachmarkMinHeight, requiredHeight));

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cameraPanCoachmarkRoot);
        }

        private Canvas ResolveRootCanvas(GameManager gameManager)
        {
            if (gameManager?.GameUI != null)
            {
                Canvas uiCanvas = gameManager.GameUI.GetComponentInParent<Canvas>();
                if (uiCanvas != null)
                {
                    return uiCanvas.rootCanvas;
                }
            }

            Canvas anyCanvas = Object.FindAnyObjectByType<Canvas>();
            return anyCanvas != null ? anyCanvas.rootCanvas : null;
        }

        private void OnCameraPanCoachmarkDismissed()
        {
            hasDismissedCameraPanCoachmarkThisGame = true;

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null && !gameManager.ShouldForceFirstGameExperience)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.CameraPanIntro);
            }

            HideCameraPanCoachmarkImmediate();
        }

        private void HideCameraPanCoachmarkImmediate()
        {
            if (cameraPanCoachmarkCanvasGroup != null)
            {
                cameraPanCoachmarkCanvasGroup.alpha = 0f;
                cameraPanCoachmarkCanvasGroup.blocksRaycasts = false;
                cameraPanCoachmarkCanvasGroup.interactable = false;
            }

            if (cameraPanCoachmarkRoot != null)
            {
                cameraPanCoachmarkRoot.gameObject.SetActive(false);
            }
        }

        private float GetPanDeltaTime()
        {
            return Mathf.Min(Time.unscaledDeltaTime, maxPanInputDeltaTime);
        }

        private float GetZoomSpeedForCurrentBoard()
        {
            if (gameManager?.Board == null)
            {
                return zoomSpeed;
            }

            float boardDimension = Mathf.Max(gameManager.Board.Width, gameManager.Board.Height);
            float fullSensitivityDimension = Mathf.Max(minBoardDimensionForZoomScaling, maxBoardDimensionForZoomScaling);
            float t = fullSensitivityDimension <= minBoardDimensionForZoomScaling
                ? 1f
                : Mathf.InverseLerp(minBoardDimensionForZoomScaling, fullSensitivityDimension, boardDimension);

            float sensitivityScale = Mathf.Lerp(minZoomSensitivityScale, 1f, t);
            return zoomSpeed * sensitivityScale;
        }

        private float GetMaxPanDistancePerFrame(Camera camera)
        {
            if (camera == null)
            {
                return maxPanDistancePerFrame;
            }

            float zoomScale = camera.orthographicSize / Mathf.Max(0.01f, GetDynamicMinZoom());
            return maxPanDistancePerFrame * Mathf.Max(1f, zoomScale);
        }

        private float GetDynamicMinZoom()
        {
            return minZoom;
        }

        private float GetDynamicMaxZoom()
        {
            float dynamicMaxZoom = maxZoom;

            if (ShouldApplySmallBoardZoomCap() && cameraCenterer != null && cameraCenterer.HasInitialFraming)
            {
                float framedZoomCap = cameraCenterer.InitialOrthographicSize * Mathf.Max(1f, maxZoomOutRelativeToInitialFraming);
                dynamicMaxZoom = Mathf.Min(dynamicMaxZoom, framedZoomCap);
            }

            return Mathf.Max(GetDynamicMinZoom(), dynamicMaxZoom);
        }

        private bool ShouldApplySmallBoardZoomCap()
        {
            Camera camera = Camera.main;
            if (camera == null || gameManager?.Board == null || cameraCenterer == null || !cameraCenterer.HasInitialFraming)
            {
                return false;
            }

            GetBoardExtents(out float boardMinX, out float boardMaxX, out float boardMinY, out float boardMaxY);
            float boardWidth = boardMaxX - boardMinX;
            float boardHeight = boardMaxY - boardMinY;
            float viewWidthAtInitialFraming = 2f * cameraCenterer.InitialOrthographicSize * camera.aspect;
            float viewHeightAtInitialFraming = 2f * cameraCenterer.InitialOrthographicSize;

            return boardWidth <= viewWidthAtInitialFraming && boardHeight <= viewHeightAtInitialFraming;
        }

        private void GetBoardExtents(out float minX, out float maxX, out float minY, out float maxY)
        {
            int boardWidth = gameManager.Board.Width;
            int boardHeight = gameManager.Board.Height;
            int visualPaddingTiles = Mathf.Max(0, gameManager.gridVisualizer?.CurrentBoardVisualPaddingTiles ?? 0);

            minX = -visualPaddingTiles;
            minY = -visualPaddingTiles;
            maxX = boardWidth + visualPaddingTiles;
            maxY = boardHeight + visualPaddingTiles;
        }

        /// <summary>
        /// Clamp camera movement against the actual board footprint rather than a loose radius.
        /// Small boards keep most of the toast visible; large boards still allow edge exploration.
        /// </summary>
        private Vector3 ClampCameraPosition(Vector3 desiredPosition)
        {
            Camera camera = Camera.main;
            if (camera == null || gameManager?.Board == null)
            {
                return desiredPosition;
            }

            GetBoardExtents(out float boardMinX, out float boardMaxX, out float boardMinY, out float boardMaxY);

            float viewHalfHeight = camera.orthographicSize;
            float viewHalfWidth = camera.orthographicSize * camera.aspect;
            float boardWidth = boardMaxX - boardMinX;
            float boardHeight = boardMaxY - boardMinY;

            float clampedX = ClampAxis(
                desiredPosition.x,
                boardMinX,
                boardMaxX,
                boardWidth,
                viewHalfWidth,
                allowFullBoardOffscreen: false);
            float clampedY = ClampAxis(
                desiredPosition.y,
                boardMinY,
                boardMaxY,
                boardHeight,
                viewHalfHeight,
                allowFullBoardOffscreen: false);

            return new Vector3(clampedX, clampedY, desiredPosition.z);
        }

        private float ClampAxis(
            float desiredCenter,
            float boardMin,
            float boardMax,
            float boardSize,
            float viewHalfSpan,
            bool allowFullBoardOffscreen)
        {
            float viewSpan = viewHalfSpan * 2f;

            if (boardSize <= viewSpan)
            {
                float visibleRequirement = boardSize * minVisibleSmallBoardFraction;
                float minCenter = boardMin + visibleRequirement - viewHalfSpan;
                float maxCenter = boardMax - visibleRequirement + viewHalfSpan;
                return Mathf.Clamp(desiredCenter, minCenter, maxCenter);
            }

            if (!autoCalculateBounds)
            {
                float boardCenter = (boardMin + boardMax) * 0.5f;
                return Mathf.Clamp(desiredCenter, boardCenter - maxDistanceFromCenter, boardCenter + maxDistanceFromCenter);
            }

            float edgePadding = allowFullBoardOffscreen ? autoBoundsPadding : Mathf.Min(autoBoundsPadding, boardSize * 0.25f);
            float minLargeBoardCenter = boardMin + viewHalfSpan - edgePadding;
            float maxLargeBoardCenter = boardMax - viewHalfSpan + edgePadding;
            return Mathf.Clamp(desiredCenter, minLargeBoardCenter, maxLargeBoardCenter);
        }
    }
}
