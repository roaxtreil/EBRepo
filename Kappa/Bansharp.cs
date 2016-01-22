using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Color = System.Drawing.Color;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace BanSharpDetector
{
    class BanSharpDetector
    {
        private static readonly Dictionary<int, List<IDetector>> _detectors = new Dictionary<int, List<IDetector>>();
        private Menu _mainMenu;
        private Vector2 _screenPos;

        public void Load()
        {
            _mainMenu = MainMenu.AddMenu("The Cheater", "BanSharpDetector");
            StringList(_mainMenu, "detection", "Detection", new []{ "Preferred", "Safe", "AntiHumanizer" }, 0);
            
            _mainMenu.Add("enabled", new CheckBox("Enabled"));
            _mainMenu.Add("drawing", new CheckBox("Drawing"));
            var posX = _mainMenu.Add("positionx", new Slider("Position X", Drawing.Width - 270, 0, Drawing.Width - 20));
            var posY = _mainMenu.Add("positiony", new Slider("Position Y", Drawing.Height / 2, 0, Drawing.Width - 20));

            
            posX.OnValueChange += (sender, args) => _screenPos.X = args.NewValue;
            posY.OnValueChange += (sender, args) => _screenPos.Y = args.NewValue;


            _screenPos.X = posX.CurrentValue;
            _screenPos.Y = posY.CurrentValue;


            Obj_AI_Base.OnNewPath += OnNewPath;
            Drawing.OnDraw += Draw;
    
            Chat.Print("BanSharpDetector loaded!");
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
            mode.OnValueChange += (sender, args) =>
            {
                foreach (var detector in _detectors)
                {
                    detector.Value.ForEach(
                        item => item.ApplySetting((DetectorSetting) args.NewValue));
                }
            };
        }

        private void Draw(EventArgs args)
        {
            if (!_mainMenu["drawing"].Cast<CheckBox>().CurrentValue) return;

            Drawing.DrawLine(new Vector2(_screenPos.X, _screenPos.Y + 15), new Vector2(_screenPos.X + 180, _screenPos.Y + 15), 2, Color.Red);
           
            var column = 1;
            Drawing.DrawText(_screenPos.X, _screenPos.Y, Color.Red, "Cheat-pattern detection:");
            foreach (var detector in _detectors)
            {
                var maxValue = detector.Value.Max(item => item.GetScriptDetections());
                Drawing.DrawText(_screenPos.X, column * 20 + _screenPos.Y, Color.Red, HeroManager.AllHeroes.First(hero => hero.NetworkId == detector.Key).Name + ": " + maxValue + (maxValue > 0 ? " (" + detector.Value.First(itemId => itemId.GetScriptDetections() == maxValue).GetName() + ")" : string.Empty));
                column++;
            }
        }

        private void OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (sender.Type != GameObjectType.AIHeroClient || !_mainMenu["enabled"].Cast<CheckBox>().CurrentValue) return;

            if (!_detectors.ContainsKey(sender.NetworkId))
            {
                var detectors = new List<IDetector> { new SacOrbwalkerDetector(), new EloBuddyOrbwalkDetector() };
                detectors.ForEach(detector => detector.Initialize((AIHeroClient)sender));
                _detectors.Add(sender.NetworkId, detectors);
            }
            else
                _detectors[sender.NetworkId].ForEach(detector => detector.FeedData(args.Path.Last()));
        }


    }
}
