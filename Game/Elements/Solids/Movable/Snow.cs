using Microsoft.Xna.Framework;

namespace DotSim
{
    class Snow : MovableSolid
    {
        public Snow(int x, int y) : base(x, y) {
            vel = new Vector3(0f, -62f, 0f);
            frictionFactor = .4f;
            inertialResistance = .8f;
            mass = 200;
            flammabilityResistance = 100;
            resetFlammabilityResistance = 35;
        }

        override public bool ReceiveHeat(WorldMatrix matrix, int heat) {
            if (heat > 0) {
                DieAndReplace(matrix, "Water");
                return true;
            }
            return false;
        }

        public override void Step(WorldMatrix matrix) {
            base.Step(matrix);
            if (vel.Y < -62) { vel.Y = rng.NextDouble() > 0.3 ? -62 : -124; }
        }
    }
}
