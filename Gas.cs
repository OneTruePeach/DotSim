using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class Gas : Element
    {
        public int density;
        public int dispersionRate;
        public Gas(int x, int y) : base(x, y) { }

        override public void SpawnSparkIfIgnited(WorldMatrix matrix) { }
        override public bool Corrode(WorldMatrix matrix) { return false; }
        override public void DarkenColor() { }
        override public void DarkenColor(float factor) { }

        public override void Step(WorldMatrix matrix) {
            if (stepped.Get(0) == true) return;
            stepped.Not();

            vel = Vector3.Subtract(vel, new Vector3(0f, -0.5f, 0f));
            vel.Y = Math.Min(vel.Y, 124);
            if (vel.Y == 124 && rng.NextDouble() < .7) { vel.Y = 64; } 
            vel.X *= .9f;

            int yModifier = vel.Y < 0 ? -1 : 1;
            int xModifier = vel.X < 0 ? -1 : 1;
            float velYDeltaTimeFloat = (Math.Abs(vel.Y) * 1/60);
            float velXDeltaTimeFloat = (Math.Abs(vel.X) * 1/60);
            int velXDeltaTime, velYDeltaTime;
            if (velXDeltaTimeFloat < 1) {
                xThreshold += velXDeltaTimeFloat;
                velXDeltaTime = (int)xThreshold;
                if (Math.Abs(velXDeltaTime) > 0) {
                    xThreshold = 0;
                }
            } else {
                xThreshold = 0;
                velXDeltaTime = (int)velXDeltaTimeFloat;
            }
            if (velYDeltaTimeFloat < 1) {
                yThreshold += velYDeltaTimeFloat;
                velYDeltaTime = (int)yThreshold;
                if (Math.Abs(velYDeltaTime) > 0) {
                    yThreshold = 0;
                }
            } else {
                yThreshold = 0;
                velYDeltaTime = (int)velYDeltaTimeFloat;
            }

            bool xDiffIsLarger = Math.Abs(velXDeltaTime) > Math.Abs(velYDeltaTime);
            int upperBound = Math.Max(Math.Abs(velXDeltaTime), Math.Abs(velYDeltaTime));
            int lowerBound = Math.Min(Math.Abs(velXDeltaTime), Math.Abs(velYDeltaTime));

            float slope = (lowerBound == 0 || upperBound == 0) ? 0f : ((float)((lowerBound + 1)/(upperBound + 1)));

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
            ApplyHeatToNeighborsIfIgnited(matrix);
            ModifyColor();
            SpawnSparkIfIgnited(matrix);
            CheckLifeSpan(matrix);
            TakeEffectsDamage(matrix);
            if (matrix.useChunks) {
                if (isIgnited) {
                    matrix.ReportToChunkActive(this);
                }
            }
        }

        protected override bool ActOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) {
            bool acted = ActOnOther(neighbor, matrix);
            if (acted) return true;
            if (neighbor is EmptyCell || neighbor is Particle) {
                if (isFinal) { SwapPositions(matrix, neighbor, modifiedMatrixX, modifiedMatrixY); }
                else { return false; }
            } else if (neighbor is Gas gasNeighbor) {
                if (CompareGasDensities(gasNeighbor)) {
                    SwapGasByDensity(matrix, gasNeighbor, modifiedMatrixX, modifiedMatrixY, lastValidLocation);
                    return false;
                }

                if (depth > 0) return true;

                if (isFinal) {
                    MoveToLastValid(matrix, lastValidLocation);
                    return true;
                }

                vel.X = vel.X > 0 ? -62 : 62;

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = GetAdditional(normalizedVel.X);
                int additionalY = GetAdditional(normalizedVel.Y);
                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.Get(matrixX + additionalX, matrixY + additionalY);

                if (isFirst) { vel.Y = AverageVel(vel.Y, neighbor.vel.Y); }
                else { vel.Y = 124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = IterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (!stoppedDiagonally) { return true; }
                }

                Element adjacentNeighbor = matrix.Get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null && adjacentNeighbor != diagonalNeighbor) {
                    bool stoppedAdjacent = IterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (stoppedAdjacent) vel.X *= -1;
                    if (!stoppedAdjacent) { return true; }
                }

                MoveToLastValid(matrix, lastValidLocation);
                return true;
            } else if (neighbor is Liquid) {
                if (depth > 0) return true;
                if (isFinal) {
                    MoveToLastValid(matrix, lastValidLocation);
                    return true;
                }
                if (neighbor.isFreeFalling) return true;

                float absY = Math.Max(Math.Abs(vel.Y) / 31, 105);
                vel.X = vel.X < 0 ? -absY : absY;

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = GetAdditional(normalizedVel.X);
                int additionalY = GetAdditional(normalizedVel.Y);
                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.Get(matrixX + additionalX, matrixY + additionalY);
                if (isFirst) { vel.Y = AverageVel(vel.Y, neighbor.vel.Y); } 
                else { vel.Y = 124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = IterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (!stoppedDiagonally) return true;
                }

                Element adjacentNeighbor = matrix.Get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null && adjacentNeighbor != diagonalNeighbor) {
                    bool stoppedAdjacently = IterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (stoppedAdjacently) vel.X *= -1;
                    if (!stoppedAdjacently) {
                        return true;
                    }
                }

                MoveToLastValid(matrix, lastValidLocation);
                return true;
            } else if (neighbor is Solid) {
                if (depth > 0) return true;
                if (isFinal) {
                    MoveToLastValid(matrix, lastValidLocation);
                    return true;
                }

                if (neighbor.isFreeFalling) return true;

                float absY = Math.Max(Math.Abs(vel.Y) / 31, 105);
                vel.X = vel.X < 0 ? -absY : absY;

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = GetAdditional(normalizedVel.X);
                int additionalY = GetAdditional(normalizedVel.Y);

                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.Get(matrixX + additionalX, matrixY + additionalY);
                if (isFirst) { vel.Y = AverageVel(vel.Y, neighbor.vel.Y); } 
                else { vel.Y = 124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = IterateToAdditional(matrix, matrixX + additionalX, matrixY + additionalY, distance);
                    if (!stoppedDiagonally) return true;
                }

                Element adjacentNeighbor = matrix.Get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null) {
                    bool stoppedAdjacently = IterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (stoppedAdjacently) vel.X *= -1;
                    if (!stoppedAdjacently) return true;
                }

                MoveToLastValid(matrix, lastValidLocation);
                return true;
            }
            return false;
        }

        private bool IterateToAdditional(WorldMatrix matrix, int startingX, int startingY, int distance) {
            int distanceModifier = distance > 0 ? 1 : -1;
            Vector3 lastValidLocation = new Vector3(matrixX, matrixY, 0);
            for (int i = 0; i <= Math.Abs(distance); i++) {
                Element neighbor = matrix.Get(startingX + (i * distanceModifier), startingY);
                bool acted = ActOnOther(neighbor, matrix);
                if (acted) return false;
                bool isFirst = i == 0;
                bool isFinal = i == Math.Abs(distance);
                if (neighbor == null) { continue; }
                if (neighbor is EmptyCell) {
                    if (isFinal) {
                        SwapPositions(matrix, neighbor, startingX + (i * distanceModifier), startingY);
                        return false;
                    }
                    lastValidLocation.X = startingX + (i * distanceModifier);
                    lastValidLocation.Y = startingY;
                    continue;
                } else if (neighbor is Gas gasNeighbor) {
                    if (CompareGasDensities(gasNeighbor)) {
                        SwapGasByDensity(matrix, gasNeighbor, startingX + (i * distanceModifier), startingY, lastValidLocation);
                        return false;
                    }
                } else if (neighbor is Solid || neighbor is Liquid) {
                    if (isFirst) { return true; }
                    MoveToLastValid(matrix, lastValidLocation);
                    return false;
                }
            }
            return true;
        }

        private void SwapGasByDensity(WorldMatrix matrix, Gas neighbor, int neighborX, int neighborY, Vector3 lastValidLocation) {
            vel.Y = 62;
            MoveToLastValidAndSwap(matrix, neighbor, neighborX, neighborY, lastValidLocation);
        }

        private bool CompareGasDensities(Gas neighbor) {
            return (density > neighbor.density && neighbor.matrixY <= matrixY);
        }

        private int GetAdditional(float value) { 
            return (value < -.1f) ? (int)Math.Floor(value) : (value > .1f) ? (int)Math.Ceiling(value) : 0;
        }

        private float AverageVel(float vel1, float vel2) {
            float avg = (vel1 + vel2) / 2;
            return (vel2 > 125f) ? 124f : (avg > 0) ? avg : Math.Min(avg, 124f);
        }

        override public bool Infect(WorldMatrix matrix) { return false; }
        override public bool Stain(float r, float g, float b, float a) { return false; }
        override public bool Stain(Color color) { return false; }

    }
}
