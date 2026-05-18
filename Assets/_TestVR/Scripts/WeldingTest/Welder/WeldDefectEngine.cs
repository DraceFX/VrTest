using UnityEngine;

public class WeldDefectEngine : MonoBehaviour
{
    [SerializeField] private WeldQualityAssessor _qualityAssessor;

    // Обрабатывает дефекты для текущего кадра сварки.
    public void ProcessDefects(WeldMeshBuilder builder, WeldProcessModel model, float power, float finalQuality, Vector3 point, Vector3 normal, Electrode electrode, RaycastHit hit)
    {
        float defectChance = 1f - finalQuality;

        // Прожог
        if (model.IsBurning(power))
        {
            builder.AddBurn(point, normal);
            _qualityAssessor?.RegisterDefect();

            int spatters = Random.Range(1, 3);
            for (int i = 0; i < spatters; i++)
                builder.AddSpatter(point, normal);
        }

        // Поры
        if (Random.value < defectChance * 0.10f)
        {
            Vector3 poreOffset = normal * 0.001f;
            builder.AddPore(point + poreOffset, normal);
            _qualityAssessor?.RegisterDefect();
        }

        // Брызги
        if (Random.value < defectChance * 0.12f)
        {
            builder.AddSpatter(point, normal);
            _qualityAssessor?.RegisterDefect();
            if (Random.value < 0.25f)
                builder.AddSpatter(point, normal);
        }

        // Недогрев
        bool underpowered = power < model.OptimalPower * 0.75f;
        if (underpowered)
        {
            if (Random.value < 0.3f)
                return; // прерывистый шов

            if (Random.value < 0.5f)
            {
                builder.AddSpatter(point, normal);
                _qualityAssessor?.RegisterDefect();
            }
        }

        // Нестабильная дуга
        float arcDistance = Vector3.Distance(electrode.Tip.position, hit.point);
        bool unstableArc = arcDistance > electrode.WeldDistance * 0.8f;
        if (unstableArc)
        {
            int extraSpatter = Random.Range(2, 6);
            for (int i = 0; i < extraSpatter; i++)
            {
                builder.AddSpatter(point, normal);
                _qualityAssessor?.RegisterDefect();
            }
            if (Random.value < 0.25f)
                return; // пропуск валика
        }
    }
}