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

        override public void spawnSparkIfIgnited(WorldMatrix matrix) { }
        override public bool corrode(WorldMatrix matrix) { return false; }
        override public void darkenColor() { }
        override public void darkenColor(float factor) { }

        public override void step(WorldMatrix matrix) {
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
                if (matrix.isWithinBounds(modifiedMatrixX, modifiedMatrixY)) {
                    Element neighbor = matrix.get(modifiedMatrixX, modifiedMatrixY);
                    if (neighbor == this) continue;
                    bool stopped = actOnNeighboringElement(neighbor, modifiedMatrixX, modifiedMatrixY, matrix, i == upperBound, i == 1, lastValidLocation, 0);
                    if (stopped) break;

                    lastValidLocation.X = modifiedMatrixX;
                    lastValidLocation.Y = modifiedMatrixY;
                } else {
                    matrix.setElementAtIndex(matrixX, matrixY, createElementByMatrix(matrixX, matrixY, "EmptyCell"));
                    return;
                }
            }
            applyHeatToNeighborsIfIgnited(matrix);
            modifyColor();
            spawnSparkIfIgnited(matrix);
            checkLifeSpan(matrix);
            takeEffectsDamage(matrix);
            if (matrix.useChunks) {
                if (isIgnited) {
                    matrix.reportToChunkActive(this);
                }
            }
        }

        protected override bool actOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) {
            bool acted = actOnOther(neighbor, matrix);
            if (acted) return true;
            if (neighbor is EmptyCell || neighbor is Particle) {
                if (isFinal) { swapPositions(matrix, neighbor, modifiedMatrixX, modifiedMatrixY); }
                else { return false; }
            } else if (neighbor is Gas) {
                Gas gasNeighbor = (Gas)neighbor;
                if (compareGasDensities(gasNeighbor)) {
                    swapGasByDensity(matrix, gasNeighbor, modifiedMatrixX, modifiedMatrixY, lastValidLocation);
                    return false;
                }

                if (depth > 0) return true;

                if (isFinal) {
                    moveToLastValid(matrix, lastValidLocation);
                    return true;
                }

                vel.X = vel.X > 0 ? -62 : 62;

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = getAdditional(normalizedVel.X);
                int additionalY = getAdditional(normalizedVel.Y);
                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.get(matrixX + additionalX, matrixY + additionalY);

                if (isFirst) { vel.Y = averageVel(vel.Y, neighbor.vel.Y); }
                else { vel.Y = 124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = iterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (!stoppedDiagonally) { return true; }
                }

                Element adjacentNeighbor = matrix.get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null && adjacentNeighbor != diagonalNeighbor) {
                    bool stoppedAdjacent = iterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (stoppedAdjacent) vel.X *= -1;
                    if (!stoppedAdjacent) { return true; }
                }

                moveToLastValid(matrix, lastValidLocation);
                return true;
            } else if (neighbor is Liquid) {
                if (depth > 0) return true;
                if (isFinal) {
                    moveToLastValid(matrix, lastValidLocation);
                    return true;
                }
                if (neighbor.isFreeFalling) return true;

                float absY = Math.Max(Math.Abs(vel.Y) / 31, 105);
                vel.X = vel.X < 0 ? -absY : absY;

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = getAdditional(normalizedVel.X);
                int additionalY = getAdditional(normalizedVel.Y);
                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.get(matrixX + additionalX, matrixY + additionalY);
                if (isFirst) { vel.Y = averageVel(vel.Y, neighbor.vel.Y); } 
                else { vel.Y = 124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = iterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (!stoppedDiagonally) return true;
                }

                Element adjacentNeighbor = matrix.get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null && adjacentNeighbor != diagonalNeighbor) {
                    bool stoppedAdjacently = iterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (stoppedAdjacently) vel.X *= -1;
                    if (!stoppedAdjacently) {
                        return true;
                    }
                }

                moveToLastValid(matrix, lastValidLocation);
                return true;
            } else if (neighbor is Solid) {
                if (depth > 0) return true;
                if (isFinal) {
                    moveToLastValid(matrix, lastValidLocation);
                    return true;
                }

                if (neighbor.isFreeFalling) return true;

                float absY = Math.Max(Math.Abs(vel.Y) / 31, 105);
                vel.X = vel.X < 0 ? -absY : absY;

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = getAdditional(normalizedVel.X);
                int additionalY = getAdditional(normalizedVel.Y);

                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.get(matrixX + additionalX, matrixY + additionalY);
                if (isFirst) { vel.Y = averageVel(vel.Y, neighbor.vel.Y); } 
                else { vel.Y = 124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = iterateToAdditional(matrix, matrixX + additionalX, matrixY + additionalY, distance);
                    if (!stoppedDiagonally) return true;
                }

                Element adjacentNeighbor = matrix.get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null) {
                    bool stoppedAdjacently = iterateToAdditional(matrix, matrixX + additionalX, matrixY, distance);
                    if (stoppedAdjacently) vel.X *= -1;
                    if (!stoppedAdjacently) return true;
                }

                moveToLastValid(matrix, lastValidLocation);
                return true;
            }
            return false;
        }

        private bool iterateToAdditional(WorldMatrix matrix, int startingX, int startingY, int distance) {
            int distanceModifier = distance > 0 ? 1 : -1;
            Vector3 lastValidLocation = new Vector3(matrixX, matrixY, 0);
            for (int i = 0; i <= Math.Abs(distance); i++) {
                Element neighbor = matrix.get(startingX + (i * distanceModifier), startingY);
                bool acted = actOnOther(neighbor, matrix);
                if (acted) return false;
                bool isFirst = i == 0;
                bool isFinal = i == Math.Abs(distance);
                if (neighbor == null) { continue; }
                if (neighbor is EmptyCell) {
                    if (isFinal) {
                        swapPositions(matrix, neighbor, startingX + (i * distanceModifier), startingY);
                        return false;
                    }
                    lastValidLocation.X = startingX + (i * distanceModifier);
                    lastValidLocation.Y = startingY;
                    continue;
                } else if (neighbor is Gas) {
                    Gas gasNeighbor = (Gas)neighbor;
                    if (compareGasDensities(gasNeighbor)) {
                        swapGasByDensity(matrix, gasNeighbor, startingX + (i * distanceModifier), startingY, lastValidLocation);
                        return false;
                    }
                } else if (neighbor is Solid || neighbor is Liquid) {
                    if (isFirst) { return true; }
                    moveToLastValid(matrix, lastValidLocation);
                    return false;
                }
            }
            return true;
        }

        private void swapGasByDensity(WorldMatrix matrix, Gas neighbor, int neighborX, int neighborY, Vector3 lastValidLocation) {
            vel.Y = 62;
            moveToLastValidAndSwap(matrix, neighbor, neighborX, neighborY, lastValidLocation);
        }

        private bool compareGasDensities(Gas neighbor) {
            return (density > neighbor.density && neighbor.matrixY <= matrixY);
        }

        private int getAdditional(float value) {
            if (value < -.1f) {
                return (int)Math.Floor(value);
            } else if (value > .1f) {
                return (int)Math.Ceiling(value);
            } else { return 0; }
        }

        private float averageVel(float vel1, float vel2) {
            if (vel2 > 125f) { return 124f; }

            float avg = (vel1 + vel2) / 2;
            return (avg > 0) ? avg : Math.Min(avg, 124f);
        }

        override public bool infect(WorldMatrix matrix) { return false; }
        override public bool stain(float r, float g, float b, float a) { return false; }
        override public bool stain(Color color) { return false; }

    }
}
