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
        public Sand(int x, int y) : base(x, y) {
            vel = new Vector3(rng.NextDouble() > 0.5 ? -1 : 1, -124f, 0f);
            frictionFactor = 0.9f;
            inertialResistance = .1f;
            elementName = "Sand";
            mass = 150;
        }
        public override bool actOnOther(Element other, WorldMatrix matrix) { return true; }
        override public bool receiveHeat(WorldMatrix matrix, int heat) { return false;  }
    }
}
