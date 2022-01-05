using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    class Water : Liquid
    {
        public Water(int x, int y) : base(x, y) {
            vel = new Vector3(0, -124f, 0);
            inertialResistance = 0;
            frictionFactor = 1f;
            density = 5;
            dispersionRate = 5;
            coolingFactor = 5;
            elementName = "Water";
            //mass = 100;
            //explosionResistance = 0;
        }

        /*override public bool receiveHeat(WorldMatrix matrix, int heat) {
            dieAndReplace(matrix, ElementType.STEAM);
            return true;
        }*/

        override public bool actOnOther(Element other, WorldMatrix matrix) { return true; }
            /*//other.cleanColor(); //water washes other materials
            if (other.shouldApplyHeat()) {
                other.receiveCooling(matrix, coolingFactor);
                coolingFactor--;
                if (coolingFactor <= 0) {
                    dieAndReplace(matrix, ElementType.STEAM);
                    return true;
                }
                return false;
            }
            return false;
        }*/
    }
    /*override public bool explode(WorldMatrix matrix, int strength) {
        if (explosionResistance < strength) {
            dieAndReplace(matrix, ElementType.STEAM);
            return true;
        } else {
            return false;
        }
    }*/
}
