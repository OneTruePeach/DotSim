using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class Element
    {
        public int pixelX;
        public int pixelY;
        public int matrixX;
        public int matrixY;
        public float xThreshold = 0;
        public float yThreshold = 0;
        public Vector3 vel = new Vector3();
        public int stoppedMovingCount = 0;
        public int stoppedMovingThreshold = 1;
        public bool isFreeFalling = true;
        public bool isDead = false;
        public float frictionFactor;

        //public Color color;
        //public ElementType elementType;
        //public PhysicsElementActor owningBody = null;
        //public Vector2 owningBodyCoords = null;
        //public List<Vector2> secondaryMatrixCoords = new List<Vector2>;

        public BitArray stepped = new BitArray(1);

        public Element(int x, int y) { 
            setCoordinatesByMatrix(x, y);
            //elementType = getEnumType();
            //color = ColorConstants.getColorForElementType(this.elementType, x, y);
            stepped.Set(0, false);
        }

        public abstract void step(WorldMatrix matrix);

        public bool actOnOther(Element other, WorldMatrix matrix) { return false; } //materials usually override this

        public void swapPositions(WorldMatrix matrix, Element toSwap) { swapPositions(matrix, toSwap, toSwap.matrixX, toSwap.matrixY); }
        public void swapPositions(WorldMatrix matrix, Element toSwap, int toSwapX, int toSwapY) {
            if (matrixX == toSwapX && matrixY == toSwapY) { return; }
            matrix.setElementAtIndex(matrixX, matrixY, toSwap);
            matrix.setElementAtIndex(toSwapX, toSwapY, this);
        }

        public void setCoordinatesByMatrix(int x, int y) {
            matrixX = x;
            pixelX = x * 6;
            matrixY = y;
            pixelY = y * 6;
        }

        protected abstract bool actOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth);

        public void moveToLastValid(WorldMatrix matrix, Vector3 moveToLocation) {
            if ((int)(moveToLocation.X) == matrixX && (int)(moveToLocation.Y) == matrixY) return;
            Element toSwap = matrix.get(moveToLocation);
            swapPositions(matrix, toSwap, (int)moveToLocation.X, (int)moveToLocation.Y);
        }

        public void moveToLastValidAndSwap(WorldMatrix matrix, Element toSwap, int toSwapX, int toSwapY, Vector3 moveToLocation) {
            Element thirdNeighbor = matrix.get(moveToLocation);
            if (this == thirdNeighbor || toSwap == thirdNeighbor) {
                swapPositions(matrix, toSwap, toSwapX, toSwapY);
                return;
            }

            if (this == toSwap) {
                swapPositions(matrix, thirdNeighbor, (int)moveToLocation.X, (int)moveToLocation.Y);
                return;
            }

            matrix.setElementAtIndex(matrixX, matrixY, thirdNeighbor);
            matrix.setElementAtIndex(toSwapX, toSwapY, this);
            matrix.setElementAtIndex((int)moveToLocation.X, (int)moveToLocation.Y, toSwap);
        }

        //public void dieAndReplace(WorldMatrix matrix, ElementType type) { die(matrix, type); }
        //public void die(WorldMatrix matrix) { die(matrix, ElementType.EMPTYCELL); }
        //public void die(WorldMatrix matrix, ElementType type) {
        //    isDead = true;
        //    Element newElement = type.createElementByMatrix(matrixX, matrixY);
        //    matrix.setElementAtIndex(matrixX, matrixY, newElement);
        //   matrix.reportToChunkActive(matrixX, matrixY);
        //    if (owningBody != null) {
        //        owningBody.elementDeath(this, newElement);
        //        foreach(Vector2 vector in secondaryMatrixCoords) {
        //            matrix.setElementAtIndex((int)vector.X, (int)vector.Y, ElementType.EMPTYCELL.createElementByMatrix(0, 0));
        //        }
        //    }
        //}

        public bool didNotMove(Vector3 formerLocation) { return formerLocation.X == matrixX && formerLocation.Y == matrixY; }
        public bool hasNotMovedBeyondThreshold() { return stoppedMovingCount >= stoppedMovingThreshold; }
    }
}