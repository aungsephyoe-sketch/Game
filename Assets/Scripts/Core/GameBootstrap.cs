using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Starts the game in any scene (even an empty one) – no prefabs or scene setup required.
    /// Everything else is constructed in code so the project stays diff-friendly and easy for AI tools to edit.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameManager.Instance != null) return;
            var go = new GameObject("[HashiraChronicles]");
            go.AddComponent<GameManager>();
        }

        // Static state must be reset when "Enter Play Mode Options" skips domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Combatant.All.Clear();
            DamageNumbers.Clear();
            TimeController.ResetAll();
            EnemyController.ResetTokens();
        }
    }
}
