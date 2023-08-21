#if !UNITY_EDITOR
using Google.XR.Cardboard;
using UnityEngine.XR.Management;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SpatialTracking;

namespace XRCardboard
{
    public class XRCardboardController : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform = default;
        [SerializeField] GameObject vrGroup = default;
        [SerializeField] GameObject standardGroup = default;
        [SerializeField] XRCardboardInputModule vrInputModule = default;
        [SerializeField] StandaloneInputModule standardInputModule = default;
        [SerializeField, Range(.05f, 2)] 
        private float dragRate = .2f;

        private TrackedPoseDriver _poseDriver;
        private Camera _camera;
        private Quaternion _initialRotation;
        private Quaternion _attitude;
        private Vector2 _dragDegrees;
        private float _defaultFov;

#if UNITY_EDITOR
        private Vector3 _lastMousePos;
        private bool _vrActive;
        public bool VRActive => _vrActive;
#endif

        void Awake()
        {
            _camera = cameraTransform.GetComponent<Camera>();
            _poseDriver = cameraTransform.GetComponent<TrackedPoseDriver>();
            _defaultFov = _camera.fieldOfView;
            _initialRotation = cameraTransform.rotation;
        }

        void Start()
        {
#if UNITY_EDITOR
            SetObjects(_vrActive);
#else
        SetObjects(UnityEngine.XR.XRSettings.enabled);
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Escape))
#else
        if (Api.IsCloseButtonPressed)
#endif
                DisableVR();

#if UNITY_EDITOR
            if (_vrActive)
                SimulateVR();
            else
                SimulateDrag();
#else
        if (UnityEngine.XR.XRSettings.enabled)
            return;

        CheckDrag();
#endif

            _attitude = _initialRotation * Quaternion.Euler(_dragDegrees.x, 0, 0);
            cameraTransform.rotation = Quaternion.Euler(0, -_dragDegrees.y, 0) * _attitude;
        }

        public void ResetCamera()
        {
            cameraTransform.rotation = _initialRotation;
            _dragDegrees = Vector2.zero;
        }

        public void DisableVR()
        {
#if UNITY_EDITOR
            _vrActive = false;
#else
        var xrManager = XRGeneralSettings.Instance.Manager;
        if (xrManager.isInitializationComplete)
        {
            xrManager.StopSubsystems();
            xrManager.DeinitializeLoader();
        }
#endif
            SetObjects(false);
            ResetCamera();
            _camera.ResetAspect();
            _camera.fieldOfView = _defaultFov;
            _camera.ResetProjectionMatrix();
            _camera.ResetWorldToCameraMatrix();
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        public void EnableVR() => EnableVRCoroutine();

        public Coroutine EnableVRCoroutine()
        {
            return StartCoroutine(enableVRRoutine());

            IEnumerator enableVRRoutine()
            {
                SetObjects(true);
#if UNITY_EDITOR
                yield return null;
                _vrActive = true;
#else
            var xrManager = XRGeneralSettings.Instance.Manager;
            if (!xrManager.isInitializationComplete)
                yield return xrManager.InitializeLoader();
            xrManager.StartSubsystems();
#endif
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                ResetCamera();
            }
        }

        void SetObjects(bool vrActive)
        {
            standardGroup.SetActive(!vrActive);
            vrGroup.SetActive(vrActive);
            standardInputModule.enabled = !vrActive;
            vrInputModule.enabled = vrActive;
            _poseDriver.enabled = vrActive;
        }

        void CheckDrag()
        {
            if (Input.touchCount <= 0)
                return;

            Touch touch = Input.GetTouch(0);
            _dragDegrees.x += touch.deltaPosition.y * dragRate;
            _dragDegrees.y += touch.deltaPosition.x * dragRate;
        }

#if UNITY_EDITOR
        void SimulateVR()
        {
            var mousePos = Input.mousePosition;
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                var delta = mousePos - _lastMousePos;
                _dragDegrees.x -= delta.y * dragRate;
                _dragDegrees.y -= delta.x * dragRate;
            }
            _lastMousePos = mousePos;
        }

        void SimulateDrag()
        {
            var mousePos = Input.mousePosition;
            if (Input.GetMouseButton(0))
            {
                var delta = mousePos - _lastMousePos;
                _dragDegrees.x += delta.y * dragRate;
                _dragDegrees.y += delta.x * dragRate;
            }
            _lastMousePos = mousePos;
        }
#endif
    }
}