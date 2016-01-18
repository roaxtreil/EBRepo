using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy.SDK;
using SharpDX;

namespace HoolaLucian
{
    static class Utility
    {
        public static Vector3 Extend(this Vector3 vector3, Vector3 toVector3, float distance)
        {
            return vector3 + (distance * (toVector3 - vector3).Normalized());
        }
    }
}
