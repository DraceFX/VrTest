using System.Collections.Generic;
using UnityEngine;

public class Weldable : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private bool _isGrounded = false;

    [Header("Свойства сварки")]
    [SerializeField] private WeldProcessModel _processModel;

    [Header("Поиск контактов")]
    [SerializeField] private CheckShape _checkShape = CheckShape.Box;
    [SerializeField] private float _sizeMultiplier = 1.0f;


    private Collider _mainCollider;
    private bool _isClamped = false;
    private enum CheckShape { Sphere, Box }

    public WeldProcessModel ProcessModel => _processModel;
    public Rigidbody Rigidbody
    {
        get
        {
            if (_rb == null)
                _rb = GetComponentInParent<Rigidbody>();
            return _rb;
        }
    }

    public bool IsGrounded
    {
        get
        {
            if (_isClamped) return true;
            if (_mainCollider == null) return false;

            Vector3 center = _mainCollider.bounds.center;
            Collider[] hits;

            if (_checkShape == CheckShape.Sphere)
            {
                // Радиус = максимальная полуось коллайдера, умноженная на множитель
                float radius = Mathf.Max(
                    _mainCollider.bounds.extents.x,
                    _mainCollider.bounds.extents.y,
                    _mainCollider.bounds.extents.z
                ) * _sizeMultiplier;

                hits = Physics.OverlapSphere(center, radius);
            }
            else // Box
            {
                // Половина размера (extents), умноженная на множитель
                Vector3 halfExtents = _mainCollider.bounds.extents;
                halfExtents.Scale(new Vector3(_sizeMultiplier, _sizeMultiplier, _sizeMultiplier));
                hits = Physics.OverlapBox(center, halfExtents, _mainCollider.transform.rotation);
            }

            HashSet<Weldable> visited = new HashSet<Weldable>();
            return CheckGroundedRecursive(visited, hits);
        }
    }

    private bool CheckGroundedRecursive(HashSet<Weldable> visited, Collider[] nearby)
    {
        if (!visited.Add(this)) return false;

        foreach (var col in nearby)
        {
            if (col == null || col == _mainCollider) continue;

            // Прямой контакт с заземлённой поверхностью
            GroundSurface surface = col.GetComponentInParent<GroundSurface>();
            if (surface != null && surface.IsActive)
                return true;

            // Контакт с другим Weldable, который заземлён
            Weldable other = col.GetComponentInParent<Weldable>();
            if (other != null && other != this)
            {
                if (other._isClamped) return true;
                if (other.CheckGroundedRecursive(visited, nearby)) return true;
            }
        }
        return false;
    }

    private void Awake()
    {
        if (_rb == null) _rb = GetComponentInParent<Rigidbody>();

        _mainCollider = GetComponentInChildren<Collider>();
        if (_mainCollider == null)
            Debug.LogError($"Weldable на {name} не имеет коллайдера. Поиск контактов не будет работать.");
    }

    public void SetClamped(bool value)
    {
        _isClamped = value;
    }

    private void OnDrawGizmosSelected()
    {
        if (_mainCollider == null) return;

        Gizmos.color = Color.green;
        Vector3 center = _mainCollider.bounds.center;

        if (_checkShape == CheckShape.Sphere)
        {
            float radius = Mathf.Max(
                _mainCollider.bounds.extents.x,
                _mainCollider.bounds.extents.y,
                _mainCollider.bounds.extents.z
            ) * _sizeMultiplier;
            Gizmos.DrawWireSphere(center, radius);
        }
        else
        {
            Vector3 size = _mainCollider.bounds.size * _sizeMultiplier;
            Gizmos.matrix = Matrix4x4.TRS(center, _mainCollider.transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }

    // private void OnValidate()
    // {
    //     _contactCheckRadius = new Vector3(gameObject.transform.localScale.x, gameObject.transform.localScale.y, gameObject.transform.localScale.z);
    // }
}