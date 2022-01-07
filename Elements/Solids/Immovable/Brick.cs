using Microsoft.Xna.Framework;

namespace DotSim
{
    class Brick : ImmovableSolid
    {
        public Brick(int x, int y) : base(x, y) {
            vel = new Vector3(0f, 0f, 0f);
            frictionFactor = 0.5f;
            inertialResistance = 1.1f;
            mass = 500;
            explosionResistance = 4;
        }

        public override bool ReceiveHeat(WorldMatrix matrix, int heat) { return false; }

    }
}
