using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class Liquid : Element {

        public int density;
        public int dispersionRate;
        public int yUnchangedCount = 0;
        public int yUnchangedThreshold = 200;

        public Liquid(int x, int y) : base(x, y) {
            stoppedMovingThreshold = 10;
        }

        public void step(WorldMatrix matrix) {
            if (stepped.Get(0) == true) return;
            stepped.Not(); //there's only one value

            if (matrix.useChunks && !matrix.shouldStepElementInChunk(this)) {

            }
        }
    }
}
