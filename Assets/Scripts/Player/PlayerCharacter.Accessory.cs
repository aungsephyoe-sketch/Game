using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Accessory effects in battle (see <see cref="AccessoryFx"/> for the list): lifesteal, burn, frost, thunder,
    /// guard, revive, swift dodges, bloom, fury, faster specials, serpent crits, thorns and ambush.
    /// </summary>
    public partial class PlayerCharacter
    {
        string accFx = "";
        int accHits;
        bool accReviveUsed, accProc;
        float accAmbushUntil;

        public string AccessoryEffect { get { return accFx; } }

        void InitAccessory()
        {
            accFx = AccessoryFx.EffectOf(Owned);
            accHits = 0;
            accReviveUsed = false;
            if (accFx == "guard") DamageTakenMultiplier *= 0.88f;
            if (accFx == "thorns") Health.Damaged += AccThorns;
        }

        /// <summary>Extra outgoing damage from the accessory (fury at low HP, the ambush after a dodge, the eclipse).</summary>
        public float AccessoryDamageMult(Combatant target)
        {
            float m = 1f;
            if (accFx == "fury" && Health.Current < Health.Max * 0.4f) m *= 1.3f;
            if (accFx == "eclipse") m *= 1.15f;
            if (accFx == "ambush" && Time.time < accAmbushUntil) { m *= 2f; accAmbushUntil = 0f; }
            return m;
        }

        float AccessoryUltMult { get { return accFx == "ultcharge" ? 1.35f : 1f; } }

        void AccessoryOnDodge()
        {
            if (accFx == "swift") dodgeCooldown *= 0.65f;
            if (accFx == "ambush") accAmbushUntil = Time.time + 1.5f;
        }

        void AccessoryOnDealt(DamageInfo info, Combatant target)
        {
            if (string.IsNullOrEmpty(accFx) || accProc || info.amount <= 0f) return;
            accProc = true;
            try
            {
                Vector3 at = target != null ? target.Position + Vector3.up : Position;
                switch (accFx)
                {
                    case "lifesteal": Health.Heal(info.amount * 0.06f); break;
                    case "eclipse":
                        Health.Heal(info.amount * 0.05f);
                        if (++accHits % 4 == 0) AccThunder(target, 0.6f);
                        break;
                    case "burn":
                        if (target != null && target.IsAlive && Random.value < 0.15f)
                        {
                            var tag = AttackTag.Basic(0.5f, new Color(1f, 0.5f, 0.15f));
                            tag.hitStop = 0f;
                            CombatSystem.ApplyHit(this, target, tag, Position);
                            VFX.Breath(at, new Color(1f, 0.5f, 0.15f), 10);
                        }
                        break;
                    case "frost":
                        if (target != null && target.IsAlive && Random.value < 0.12f)
                        {
                            var tag = AttackTag.Basic(0.25f, new Color(0.6f, 0.9f, 1f));
                            tag.stagger = 14f;
                            tag.knockback = 0f;
                            tag.hitStop = 0.05f;
                            CombatSystem.ApplyHit(this, target, tag, Position);
                            VFX.HitSpark(at, new Color(0.7f, 0.95f, 1f), 16);
                        }
                        break;
                    case "thunder":
                        if (++accHits % 5 == 0) AccThunder(target, 0.7f);
                        break;
                    case "serpent":
                        if (info.crit) Health.Heal(Health.Max * 0.015f);
                        break;
                    case "bloom":
                        if (target != null && !target.IsAlive) { Health.Heal(Health.Max * 0.03f); VFX.Breath(Position + Vector3.up, new Color(1f, 0.7f, 0.85f), 10); }
                        break;
                }
            }
            finally { accProc = false; }
        }

        void AccThunder(Combatant target, float mult)
        {
            if (target == null || !target.IsAlive) return;
            BoltFx.Strike(target.Position + Vector3.up * 12f, target.Position, new Color(1f, 0.95f, 0.5f), 0.3f, 0.2f, 0.4f);
            var tag = AttackTag.Basic(mult, new Color(1f, 0.95f, 0.5f));
            tag.hitStop = 0.02f;
            CombatSystem.HitRadius(this, target.Position, 1.8f, tag);
            if (Audio != null) Audio.PlayPitched("el_thunder", 0.5f, 1.2f);
        }

        void AccThorns(DamageInfo info)
        {
            var src = info.source;
            if (src == null || src == this || !src.IsAlive || info.amount <= 0f) return;
            var back = new DamageInfo { amount = Mathf.Max(1f, Mathf.Round(info.amount * 0.25f)), source = this };
            if (src.Health.TakeDamage(ref back))
                DamageNumbers.Spawn(src.Position + Vector3.up * 1.4f, back.amount, false, 1f, false, false);
        }

        /// <summary>Phoenix Feather: once per battle, rise again instead of falling.</summary>
        bool AccessoryRevive()
        {
            if (accFx != "revive" || accReviveUsed) return false;
            // Party AI slayers have their own knock-out handling; the feather is for your own slayers.
            if (GetComponent<PartySlayer>() != null) return false;
            accReviveUsed = true;
            Health.Revive(0.35f);
            Health.GrantInvulnerability(2f);
            VFX.Pillar(Position, new Color(1f, 0.55f, 0.2f), 9f, 0.8f);
            VFX.Shockwave(Position, 5f, new Color(1f, 0.6f, 0.2f), 0.5f);
            DamageNumbers.SpawnText(Position + Vector3.up * 2.6f, "REBORN!", new Color(1f, 0.7f, 0.3f), 60f);
            if (Audio != null) Audio.Play("sp_finish", 0.7f);
            return true;
        }
    }
}
