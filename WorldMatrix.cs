using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public class WorldMatrix
    {
        public int colSize;
        public int rowSize;
        public int pixelSizeMultiplier = 6;
        public bool useChunks = true;
        //public int drawThreadCount = 8; //multithreading todo

        private List<List<Element>> matrix;
        private List<List<Chunk>> chunks;

        public WorldMatrix(int width, int height) {
            colSize = width / pixelSizeMultiplier;
            rowSize = height / pixelSizeMultiplier;
            matrix = generateMatrix();
            chunks = generateChunks();
        }

        /// <summary>
        /// I think this works as expected, but I'm not 100% sure how nested lists behave.
        /// </summary>
        /// <returns>A 2D array of Chunk objects, just large enough to "cover" the world matrix.</returns>
        private List<List<Chunk>> generateChunks() {
            List<List<Chunk>> chunks = new List<List<Chunk>>();
            int rows = (int)Math.Ceiling((double)(rowSize / Chunk.size));
            int columns = (int)Math.Ceiling((double)(colSize / Chunk.size));
            for (int r = 0; r < rows; r++) {
                chunks.Add(new List<Chunk>());
                for (int c = 0; c < columns; c++) {
                    int xPos = c * Chunk.size;
                    int yPos = r * Chunk.size;
                    Chunk newChunk = new Chunk();
                    chunks[r].Add(newChunk);
                    newChunk.topLeft = new Vector3(Math.Min(xPos, colSize), Math.Min(yPos, rowSize), 0);
                    newChunk.bottomRight = new Vector3(Math.Min(xPos + Chunk.size, colSize), Math.Min(yPos + Chunk.size, rowSize), 0);
                }
            }
            return chunks;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private List<List<Element>> generateMatrix() {
            List<List<Element>> outerArray = new List<List<Element>>(rowSize);
            for (int y = 0; y < rowSize; y++) {
                List<Element> innerArr = new List<Element>(colSize);
                for ( int x = 0; x < colSize; x++) {
                    innerArr.Add(ElementType.EMPTYCELL.createElementByMatrix(x, y));
                }
            }
        }

        public bool shouldStepElementInChunk(Element element) {
            return GetChunkForElement(element).shouldStep;
        }

        public Chunk GetChunkForElement(Element element) {
            return GetChunkForCoordinates(element.matrixX, element.matrixY);
        }

        public Chunk GetChunkForCoordinates(int x, int y) {
            if (isWithinBounds(x, y)) {
                int chunkY = y / Chunk.size;
                int chunkX = x / Chunk.size;
                return chunks[chunkY][chunkX];
            }
            return null;
        }

        public bool isWithinBounds(int matrixX, int matrixY) {
            return matrixX >= 0 && matrixY >= 0 && matrixX < rowSize && matrixY < colSize;
        }
    }
}
