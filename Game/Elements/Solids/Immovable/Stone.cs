using Microsoft.Xna.Framework;

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

        override public bool ReceiveHeat(WorldMatrix matrix, int heat) { return false; } 
    }
}
