using Microsoft.Xna.Framework;

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
            mass = 100;
            explosionResistance = 0;
        }

        override public bool ReceiveHeat(WorldMatrix matrix, int heat) {
            DieAndReplace(matrix, "Steam");
            return true;
        }

        override public bool ActOnOther(Element other, WorldMatrix matrix) {
            other.CleanColor(); //water washes other materials
            if (other.ShouldApplyHeat()) {
                other.ReceiveCooling(matrix, coolingFactor);
                coolingFactor--;
                if (coolingFactor <= 0) {
                    DieAndReplace(matrix, "Steam");
                    return true;
                }
                return false;
            }
            return false;
        }

        override public bool Explode(WorldMatrix matrix, int strength) { 
            if (explosionResistance < strength) {
                DieAndReplace(matrix, "Steam");
                return true;
            } else { return false; }
        }
    }
    
}
