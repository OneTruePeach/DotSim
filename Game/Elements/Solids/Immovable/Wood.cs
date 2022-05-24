using Microsoft.Xna.Framework;

namespace DotSim
{
    class Wood : ImmovableSolid 
    {
        public Wood(int x, int y) : base(x, y) {
            vel = new Vector3(0f, 0f, 0f);
            frictionFactor = 0.5f;
            inertialResistance = 1.1f;
            mass = 500;
            health = rng.Next(100) + 100;
            flammabilityResistance = 40;
            resetFlammabilityResistance = 25;
        }

        override public void CheckIfDead(WorldMatrix matrix) {
            if(health <= 0) {
                if (isIgnited && rng.NextDouble() > 0.95f) {
                    DieAndReplace(matrix, "Ember");
                } else { Die(matrix); }
            }
        }
    }
}
