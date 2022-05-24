using Microsoft.Xna.Framework;

namespace DotSim
{
    class Ground : ImmovableSolid
    {
        public Ground(int x, int y) : base(x, y) {
            vel = new Vector3(0f, 0f, 0f);
            frictionFactor = 0.5f;
            inertialResistance = 1.1f;
            mass = 200;
            health = 250;
        }

        public override bool ReceiveHeat(WorldMatrix matrix, int heat) { return false; }

        override public void CustomElementFunctions(WorldMatrix matrix) {
            /*Element above = matrix.Get(matrixX, matrixY + 1);
            if (above == null || above is EmptyCell) {
                color = GetColorForThisElement("Grass");
            } else {
                color = GetColorForThisElement("Ground");
            }*/
        }
    }
}
