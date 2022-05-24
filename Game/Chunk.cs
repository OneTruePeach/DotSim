using Microsoft.Xna.Framework;

namespace DotSim
{
    public class Chunk
    {
        public static int size = 32;
        public bool ShouldStep { get; set; }
        public bool ShouldStepNextFrame { get; set; }
        public Vector3 TopLeft { get; set; }
        public Vector3 BottomRight { get; set; }

        public Chunk(Vector3 topLeft, Vector3 bottomRight) {
            TopLeft = topLeft;
            BottomRight = bottomRight;
            ShouldStep = true;
            ShouldStepNextFrame = true;
        }

        public Chunk() { }

    }
}
