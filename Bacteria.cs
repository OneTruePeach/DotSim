using Microsoft.Xna.Framework;

namespace DotSim
{
    class Bacteria : ImmovableSolid
    {
        public Bacteria(int x, int y) : base(x, y) {
            vel = new Vector3(0f, 0f, 0f);
            frictionFactor = 0.5f;
            inertialResistance = 1.1f;
            mass = 500;
            flammabilityResistance = 10;
            resetFlammabilityResistance = 0;
            health = 40;
        }

        override public void Step(WorldMatrix matrix) {
            base.Step(matrix);
            infectNeighbors(matrix);
        }

        private bool infectNeighbors(WorldMatrix matrix) {
            if (!IsEffectsFrame() || isIgnited) return false;
            for (int x = matrixX - 1; x <= matrixX + 1; x++) {
                for (int y = matrixY - 1; y <= matrixY + 1; y++) {
                    if (!(x == 0 && y == 0)) {
                        Element neighbor = matrix.Get(x, y);
                        if (neighbor != null) { neighbor.Infect(matrix); }
                    }
                }
            }
            return true;
        }

        override public void TakeFireDamage(WorldMatrix matrix) { health -= fireDamage; }
        override public bool Infect(WorldMatrix matrix) { return false; }
        override public void ModifyColor() {
            if (isIgnited) {
                color = GetRandomFireColor();
            } else {
                color = defaultColor;
            }
        }

    }
}
