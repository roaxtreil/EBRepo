using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace Mastery_Badge_Spammer
{
    public static class Program
    {
        public static Menu Menu;
        public static int LastEmoteSpam = 0;
        public static int MyKills = 0;
        public static int MyAssits = 0;
        public static int MyDeaths = 0;
        public static Random Random;
        public static SpellSlot FlashSlot = SpellSlot.Unknown;
        public static SpellSlot IgniteSlot = SpellSlot.Unknown;
        public static string[] KnownDisrespectStarts = new[] {"", "kkkkk", "SOLADO", "é led?", "plasma?", "eoq", "faliceu", "lago ai?", "vc caindo toda hora nem da graça", "rekt jojojo", "riot me colocando pra jogar contra bots...", "joga muito", "mulekin jr"};
        public static string[] KnownDisrespectEndings = new[] {"", " kkkkk", " kkkkkkkkkk", " HUEUEUHE"};
        public static int LastDeathNetworkId = 0;
        public static int LastChat = 0;
        public static Dictionary<int, int> DeathsHistory = new Dictionary<int, int>(); 
        static void Main(string[] args)
        {
           Loading.OnLoadingComplete += OnGameLoad;
        }

        public static void OnGameLoad(EventArgs args)
        {
            Menu = MainMenu.AddMenu("Mastery Emote Spammer", "masteryemotespammermenu");
            Menu.AddLabel("Made by imsosharp, ported by ThugDoge");
            StringList(Menu, "mode", "Mode", new []{ "MASTERY", "LAUGH" }, 0);
            StringList(Menu, "chatdisrespectmode", "Chat Disrespect Mode", new[] { "DISABLED", "CHAMPION NAME", "SUMMONER NAME" }, 0);
            Menu.Add("onkill", new CheckBox("After Kill"));
            Menu.Add("onassist", new CheckBox("After Assist"));
            Menu.Add("ondeath", new CheckBox("After Death", false));
            Menu.Add("neardead", new CheckBox("Near Dead Bodies"));
            Menu.Add("ondodgedskillshot", new CheckBox("After you dodge a skillshot"));
            Menu.Add("afterignite", new CheckBox("Dubstep Ignite"));
            Menu.Add("afterflash", new CheckBox("Challenger Flash", false));
            Menu.Add("afterq", new CheckBox("After Q", false));
            Menu.Add("afterw", new CheckBox("After W", false));
            Menu.Add("aftere", new CheckBox("After E", false));
            Menu.Add("afterr", new CheckBox("After R", false));
           
            Random = new Random();
            FlashSlot = ObjectManager.Player.GetSpellSlotFromName("SummonerFlash");
            IgniteSlot = ObjectManager.Player.GetSpellSlotFromName("SummonerDot");
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            
            //init chat disrespekter
            foreach (var en in ObjectManager.Get<AIHeroClient>().Where(h => h.IsEnemy))
            {
                DeathsHistory.Add(en.NetworkId, en.Deaths);
            }
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

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var sData = SpellDatabase.GetByName(args.SData.Name);
            if (Menu["ondodgedskillshot"].Cast<CheckBox>().CurrentValue && sender.IsEnemy && sData != null &&
                ObjectManager.Player.Distance(sender) < sData.Range)
            {
                Core.DelayAction(DoEmote, (int)Math.Round(sData.Delay + sender.Distance(ObjectManager.Player)/sData.MissileSpeed));
            }
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.Q && Menu["afterq"].Cast<CheckBox>().CurrentValue)
                {
                    Core.DelayAction(DoEmote, Random.Next(250, 500));
                }
                if (args.Slot == SpellSlot.W && Menu["afterw"].Cast<CheckBox>().CurrentValue)
                {
                    Core.DelayAction(DoEmote, Random.Next(250, 500));
                }
                if (args.Slot == SpellSlot.E && Menu["aftere"].Cast<CheckBox>().CurrentValue)
                {
                    Core.DelayAction(DoEmote, Random.Next(250, 500));
                }
                if (args.Slot == SpellSlot.R && Menu["afterr"].Cast<CheckBox>().CurrentValue)
                {
                    Core.DelayAction(DoEmote, Random.Next(250, 500));
                }
                if (IgniteSlot != SpellSlot.Unknown && args.Slot == IgniteSlot && Menu["afterignite"].Cast<CheckBox>().CurrentValue)
                {
                    Core.DelayAction(DoEmote, Random.Next(250, 500));
                }
                if (FlashSlot != SpellSlot.Unknown && args.Slot == FlashSlot && Menu["afterflash"].Cast<CheckBox>().CurrentValue)
                {
                    Core.DelayAction(DoEmote, Random.Next(250, 500));
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.ChampionsKilled > MyKills && Menu["onkill"].Cast<CheckBox>().CurrentValue)
            {
                MyKills = ObjectManager.Player.ChampionsKilled;
                DoEmote();
            }
            if (ObjectManager.Player.Assists > MyAssits && Menu["onassist"].Cast<CheckBox>().CurrentValue)
            {
                MyAssits = ObjectManager.Player.Assists;
                DoEmote();
            }
            if (ObjectManager.Player.Deaths > MyDeaths && Menu["ondeath"].Cast<CheckBox>().CurrentValue)
            {
                MyDeaths = ObjectManager.Player.Deaths;
                DoEmote();
            }
            if (Menu["neardead"].Cast<CheckBox>().CurrentValue &&
                ObjectManager.Get<AIHeroClient>()
                    .Any(h => h.IsEnemy && h.IsVisible && h.IsDead && ObjectManager.Player.Distance(h) < 300))
            {
                DoEmote();
            }

            switch (Menu["chatdisrespectmode"].Cast<Slider>().CurrentValue)
            {
                case 0:
                    break;
                case 1:
                    foreach (var en in ObjectManager.Get<AIHeroClient>().Where(h => h.IsEnemy))
                    {
                        if (DeathsHistory.FirstOrDefault(record => record.Key == en.NetworkId).Value < en.Deaths)
                        {
                            var championName = en.ChampionName.ToLower();
                            DeathsHistory.Remove(en.NetworkId);
                            DeathsHistory.Add(en.NetworkId, en.Deaths);
                            if (en.Distance(ObjectManager.Player) < 1000)
                            {
                                Core.DelayAction(() => DoChatDisrespect(championName), Random.Next(1000, 5000));
                            }
                        }
                    }
                    break;
                case 2:
                    foreach (var en in ObjectManager.Get<AIHeroClient>().Where(h => h.IsEnemy))
                    {
                        if (DeathsHistory.FirstOrDefault(record => record.Key == en.NetworkId).Value < en.Deaths)
                        {
                            var name = en.Name.ToLower();
                            DeathsHistory.Remove(en.NetworkId);
                            DeathsHistory.Add(en.NetworkId, en.Deaths);
                            if (en.Distance(ObjectManager.Player) < 1000)
                            {
                                Core.DelayAction(() => DoChatDisrespect(name), Random.Next(1000, 5000));
                            }
                        }
                    }
                    break;
            }
        }

        public static void DoEmote()
        {
            if (Core.GameTickCount - LastEmoteSpam > Random.Next(5000, 15000))
            {
                LastEmoteSpam = Core.GameTickCount;
                Chat.Say(Menu["mode"].Cast<Slider>().CurrentValue == 0 ? "/masterybadge" : "/l");
            }
        }

        public static void DoChatDisrespect(string theTarget)
        {
            if (Core.GameTickCount - LastChat > Random.Next(5000, 20000))
            {
                LastChat = Core.GameTickCount;
                Chat.Say("/all " + KnownDisrespectStarts[Random.Next(0, KnownDisrespectStarts.Length - 1)] +
                         (Random.Next(1, 2) == 1 ? theTarget : "") +
                         KnownDisrespectEndings[Random.Next(0, KnownDisrespectEndings.Length - 1)]);
            }
        }
    }
}
