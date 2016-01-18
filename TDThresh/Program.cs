using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Thresh.Utility;
using Color = System.Drawing.Color;
using HitChance = Thresh.Utility.HitChance;
using Prediction = Thresh.Utility.Prediction;

namespace Thresh
{
    internal class Thresh
    {
        private static Menu ThreshMenu, QMenu, WMenu, EMenu, RMenu, DrawingsMenu, PredictionMenu, Activator;
        private static Spell.Skillshot Q, W, E;
        public static Spell.Active Q2, R;
        public static List<AIHeroClient> Enemies = new List<AIHeroClient>(), Allies = new List<AIHeroClient>();
        private static SpellSlot exhaust, ignite, heal;
        private int grab = 0, grabS = 0;
        private float grabW = 0;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool Combo
        {
            get { return (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)); }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Load;
        }

        public static void Load(EventArgs args)
        {
			if (ObjectManager.Player.Hero != Champion.Thresh) return;
            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
                if (hero.IsAlly)
                    Allies.Add(hero);
            }
            Q = new Spell.Skillshot(SpellSlot.Q, 1040, SkillShotType.Linear, (int) 0.5f, (int?) 1900f, 70);
			Q.AllowedCollisionCount = 0;
            Q2 = new Spell.Active(SpellSlot.Q, 9000);// kappa
            W = new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, int.MaxValue, 10);
            W.AllowedCollisionCount = int.MaxValue;
            E = new Spell.Skillshot(SpellSlot.E, 480, SkillShotType.Linear, (int) 0.25f, int.MaxValue, 50);
            E.AllowedCollisionCount = int.MaxValue;
            R = new Spell.Active(SpellSlot.R, 350);
            ignite = Player.GetSpellSlotFromName("summonerdot");
            exhaust = Player.GetSpellSlotFromName("summonerexhaust");
            heal = Player.GetSpellSlotFromName("summonerheal");
            ThreshMenu = MainMenu.AddMenu("TDThresh", "thresh");
            ThreshMenu.Add("AACombo", new CheckBox("Disable AA if can use E"));
            QMenu = ThreshMenu.AddSubMenu("Q", "q");
            QMenu.Add("ts", new CheckBox("Use common TargetSelector"));
            QMenu.Add("ts1", new CheckBox("Only one target", false));
            QMenu.Add("ts2", new CheckBox("All grab-able targets"));
            QMenu.Add("qCC", new CheckBox("Auto Q cc & dash enemy"));
            QMenu.Add("minGrab", new Slider("Min range grab", 250, 125, (int) Q.Range));
            QMenu.Add("maxGrab", new Slider("Max range grab", (int) Q.Range, 125, (int) Q.Range));
            QMenu.AddLabel("Grab:");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != Player.Team))              
                QMenu.Add("grab" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
				QMenu.AddSeparator();
            QMenu.Add("GapQ", new CheckBox("OnEnemyGapcloser Q"));

            WMenu = ThreshMenu.AddSubMenu("W", "w");
            WMenu.Add("autoW", new CheckBox("Auto W"));
            WMenu.Add("Wdmg", new Slider("W dmg % hp", 10, 100, 0));
            WMenu.Add("autoW2", new CheckBox("Auto W if Q succesfull"));
            WMenu.Add("autoW3", new CheckBox("Auto W shield big dmg"));
            WMenu.Add("wCount", new Slider("Auto W if x enemies near ally", 2, 0, 5));

            EMenu = ThreshMenu.AddSubMenu("E", "E");
            EMenu.Add("autoE", new CheckBox("Auto E"));
            EMenu.Add("pushE", new CheckBox("Auto push"));
            EMenu.Add("inter", new CheckBox("OnPossibleToInterrupt"));
            EMenu.Add("Gap", new CheckBox("OnEnemyGapcloser"));

            RMenu = ThreshMenu.AddSubMenu("R", "R");
            RMenu.Add("rCount", new Slider("Auto R if x enemies in range", 2, 0, 5));
            RMenu.Add("rKs", new CheckBox("R ks", false));
            RMenu.Add("comboR", new CheckBox("Always R in combo", false));

            PredictionMenu = ThreshMenu.AddSubMenu("Prediction", "prediction");
            StringList(PredictionMenu, "Qpred", "Q Prediction", new[] { "Low","Medium", "High", "Very High"}, 1);
            StringList(PredictionMenu, "Epred", "E Prediction", new[] { "Low", "Medium", "High", "Very High" }, 1);
            Activator = ThreshMenu.AddSubMenu("Activator", "activator");
            Activator.AddLabel("Summoner Spells:");
            if (exhaust != null)
            {
                Activator.AddLabel("Exhaust:");
                Activator.Add("Exhaust", new CheckBox("Exhaust"));
                Activator.Add("Exhaust1", new CheckBox("Exhaust if Channeling Important Spell"));
                Activator.Add("Exhaust2", new CheckBox("Always in combo", false));
            }
            if (heal != null)
            {
                Activator.AddLabel("Heal:");
                Activator.Add("Heal", new CheckBox("Heal"));
                Activator.Add("AllyHeal", new CheckBox("AllyHeal"));
            }
            if (ignite != null)
            {
                Activator.AddLabel("Ignite:");
                Activator.Add("Ignite", new CheckBox("Ignite"));
            }
            Activator.AddSeparator();
            Activator.AddLabel("Potions:");
            Activator.Add("pots", new CheckBox("Potion, ManaPotion, Flask, Biscuit"));
            Activator.AddSeparator();
            Activator.AddLabel("Defensive items:");
            Activator.Add("Randuin", new CheckBox("Randuin"));
            Activator.Add("FaceOfTheMountain", new CheckBox("FaceOfTheMountain"));
            Activator.Add("Seraph", new CheckBox("Seraph"));
            Activator.Add("Solari", new CheckBox("Solari"));
            Activator.AddSeparator();
            Activator.AddLabel("Cleanse:");
            Activator.Add("Clean", new CheckBox("Quicksilver, Mikaels, Mercurial, Dervish"));
            Activator.AddLabel("Allies to cast:");
            foreach (var ally in ObjectManager.Get<AIHeroClient>().Where(ally => ally.IsAlly && !ally.IsMe))
                Activator.Add("MikaelsAlly" + ally.BaseSkinName, new CheckBox(ally.BaseSkinName));
            Activator.AddSeparator();
            Activator.Add("cleanHP", new Slider("Use only under % HP", 80));
            Activator.Add("CleanSpells", new CheckBox("ZedR FizzR MordekaiserR VladimirR"));
            Activator.Add("Stun", new CheckBox("Stun"));
            Activator.Add("Snare", new CheckBox("Snare"));
            Activator.Add("Charm", new CheckBox("Charm"));
            Activator.Add("Fear", new CheckBox("Fear"));
            Activator.Add("Suppression", new CheckBox("Suppression"));
            Activator.Add("Taunt", new CheckBox("Taunt"));
            Activator.Add("Blind", new CheckBox("Blind"));

            DrawingsMenu = ThreshMenu.AddSubMenu("Drawings", "drawings");
            DrawingsMenu.Add("qRange", new CheckBox("Q range"));
            DrawingsMenu.Add("wRange", new CheckBox("W range"));
            DrawingsMenu.Add("eRange", new CheckBox("E range"));
            DrawingsMenu.Add("rRange", new CheckBox("R range"));
            DrawingsMenu.Add("onlyRdy", new CheckBox("Draw when skill rdy"));
            
            Obj_AI_Base.OnProcessSpellCast += Utils.OnProcessSpellCast;
            TickManager.Tick();
            Game.OnTick += Qcoltick;
            Orbwalker.OnPreAttack += OnPreAttack;
            Orbwalker.OnPostAttack += OnPostAttack;
            Interrupter.OnInterruptableSpell += OnInterruptable;
            Game.OnUpdate += Game_OnGameUpdate;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableTarget;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }

        private static void Interrupter_OnInterruptableTarget(Obj_AI_Base sender,
            Interrupter.InterruptableSpellEventArgs args)
        {
            if (E.IsReady() && Config.inter && sender.IsValidTarget(E.Range) && sender is AIHeroClient && sender.IsEnemy)
            {
                E.Cast(sender.ServerPosition);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloser)
        {
            if (E.IsReady() && Config.Gap && gapcloser.Sender.IsValidTarget(E.Range) && sender.IsEnemy)
            {
                E.Cast(gapcloser.Sender);
            }
            else if (Q.IsReady() && Config.GapQ && gapcloser.Sender.IsValidTarget(Q.Range) && sender.IsEnemy && ObjectManager.Player.IsFacing(sender))
            {
                Q.Cast(gapcloser.Sender);
            }
        }

        private static bool CanUse(SpellSlot sum)
        {
            if (sum != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(sum) == SpellState.Ready)
                return true;
            return false;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy || sender.Type != GameObjectType.obj_AI_Base)
                return;


            if (sender.Distance(Player.Position) > 1600)
                return;
            var kappa = sender as AIHeroClient;

            if (CanUse(exhaust) && Config.Exhaust)
            {
                foreach (
                    var ally in
                        Allies.Where(
                            ally =>
                                ally.IsValid && !ally.IsDead && ally.HealthPercent < 51 &&
                                Player.Distance(ally.ServerPosition) < 700))
                {
                    double dmg = 0;
                    if (args.Target != null && args.Target.NetworkId == ally.NetworkId)
                    {
                        dmg = dmg + kappa.GetSpellDamage(ally, args.Slot);
                    }
                    else
                    {
                        var castArea = ally.Distance(args.End)*(args.End - ally.ServerPosition).Normalized() +
                                       ally.ServerPosition;
                        if (castArea.Distance(ally.ServerPosition) < ally.BoundingRadius/2)
                            dmg = dmg + kappa.GetSpellDamage(ally, args.Slot);
                        else
                            continue;
                    }

                    if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*40)
                        Player.Spellbook.CastSpell(exhaust, sender);
                }
            }
        }

        private static void Survival()
        {
            if (Player.HealthPercent < 60 && (Ids.Seraph.IsReady() || Ids.Zhonya.IsReady()))
            {
                var dmg = Utils.GetIncomingDamage(Player, 1);
                if (dmg > 0)
                {
                    if (Ids.Seraph.IsReady() && Config.Seraph)
                    {
                        var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Seraph.Id);
                        var value = Player.Mana*0.2 + 150;
                        if (dmg > value && Player.HealthPercent < 50)
                        {
                            if (inventorySlot != null)
                            {
                                var firstOrDefault =
                                    inventorySlot.SpellSlot;
                                EloBuddy.Player.CastSpell(firstOrDefault);
                            }
                        }

                        else if (Player.Health - dmg < Player.CountEnemiesInRange(700)*Player.Level*20)
                            if (inventorySlot != null)
                            {
                                var firstOrDefault =
                                    inventorySlot.SpellSlot;
                                EloBuddy.Player.CastSpell(firstOrDefault);
                            }
                        if (Player.Health - dmg < Player.Level*10)
                            if (inventorySlot != null)
                            {
                                var firstOrDefault =
                                    inventorySlot.SpellSlot;
                                EloBuddy.Player.CastSpell(firstOrDefault);
                            }
                    }
                }
            }


            foreach (
                var ally in
                    Allies.Where(
                        ally =>
                            ally.IsValid && !ally.IsDead && ally.HealthPercent < 50 &&
                            Player.Distance(ally.ServerPosition) < 700))
            {
                var dmg = Utils.GetIncomingDamage(ally, 1);
                if (dmg/20 > ally.Health)
                    continue;

                if (CanUse(heal) && Config.Heal)
                {
                    if (!Config.AllyHeal && !ally.IsMe)
                        return;

                    if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*15)
                        Player.Spellbook.CastSpell(heal, ally);
                    else if (ally.Health - dmg < ally.Level*15)
                        Player.Spellbook.CastSpell(heal, ally);
                }

                if (Config.Solari && Ids.Solari.IsReady() && Player.Distance(ally.ServerPosition) < Ids.Solari.Range)
                {
                    var inventorySlot =
                        Player.InventoryItems.FirstOrDefault(item => item.Id == ItemId.Locket_of_the_Iron_Solari);

                    var value = 75 + (15*Player.Level);
                    if (dmg > value && Player.HealthPercent < 50)
                    {
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    }
                    else if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*15)
                    {
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    }
                    else if (ally.Health - dmg < ally.Level*10)
                    {
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    }
                }

                if (Config.FaceOfTheMountain && Ids.FaceOfTheMountain.IsOwned() && Ids.FaceOfTheMountain.IsReady() &&
                    Player.Distance(ally.Position) < Ids.FaceOfTheMountain.Range)
                {
                    var value = 0.1*Player.MaxHealth;
                    var inventorySlot =
                        Player.InventoryItems.FirstOrDefault(item => item.Id == ItemId.Face_of_the_Mountain);
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault, ally);
                    }
                    if (dmg > value && Player.HealthPercent < 50)
                    {
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, Player);
                        }
                    }
                    else if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*15)
                    {
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    }

                    else if (ally.Health - dmg < ally.Level*10)
                    {
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    }
                }
            }
        }

        public static bool InFountain(AIHeroClient hero)
        {
            var map = Game.MapId;
            var mapIsSR = map == GameMapId.SummonersRift;
            float fountainRange = mapIsSR ? 1050 : 750;
            return hero.IsVisible &&
                   ObjectManager.Get<Obj_SpawnPoint>()
                       .Any(sp => sp.Team == hero.Team && hero.Distance(sp.Position) < fountainRange);
        }

        private static void Cleansers()
        {
            if (!Ids.Quicksilver.IsReady() && !Ids.Mikaels.IsReady() && !Ids.Mercurial.IsReady() &&
                !Ids.Dervish.IsReady())
                return;

            if (Player.HealthPercent >= Config.cleanHP || !Config.Clean)
                return;

            if (Player.HasBuff("zedrdeathmark") || Player.HasBuff("FizzMarinerDoom") ||
                Player.HasBuff("MordekaiserChildrenOfTheGrave") || Player.HasBuff("PoppyDiplomaticImmunity") ||
                Player.HasBuff("VladimirHemoplague") && Config.CleanSpells)
                Clean();

            if (Ids.Mikaels.IsReady())
            {
                var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Mikaels.Id);
                foreach (var ally in Allies.Where(
                    ally =>
                        ally.IsValid && !ally.IsDead &&
                        Activator["MikaelsAlly" + ally.BaseSkinName].Cast<CheckBox>().CurrentValue &&
                        Player.Distance(ally.Position) < Ids.Mikaels.Range
                        && ally.HealthPercent < (float) Config.cleanHP))
                {
                    if (ally.HasBuff("zedrdeathmark") || ally.HasBuff("FizzMarinerDoom") ||
                        ally.HasBuff("MordekaiserChildrenOfTheGrave") || ally.HasBuff("PoppyDiplomaticImmunity") ||
                        ally.HasBuff("VladimirHemoplague"))
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Stun) && Config.Stun)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Snare) && Config.Snare)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Charm) && Config.Charm)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Fear) && Config.Fear)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Stun) && Config.Stun)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Taunt) && Config.Taunt)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Suppression) && Config.Suppression)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                    if (ally.HasBuffOfType(BuffType.Blind) && Config.Blind)
                        if (inventorySlot != null)
                        {
                            var firstOrDefault =
                                inventorySlot.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault, ally);
                        }
                }
            }

            if (Player.HasBuffOfType(BuffType.Stun) && Config.Stun)
                Clean();
            if (Player.HasBuffOfType(BuffType.Snare) && Config.Snare)
                Clean();
            if (Player.HasBuffOfType(BuffType.Charm) && Config.Charm)
                Clean();
            if (Player.HasBuffOfType(BuffType.Fear) && Config.Fear)
                Clean();
            if (Player.HasBuffOfType(BuffType.Stun) && Config.Stun)
                Clean();
            if (Player.HasBuffOfType(BuffType.Taunt) && Config.Taunt)
                Clean();
            if (Player.HasBuffOfType(BuffType.Suppression) && Config.Suppression)
                Clean();
            if (Player.HasBuffOfType(BuffType.Blind) && Config.Blind)
                Clean();
        }

        private static void Clean()
        {
            var qss = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Quicksilver.Id);
            var mercurial = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Mercurial.Id);
            var dervish = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Dervish.Id);
            if (Ids.Quicksilver.IsReady())
                if (qss != null)
                {
                    var firstOrDefault =
                        qss.SpellSlot;
                    EloBuddy.Player.CastSpell(firstOrDefault);
                }
                else if (Ids.Mercurial.IsReady())
                    if (mercurial != null)
                    {
                        var firstOrDefault =
                            mercurial.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
                    else if (Ids.Dervish.IsReady())
                        if (dervish != null)
                        {
                            var firstOrDefault =
                                dervish.SpellSlot;
                            EloBuddy.Player.CastSpell(firstOrDefault);
                        }
        }

        private static void Defensive()
        {
            var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == ItemId.Randuins_Omen);
            if (Ids.Randuin.IsReady() && Config.Randuin && Player.CountEnemiesInRange(Ids.Randuin.Range) > 0)
            {
                if (inventorySlot != null)
                {
                    var firstOrDefault =
                        inventorySlot.SpellSlot;
                    EloBuddy.Player.CastSpell(firstOrDefault);
                }
            }
        }

        private static void Ignite()
        {
            if (CanUse(ignite) && Config.Ignite)
            {
                var enemy = TargetSelector.GetTarget(600, DamageType.True);
                if (enemy.IsValidTarget())
                {
                    var pred = enemy.Health - Utils.GetIncomingDamage(enemy);
                    if (pred < 0)
                        return;

                    var IgnDmg = Player.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite);

                    if (pred <= IgnDmg && Player.Distance(enemy.ServerPosition) > 500 &&
                        enemy.CountAlliesInRange(500) < 2)
                        Player.Spellbook.CastSpell(ignite, enemy);

                    if (pred <= 2*IgnDmg)
                    {
                        if (enemy.PercentLifeStealMod > 10)
                            Player.Spellbook.CastSpell(ignite, enemy);

                        if (enemy.HasBuff("RegenerationPotion") || enemy.HasBuff("ItemMiniRegenPotion") ||
                            enemy.HasBuff("ItemCrystalFlask"))
                            Player.Spellbook.CastSpell(ignite, enemy);

                        if (enemy.Health > Player.Health)
                            Player.Spellbook.CastSpell(ignite, enemy);
                    }
                }
            }
        }

        protected static void OnInterruptable(Obj_AI_Base sender,
            Interrupter.InterruptableSpellEventArgs args)
        {
            {
                if (Config.Exhaust1)
                {
                    foreach (
                        var enemy in
                            Enemies.Where(enemy => enemy.IsValidTarget(650)))
                    {
                        Player.Spellbook.CastSpell(exhaust, enemy);
                    }
                }
            }
        }

        private static void Exhaust()
        {
            if (Config.Exhaust2 && Combo)
            {
                var t = TargetSelector.GetTarget(650, DamageType.Physical);
                if (t.IsValidTarget())
                {
                    Player.Spellbook.CastSpell(exhaust, t);
                }
            }
        }

        private static void PotionManagement()
        {
            if (Player.HasBuff("RegenerationPotion") || Player.HasBuff("ItemMiniRegenPotion") ||
                Player.HasBuff("ItemCrystalFlaskJungle") || Player.HasBuff("ItemDarkCrystalFlask"))
                return;

            if (Ids.Potion.IsReady())
            {
                var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == ItemId.Health_Potion);
                if (Player.Health + 200 < Player.MaxHealth && Player.CountEnemiesInRange(700) > 0)
                {
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
                }
                else if (Player.Health < Player.MaxHealth*0.6)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
            }
            else if (Ids.Biscuit.IsReady())
            {
                var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Biscuit.Id);
                if (Player.Health + 250 < Player.MaxHealth && Player.CountEnemiesInRange(700) > 0)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
                if (Player.Health < Player.MaxHealth*0.6)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
            }
            else if (Ids.Hunter.IsReady())
            {
                var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Hunter.Id);
                if (Player.Health + 250 < Player.MaxHealth && Player.CountEnemiesInRange(700) > 0)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
                if (Player.Health < Player.MaxHealth*0.6)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
            }
            else if (Ids.Corrupting.IsReady())
            {
                var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Corrupting.Id);
                if (Player.Health + 250 < Player.MaxHealth && Player.CountEnemiesInRange(700) > 0)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
                if (Player.Health < Player.MaxHealth*0.6)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
            }
            else if (Ids.Refillable.IsReady())
            {
                var inventorySlot = Player.InventoryItems.FirstOrDefault(item => item.Id == Ids.Refillable.Id);
                if (Player.Health + 250 < Player.MaxHealth && Player.CountEnemiesInRange(700) > 0)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
                if (Player.Health < Player.MaxHealth*0.6)
                    if (inventorySlot != null)
                    {
                        var firstOrDefault =
                            inventorySlot.SpellSlot;
                        EloBuddy.Player.CastSpell(firstOrDefault);
                    }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsRecalling() || Player.IsDead)
                return;

            Cleansers();
            Survival();

            if (Config.pots)
                PotionManagement();

            Ignite();
            Exhaust();
            Defensive();
            if (Combo && Config.AACombo)
            {
                if (!E.IsReady())
                    Orbwalker.DisableAttacking = false;

                else
                    Orbwalker.DisableAttacking = true;
            }
            else
                Orbwalker.DisableAttacking = false;


            if (Q.IsReady())
                LogicQ();
            if (E.IsReady() && Config.autoE)
                LogicE();
            if (W.IsReady())
                LogicW();
            if (R.IsReady())
                LogicR();
        }

        private static void LogicE()
        {
            var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (t.IsValidTarget() && !t.HasBuff("ThreshQ") && Utils.CanMove(t))
            {  
                if (Combo)
                {
                    CastE(false, t);
                }
                else if (Config.pushE)
                {
                    CastE(true, t);
                }
      
            }
        }

        private static void LogicQ()
        {
            foreach (
                var enemy in
                    Enemies.Where(
                        enemy =>
                            enemy.IsValidTarget(Q.Range + 300) && enemy.HasBuff("ThreshQ") && !enemy.IsMinion &&
                            !enemy.IsMonster))
            {
                if (Combo)
                {
                    if (W.IsReady() && Config.autoW2)
                    {
                        foreach (
                            var ally in
                                Allies.Where(
                                    ally =>
                                        ally.IsValid && !ally.IsDead &&
                                        Player.Distance(ally.ServerPosition) < W.Range + 500))
                        {
                            if (enemy.Distance(ally.ServerPosition) > 800 && Player.Distance(ally.ServerPosition) > 600)
                            {
                                CastW(W.GetPrediction(ally).CastPosition);
                            }
                        }
                    }

                    if (Utils.GetPassiveTime(enemy, "ThreshQ") < 0.4)
                        Q2.Cast();
                }
                return;
            }

            if (Combo && Config.ts)
            {
                var t = TargetSelector.GetTarget(Config.maxGrab, DamageType.Physical);

                if (t.IsValidTarget(Config.maxGrab) && !t.HasBuffOfType(BuffType.SpellImmunity) &&
                    !t.HasBuffOfType(BuffType.SpellShield) && QMenu["grab" + t.ChampionName].Cast<CheckBox>().CurrentValue && Player.Distance(t.ServerPosition) > Config.minGrab)
                    CastSpell(Q, t, predQ(), Config.maxGrab);
            }


            foreach (var t in Enemies.Where(t => t.IsValidTarget(Config.maxGrab) && QMenu["grab" + t.ChampionName].Cast<CheckBox>().CurrentValue))
            {
                if (!t.HasBuffOfType(BuffType.SpellImmunity) && !t.HasBuffOfType(BuffType.SpellShield) &&
                    Player.Distance(t.ServerPosition) > Config.minGrab)
                {
                    if (Combo && !Config.ts)
                        CastSpell(Q, t, predQ(), Config.maxGrab);

                    if (Config.qCC)
                    {
                        if (!Utils.CanMove(t))
                            Q.Cast(t);
                        var pred = Q.GetPrediction(t);
                        if (pred.HitChance == (EloBuddy.SDK.Enumerations.HitChance)HitChance.Dashing)
                        {
                            Q.Cast(t);
                        }
                        if (pred.HitChance == (EloBuddy.SDK.Enumerations.HitChance)HitChance.Immobile)
                        {
                            Q.Cast(t);
                        }
                    }
                }
            }
        }

        private static bool collision;
        private static void Qcoltick(EventArgs args)
        {
            if (Orbwalker.GetTarget() != null && Orbwalker.GetTarget() is AIHeroClient)
            {
                var target = Orbwalker.GetTarget() as AIHeroClient;
            var pred = Q.GetPrediction(target);
                if (pred.CollisionObjects.Any())
                {
                    collision = true;
                }
                else
                {
                    collision = false;
                }
            }
        }
        internal static bool IsAutoAttacking;
        private static void CastSpell(Spell.Skillshot QWER, Obj_AI_Base target, HitChance hitchance, int MaxRange)
        {
            var coreType2 = SkillshotType.SkillshotLine;
            var aoe2 = false;
            if ((int)QWER.Type == (int)SkillshotType.SkillshotCircle)
            {
                coreType2 = SkillshotType.SkillshotCircle;
                aoe2 = true;
            }
            if (QWER.Width > 80 && QWER.AllowedCollisionCount < 100)
                aoe2 = true;
            var predInput2 = new PredictionInput
            {
                Aoe = aoe2,
                Collision = QWER.AllowedCollisionCount < 100,
                Speed = QWER.Speed,
                Delay = QWER.CastDelay,
                Range = MaxRange,
                From = Player.ServerPosition,
                Radius = QWER.Radius,
                Unit = target,
                Type = coreType2
            };
            var poutput2 = Prediction.GetPrediction(predInput2);
            Chat.Print(QWER.Slot + " " + predInput2.Collision + poutput2.Hitchance);
            if (QWER.Speed < float.MaxValue && Utils.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                return;

            if (hitchance == HitChance.VeryHigh)
            {
                if (poutput2.Hitchance >= HitChance.VeryHigh)
                    QWER.Cast(poutput2.CastPosition);
                else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 &&
                         poutput2.Hitchance >= HitChance.High)
                {
                    QWER.Cast(poutput2.CastPosition);
                }
            }
            else if (hitchance == HitChance.High)
            {
                if (poutput2.Hitchance >= HitChance.High)
                    QWER.Cast(poutput2.CastPosition);
            }
            else if (hitchance == HitChance.Medium)
            {
                if (poutput2.Hitchance >= HitChance.Medium)
                    QWER.Cast(poutput2.CastPosition);
            }
            else if (hitchance == HitChance.Low)
            {
                if (poutput2.Hitchance >= HitChance.Low)
                    QWER.Cast(poutput2.CastPosition);
            }
        }

        protected static void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsAutoAttacking = false;
        }

        protected static void OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsAutoAttacking = true;
        }

        public static float RDamage(Obj_AI_Base target)
        {
            return ObjectManager.Player.CalculateDamageOnUnit(target, DamageType.Physical,
                (float)
                    (new double[] {80, 120, 160, 200, 240}[
                        ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level - 1]
                     + 1*(ObjectManager.Player.TotalMagicalDamage)));
        }

        private static void LogicR()
        {
            var rKs = Config.rKs;
            foreach (
                var target in Enemies.Where(target => target.IsValidTarget(R.Range) && target.HasBuff("rocketgrab2")))
            {
                if (rKs && RDamage(target) > target.Health)
                    R.Cast();
            }
            if (Player.CountEnemiesInRange(R.Range) >= Config.rCount && Config.rCount > 0)
                R.Cast();
            if (Config.comboR)
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (t.IsValidTarget() && ((Player.UnderTurret(false) && !Player.UnderTurret(true)) || Combo))
                {
                    if (ObjectManager.Player.Distance(t.ServerPosition) > ObjectManager.Player.Distance(t.Position))
                        R.Cast();
                }
            }
        }

        private static void CastW(Vector3 pos)
        {
            if (Player.Distance(pos) < W.Range)
                W.Cast(pos);
            else
                W.Cast(Player.Position.ExtendVector3(pos, W.Range));
        }

        private static void LogicW()
        {
            if (Allies.Any())
            foreach (var ally in Allies.Where(ally => ally.IsValid && !ally.IsDead && Player.Distance(ally) < W.Range))
            {
                var nearEnemys = ally.CountEnemiesInRange(900);

                if (nearEnemys >= Config.wCount && Config.wCount > 0)
                    CastW(W.GetPrediction(ally).CastPosition);

                if (Config.autoW)
                {
                    var dmg = Utils.GetIncomingDamage(ally);
                    if (dmg == 0)
                        continue;

                    var sensitivity = 20;

                    var HpPercentage = (dmg*100)/ally.Health;
                    var shieldValue = 20 + (Player.Level*20) + (0.4*Player.FlatMagicDamageMod);

                    nearEnemys = (nearEnemys == 0) ? 1 : nearEnemys;

                    if (dmg > shieldValue && Config.autoW3)
                        W.Cast(W.GetPrediction(ally).CastPosition);
                    else if (dmg > 100 + Player.Level*sensitivity)
                        W.Cast(W.GetPrediction(ally).CastPosition);
                    else if (ally.Health - dmg < nearEnemys*ally.Level*sensitivity)
                        W.Cast(W.GetPrediction(ally).CastPosition);
                    else if (HpPercentage >= Config.Wdmg)
                        W.Cast(W.GetPrediction(ally).CastPosition);
                }
            }
        }
      

        private static void CastE(bool pull, AIHeroClient target)
        {
            var coreType2 = SkillshotType.SkillshotLine;
            var aoe2 = false;
            if ((int)E.Type == (int)SkillshotType.SkillshotCircle)
            {
                coreType2 = SkillshotType.SkillshotCircle;
                aoe2 = true;
            }
            if (E.Width > 80 && E.AllowedCollisionCount < 100)
                aoe2 = true;
            var predInput2 = new PredictionInput
            {
                Aoe = aoe2,
                Collision = E.AllowedCollisionCount < 100,
                Speed = E.Speed,
                Delay = E.CastDelay,
                Range = E.Range,
                From = Player.ServerPosition,
                Radius = E.Radius,
                Unit = target,
                Type = coreType2
            };
            var eprediction = Utility.Prediction.GetPrediction(predInput2);
            if (pull && eprediction.Hitchance >= predE())
            {
                CastSpell(E, target, predE(), (int) E.Range);
            }
            else
            {
                var position = Player.ServerPosition - (eprediction.CastPosition - Player.ServerPosition);
                E.Cast(position);
            }
        }

        private static HitChance predQ()
        {
            switch (Config.Qpred)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
            }
            return HitChance.Medium;
        }

        private static HitChance predE()
        {
            switch (Config.Epred)
            {
                case 0:
                    return HitChance.Low; ;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
            }
            return HitChance.Medium;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.qRange)
            {
                if (Config.onlyRdy)
                {
                    if (Q.IsReady())
                        Drawing.DrawCircle(Player.Position, Config.maxGrab, Color.Cyan);
                }
                else
                    Drawing.DrawCircle(Player.Position, Config.maxGrab, Color.Cyan);
            }

            if (Config.wRange)
            {
                if (Config.onlyRdy)
                {
                    if (E.IsReady())
                        Drawing.DrawCircle(Player.Position, W.Range, Color.Cyan);
                }
                else
                    Drawing.DrawCircle(Player.Position, W.Range, Color.Cyan);
            }

            if (Config.eRange)
            {
                if (Config.onlyRdy)
                {
                    if (E.IsReady())
                        Drawing.DrawCircle(Player.Position, E.Range, Color.Orange);
                }
                else
                    Drawing.DrawCircle(Player.Position, E.Range, Color.Orange);
            }

            if (Config.rRange)
            {
                if (Config.onlyRdy)
                {
                    if (R.IsReady())
                        Drawing.DrawCircle(Player.Position, R.Range, Color.Gray);
                }
                else
                    Drawing.DrawCircle(Player.Position, R.Range, Color.Gray);
            }
        }

        public static class Ids
        {
            //Cleans
            public static readonly Item Mikaels = new Item(3222, 600f);
            public static readonly Item Quicksilver = new Item(3140);
            public static readonly Item Mercurial = new Item(3139);
            public static readonly Item Dervish = new Item(3137);
            //REGEN
            public static readonly Item Potion = new Item(2003);
            public static readonly Item ManaPotion = new Item(2004);
            public static readonly Item Flask = new Item(204);
            public static readonly Item Biscuit = new Item(2010);
            public static readonly Item Refillable = new Item(2031);
            public static readonly Item Hunter = new Item(2032);
            public static readonly Item Corrupting = new Item(2033);
            //def
            public static readonly Item FaceOfTheMountain = new Item(ItemId.Face_of_the_Mountain, 600f);
            public static readonly Item Zhonya = new Item(3157);
            public static readonly Item Seraph = new Item(3040);
            public static readonly Item Solari = new Item(3190, 600f);
            public static readonly Item Randuin = new Item(3143, 400f);
        }

        public static class Config
        {
            public static bool AACombo
            {
                get { return ThreshMenu["AACombo"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool ts
            {
                get { return QMenu["ts"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool ts1
            {
                get { return QMenu["ts1"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool ts2
            {
                get { return QMenu["ts2"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool qCC
            {
                get { return QMenu["qCC"].Cast<CheckBox>().CurrentValue; }
            }

            public static int minGrab
            {
                get { return QMenu["minGrab"].Cast<Slider>().CurrentValue; }
            }

            public static int maxGrab
            {
                get { return QMenu["maxGrab"].Cast<Slider>().CurrentValue; }
            }

            public static bool GapQ
            {
                get { return QMenu["GapQ"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoW
            {
                get { return WMenu["autoW"].Cast<CheckBox>().CurrentValue; }
            }

            public static int Wdmg
            {
                get { return WMenu["Wdmg"].Cast<Slider>().CurrentValue; }
            }

            public static bool autoW2
            {
                get { return WMenu["autoW2"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool autoW3
            {
                get { return WMenu["autoW3"].Cast<CheckBox>().CurrentValue; }
            }

            public static int wCount
            {
                get { return WMenu["wCount"].Cast<Slider>().CurrentValue; }
            }

            public static bool autoE
            {
                get { return EMenu["autoE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool pushE
            {
                get { return EMenu["pushE"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool inter
            {
                get { return EMenu["inter"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Gap
            {
                get { return EMenu["Gap"].Cast<CheckBox>().CurrentValue; }
            }

            public static int rCount
            {
                get { return RMenu["rCount"].Cast<Slider>().CurrentValue; }
            }

            public static bool rKs
            {
                get { return RMenu["rKs"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool comboR
            {
                get { return RMenu["comboR"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool qRange
            {
                get { return DrawingsMenu["qRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool wRange
            {
                get { return DrawingsMenu["wRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool eRange
            {
                get { return DrawingsMenu["eRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool rRange
            {
                get { return DrawingsMenu["rRange"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool onlyRdy
            {
                get { return DrawingsMenu["onlyRdy"].Cast<CheckBox>().CurrentValue; }
            }

            public static int Qpred
            {
                get { return PredictionMenu["Qpred"].Cast<Slider>().CurrentValue; }
            }

            public static int Epred
            {
                get { return PredictionMenu["Epred"].Cast<Slider>().CurrentValue; }
            }

            public static bool Exhaust
            {
                get { return Activator["Exhaust"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Exhaust1
            {
                get { return Activator["Exhaust1"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Exhaust2
            {
                get { return Activator["Exhaust2"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Heal
            {
                get { return Activator["Heal"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool AllyHeal
            {
                get { return Activator["AllyHeal"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Ignite
            {
                get { return Activator["Ignite"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool pots
            {
                get { return Activator["pots"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Randuin
            {
                get { return Activator["Randuin"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool FaceOfTheMountain
            {
                get { return Activator["FaceOfTheMountain"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Seraph
            {
                get { return Activator["Seraph"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Solari
            {
                get { return Activator["Solari"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Clean
            {
                get { return Activator["Clean"].Cast<CheckBox>().CurrentValue; }
            }

            public static int cleanHP
            {
                get { return Activator["cleanHP"].Cast<Slider>().CurrentValue; }
            }

            public static bool CleanSpells
            {
                get { return Activator["CleanSpells"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Stun
            {
                get { return Activator["Stun"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Snare
            {
                get { return Activator["Snare"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Charm
            {
                get { return Activator["Charm"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Fear
            {
                get { return Activator["Fear"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Suppression
            {
                get { return Activator["Suppression"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Taunt
            {
                get { return Activator["Taunt"].Cast<CheckBox>().CurrentValue; }
            }

            public static bool Blind
            {
                get { return Activator["Blind"].Cast<CheckBox>().CurrentValue; }
            }
        }
    }
}