using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InteractableTrigger : MonoBehaviour, IObjectEnter
{
    [Header("Filter")]
    [SerializeField] private string _tag;
    [SerializeField] private string _id;

    [Header("Settings")]
    [SerializeField] private UseCondition _conditions;
    [SerializeField] private float _snapSpeed = 5f;
    [SerializeField] private float _autoSnapDistance = 0.2f;
    [SerializeField] private Transform _snapTransform;


    [Header("Hologram")]
    [SerializeField] private Material _hologramMaterial;

    private InteractableObject _currentObject;
    private GameObject _hologramInstance;

    private bool _isSnapping;

    private Transform SnapTarget => _snapTransform != null ? _snapTransform : transform;

    public string TAG { get => _tag; set => _tag = value; }
    public string Id { get => _id; set => _id = value; }

    private void OnTriggerEnter(Collider other)
    {
        if (_isSnapping) return;
        if (_currentObject != null) return;
        if (!other.CompareTag(_tag)) return;

        var obj = other.GetComponent<InteractableObject>();
        if (obj == null || obj.Id != _id) return;

        _currentObject = obj;
CreateHologram(obj.gameObject);
Subscribe(obj);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_currentObject == null) return;

        if (other.gameObject == _currentObject.gameObject)
        {
           Unsubscribe(_currentObject);
ClearHologram();
_currentObject = null;
        }
    }

    private void Subscribe(InteractableObject obj)
{
    if (obj.Grab != null)
    {
        obj.Grab.selectExited.AddListener(OnReleased);
obj.Grab.activated.AddListener(OnActivated);
    }
}

private void Unsubscribe(InteractableObject obj)
{
    if (obj != null && obj.Grab != null)
    {
        obj.Grab.selectExited.RemoveListener(OnReleased);
obj.Grab.activated.RemoveListener(OnActivated);
    }
}

private void OnReleased(SelectExitEventArgs args)
{
    if (!_conditions.HasFlag(UseCondition.ReleaseGrab)) return;
    if (_currentObject == null || _isSnapping) return;

    StartSnap();
}

private void OnActivated(ActivateEventArgs args)
{
    if (!_conditions.HasFlag(UseCondition.TriggerPress)) return;
    if (_currentObject == null || _isSnapping) return;

    StartSnap();
}

private void StartSnap()
{
    if (_isSnapping) return;

    _isSnapping = true;
    ClearHologram();
    StartCoroutine(SnapAndLock());
}

    // private bool CheckConditions()
    // {
    //     var grab = _currentObject.Grab;

    //     // Release Grab
    //     if (_conditions.HasFlag(UseCondition.ReleaseGrab))
    //     {
    //         if (grab != null && !grab.isSelected)
    //             return true;
    //     }

    //     // Trigger Press
    //     if (_conditions.HasFlag(UseCondition.TriggerPress))
    //     {
    //         if (grab != null && grab.isSelected)
    //         {
    //             var interactor = grab.firstInteractorSelecting;

    //             if (interactor is XRDirectInteractor direct)
    //             {
    //                 bool isPressed = direct.xrController.activateInteractionState.active;

    //                 if (isPressed && !_wasTriggerPressed)
    //                 {
    //                     _wasTriggerPressed = true;
    //                     return true;
    //                 }

    //                 _wasTriggerPressed = isPressed;
    //             }
    //         }
    //         else
    //         {
    //             _wasTriggerPressed = false;
    //         }
    //     }

    //     // AutoSnap
    //     if (_conditions.HasFlag(UseCondition.AutoSnap))
    //     {
    //         float dist = Vector3.Distance(_currentObject.transform.position, SnapTarget.position);

    //         if (dist < _autoSnapDistance)
    //             return true;
    //     }

    //     return false;
    // }

    private void FixedUpdate()
{
    if (!_conditions.HasFlag(UseCondition.AutoSnap)) return;
    if (_currentObject == null || _isSnapping) return;

    float dist = Vector3.Distance(
        _currentObject.transform.position,
        SnapTarget.position
    );

    if (dist < _autoSnapDistance)
    {
        StartSnap();
    }
}
    

    private void CreateHologram(GameObject source)
    {
        ClearHologram();

        if (_hologramInstance != null) return;

        _hologramInstance = Instantiate(source, SnapTarget.position, SnapTarget.rotation);

        var grab = _hologramInstance.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.enabled = false;

        var rb = _hologramInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in _hologramInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        ApplyMaterial(_hologramInstance);
    }

    private void ApplyMaterial(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            var shared = r.sharedMaterials;
            var mats = new Material[shared.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = _hologramMaterial;
            }

            r.materials = mats;
        }
    }

    private void ClearHologram()
    {
        if (_hologramInstance == null) return;

        Destroy(_hologramInstance);
        _hologramInstance = null;
    }

    private IEnumerator SnapAndLock()
    {
        var obj = _currentObject.transform;
        var grab = _currentObject.Grab;
        var rb = _currentObject.GetComponent<Rigidbody>();

        if (grab != null)
            grab.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 startPos = obj.position;
        Quaternion startRot = obj.rotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * _snapSpeed;

            obj.position = Vector3.Lerp(startPos, SnapTarget.position, t);
            obj.rotation = Quaternion.Lerp(startRot, SnapTarget.rotation, t);

            yield return null;
        }

        obj.SetParent(SnapTarget);
        obj.localPosition = Vector3.zero;
        obj.localRotation = Quaternion.identity;

        InteractionManager.Instance.NotifyUsed(this);

        Unsubscribe(_currentObject);
        _currentObject = null;
        _isSnapping = false;
    }
}
