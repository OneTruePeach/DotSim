using Microsoft.Xna.Framework;

namespace DotSim
{
    public abstract class Solid : Element
    {
        public Solid(int x, int y) : base(x, y){}
        protected override bool ActOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) { return true; }
        override public void Step(WorldMatrix matrix) { return; }
    }
}