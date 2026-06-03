using UnityEngine;

public interface IWeldingTool
{
    // Геометрия
    public Vector3 TipPosition { get; }
    public Vector3 TipForward { get; }

    // Параметры дуги (зависят от инструмента)
    public float StrikeMinGap { get; }     // минимальный зазор, при котором начинается залипание
    public float ArcMaxDistance { get; }   // максимальная дистанция для поддержания дуги
    public float WeldDistance { get; }     // идеальная дистанция сварки

    // Проверка попадания в рабочую зону дуги
    public bool IsInArcGap(float distance);

    // Физический контакт с поверхностью (если есть)
    public bool TryGetArcContact(out RaycastHit hit, out float distance);

    // Расходуемый ли инструмент?
    public bool IsConsumable { get; }
    public void Consume(float amount);      // если да — израсходовать

    public void StartWeldEffects(float power, float optimal);
    public void StopWeldEffects();
    public void UpdateWeldEffects(float power);
}
