using Microsoft.Xna.Framework;
using System;

namespace DotSim
{
    class Particle : Element 
    {
        public Element containedElement;
        public string containedElementName;

        public Particle(int x, int y, Vector3 velocity, Element element, Color containedColor, bool ignited) : base(x, y) {
            if (element is Particle) { throw new ArgumentException("Containing element cannot be a particle."); }
            containedElement = element;
            containedElementName = element.elementName;
            vel = new Vector3();
            Vector3 localVel = velocity == null ? new Vector3(0, 124, 0) : velocity;
            vel.X = localVel.X;
            vel.Y = localVel.Y;
            color = containedColor;
            isIgnited = ignited;
            if (isIgnited) { flammabilityResistance = 0; }
        }

        public Particle(int x, int y, Vector3 velocity, Element element) : base(x, y) {
            if (element is Particle) { throw new ArgumentException("Containing element cannot be a particle."); }
            containedElement = element;
            vel = new Vector3();
            Vector3 localVel = velocity == null ? new Vector3(0, 124, 0) : velocity;
            vel.X = localVel.X;
            vel.Y = localVel.Y;
            color = element.color;
            isIgnited = element.isIgnited;
            if (isIgnited) { flammabilityResistance = 0; }
        }

        override public bool ReceiveHeat(WorldMatrix matrix, int heat) { return false; }
        override public void DieAndReplace(WorldMatrix matrix, string element) { ParticleDeathAndSpawn(matrix); }

        private void ParticleDeathAndSpawn(WorldMatrix matrix) {
            Element currentLocation = matrix.Get(matrixX, matrixY);
            if (currentLocation == this || currentLocation is EmptyCell) {
                Die(matrix);
                Element newElement = CreateElementByMatrix(matrixX, matrixY, containedElementName);
                newElement.color = color;
                newElement.isIgnited = isIgnited;
                if (newElement.isIgnited) { newElement.flammabilityResistance = 0; }
                matrix.SetElementAtIndex(matrixX, matrixY, newElement);
                matrix.ReportToChunkActive(matrixX, matrixY);
            } else {
                int yIndex = 0;
                while (true) {
                    Element elementAtNewPos = matrix.Get(matrixX, matrixY + yIndex);
                    if (elementAtNewPos == null) break;
                    else if (elementAtNewPos is EmptyCell) {
                        Die(matrix);
                        matrix.SetElementAtIndex(matrixX, matrixY + yIndex, CreateElementByMatrix(matrixX, matrixY, containedElementName));
                        matrix.ReportToChunkActive(matrixX, matrixY + yIndex);
                        break;
                    }
                    yIndex++;
                }
            }
        }

        public override void Step(WorldMatrix matrix) {
            if (stepped.Get(0) == true) { return; }
            stepped.Not();

            if (vel.Y > -64 && vel.Y < 32) { vel.Y = -64; }
            vel = Vector3.Add(vel, new Vector3(0f, -0.5f, 0f));
            if (vel.Y < -500) { vel.Y = -500; }
            else if (vel.Y > 500) { vel.Y = 500; }

            int xModifier = vel.X < 0 ? -1 : 1;
            int yModifier = vel.Y < 0 ? -1 : 1;
            int velXDeltaTime = (int)(Math.Abs(vel.X) * 1/60); //something something monogame GameTime.ElapsedGameTime dont care
            int velYDeltaTime = (int)(Math.Abs(vel.Y) * 1/60);

            bool xDiffIsLarger = Math.Abs(velXDeltaTime) > Math.Abs(velYDeltaTime);
            int upperBound = Math.Max(Math.Abs(velXDeltaTime), Math.Abs(velYDeltaTime));
            int lowerBound = Math.Min(Math.Abs(velXDeltaTime), Math.Abs(velYDeltaTime));

            float slope = (lowerBound == 0 || upperBound == 0) ? 0f : ((float)((lowerBound + 1) / (upperBound + 1)));

            int smallerCount;
            Vector3 lastValidLocation = new Vector3(matrixX, matrixY, 0);
            for (int i = 1; i <= upperBound; i++) {
                smallerCount = (int)Math.Floor(i * slope);

                int yIncrease, xIncrease;
                if (xDiffIsLarger) {
                    xIncrease = i;
                    yIncrease = smallerCount;
                } else {
                    yIncrease = i;
                    xIncrease = smallerCount;
                }

                int modifiedMatrixX = matrixX + (yIncrease * yModifier);
                int modifiedMatrixY = matrixY + (xIncrease * xModifier);
                if (matrix.IsWithinBounds(modifiedMatrixX, modifiedMatrixY)) {
                    Element neighbor = matrix.Get(modifiedMatrixX, modifiedMatrixY);
                    if (neighbor == this) continue;
                    bool stopped = ActOnNeighboringElement(neighbor, modifiedMatrixX, modifiedMatrixY, matrix, i == upperBound, i == 1, lastValidLocation, 0);
                    if (stopped) break;
                    lastValidLocation.X = modifiedMatrixX;
                    lastValidLocation.Y = modifiedMatrixY;
                } else {
                    matrix.SetElementAtIndex(matrixX, matrixY, CreateElementByMatrix(matrixX, matrixY, "EmptyCell"));
                    return;
                }
            }
            ModifyColor();
        }

        protected override bool ActOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) {
            if (neighbor is EmptyCell || neighbor is Particle) {
                if (isFinal) { SwapPositions(matrix, neighbor, modifiedMatrixX, modifiedMatrixY); }
                else { return false; }
            } else if (neighbor is Liquid || neighbor is Solid) {
                MoveToLastValid(matrix, lastValidLocation);
                DieAndReplace(matrix, containedElementName);
                return true;
            } else if (neighbor is Gas) {
                if (isFinal) {
                    MoveToLastValidAndSwap(matrix, neighbor, modifiedMatrixX, modifiedMatrixY, lastValidLocation);
                    return true;
                }
                return false;
            }
            return false;
        }

        public override bool ActOnOther(Element other, WorldMatrix matrix) { return false; }
    }
}