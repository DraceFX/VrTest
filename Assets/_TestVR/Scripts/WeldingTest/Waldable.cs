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
    private BoxCollider _boxCollider;
    private SphereCollider _sphereCollider;
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
                float radius = GetSphereCheckRadius();
                hits = Physics.OverlapSphere(center, radius);
            }
            else // Box
            {
                Vector3 halfExtents = GetBoxCheckHalfExtents();
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

            GroundSurface surface = col.GetComponentInParent<GroundSurface>();
            if (surface != null && surface.IsActive) return true;

            Weldable other = col.GetComponentInParent<Weldable>();
            if (other != null && other != this)
            {
                if (other._isClamped) return true;

                // Рекурсивно проверяем other, используя ЕГО соседей
                if (other.CheckGroundedRecursive(visited, other.GetNearbyColliders())) return true;
            }
        }
        return false;
    }

    private Collider[] GetNearbyColliders()
    {
        if (_mainCollider == null) return new Collider[0];

        Vector3 center = _mainCollider.bounds.center;
        if (_checkShape == CheckShape.Sphere)
        {
            float radius = GetSphereCheckRadius();
            return Physics.OverlapSphere(center, radius);
        }
        else
        {
            Vector3 halfExtents = GetBoxCheckHalfExtents();
            return Physics.OverlapBox(center, halfExtents, _mainCollider.transform.rotation);
        }
    }

    // Вычисляет постоянный радиус для сферы, не зависящий от поворота
    private float GetSphereCheckRadius()
    {
        if (_sphereCollider != null)
        {
            // Для SphereCollider: радиус * максимальный масштаб (если неравномерный масштаб, охватываем всю фигуру)
            Vector3 scale = _mainCollider.transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return _sphereCollider.radius * maxScale * _sizeMultiplier;
        }
        else if (_boxCollider != null)
        {
            // Для BoxCollider: радиус = длина половины диагонали мирового бокса (постоянна при повороте)
            Vector3 worldHalfExtents = Vector3.Scale(_boxCollider.size * 0.5f, _mainCollider.transform.lossyScale);
            return worldHalfExtents.magnitude * _sizeMultiplier;
        }
        else
        {
            // Запасной вариант (менее точный для непрямоугольных коллайдеров)
            // Использует AABB, который меняется при повороте – поэтому выводим предупреждение
            Debug.LogWarning($"Weldable на {name}: для точного радиуса сферы используйте BoxCollider или SphereCollider.", this);
            Vector3 ext = _mainCollider.bounds.extents;
            return Mathf.Max(ext.x, ext.y, ext.z) * _sizeMultiplier;
        }
    }

    // Вычисляет половину размера для бокса, не зависящую от поворота
    private Vector3 GetBoxCheckHalfExtents()
    {
        if (_boxCollider != null)
        {
            // Реальный размер BoxCollider в мировых единицах (ширина, высота, глубина), умноженный на множитель
            return Vector3.Scale(_boxCollider.size * 0.5f, _mainCollider.transform.lossyScale) * _sizeMultiplier;
        }
        else
        {
            // Запасной вариант – AABB экстенты (меняются при повороте)
            Debug.LogWarning($"Weldable на {name}: режим Box лучше работает с BoxCollider. Область может быть некорректной.", this);
            return _mainCollider.bounds.extents * _sizeMultiplier;
        }
    }

    private void Awake()
    {
        if (_rb == null) _rb = GetComponentInParent<Rigidbody>();

        _mainCollider = GetComponentInChildren<Collider>();
        _boxCollider = _mainCollider as BoxCollider;
        _sphereCollider = _mainCollider as SphereCollider;

        if (_mainCollider == null)
            Debug.LogError($"Weldable на {name} не имеет коллайдера. Поиск контактов не будет работать.");
        else if (_checkShape == CheckShape.Box && _boxCollider == null)
            Debug.LogWarning($"Weldable на {name}: выбран режим Box, но коллайдер не BoxCollider. Визуализация и проверка могут быть неточными.", this);
        else if (_checkShape == CheckShape.Sphere && _sphereCollider == null && _boxCollider == null)
            Debug.LogWarning($"Weldable на {name}: для точной сферы используйте SphereCollider или BoxCollider.", this);
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
            float radius = GetSphereCheckRadius();
            Gizmos.DrawWireSphere(center, radius);
        }
        else // Box
        {
            Vector3 halfExtents = GetBoxCheckHalfExtents();
            // Размер бокса для Gizmos — полный, т.е. halfExtents * 2
            Vector3 size = halfExtents * 2f;
            Gizmos.matrix = Matrix4x4.TRS(center, _mainCollider.transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}