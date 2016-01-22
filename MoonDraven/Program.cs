// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoonDraven.cs" company="ChewyMoon">
//   Copyright (C) 2015 ChewyMoon
//   
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General public static License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//   
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General public static License for more details.
//   
//   You should have received a copy of the GNU General public static License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <summary>
//   The MoonDraven class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Draven;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;

namespace MoonDraven
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using EloBuddy;
    using EloBuddy.SDK;

    using SharpDX;

    using Color = System.Drawing.Color;
    using EloBuddy.SDK.Menu;

    /// <summary>
    ///     The MoonDraven class.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly",
		Justification = "Reviewed. Suppression is OK here.")]
	internal class MoonDraven
	{
		#region public static Properties

		/// <summary>
		///     Gets or sets the e.
		/// </summary>
		/// <value>
		///     The e.
		/// </value>
		public static Spell.Skillshot E { get; set; }

		/// <summary>
		///     Gets the mana percent.
		/// </summary>
		/// <value>
		///     The mana percent.
		/// </value>
		public static float ManaPercent
		{
			get
			{
				return Player.Mana / Player.MaxMana * 100;
			}
		}

		/// <summary>
		///     Gets or sets the menu.
		/// </summary>
		/// <value>
		///     The menu.
		/// </value>
		public static Menu Menu { get; set; }

	
		/// <summary>
		///     Gets the player.
		/// </summary>
		/// <value>
		///     The player.
		/// </value>
		public static AIHeroClient Player
		{
			get
			{
				return ObjectManager.Player;
			}
		}

		/// <summary>
		///     Gets or sets the q.
		/// </summary>
		/// <value>
		///     The q.
		/// </value>
		public static Spell.Active Q { get; set; }

		/// <summary>
		///     Gets the q count.
		/// </summary>
		/// <value>
		///     The q count.
		/// </value>
		public static int QCount
		{
			get
			{
				return (Player.HasBuff("dravenspinning") ? 1 : 0)
					+ (Player.HasBuff("dravenspinningleft") ? 1 : 0) + QReticles.Count;
			}
		}

		/// <summary>
		///     Gets or sets the q reticles.
		/// </summary>
		/// <value>
		///     The q reticles.
		/// </value>
		public static List<QRecticle> QReticles { get; set; }

		/// <summary>
		///     Gets or sets the r.
		/// </summary>
		/// <value>
		///     The r.
		/// </value>
		public static Spell.Skillshot R { get; set; }

		/// <summary>
		///     Gets or sets the w.
		/// </summary>
		/// <value>
		///     The w.
		/// </value>
		public static Spell.Active W { get; set; }

		#endregion

		#region Properties

		/// <summary>
		///     Gets or sets the last axe move time.
		/// </summary>
		private int LastAxeMoveTime { get; set; }

        #endregion

        #region public static Methods and Operators

        private static void GameOnOnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName == "Draven")
            {
                Load();
            }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += GameOnOnGameLoad;
        }

        public static bool canMove = true;
        public static bool canAttack = true;

        /// <summary>
        ///     Loads this instance.
        /// </summary>
        public static void Load()
		{
			// Create spells
			Q = new Spell.Active(SpellSlot.Q, (uint)Player.GetAutoAttackRange());
			W = new Spell.Active(SpellSlot.W);
			E = new Spell.Skillshot(SpellSlot.E, 1050, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 250, 1400, 130);
			R = new Spell.Skillshot(SpellSlot.R, int.MaxValue, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 400, 200, 160);

			QReticles = new List<QRecticle>();

			CreateMenu();

            Chat.Print("<font color=\"#7CFC00\"><b>MoonDraven:</b></font> Loaded");

			//Obj_AI_Base.OnNewPath += Obj_AI_Base_OnNewPath;
			GameObject.OnCreate += GameObjectOnOnCreate;
			GameObject.OnDelete += GameObjectOnOnDelete;
			Gapcloser.OnGapcloser += AntiGapcloserOnOnEnemyGapcloser;
			Interrupter.OnInterruptableSpell += Interrupter2OnOnInterruptableTarget;
			Drawing.OnDraw += DrawingOnOnDraw;
			Game.OnUpdate += GameOnOnUpdate;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Called on an enemy gapcloser.
		/// </summary>
		/// <param name="gapcloser">The gapcloser.</param>
		private static void AntiGapcloserOnOnEnemyGapcloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs gapcloser)
		{
			if (!Getcheckboxvalue(MiscMenu, "UseEGapcloser") || !E.IsReady()
				|| !gapcloser.Sender.IsValidTarget(E.Range))
			{
				return;
			}

			E.Cast(gapcloser.Sender);
		}

        public static bool catchingaxe;
		/// <summary>
		///     Catches the axe.
		/// </summary>
		private static void CatchAxe()
		{
			var catchOption = Getslidervalue(AxeMenu, "AxeMode");

			if (((catchOption == 0 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
				|| (catchOption == 1 && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None)))
				|| catchOption == 2)
			{
				var bestReticle =
					QReticles.Where(
						x =>
						x.Object.Position.Distance(Game.CursorPos)
						< Getslidervalue(AxeMenu, "CatchAxeRange"))
						.OrderBy(x => x.Position.Distance(Player.ServerPosition))
						.ThenBy(x => x.Position.Distance(Game.CursorPos))
						.ThenBy(x => x.ExpireTime)
						.FirstOrDefault();

				if (bestReticle != null && bestReticle.Object.Position.Distance(Player.ServerPosition) > 100)
				{
					var eta = 1000 * (Player.Distance(bestReticle.Position) / Player.MoveSpeed);
					var expireTime = bestReticle.ExpireTime - Environment.TickCount;

					if (eta >= expireTime && Getcheckboxvalue(AxeMenu, "UseWForQ"))
					{
						W.Cast();
					}

					if (Getcheckboxvalue(AxeMenu, "DontCatchUnderTurret"))
					{
						// If we're under the turret as well as the axe, catch the axe
						if (Player.IsUnderEnemyturret() && bestReticle.Object.Position.UnderTurret(true))
						{
							if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
							{								
                                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
							else
							{

                                    Orbwalker.DisableMovement = true;
                                Orbwalker.OrbwalkTo(bestReticle.Position);
                            }
						}
						else if (!bestReticle.Position.UnderTurret(true))
						{
							if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
							{

                                    Orbwalker.DisableMovement = true;
                                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
							else
							{

                                    Orbwalker.DisableMovement = true;
                                Orbwalker.OrbwalkTo(bestReticle.Position);
							}
						}
					}
					else
					{
						if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
						{

                                Orbwalker.DisableMovement = true;
                            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);                         
                        }
						else
						{

                                Orbwalker.DisableMovement = true;
                            Orbwalker.OrbwalkTo(bestReticle.Position);
						}
					}
				}
				else
				{  
                    Orbwalker.DisableMovement = false;
                    // Orbwalker.OrbwalkTo(Game.CursorPos);
                }
			}
			else
			{
               // Orbwalker.DisableMovement = false;
                //  Orbwalker.OrbwalkTo(Game.CursorPos);
            }
		}

		/// <summary>
		///     Does the combo.
		/// </summary>
		private static void Combo()
		{
			var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

			if (!target.IsValidTarget())
			{
				return;
			}

			var useQ = Getcheckboxvalue(ComboMenu, "UseQCombo");
			var useW = Getcheckboxvalue(ComboMenu, "UseWCombo");
            var useE = Getcheckboxvalue(ComboMenu, "UseECombo");
            var useR = Getcheckboxvalue(ComboMenu, "UseRCombo");

            if (useQ && QCount < Getslidervalue(AxeMenu, "MaxAxes") - 1 && Q.IsReady()
				&& Player.IsInAutoAttackRange(target) && !Player.Spellbook.IsAutoAttacking)
			{
				Q.Cast();
			}

			if (useW && W.IsReady()
				&& ManaPercent > Getslidervalue(MiscMenu, "UseWManaPercent"))
			{
				if (Getcheckboxvalue(MiscMenu, "UseWSetting"))
				{
					W.Cast();
				}
				else
				{
					if (!Player.HasBuff("dravenfurybuff"))
					{
						W.Cast();
					}
				}
			}

			if (useE && E.IsReady())
			{
				E.Cast(target);
			}

			if (!useR || !R.IsReady())
			{
				return;
			}

			// Patented Advanced Algorithms D321987
			var killableTarget =
				HeroManager.Enemies.Where(x => x.IsValidTarget(2000))
					.FirstOrDefault(
						x =>
						Player.GetSpellDamage(x, SpellSlot.R) * 2 > x.Health
						&& (!Player.IsInAutoAttackRange(x) || Player.CountEnemiesInRange(E.Range) > 2));

			if (killableTarget != null)
			{
				R.Cast(killableTarget);
			}
		}

        private static Menu ComboMenu, HarassMenu, AxeMenu, LaneClearMenu, MiscMenu, DrawingsMenu;
		private static void CreateMenu()
		{
			Menu = MainMenu.AddMenu("MoonDraven", "cmMoonDraven");

		    ComboMenu = Menu.AddSubMenu("Combo");
		    ComboMenu.Add("UseQCombo", new CheckBox("Use Q"));
            ComboMenu.Add("UseWCombo", new CheckBox("Use W"));
            ComboMenu.Add("UseECombo", new CheckBox("Use E"));
            ComboMenu.Add("UseRCombo", new CheckBox("Use R"));


		    HarassMenu = Menu.AddSubMenu("Harass");
            HarassMenu.Add("UseEHarass", new CheckBox("Use E"));
		    HarassMenu.Add("UseHarassToggle", new KeyBind("Harass! (Toggle)", false, KeyBind.BindTypes.PressToggle, 84));

		    LaneClearMenu = Menu.AddSubMenu("Wave Clear");
		    LaneClearMenu.Add("UseQWaveClear", new CheckBox("Use Q"));
            LaneClearMenu.Add("UseWWaveClear", new CheckBox("Use W"));
            LaneClearMenu.Add("UseEWaveClear", new CheckBox("Use E", false));
            LaneClearMenu.Add("WaveClearManaPercent", new Slider("Mana Percent", 50));

		    AxeMenu = Menu.AddSubMenu("Axe Settings");
            StringList(AxeMenu, "AxeMode", "Catch Axe on Mode:", new []{ "Combo", "Any", "Always" }, 2);
            AxeMenu.Add("CatchAxeRange", new Slider("Catch Axe Range", 800, 120, 1500));
            AxeMenu.Add("MaxAxes", new Slider("Maximum Axes", 2, 1, 3));
            AxeMenu.Add("UseWForQ", new CheckBox("Use W if Axe too far"));
            AxeMenu.Add("DontCatchUnderTurret", new CheckBox("Don't Catch Axe Under Turret"));

            DrawingsMenu = Menu.AddSubMenu("Drawing");
            DrawingsMenu.Add("DrawE", new CheckBox("Draw E"));
            DrawingsMenu.Add("DrawAxeLocation", new CheckBox("Draw Axe Location"));
            DrawingsMenu.Add("DrawAxeRange", new CheckBox("Draw Axe Catch Range"));

            MiscMenu = Menu.AddSubMenu("Misc");
            MiscMenu.Add("UseWSetting", new CheckBox("Use W Instantly(When Available)", false));
            MiscMenu.Add("UseEGapcloser", new CheckBox("Use E on Gapcloser"));
            MiscMenu.Add("UseEInterrupt", new CheckBox("Use E to Interrupt"));
            MiscMenu.Add("UseWManaPercent", new Slider("Use W Mana Percent", 50));
            MiscMenu.Add("UseWSlow", new CheckBox("Use W if Slowed"));

		}

        private static bool Getcheckboxvalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<CheckBox>().CurrentValue;
        }
        private static bool Getkeybindvalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<KeyBind>().CurrentValue;
        }
        private static int Getslidervalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<Slider>().CurrentValue;
        }

        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }

        /// <summary>
        ///     Called when the game draws itself.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void DrawingOnOnDraw(EventArgs args)
		{
			var drawE = Getcheckboxvalue(DrawingsMenu, "DrawE");
			var drawAxeLocation = Getcheckboxvalue(DrawingsMenu, "DrawAxeLocation");
			var drawAxeRange = Getcheckboxvalue(DrawingsMenu, "DrawAxeRange");

			if (drawE)
			{
				Drawing.DrawCircle(
					ObjectManager.Player.Position,
					E.Range,
					E.IsReady() ? Color.Aqua : Color.Red);
			}

			if (drawAxeLocation)
			{
				var bestAxe =
					QReticles.Where(
						x =>
						x.Position.Distance(Game.CursorPos) < Getslidervalue(AxeMenu, "CatchAxeRange"))
						.OrderBy(x => x.Position.Distance(Player.ServerPosition))
						.ThenBy(x => x.Position.Distance(Game.CursorPos))
						.FirstOrDefault();

				if (bestAxe != null)
				{
                    Drawing.DrawCircle(bestAxe.Position, 120, Color.LimeGreen);
				}

				foreach (var axe in
					QReticles.Where(x => x.Object.NetworkId != (bestAxe == null ? 0 : bestAxe.Object.NetworkId)))
				{
                    Drawing.DrawCircle(axe.Position, 120, Color.Yellow);
				}
			}

			if (drawAxeRange)
			{
                Drawing.DrawCircle(
					Game.CursorPos,
					Getslidervalue(AxeMenu, "CatchAxeRange"),
					Color.DodgerBlue);
			}
		}

		/// <summary>
		///     Called when a game object is created.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
		private static void GameObjectOnOnCreate(GameObject sender, EventArgs args)
		{
			if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
			{
				return;
			}

			QReticles.Add(new QRecticle(sender, Environment.TickCount + 1800));
			Core.DelayAction(() => QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId), 1800);
		}

		/// <summary>
		///     Called when a game object is deleted.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
		private static void GameObjectOnOnDelete(GameObject sender, EventArgs args)
		{
			if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
			{
				return;
			}

			QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId);
		}

		/// <summary>
		///     Called when the game updates.
		/// </summary>
		/// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
		private static void GameOnOnUpdate(EventArgs args)
		{
			QReticles.RemoveAll(x => x.Object.IsDead);
            foreach (var a in QReticles)
            {
                if (!a.CanOrbwalkWithUserDelay)
                {
                    canMove = false;
                }
                if (!a.CanAttack)
                {
                    canAttack = false;
                }
            }

            CatchAxe();

			if (W.IsReady() && Getcheckboxvalue(MiscMenu, "UseWSlow") && Player.HasBuffOfType(BuffType.Slow))
			{
				W.Cast();
			}

			switch (Orbwalker.ActiveModesFlags)
			{
			case Orbwalker.ActiveModes.Harass:
				Harass();
				break;
			case Orbwalker.ActiveModes.LaneClear:
				LaneClear();
				break;
			case Orbwalker.ActiveModes.Combo:
				Combo();
				break;
			}

			if (Getkeybindvalue(HarassMenu, "UseHarassToggle"))
			{
				Harass();
			}
		}

		/// <summary>
		///     Harasses the enemy.
		/// </summary>
		private static void Harass()
		{
			var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

			if (!target.IsValidTarget())
			{
				return;
			}

			if (Getcheckboxvalue(HarassMenu, "UseEHarass") && E.IsReady())
			{
				E.Cast(target);
			}
		}

		/// <summary>
		///     Interrupts an interruptable target.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="Interrupter2.InterruptableTargetEventArgs" /> instance containing the event data.</param>
		private static void Interrupter2OnOnInterruptableTarget(
			Obj_AI_Base sender,
			Interrupter.InterruptableSpellEventArgs args)
		{
			if (!Getcheckboxvalue(MiscMenu, "UseEInterrupt") || !E.IsReady() || !sender.IsValidTarget(E.Range))
			{
				return;
			}

			if (args.DangerLevel == DangerLevel.Medium || args.DangerLevel == DangerLevel.High)
			{
				E.Cast(sender);
			}
		}

		/// <summary>
		///     Clears the lane of minions.
		/// </summary>
		private static void LaneClear()
		{
			var useQ = Getcheckboxvalue(LaneClearMenu, "UseQWaveClear");
			var useW = Getcheckboxvalue(LaneClearMenu, "UseWWaveClear");
			var useE = Getcheckboxvalue(LaneClearMenu, "UseEWaveClear");

			if (ManaPercent < Getslidervalue(LaneClearMenu, "WaveClearManaPercent"))
			{
				return;
			}

			if (useQ && QCount < Getslidervalue(AxeMenu, "MaxAxes") - 1 && Q.IsReady()
				&& Orbwalker.GetTarget() is Obj_AI_Minion && !Player.Spellbook.IsAutoAttacking
				&& !Player.Spellbook.IsAutoAttacking)
			{
				Q.Cast();
			}

			if (useW && W.IsReady()
				&& ManaPercent > Getslidervalue(MiscMenu, "UseWManaPercent"))
			{
				if (Getcheckboxvalue(MiscMenu, "UseWSetting"))
				{
					W.Cast();
				}
				else
				{
					if (!Player.HasBuff("dravenfurybuff"))
					{
						W.Cast();
					}
				}
			}

			if (!useE || !E.IsReady())
			{
				return;
			}
		    var bestLocation =
		        EntityManager.MinionsAndMonsters.GetLineFarmLocation(
		            EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position,
		                E.Range), E.Width, (int) E.Range);

			if (bestLocation.HitNumber > 1)
			{
				E.Cast(bestLocation.CastPosition);
			}
		}

		/// <summary>
		///     Fired when the OnNewPath event is called.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="GameObjectNewPathEventArgs" /> instance containing the event data.</param>
		private static void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
		{
			if (!sender.IsMe)
			{
				return;
			}

			CatchAxe();
		}

		#endregion

		/// <summary>
		///     A represenation of a Q circle on Draven.
		/// </summary>
		internal class QRecticle
		{
			#region Constructors and Destructors

			/// <summary>
			///     Initializes a new instance of the <see cref="QRecticle" /> class.
			/// </summary>
			/// <param name="rectice">The rectice.</param>
			/// <param name="expireTime">The expire time.</param>
			public QRecticle(GameObject rectice, int expireTime)
			{
				Object = rectice;
				ExpireTime = expireTime;
			}

			#endregion

			#region public static Properties

			/// <summary>
			///     Gets or sets the expire time.
			/// </summary>
			/// <value>
			///     The expire time.
			/// </value>
			public int ExpireTime { get; set; }

			/// <summary>
			///     Gets or sets the object.
			/// </summary>
			/// <value>
			///     The object.
			/// </value>
			public GameObject Object { get; set; }
            public float TimeLeft
            {
                get
                {
                    if (MissileIsValid || ReticleIsValid)
                        return LimitTime - (Game.Time - this.StartTime);//2 * Extensions.Distance(this.Reticle.Position, this.Missile.Position) / this.Speed; //
                    return float.MaxValue;
                }

            }
            public MissileClient Missile = null;
            public bool MissileIsValid
            {
                get
                {
                    return Missile != null && Missile.IsValid;
                }
            }

            /// <summary>
            ///     Gets the position.
            /// </summary>
            /// <value>
            ///     The position.
            /// </value>
            public Vector3 Position
			{
				get
				{
					return Object.Position;
				}
			}

			#endregion
		}
	}
}