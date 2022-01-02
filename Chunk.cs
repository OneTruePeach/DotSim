using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public class Chunk
    {
        public static int size = 32;
        public bool shouldStep { get; set; }
        public bool shouldStepNextFrame { get; set; }
        public Vector3 topLeft { get; set; }
        public Vector3 bottomRight { get; set; }

        public Chunk(Vector3 topLeft, Vector3 bottomRight) {
            this.topLeft = topLeft;
            this.bottomRight = bottomRight;
            this.shouldStep = true;
            this.shouldStepNextFrame = true;
        }

        public Chunk() { }

    }
}
