using UnityEngine;

[RequireComponent(typeof(DetectableGameObject), typeof(Outline))]
public class OutlineCharacter : MonoBehaviour
{
    [SerializeField] private Outline outline;
    [SerializeField] private DetectableGameObject detectableGameObject;
    [SerializeField] private float outlineWidth = 1f;
    [SerializeField] private bool alwaysVisible = false;
    private bool enableVisibility = true;
    private bool forceVisibility = false;

    private CameraZoom cameraZoom;
    private float zoomValue = 0.5f;
    private bool isLookingAt => detectableGameObject.isLookingAt;
    private bool isZooming => cameraZoom != null && cameraZoom.IsZooming(zoomValue);

    void Start()
    {
        cameraZoom = Camera.main.GetComponent<CameraZoom>();

        CameraZoomSettings settings = GlobalCameraSettings.Instance.GetSettings<CameraZoomSettings>(ObjectType.Character);
        zoomValue = settings.zoomValue;
    }

    void Update()
    {
        if ((((isLookingAt && isZooming) || alwaysVisible) && enableVisibility) || forceVisibility)
        {
            outline.OutlineWidth = outlineWidth;
        }
        else
        {
            outline.OutlineWidth = 0f;
        }
    }

    public void DisableAlwaysVisible()
    {
        alwaysVisible = false;
    }

    public void ForceVisibility(bool visible)
    {
        forceVisibility = visible;
    }

    public void EnableVisibility(bool visible)
    {
        enableVisibility = visible;
    }
}
