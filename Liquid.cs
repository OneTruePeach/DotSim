using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class Liquid : Element {

        public int density;
        public int dispersionRate;
        public int yUnchangedCount = 0;
        public int yUnchangedThreshold = 200;

        public Liquid(int x, int y) : base(x, y) { stoppedMovingThreshold = 10; }

        override public void Step(WorldMatrix matrix) {
            if (stepped.Get(0) == true) return;
            stepped.Not();
            if (matrix.useChunks && !matrix.ShouldStepElementInChunk(this)) return;

            vel = Vector3.Add(vel, new Vector3(0f, -0.5f, 0f));
            if (isFreeFalling) vel.X *= 0.8f;

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

            float slope = (lowerBound == 0 || upperBound == 0) ? 0f : ((float)((lowerBound + 1) / (upperBound + 1)));

            int smallerCount;
            Vector3 formerLocation = new Vector3(matrixX, matrixY, 0);
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

                int modifiedMatrixY = matrixY + (yIncrease * yModifier);
                int modifiedMatrixX = matrixX + (xIncrease * xModifier);
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

            stoppedMovingCount = DidNotMove(formerLocation) ? stoppedMovingCount + 1 : 0;
            if (stoppedMovingCount > stoppedMovingThreshold) { stoppedMovingCount = stoppedMovingThreshold; }
            if (matrix.useChunks) {
                if (!HasNotMovedBeyondThreshold()) {
                    matrix.ReportToChunkActive(this);
                    matrix.ReportToChunkActive((int)formerLocation.X, (int)formerLocation.Y);
                }
            }

        }

        override protected bool ActOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) {
            bool acted = ActOnOther(neighbor, matrix);
            if (acted) return false;
            if (neighbor is EmptyCell) { //or particle (todo)
                if (isFinal) {
                    isFreeFalling = true;
                    SwapPositions(matrix, neighbor, modifiedMatrixX, modifiedMatrixY);
                } else {
                    return false;
                }
            }
            else if (neighbor is Liquid liquidNeighbor) {
                if (CompareDensities(liquidNeighbor)) {
                    if (isFinal) {
                        SwapLiquidByDensity(matrix, liquidNeighbor, modifiedMatrixX, modifiedMatrixY, lastValidLocation);
                        return true;
                    } else {
                        lastValidLocation.X = modifiedMatrixX;
                        lastValidLocation.Y = modifiedMatrixY;
                        return false;
                    }
                }

                if (depth > 0) return true;

                if (isFinal) {
                    MoveToLastValid(matrix, lastValidLocation);
                    return true;
                }

                if (isFreeFalling) {
                    float absY = Math.Max(Math.Abs(vel.Y) / 31, 105);
                    vel.X = vel.X < 0 ? -absY : absY;
                }

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = GetAdditional(normalizedVel.X);
                int additionalY = GetAdditional(normalizedVel.Y);

                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.Get(matrixX + additionalX, matrixY + additionalY);
                if (isFirst) {
                    vel.Y = AverageVel(vel.Y, neighbor.vel.Y);
                } else {
                    vel.Y = -124;
                }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = IterateToAdditional(matrix, matrixX + additionalX, matrixY + additionalY, distance, lastValidLocation);
                    if (!stoppedDiagonally) {
                        isFreeFalling = true;
                        return true;
                    }
                }

                isFreeFalling = false;

                MoveToLastValid(matrix, lastValidLocation);
                return true;
            } else if (neighbor is Solid) {
                if (depth > 0) return true;
                if (isFinal) {
                    MoveToLastValid(matrix, lastValidLocation);
                    return true;
                }

                if (isFreeFalling) {
                    float absY = Math.Max(Math.Abs(vel.Y) / 31, 105);
                    vel.X = vel.X < 0 ? -absY : absY;
                }

                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = GetAdditional(normalizedVel.X);
                int additionalY = GetAdditional(normalizedVel.Y);

                int distance = additionalX * (rng.NextDouble() > 0.5 ? dispersionRate + 2 : dispersionRate - 1);

                Element diagonalNeighbor = matrix.Get(matrixX + additionalX, matrixY + additionalY);
                if (isFirst) {
                    vel.Y = AverageVel(vel.Y, neighbor.vel.Y);
                } else { vel.Y = -124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = IterateToAdditional(matrix, matrixX + additionalX, matrixY + additionalY, distance, lastValidLocation);
                    if (!stoppedDiagonally) {
                        isFreeFalling = true;
                        return true;
                    }
                }

                Element adjacentNeighbor = matrix.Get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null) {
                    bool stoppedAdjacently = IterateToAdditional(matrix, matrixX + additionalX, matrixY, distance, lastValidLocation);
                    if (stoppedAdjacently) vel.X *= -1;
                    if (!stoppedAdjacently) {
                        isFreeFalling = false;
                        return true;
                    }
                }

                isFreeFalling = false;

                MoveToLastValid(matrix, lastValidLocation);
                return true;
            }
            return false;
        }

        private bool IterateToAdditional(WorldMatrix matrix, int startingX, int startingY, int distance, Vector3 lastValid) {
            int distanceModifier = distance > 0 ? 1 : -1;
            Vector3 lastValidLocation = lastValid;
            for (int i =0; i <= Math.Abs(distance); i++) {
                int modifiedX = startingX + i * distanceModifier;

                Element neighbor = matrix.Get(modifiedX, startingY);
                if (neighbor == null) return true;

                bool acted = ActOnOther(neighbor, matrix);
                if (acted) return false;

                bool isFirst = i == 0;
                bool isFinal = i == Math.Abs(distance);

                if (neighbor is EmptyCell) { //or particle (todo)
                    if (isFinal) {
                        SwapPositions(matrix, neighbor, modifiedX, startingY);
                        return false;
                    }
                    lastValidLocation.X = modifiedX;
                    lastValidLocation.Y = startingY;
                } else if (neighbor is Liquid liquidNeighbor) {
                    if (isFinal) {
                        if (CompareDensities(liquidNeighbor)) {
                            SwapLiquidByDensity(matrix, liquidNeighbor, modifiedX, startingY, lastValidLocation);
                            return false;
                        }
                    }
                } else if (neighbor is Solid) {
                    if (isFirst) return true;
                    MoveToLastValid(matrix, lastValidLocation);
                    return false;
                }
            }
            return true;
        }

        private void SwapLiquidByDensity(WorldMatrix matrix, Liquid neighbor, int neighborX, int neighborY, Vector3 lastValidLocation) {
            vel.Y = -62;
            if (rng.NextDouble() > 0.8f) vel.X *= -1;
            MoveToLastValidAndSwap(matrix, neighbor, neighborX, neighborY, lastValidLocation);
        }

        private bool CompareDensities(Liquid neighbor) {
            return (density > neighbor.density && neighbor.matrixY <= matrixY);
        }

        private int GetAdditional(float value) {
            if (value < -.1f) {
                return (int)Math.Floor(value);
            } else if (value > .1f){
                return (int)Math.Ceiling(value);
            } else {
                return 0;
            }
        }

        private float AverageVel(float vel1, float vel2) {
            if (vel2 > -125f) return -124f;

            float avg = (vel1 + vel2) / 2;
            return (avg > 0) ? avg : Math.Min(avg, -124f);
        }

    }
}