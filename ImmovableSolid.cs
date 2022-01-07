using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class ImmovableSolid : Solid
    {
        public ImmovableSolid(int x, int y) : base(x, y) {
            isFreeFalling = false;
        }

        public override void Step(WorldMatrix matrix) { base.Step(matrix); }
        protected override bool ActOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) { return true; }
        public override bool ActOnOther(Element other, WorldMatrix matrix) { return true; }
    }
}
