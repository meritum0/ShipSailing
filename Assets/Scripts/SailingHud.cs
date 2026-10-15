using UnityEngine;
using UnityEngine.UIElements;

namespace SailingGame
{
    /// <summary>
    /// Shows the sail opening and rotation in two UI Toolkit labels.
    /// Each label doubles as a gauge: its background image is used as a fill bar.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class SailingHud : MonoBehaviour
    {
        private const string OpeningLabelName = "sail-opening-label";   // same name in UI Builder Window
        private const string RotationLabelName = "sail-rotation-label"; // same name in UI Builder Window

        [SerializeField] private Sail _sail;

        private PanelRenderer _panelRenderer;
        private Label _openingLabel;
        private Label _rotationLabel;
        private int _shownOpening = int.MinValue;
        private int _shownRotation = int.MinValue;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnEnable()
        {
            _sail.OpeningChanged += UpdateOpening;
            _sail.AngleChanged += UpdateRotation;
        }

        private void OnDisable()
        {
            _sail.OpeningChanged -= UpdateOpening;
            _sail.AngleChanged -= UpdateRotation;
        }

        private void OnDestroy()
        {
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        // Called when the visual tree is created or reloaded, so the labels are looked up again.
        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            _openingLabel = root.Q<Label>(OpeningLabelName);
            _rotationLabel = root.Q<Label>(RotationLabelName);

            // The rotation fill is measured in pixels, so it is redrawn whenever the label is resized.
            _rotationLabel?.RegisterCallback<GeometryChangedEvent>(OnRotationLabelGeometryChanged);

            // The labels were just (re)created, so force a refresh of their texts and fills.
            _shownOpening = int.MinValue;
            _shownRotation = int.MinValue;
            UpdateOpening(_sail.Opening);
            UpdateRotation(_sail.AngleDegrees);
        }

        private void OnRotationLabelGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateRotation(_sail.AngleDegrees);
        }

        private void UpdateOpening(float opening)
        {
            if (_openingLabel == null)
            {
                return;
            }

            // The fill grows from the left edge, so only its width has to change.
            float percent = opening / Sail.MaxOpening * 100f;
            _openingLabel.style.backgroundSize = new StyleBackgroundSize(
                new BackgroundSize(Length.Percent(percent), Length.Percent(100f)));

            int shown = Mathf.RoundToInt(opening);
            if (shown == _shownOpening)
            {
                return;
            }

            // The text is rebuilt only when the displayed value changes.
            _shownOpening = shown;
            _openingLabel.text = $"SAIL OPENING  {shown}%";
        }

        private void UpdateRotation(float angleDegrees)
        {
            if (_rotationLabel == null)
            {
                return;
            }

            UpdateRotationFill(angleDegrees);

            int shown = Mathf.RoundToInt(angleDegrees);
            if (shown == _shownRotation)
            {
                return;
            }

            _shownRotation = shown;
            string side = shown > 0 ? " RIGHT" : shown < 0 ? " LEFT" : string.Empty;
            _rotationLabel.text = $"SAIL ROTATION  {Mathf.Abs(shown)}°{side}";
        }

        private void UpdateRotationFill(float angleDegrees)
        {
            float labelWidth = _rotationLabel.resolvedStyle.width;
            if (float.IsNaN(labelWidth))
            {
                // The layout is not ready yet. A GeometryChangedEvent will redraw the fill.
                return;
            }

            // The fill starts in the middle of the label and grows towards the side the sail is rotated to.
            float halfWidth = labelWidth * 0.5f;
            float fillWidth = Mathf.Abs(angleDegrees) / Sail.MaxAngleDegrees * halfWidth;
            float fillLeft = angleDegrees >= 0f ? halfWidth : halfWidth - fillWidth;

            _rotationLabel.style.backgroundSize = new StyleBackgroundSize(
                new BackgroundSize(new Length(fillWidth), Length.Percent(100f)));
            _rotationLabel.style.backgroundPositionX = new StyleBackgroundPosition(
                new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(fillLeft)));
        }
    }
}