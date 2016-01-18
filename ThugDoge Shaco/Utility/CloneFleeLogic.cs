using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Security.Cryptography;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;


namespace Shaco.Utility
{
    class CloneFleeLogic
    {
        public static Obj_AI_Base Clone;

        private static Obj_AI_Base Player
        {
            get { return ObjectManager.Player; }
        }
        public static LastSpellCast LastSpell = new LastSpellCast();
        public static List<LastSpellCast> LastSpellsCast = new List<LastSpellCast>();
        public static void FleeLogic()
        {
            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;      
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender == null || !sender.IsValid || !sender.Name.Equals(Player.Name))
            {
                return;
            }

            Clone = sender as Obj_AI_Base;
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender == null || !sender.IsValid || !sender.Name.Equals(Player.Name))
            {
                return;
            }

            Clone = null;
        }

        public class LastSpellCast
        {
            public int CastTick;
            public SpellSlot Slot = SpellSlot.Unknown;
        }      

        private static void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                if (Player.HealthPercent <= ThugDogeShaco.MiscConfig.JukeFleePercentage && ThugDogeShaco.R.IsReady())
                {
                var pet = Clone; //Player.Pet as Obj_AI_Base;      
                    if (Clone == null && ThugDogeShaco.R2.IsReady())
                    {
                        ThugDogeShaco.R2.Cast();
                    }              
                    EloBuddy.Player.IssueOrder(
                        GameObjectOrder.MovePet,
                        (pet.Position - Player.Position).Normalized()
                )
                    ;
                }
            }
            catch { }
        }
        

    }
}
