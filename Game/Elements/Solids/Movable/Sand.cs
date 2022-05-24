using Microsoft.Xna.Framework;

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
        public override bool ActOnOther(Element other, WorldMatrix matrix) { return true; }
        override public bool ReceiveHeat(WorldMatrix matrix, int heat) { return false;  }
    }
}
