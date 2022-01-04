using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    class ImmovableSolid : Solid
    {
        public ImmovableSolid(int x, int y) : base(x, y) {
            isFreeFalling = false;
        }

        public override void step(WorldMatrix matrix) { base.step(matrix); }
        protected override bool actOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) { return true; }
    }
}
