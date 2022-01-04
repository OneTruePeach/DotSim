using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    class EmptyCell : Element {
        private static Element element;

        private EmptyCell(int x, int y) : base(x, y){}

        public static Element getInstance() {
            if (element == null) {
                element = new EmptyCell(-1, -1);
            }
            return element;
        }

        override public void step(WorldMatrix matrix){}
        override protected bool actOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) { return true; }

    }
}
