using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class Solid : Element
    {
        public Solid(int x, int y) : base(x, y){}
        protected override bool actOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) { return true; }
        override public void step(WorldMatrix matrix) { return; }
    }
}