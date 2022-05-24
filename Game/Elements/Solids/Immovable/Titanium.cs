using Microsoft.Xna.Framework;

namespace DotSim
{
    class Titanium : ImmovableSolid
    {
        public Titanium(int x, int y) : base(x, y) {
            vel = new Vector3(0f, 0f, 0f);
            frictionFactor = 0.5f;
            inertialResistance = 1.1f;
            mass = 1000;
            explosionResistance = 5;
        }

        override public bool ReceiveHeat(WorldMatrix matrix, int heat) { return false; }
        override public bool Corrode(WorldMatrix matrix) { return false; }
        override public bool Infect(WorldMatrix matrix) { return false; }
    }
}
