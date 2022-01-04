using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    class Sand : MovableSolid
    {
        private Random rng = new Random();
        public Sand(int x, int y) : base(x, y) {
            vel = new Vector3(rng.Next() > 0.5 ? -1 : 1, -124f, 0f);
            frictionFactor = 0.9f;
            inertialResistance = .1f;
            //mass = 150;
        }

        //override public bool receiveHeat(WorldMatrix matrix, int heat) { return false;  }
    }
}
