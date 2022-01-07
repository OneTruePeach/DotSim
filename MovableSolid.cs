using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class MovableSolid : Solid
    {
        public MovableSolid(int x, int y) : base(x, y) {
            stoppedMovingThreshold = 5;
        }

        override public void step(WorldMatrix matrix) {
            if (stepped.Get(0) == true) return;
            stepped.Not();
            if (this.owningBody != null) {
                stepAsPartOfPhysicsBody(matrix);
                return;
            }

            if (matrix.useChunks && !matrix.shouldStepElementInChunk(this)) return;

            vel = Vector3.Add(vel, new Vector3(0f, -0.5f, 0f));
            if (isFreeFalling) vel.X *= 0.9f;

            int yModifier = vel.Y < 0 ? -1 : 1;
            int xModifier = vel.X < 0 ? -1 : 1;
            float velYDeltaTimeFloat = (Math.Abs(vel.Y) * 1/60);
            float velXDeltaTimeFloat = (Math.Abs(vel.X) * 1/60);
            int velXDeltaTime;
            int velYDeltaTime;
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
            stoppedMovingCount = didNotMove(formerLocation) ? stoppedMovingCount + 1 : 0;
            if (stoppedMovingCount > stoppedMovingThreshold) { stoppedMovingCount = stoppedMovingThreshold; }
            if (matrix.useChunks) {
                if (isFreeFalling || !hasNotMovedBeyondThreshold()) {
                    matrix.reportToChunkActive(this);
                    matrix.reportToChunkActive((int)formerLocation.X, (int)formerLocation.Y);
                }
            }
        }

        override protected bool actOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth) {
            if (neighbor is EmptyCell) { //or particle
                setAdjacentNeighborsFreeFalling(matrix, depth, lastValidLocation);
                if (isFinal) {
                    isFreeFalling = true;
                    swapPositions(matrix, neighbor, modifiedMatrixX, modifiedMatrixY);
                } else {
                    return false;
                }
            } else if (neighbor is Liquid) {
                if (depth > 0) {
                    isFreeFalling = true;
                    setAdjacentNeighborsFreeFalling(matrix, depth, lastValidLocation);
                    swapPositions(matrix, neighbor, modifiedMatrixX, modifiedMatrixY);
                } else {
                    isFreeFalling = true;
                    moveToLastValidAndSwap(matrix, neighbor, modifiedMatrixX, modifiedMatrixY, lastValidLocation);
                    return true;
                }
            } else if (neighbor is Solid) {
                if (depth > 0) return true;
                if (isFinal) {
                    moveToLastValid(matrix, lastValidLocation);
                    return true;
                }
                if (isFreeFalling) {
                    float absY = Math.Max(Math.Abs(vel.Y) / 31, 105);
                    vel.X = vel.X < 0 ? -absY : absY;
                }
                Vector3 normalizedVel = vel;
                normalizedVel.Normalize();

                int additionalX = getAdditional(normalizedVel.X);
                int additionalY = getAdditional(normalizedVel.Y);

                Element diagonalNeighbor = matrix.get(matrixX + additionalX, matrixY + additionalY);
                if (isFirst) {
                    vel.Y = averageVel(vel.Y, neighbor.vel.Y);
                } else { vel.Y = -124; }

                neighbor.vel.Y = vel.Y;
                vel.X *= frictionFactor * neighbor.frictionFactor;
                if (diagonalNeighbor != null) {
                    bool stoppedDiagonally = actOnNeighboringElement(diagonalNeighbor, matrixX + additionalX, matrixY + additionalY, matrix, true, false, lastValidLocation, depth + 1);
                    if (!stoppedDiagonally) {
                        isFreeFalling = true;
                        return true;
                    }
                }

                Element adjacentNeighbor = matrix.get(matrixX + additionalX, matrixY);
                if (adjacentNeighbor != null && adjacentNeighbor != diagonalNeighbor) {
                    bool stoppedAdjacently = actOnNeighboringElement(adjacentNeighbor, matrixX + additionalX, matrixY, matrix, true, false, lastValidLocation, depth + 1);
                    if (stoppedAdjacently) vel.X *= -1;
                    if (!stoppedAdjacently) {
                        isFreeFalling = false;
                        return true;
                    }
                }

                isFreeFalling = false;

                moveToLastValid(matrix, lastValidLocation);
                return true;
            }
            return false;
        }

        private void stepAsPartOfPhysicsBody(WorldMatrix matrix) { return; }
        private void setAdjacentNeighborsFreeFalling(WorldMatrix matrix, int depth, Vector3 lastValidLocation) {
            if (depth > 0) return;

            Element adjacentNeighbor1 = matrix.get(lastValidLocation.X + 1, lastValidLocation.Y);
            if (adjacentNeighbor1 is Solid) {
                bool wasSet = setElementFreeFalling(adjacentNeighbor1);
                if (wasSet) {
                    matrix.reportToChunkActive(adjacentNeighbor1);
                }
            }

            Element adjacentNeighbor2 = matrix.get(lastValidLocation.X - 1, lastValidLocation.Y);
            if (adjacentNeighbor2 is Solid) {
                bool wasSet = setElementFreeFalling(adjacentNeighbor2);
                if (wasSet) {
                    matrix.reportToChunkActive(adjacentNeighbor2);
                }
            }
        }

        private bool setElementFreeFalling(Element element) {
            element.isFreeFalling = rng.NextDouble() > element.inertialResistance || element.isFreeFalling;
            return element.isFreeFalling;
        }



        private int getAdditional(float val) {
            if (val < -.1f) {
                return (int)Math.Floor(val);
            }
            else if (val > .1f) {
                return (int)Math.Ceiling(val);
            } else {
                return 0;
            }
        }

        private float averageVel(float vel, float otherVel) {
            if (otherVel > -125f) {
                return -124f;
            }
            float avg = (vel + otherVel) / 2;
            if (avg > 0) {
                return avg;
            } else {
                return Math.Min(avg, -124f);
            }
        }
    }
}
