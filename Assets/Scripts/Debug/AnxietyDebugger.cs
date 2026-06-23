using UnityEngine;



public class AnxietyDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private KeyCode increaseAnxietyKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode decreaseAnxietyKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode maxAnxietyKey = KeyCode.P;
    [SerializeField] private KeyCode triggerFaintKey = KeyCode.O;
    [SerializeField] private KeyCode resetFaintsKey = KeyCode.R;

    [SerializeField] private float anxietyChangeAmount = 10f;

    private void Update()
    {
        if (AnxietySystem.Instance == null) return;

        // Aumentar ansiedad
        if (Input.GetKeyDown(increaseAnxietyKey))
        {
            AnxietySystem.Instance.ModifyAnxiety(anxietyChangeAmount);
            Debug.Log($"[Debugger] Ansiedad aumentada. Actual: {AnxietySystem.Instance.AnxietyValue:F1}");
        }

        // Disminuir ansiedad
        if (Input.GetKeyDown(decreaseAnxietyKey))
        {
            AnxietySystem.Instance.ModifyAnxiety(-anxietyChangeAmount);
            Debug.Log($"[Debugger] Ansiedad disminuida. Actual: {AnxietySystem.Instance.AnxietyValue:F1}");
        }

        // Ansiedad al máximo (fuerza pre-colapso)
        if (Input.GetKeyDown(maxAnxietyKey))
        {
            AnxietySystem.Instance.SetAnxiety(100f);
            Debug.Log("[Debugger] Ansiedad forzada al máximo. Iniciando pre-colapso en 4 segundos...");
        }

        // Fuerza desmayo
        if (Input.GetKeyDown(triggerFaintKey))
        {
            // Invocar el método Faint() mediante reflexión (privado)
            var method = AnxietySystem.Instance.GetType().GetMethod("Faint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
                method.Invoke(AnxietySystem.Instance, null);
            Debug.Log($"[Debugger] Desmayo forzado. Contador: {AnxietySystem.Instance.FaintingCount}");
        }

        // Resetear desmayos
        if (Input.GetKeyDown(resetFaintsKey))
        {
            var field = AnxietySystem.Instance.GetType().GetField("faintingCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(AnxietySystem.Instance, 0);
            Debug.Log("[Debugger] Contador de desmayos resetado.");
        }
    }
}
