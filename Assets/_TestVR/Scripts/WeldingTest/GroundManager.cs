using System.Collections.Generic;
using UnityEngine;

public static class GroundManager
{
    private static List<Weldable> allWeldables = new List<Weldable>();

    public static void RegisterWeldable(Weldable w)
    {
        if (!allWeldables.Contains(w))
            allWeldables.Add(w);
    }

    public static void UnregisterWeldable(Weldable w)
    {
        allWeldables.Remove(w);
    }

    public static void NotifyGroundingChanged()
    {
        // Сбрасываем все заземления
        foreach (var w in allWeldables)
            w.SetGroundedInternal(false);

        // Итеративно распространяем заземление через контакты
        bool changed = true;
        int maxIter = 20;
        while (changed && maxIter-- > 0)
        {
            changed = false;
            foreach (var w in allWeldables)
            {
                bool wasGrounded = w.IsGrounded;
                w.RefreshGrounding();
                if (w.IsGrounded != wasGrounded)
                    changed = true;
            }
        }
    }
}