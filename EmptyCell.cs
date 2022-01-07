using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    class EmptyCell : Element {

        private static readonly Element element;

        private EmptyCell(int x, int y) : base(x, y){ elementName = "EmptyCell"; }

        public static Element GetInstance() { return (element == null) ? new EmptyCell(-1, -1) : element; }

        override public void Step(WorldMatrix matrix){}
        override protected bool ActOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) { return true; }
        public override bool ActOnOther(Element other, WorldMatrix matrix) { return true; }
    }
}