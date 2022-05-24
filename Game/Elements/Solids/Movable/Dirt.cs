using Microsoft.Xna.Framework;

namespace DotSim
{
    class Dirt : MovableSolid
    {
        public Dirt(int x, int y) : base(x, y) {
            vel = new Vector3(0f, -124f, 0f);
            frictionFactor = .6f;
            inertialResistance = .8f;
            mass = 200;
        }

        public override bool ReceiveHeat(WorldMatrix matrix, int heat) { return false; }

    }
}
