﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class MovableSolid : Solid
    {
        private Random rng = new Random();

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
            float velYDeltaTimeFloat = (Math.Abs(vel.Y) * 1 / 60);
            float velXDeltaTimeFloat = (Math.Abs(vel.X) * 1 / 60);
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
                    matrix.setElementAtIndex(matrixX, matrixY, ElementType.EMPTYCELL.createElementByMatrix(matrixX, matrixY));
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

        private void stepAsPartOfPhysicsBody() { return; }

    }
}