using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Routes input (touch + keyboard) to the active slayer and handles team switching / pause.</summary>
    public class PlayerController : MonoBehaviour
    {
        public TeamSystem Team;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || Team == null) return;
            var input = gm.Controls != null ? gm.Controls.Current : InputState.Empty;

            if (input.pauseDown) gm.TogglePause();
            if (TimeController.Paused || BattleController.Current == null || BattleController.Current.Finished) return;

            if (input.switchTo >= 0) Team.TrySwitch(input.switchTo);
            var active = Team.Active;
            if (active != null && active.gameObject.activeInHierarchy) active.HandleInput(input, Time.deltaTime);
        }
    }
}
