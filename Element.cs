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
        public Vector3 vel;
        public int stoppedMovingThreshold = 1;

        public List<Vector2> secondaryMatrixCoords = new List<Vector2>();

        public float xThreshold = 0;
        public float yThreshold = 0;

        public bool isDead = false;

        //public Color color;

        public BitArray stepped = new BitArray(1);

        public Element(int x, int y) {
            setCoordinatesByMatrix(x, y);
        }

        public void setCoordinatesByMatrix(int x, int y) {
            matrixX = x;
            pixelX = x * 6;
            matrixY = y;
            pixelY = y * 6;
        }
    }
}
