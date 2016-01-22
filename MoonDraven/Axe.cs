using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draven
{
    class Axe
    {
        public static float LimitTime = 1.20f;
        public float TimeToCatchReticle
        {
            get
            {
                var TimeLefts = 0f;
                foreach (Axe a1 in MoonDraven.MoonDraven.QReticles.Where(m => m.InTime && m.TimeLeft < TimeLeft))
                {
                    TimeLefts += a1.TimeLeft;
                }
                return TimeLeft - (TimeLefts + TimeNeededToCatchReticle);
            }
        }
        public bool CanOrbwalkWithUserDelay
        {
            get
            {
                return TimeToCatchReticle - (1 - 0) * LimitTime - 0f > 0;
            }
        }
    }
}
