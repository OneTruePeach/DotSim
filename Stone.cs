using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    class Stone : ImmovableSolid
    {
        public Stone(int x, int y) : base(x, y)
        {
            vel = new Vector3(0f, 0f, 0f);
            frictionFactor = 0.5f;
            inertialResistance = 1.1f;
            elementName = "Stone";
            mass = 500;
            explosionResistance = 4;
        }

        override public bool receiveHeat(WorldMatrix matrix, int heat) { return false; } 
    }
}
