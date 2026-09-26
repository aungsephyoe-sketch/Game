using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The story, told in-engine. Original characters and plot:
    /// Ren's master falls in Kiriha; allies join on the road; the Chancellor's betrayal; the temple's revelation that
    /// Ren carries the Dawn half of the Demon Lord's heart; Homura's stand at the gate; and the final confrontation.
    /// </summary>
    public static class CutsceneDatabase
    {
        static readonly Dictionary<string, Cutscene> scenes = new Dictionary<string, Cutscene>();
        static bool built;

        static readonly Color Dawn = new Color(1f, 0.8f, 0.35f);
        static readonly Color Eclipse = new Color(0.7f, 0.1f, 0.35f);

        public static Cutscene Get(string id)
        {
            Build();
            Cutscene c;
            return id != null && scenes.TryGetValue(id, out c) ? c : null;
        }

        static Cutscene New(string id)
        {
            var c = new Cutscene(id);
            scenes[id] = c;
            return c;
        }

        static ArenaTheme Region(string id) { return GameDatabase.GetRegion(id).theme; }

        public static ArenaTheme PeacefulVillage()
        {
            var t = new ArenaTheme
            {
                kind = EnvironmentKind.Village, night = false, burning = false, petals = false,
                ground = new Color(0.3f, 0.34f, 0.2f), groundAccent = new Color(0.45f, 0.38f, 0.25f),
                sky = new Color(0.95f, 0.62f, 0.45f), fog = new Color(0.95f, 0.72f, 0.55f), fogStart = 30f, fogEnd = 95f,
                lantern = new Color(1f, 0.7f, 0.35f), foliage = new Color(0.4f, 0.55f, 0.25f),
                sun = new Color(1f, 0.78f, 0.55f), sunIntensity = 1.15f
            };
            return t;
        }

        static void Build()
        {
            if (built) return;
            built = true;
            GameDatabase.EnsureBuilt();

            // ------------------------------------------------ Opening
            New("opening")
                .Env(PeacefulVillage()).Music(MusicState.Menu).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, 0f), 0f)
                .Actor("tessai", "npc_tessai", new Vector3(0f, 0f, 4f), 180f)
                .Cut(new Vector3(-14f, 9f, -18f), new Vector3(0f, 2f, 2f))
                .Fade(0f, 2f)
                .Title("KIRIHA VILLAGE", "A village of farmers and swordsmiths, three days' walk from anywhere", 3.5f)
                .Dolly(new Vector3(-3.5f, 2.2f, -3.5f), new Vector3(0f, 1.4f, 2f), 4f, false)
                .Anim("ren", "swing").Wait(0.6f).Anim("ren", "spin").Wait(1.2f)
                .Say("Master Tessai", "Again. Your blade still hesitates, Ren.")
                .Say("Ren", "Master, there hasn't been a demon in Kiriha for a hundred years.")
                .Say("Master Tessai", "Then you have a hundred years of practice to catch up on.")
                .Anim("ren", "heavy").Wait(0.4f)
                .Say("Master Tessai", "...Good. That one was almost honest.")
                .Cut(new Vector3(2.5f, 1.8f, -1.5f), new Vector3(0f, 1.6f, 2f))
                .Wait(0.6f).Shake(0.4f).Sfx("roar")
                .Sky(new Color(0.25f, 0.03f, 0.06f), new Color(0.35f, 0.08f, 0.08f), 2.5f)
                .Say("Ren", "Master... the sky.")
                .Dolly(new Vector3(0f, 3f, -6f), new Vector3(0f, 18f, 60f), 3f)
                .Fx("darkpulse", new Vector3(0f, 22f, 60f), Eclipse).Music(MusicState.Boss)
                .Say("Master Tessai", "The Eclipse. So it has returned... after five hundred years.")
                .Say("Master Tessai", "Ren. Listen to me. Whatever happens tonight — do not let them take you.")
                .Say("Ren", "Take ME? Master, what are you talking about?")
                .Fade(1f, 1f)
                .Env(Region("village"))
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -2f), 0f)
                .Actor("tessai", "npc_tessai", new Vector3(1.5f, 0f, -1f), 0f)
                .Actor("d1", "grunt", new Vector3(-2f, 0f, 8f), 180f)
                .Actor("d2", "grunt", new Vector3(2f, 0f, 9f), 180f)
                .Actor("d3", "runner", new Vector3(0f, 0f, 11f), 180f)
                .Actor("villager", "npc_villager", new Vector3(-3f, 0f, 2f), 180f)
                .Cut(new Vector3(0f, 2f, -7f), new Vector3(0f, 1.5f, 4f))
                .Fade(0f, 1f)
                .Move("villager", new Vector3(-4f, 0f, -8f), 1.6f)
                .Say("Villager", "DEMONS! Demons in the square! Run!")
                .Move("d1", new Vector3(-1.5f, 0f, 5f), 1.2f).Move("d2", new Vector3(1.5f, 0f, 6f), 1.2f)
                .Dolly(new Vector3(-2.5f, 1.6f, -4f), new Vector3(0f, 1.5f, 0f), 1.5f)
                .Anim("ren", "charge")
                .Say("Master Tessai", "Ren! Hold the gate. I'll draw the rest to the shrine.")
                .Say("Ren", "I'm not a child anymore. I can fight!")
                .Say("Master Tessai", "...Then prove it. Stay alive.")
                .Move("tessai", new Vector3(8f, 0f, 6f), 1.2f)
                .Wait(0.8f)
                .Title("CHAPTER 1", "THE FALLEN VILLAGE", 3f);

            // ------------------------------------------------ Chapter 1 boss
            New("c1_boss")
                .Env(Region("village")).Music(MusicState.Boss).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -4f), 0f)
                .Actor("tessai", "npc_tessai", new Vector3(0f, 0f, 1f), 0f)
                .Actor("boss", "boss_gorvath", new Vector3(0f, 0f, 6f), 180f)
                .Cut(new Vector3(0f, 5f, 16f), new Vector3(0f, 2f, 0f)).Fade(0f, 1f)
                .Say("Gorvath", "An old man and a boy. The Lord said this village hid something precious.")
                .Say("Master Tessai", "You'll find nothing here but a sword, demon.")
                .Cut(new Vector3(3f, 2f, -1f), new Vector3(0f, 1.5f, 4f))
                .Anim("boss", "heavy").Wait(0.2f).Anim("tessai", "knockdown").Shake(0.6f).Sfx("slam")
                .Say("Ren", "MASTER!")
                .Orbit("ren", 3.5f, 1.4f, 200f, 150f, 2.5f, false)
                .Fx("dawn", new Vector3(0f, 1f, -4f), Dawn).Anim("ren", "glow").Shake(0.8f).Sfx("ultimate")
                .Wait(1.4f)
                .Say("Gorvath", "That light... So it IS here. The Lord will be very pleased.")
                .Say("Ren", "(What is this... this burning in my chest?)")
                .Title("BOSS", "GORVATH — The Horned Butcher of Kiriha", 2.5f);

            New("c1_end")
                .Env(Region("village")).Music(MusicState.Story).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, 0f), 0f)
                .Actor("tessai", "npc_tessai", new Vector3(0f, 0f, 1.4f), 180f)
                .Anim("tessai", "knockdown").Anim("ren", "defeat")
                .Cut(new Vector3(2.2f, 1.2f, 0.4f), new Vector3(0f, 0.8f, 0.8f)).Fade(0f, 1.5f)
                .Say("Master Tessai", "Ren... that light. Your parents... carried it too.")
                .Say("Ren", "Don't talk. I'll get help — the healer, somebody—")
                .Say("Master Tessai", "Go to Solmere. The capital. Get strong... strong enough to end this.")
                .Say("Master Tessai", "And Ren... you were never... a disappointment.")
                .Wait(1f).Anim("tessai", "fade").Wait(1.2f)
                .Dolly(new Vector3(-8f, 4f, -10f), new Vector3(0f, 1f, 0f), 5f, false)
                .Say("Ren", "...")
                .Anim("ren", "getup").Wait(0.6f)
                .Say("Ren", "I'll become strong enough. Strong enough to defeat the Demon Lord. I swear it.")
                .Fade(1f, 2f)
                .Title("THE JOURNEY BEGINS", "", 4f);

            // ------------------------------------------------ Chapter 2
            New("c2_forest")
                .Env(Region("forest")).Music(MusicState.Explore).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -8f), 0f)
                .Cut(new Vector3(0f, 12f, -22f), new Vector3(0f, 2f, 0f)).Fade(0f, 1.5f)
                .Move("ren", new Vector3(0f, 0f, -2f), 3f)
                .Dolly(new Vector3(-3f, 2f, -6f), new Vector3(0f, 1.5f, 0f), 3f)
                .Say("Ren", "The Forest of Shadows. Master said nobody walks through here at night.")
                .Say("Ren", "...It's always night in here.")
                .Fx("darkpulse", new Vector3(0f, 2f, 8f), Eclipse).Sfx("roar")
                .Say("???", "Dawn... child... we remember you... we remember the light...")
                .Say("Ren", "Who's there?! Show yourself!")
                .Title("CHAPTER 2", "THE FOREST OF SHADOWS", 3f);

            New("c2_sora")
                .Env(Region("forest")).Music(MusicState.Menu).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1.5f, 0f, 0f), 90f)
                .Actor("sora", "sora_initiate", new Vector3(1.5f, 0f, 0f), 270f)
                .Anim("sora", "defeat")
                .Cut(new Vector3(0f, 1.8f, -4.5f), new Vector3(0f, 1.2f, 0f)).Fade(0f, 1f)
                .Say("Sora", "D-don't hurt me! I'm not a demon, I swear!")
                .Say("Ren", "You're a slayer. Where's the rest of your squad?")
                .Say("Sora", "Gone. All of them. I ran. I always run.")
                .Say("Ren", "You didn't run just now. You jumped between that demon and me.")
                .Say("Sora", "...I did?")
                .Anim("sora", "getup")
                .Say("Ren", "I'm heading to Solmere. I could use someone that fast.")
                .Say("Sora", "If I die, I'm haunting you. Forever. Every night.")
                .Anim("sora", "victory")
                .Title("SORA JOINS YOUR TEAM", "Storm Breathing · Burst", 2.5f);

            New("c2_end")
                .Env(Region("forest")).Music(MusicState.Menu).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1f, 0f, 0f), 30f)
                .Actor("sora", "sora_initiate", new Vector3(1f, 0f, 0f), 330f)
                .Cut(new Vector3(0f, 2f, -5f), new Vector3(0f, 1.3f, 1f)).Fade(0f, 1f)
                .Say("Sora", "Before it died, that thing said... 'the Lord's servants are everywhere.'")
                .Say("Ren", "Then this is bigger than Kiriha. Bigger than one village.")
                .Fx("darkpulse", new Vector3(0f, 6f, 10f), Eclipse).Shake(0.3f).Sky(new Color(0.1f, 0.02f, 0.05f), new Color(0.15f, 0.05f, 0.08f), 1f)
                .Say("Veyrath", "Ren Kagami. Run to your little kingdom. I will be waiting there.")
                .Say("Ren", "It... knows my name.")
                .Say("Sora", "Why does the scary sky voice know your NAME?!");

            // ------------------------------------------------ Chapter 3
            New("c3_mountain")
                .Env(Region("mountain")).Music(MusicState.Explore).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1f, 0f, -6f), 0f)
                .Actor("sora", "sora_initiate", new Vector3(1f, 0f, -7f), 0f)
                .Cut(new Vector3(-10f, 8f, -18f), new Vector3(0f, 2f, 0f)).Fade(0f, 1.5f)
                .Move("ren", new Vector3(-1f, 0f, -1f), 3f).Move("sora", new Vector3(1f, 0f, -2f), 3f)
                .Say("Sora", "Why. Does it. Have to be. SNOW.")
                .Say("Ren", "Solmere is on the other side of the pass. Keep moving.")
                .Title("CHAPTER 3", "THE KINGDOM", 3f);

            New("c3_kingdom")
                .Env(Region("kingdom")).Music(MusicState.Menu).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1f, 0f, -3f), 0f)
                .Actor("sora", "sora_initiate", new Vector3(1f, 0f, -3.5f), 0f)
                .Actor("chancellor", "npc_chancellor", new Vector3(-1f, 0f, 3f), 180f)
                .Actor("tetsu", "tetsu_guard", new Vector3(1.5f, 0f, 3f), 180f)
                .Cut(new Vector3(0f, 14f, -26f), new Vector3(0f, 3f, 20f)).Fade(0f, 1.5f)
                .Title("ROYAL CAPITAL SOLMERE", "Summoning and the Shop are now open", 3f)
                .Dolly(new Vector3(-2.5f, 1.8f, -6.5f), new Vector3(0f, 1.5f, 1f), 3f)
                .Say("Sora", "Look at all the FOOD.")
                .Say("Chancellor Mikado", "Travellers from Kiriha? How... fortunate. We heard your village fell.")
                .Say("Tetsu", "Captain Tetsu Ganryu, royal guard. Everyone who wants to fight for Solmere proves it in the arena first.")
                .Say("Ren", "Then point me to the arena.");

            New("c3_kiba")
                .Env(Region("kingdom")).Music(MusicState.Menu).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1.5f, 0f, 0f), 90f)
                .Actor("kiba", "kiba_initiate", new Vector3(1.5f, 0f, 0f), 270f)
                .Cut(new Vector3(0f, 1.8f, -4.5f), new Vector3(0f, 1.3f, 0f)).Fade(0f, 1f)
                .Say("Kiba", "Tch. You won. Don't get used to it.")
                .Say("Ren", "You're the arena champion. Why follow us?")
                .Say("Kiba", "Someone has to keep you alive long enough to lose to me properly.")
                .Anim("kiba", "swing")
                .Title("KIBA JOINS YOUR TEAM", "Fang Breathing · DPS", 2.5f);

            New("c3_hana")
                .Env(Region("kingdom")).Music(MusicState.Menu).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1.5f, 0f, 0f), 90f)
                .Actor("hana", "hana_healer", new Vector3(1.5f, 0f, 0f), 270f)
                .Cut(new Vector3(0f, 1.8f, -4.5f), new Vector3(0f, 1.3f, 0f)).Fade(0f, 1f)
                .Say("Hana", "These demons... they were citizens. Someone inside the palace is turning them.")
                .Say("Ren", "Inside the palace? Who has that kind of access?")
                .Say("Hana", "That's what I intend to find out. I'm coming with you.")
                .Title("HANA JOINS YOUR TEAM", "Blossom Breathing · Support (heals the team)", 2.5f);

            New("c3_betrayal")
                .Env(Region("kingdom")).Music(MusicState.Boss).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -3f), 0f)
                .Actor("tetsu", "tetsu_guard", new Vector3(2f, 0f, -3.5f), 0f)
                .Actor("chancellor", "npc_chancellor", new Vector3(0f, 0f, 4f), 180f)
                .Cut(new Vector3(0f, 2f, 8f), new Vector3(0f, 1.5f, 0f)).Fade(0f, 1f)
                .Say("Chancellor Mikado", "You were supposed to die in the forest, boy.")
                .Say("Tetsu", "Chancellor...? What are you saying?")
                .Fx("darkpulse", new Vector3(0f, 1.5f, 4f), Eclipse).Shake(0.5f).Anim("chancellor", "glow")
                .Say("Chancellor Mikado", "Twenty years I've served the Lord of the Eclipse from inside this palace. Twenty years, Captain.")
                .Say("Tetsu", "No... I trusted you with the King's life...")
                .Remove("chancellor").Actor("boss", "boss_chancellor", new Vector3(0f, 0f, 5f), 180f)
                .Fx("explosion", new Vector3(0f, 1f, 5f), Eclipse).Shake(0.7f)
                .Say("Ren", "Everyone get behind me!")
                .Title("BOSS", "CHANCELLOR MIKADO — The Masked Servant", 2.5f);

            New("c3_end")
                .Env(Region("kingdom")).Music(MusicState.Menu).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1.5f, 0f, 0f), 90f)
                .Actor("tetsu", "tetsu_guard", new Vector3(1.5f, 0f, 0f), 270f)
                .Cut(new Vector3(0f, 1.8f, -4.5f), new Vector3(0f, 1.3f, 0f)).Fade(0f, 1f)
                .Say("Tetsu", "Twenty years. I stood beside him for twenty years and never saw it.")
                .Say("Ren", "Then stand beside us now. We could use a wall.")
                .Say("Tetsu", "...The Guard is yours, Ren Kagami.")
                .Title("TETSU JOINS YOUR TEAM", "Stone Breathing · Tank", 2.5f);

            // ------------------------------------------------ Chapter 4
            New("c4_wastes")
                .Env(Region("demonland")).Music(MusicState.Explore).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1f, 0f, -3f), 0f)
                .Actor("homura", "homura_pillar", new Vector3(0f, 0f, 5f), 180f)
                .Cut(new Vector3(0f, 10f, -20f), new Vector3(0f, 2f, 5f)).Fade(0f, 1.5f)
                .Title("CHAPTER 4", "THE DEMON TERRITORY", 3f)
                .Dolly(new Vector3(-2f, 1.8f, 0f), new Vector3(0f, 1.6f, 5f), 2.5f)
                .Fx("fire", new Vector3(0f, 0f, 5f), new Color(1f, 0.5f, 0.1f))
                .Say("Homura", "So you're the boy with the dawn in his chest.")
                .Say("Ren", "Who are you?")
                .Say("Homura", "Homura Enjoji. Flame Pillar. The King sent me to watch you. I decided to help instead.")
                .Anim("homura", "victory")
                .Title("HOMURA JOINS YOUR TEAM", "Blaze Breathing · Legendary", 2.5f);

            New("c4_end")
                .Env(Region("demonland")).Music(MusicState.Story).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -2f), 0f)
                .Actor("goken", "boss_goken", new Vector3(0f, 0f, 2f), 180f, 0.8f)
                .Anim("goken", "defeat")
                .Cut(new Vector3(2f, 1.5f, -3f), new Vector3(0f, 1.2f, 1f)).Fade(0f, 1f)
                .Say("Goken", "Heh... that was a good fight. The best in a century.")
                .Say("Goken", "The Lord knew your name before you were born, Ren Kagami. Go to the temple. Ask it... why.")
                .Anim("goken", "fade").Fx("darkpulse", new Vector3(0f, 1f, 2f), Eclipse)
                .Say("Ren", "Before I was born...?");

            // ------------------------------------------------ Chapter 5
            New("c5_temple")
                .Env(Region("temple")).Music(MusicState.Explore).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(-1f, 0f, -4f), 0f)
                .Actor("hana", "hana_healer", new Vector3(1f, 0f, -4.5f), 0f)
                .Cut(new Vector3(0f, 12f, -22f), new Vector3(0f, 3f, 5f)).Fade(0f, 1.5f)
                .Title("CHAPTER 5", "THE FORGOTTEN TEMPLE", 3f)
                .Dolly(new Vector3(-2f, 1.8f, -7f), new Vector3(0f, 1.8f, 2f), 3f)
                .Say("Hana", "These symbols... this is Akatsuki's script. The hero from the old legends.")
                .Say("Ren", "The seals on the doors glow in a pattern. Strike them in the same order.");

            New("c5_truth")
                .Env(Region("temple")).Music(MusicState.Story).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -1f), 0f)
                .Actor("hana", "hana_healer", new Vector3(-1.8f, 0f, -2f), 20f)
                .Actor("kiba", "kiba_initiate", new Vector3(1.8f, 0f, -2f), 340f)
                .Cut(new Vector3(0f, 2f, -6f), new Vector3(0f, 2.5f, 4f)).Fade(0f, 1.5f)
                .Fx("dawn", new Vector3(0f, 3f, 5f), new Color(0.5f, 0.9f, 1f))
                .Say("Akatsuki's Echo", "Whoever reads this carries my failure. I could not kill Veyrath. He was my brother.")
                .Say("Akatsuki's Echo", "So I split his heart. The Dark half I sealed away. The Dawn half I hid in a newborn child — and in that child's children, forever.")
                .Dolly(new Vector3(1.2f, 1.6f, -3f), new Vector3(0f, 1.5f, -1f), 2f)
                .Say("Ren", "...The Dawn half of the Demon Lord's heart. That's what's inside me.")
                .Say("Kiba", "So you're half a Demon Lord. Great. I'm still going to beat you.")
                .Say("Hana", "It doesn't matter where the light came from, Ren. It matters what you do with it.")
                .Fx("dawn", new Vector3(0f, 1f, -1f), Dawn).Anim("ren", "glow").Shake(0.5f).Music(MusicState.Victory)
                .Say("Ren", "Then I'll use it to end him.")
                .Title("REN — DAWN MARK", "A new version of Ren has joined your collection", 3f);

            // ------------------------------------------------ Chapter 6
            New("c6_siege")
                .Env(Region("fallen")).Music(MusicState.Boss).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -2f), 0f)
                .Actor("tetsu", "tetsu_guard", new Vector3(-1.8f, 0f, -2.5f), 0f)
                .Actor("homura", "homura_pillar", new Vector3(1.8f, 0f, -2.5f), 0f)
                .Cut(new Vector3(0f, 16f, -24f), new Vector3(0f, 4f, 10f)).Fade(0f, 1.5f)
                .Title("CHAPTER 6", "THE FALLEN KINGDOM", 3f)
                .Dolly(new Vector3(0f, 2f, -7f), new Vector3(0f, 1.6f, 0f), 2.5f)
                .Say("Tetsu", "The outer wall is gone. The King is evacuating everyone through the north gate.")
                .Say("Homura", "Then we hold the main street. Nobody gets past us.")
                .Say("Ren", "Royal guard — with us!");

            New("c6_sacrifice")
                .Env(Region("fallen")).Music(MusicState.Story).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -4f), 180f)
                .Actor("homura", "homura_pillar", new Vector3(0f, 0f, 1f), 0f)
                .Actor("sora", "sora_initiate", new Vector3(-1.8f, 0f, -5f), 180f)
                .Actor("kiba", "kiba_initiate", new Vector3(1.8f, 0f, -5f), 180f)
                .Actor("d1", "castle_sentinel", new Vector3(-2f, 0f, 9f), 180f).Actor("d2", "elite", new Vector3(2f, 0f, 8f), 180f)
                .Cut(new Vector3(3f, 1.8f, -1.5f), new Vector3(0f, 1.6f, 0f)).Fade(0f, 1f)
                .Say("Homura", "Ren. Take everyone through the gate.")
                .Say("Ren", "Not without you!")
                .Say("Homura", "A flame that runs away isn't a flame. GO! I'll hold them here.")
                .Anim("homura", "charge").Fx("fire", new Vector3(0f, 0f, 1f), new Color(1f, 0.5f, 0.1f))
                .Say("Ren", "HOMURA!")
                .Move("homura", new Vector3(0f, 0f, 6f), 0.8f).Fx("explosion", new Vector3(0f, 1f, 7f), new Color(1f, 0.45f, 0.1f)).Shake(1f)
                .Fade(1f, 1.5f).Remove("homura").Remove("d1").Remove("d2")
                .Cut(new Vector3(0f, 1.8f, -9f), new Vector3(0f, 1.4f, -4.5f)).Fade(0f, 1.5f)
                .Say("Sora", "He's... he's still fighting in there. I can see the fire.")
                .Say("Kiba", "We should've stayed! All of us together, we could have—")
                .Say("Tetsu", "And died. All of us. He chose this so we wouldn't have to.")
                .Say("Kiba", "Don't you dare tell me that was the right call!")
                .Say("Ren", "ENOUGH. ...We finish this. For him.");

            New("c6_end")
                .Env(Region("fallen")).Music(MusicState.Story).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -2f), 0f)
                .Actor("boss", "boss_morgrath", new Vector3(0f, 0f, 2.5f), 180f, 0.8f).Anim("boss", "defeat")
                .Cut(new Vector3(2f, 1.6f, -3f), new Vector3(0f, 1.3f, 1f)).Fade(0f, 1f)
                .Say("Morgrath", "You're too late... the Lord is already... almost whole.")
                .Anim("boss", "fade").Sky(new Color(0.08f, 0.02f, 0.06f), new Color(0.12f, 0.03f, 0.08f), 1.5f)
                .Say("Veyrath", "Come to my castle, Dawn. Let us become one again.")
                .Title("THE CASTLE OF THE ECLIPSE", "is now open on the map", 3f);

            // ------------------------------------------------ Final chapter
            New("c7_castle")
                .Env(Region("castle")).Music(MusicState.Explore).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -5f), 0f)
                .Actor("sora", "sora_initiate", new Vector3(-1.8f, 0f, -6f), 0f).Actor("kiba", "kiba_initiate", new Vector3(1.8f, 0f, -6f), 0f)
                .Actor("hana", "hana_healer", new Vector3(-3f, 0f, -7f), 0f).Actor("tetsu", "tetsu_guard", new Vector3(3f, 0f, -7f), 0f)
                .Cut(new Vector3(0f, 18f, -26f), new Vector3(0f, 6f, 10f)).Fade(0f, 2f)
                .Title("FINAL CHAPTER", "THE DEMON LORD", 3.5f)
                .Dolly(new Vector3(0f, 1.8f, -10f), new Vector3(0f, 1.5f, -5f), 3f)
                .Say("Sora", "For the record, I am terrified.")
                .Say("Kiba", "For the record, so is everyone.")
                .Say("Hana", "Whatever happens in there — we walk out together.")
                .Say("Tetsu", "Together.")
                .Say("Ren", "Let's end this.");

            New("c7_truth")
                .Env(Region("castle")).Music(MusicState.Story).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -2f), 0f)
                .Cut(new Vector3(0f, 2f, -6f), new Vector3(0f, 4f, 10f)).Fade(0f, 1f)
                .Fx("darkpulse", new Vector3(0f, 6f, 12f), Eclipse).Shake(0.4f)
                .Say("Veyrath", "My brother cut out my heart and called it salvation. Five hundred years I waited for it to come home.")
                .Say("Ren", "You burned my village for a piece of yourself?")
                .Say("Veyrath", "OUR village, Ren. Kiriha was built on my grave. Your master knew. He guarded you for me, in a way.")
                .Say("Ren", "...Don't you talk about him.");

            New("c7_final")
                .Env(Region("castle")).Music(MusicState.Boss).Fade(1f, 0f)
                .Actor("ren", "ren_initiate", new Vector3(0f, 0f, -4f), 0f)
                .Actor("boss", "boss_veyrath", new Vector3(0f, 0f, 5f), 180f)
                .Cut(new Vector3(0f, 1.2f, -8f), new Vector3(0f, 3f, 5f)).Fade(0f, 1.5f)
                .Orbit("boss", 6f, 2.5f, 160f, 200f, 3f, false)
                .Say("Veyrath", "At last. Give me the Dawn, and the Eclipse will end. No more demons. No more war. Only me.")
                .Say("Ren", "No. The Dawn stays with the people you hurt.")
                .Fx("dawn", new Vector3(0f, 1f, -4f), Dawn).Anim("ren", "glow").Fx("darkpulse", new Vector3(0f, 2f, 5f), Eclipse).Shake(0.8f)
                .Title("FINAL BATTLE", "VEYRATH — The Demon Lord of the Eclipse", 3f);

            New("ending")
                .Env(Region("castle")).Music(MusicState.Story).Fade(1f, 0f)
                .Actor("ren", "ren_sundance", new Vector3(0f, 0f, -2f), 0f)
                .Actor("boss", "boss_veyrath", new Vector3(0f, 0f, 3f), 180f).Anim("boss", "defeat")
                .Cut(new Vector3(2.5f, 1.5f, -3f), new Vector3(0f, 1.5f, 1f)).Fade(0f, 1.5f)
                .Say("Veyrath", "Brother... was it... worth it?")
                .Say("Ren", "Yes.")
                .Fx("dawn", new Vector3(0f, 1f, 3f), Dawn).Anim("boss", "fade").Shake(0.6f).Wait(1.5f)
                .Fade(1f, 2f)
                .Env(PeacefulVillage()).Music(MusicState.Victory)
                .Actor("ren", "ren_sundance", new Vector3(0f, 0f, 0f), 0f)
                .Actor("sora", "sora_initiate", new Vector3(-2f, 0f, -1.5f), 20f).Actor("kiba", "kiba_initiate", new Vector3(2f, 0f, -1.5f), 340f)
                .Actor("hana", "hana_healer", new Vector3(-3.4f, 0f, -2.5f), 20f).Actor("tetsu", "tetsu_guard", new Vector3(3.4f, 0f, -2.5f), 340f)
                .Cut(new Vector3(0f, 2f, -9f), new Vector3(0f, 1.5f, 2f)).Fade(0f, 2f)
                .Title("KIRIHA VILLAGE", "One year later", 3f)
                .Say("Ren", "Master. It's over. They're rebuilding the shrine.")
                .Say("Sora", "Wait — who's THAT?")
                .Actor("homura", "homura_lastflame", new Vector3(0f, 0f, 9f), 180f)
                .Move("homura", new Vector3(0f, 0f, 3.5f), 2.5f, true)
                .Say("Ren", "...Homura?!")
                .Say("Homura", "A flame this stubborn doesn't go out. Did I miss anything?")
                .Anim("ren", "victory").Anim("sora", "victory").Anim("kiba", "victory")
                .Dolly(new Vector3(0f, 6f, -14f), new Vector3(0f, 8f, 30f), 5f, false)
                .Say("Ren", "...It's morning.")
                .Fade(1f, 3f)
                .Title("THE END", "Thank you for playing Hashira Chronicles", 5f);
        }
    }
}
