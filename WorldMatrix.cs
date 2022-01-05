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
        public int colSize; //inner
        public int rowSize; //outer
        public int pixelSizeMultiplier = 6;
        private List<int> shuffledXIndexes { get; set; }
        public bool useChunks = true;
        //public int drawThreadCount = 8; //multithreading todo

        private List<List<Element>> matrix;
        private List<List<Chunk>> chunks;

        public WorldMatrix(int width, int height) {
            colSize = width / pixelSizeMultiplier;
            rowSize = height / pixelSizeMultiplier;
            matrix = generateMatrix();
            chunks = generateChunks();
            shuffledXIndexes = generateShuffledIndexes(colSize);
        }

        /// <summary>
        /// Generates chunks for element matrix
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
        /// Fills the matrix with empty elements to initialize it
        /// </summary>
        /// <returns> a 2D array of "Element.EmptyCell"s</returns>
        private List<List<Element>> generateMatrix() {
            List<List<Element>> outerArray = new List<List<Element>>(rowSize);
            for (int y = 0; y < rowSize; y++) {
                List<Element> innerArray = new List<Element>(colSize);
                for ( int x = 0; x < colSize; x++) {
                    innerArray.Add(Element.createElementByMatrix(x, y, "EmptyCell"));
                }
                outerArray.Add(innerArray);
            }
            return outerArray;
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

        private void stepAll() {
            for (int y = 0; y < colSize; y++) {
                List<Element> row = getRow(y);
                foreach (int x in shuffledXIndexes) {
                    Element element = row[x];
                    if (element != null) {
                        element.step(this);
                    }
                }
            }
        }

        public List<Element> getRow(int index) { return matrix[index]; }
        public Element get(Vector3 location) { return get((int)location.X, (int)location.Y); }
        public Element get(float x, float y) { return get((int)x, (int)y); }
        public Element get(int x, int y) {
            if (isWithinBounds(x, y)) {
                return matrix[y][x];
            } else {
                return null;
            }
        }

        public int toMatrix(float pixelVal) { return toMatrix((int)pixelVal); }
        public int toMatrix(int pixelVal) { return pixelVal / 6; }

        public bool setElementAtIndex(int x, int y, Element element) {
            matrix[x][y] = element;
            element.setCoordinatesByMatrix(x, y);
            return true;
        }


        public void spawnElementByPixel(int pixelX, int pixelY, Element element) {
            int matrixX = toMatrix(pixelX);
            int matrixY = toMatrix(pixelY);
            spawnElementByMatrix(matrixX, matrixY, element);
        }

        public Element spawnElementByMatrix(int matrixX, int matrixY, Element element) {
            if (isWithinBounds(matrixX, matrixY)) {
                Element currentElement = get(matrixX, matrixY);
                //get(matrixX, matrixY).die(this);
                Element newElement = Element.createElementByMatrix(matrixX, matrixY, element.elementName);
                setElementAtIndex(matrixX, matrixY, newElement);
                reportToChunkActive(newElement);
                return newElement;
            }
            return null;
        }

        public bool isWithinBounds(Vector2 vec) { return isWithinBounds((int)vec.X, (int)vec.Y); }
        public bool isWithinBounds(int matrixX, int matrixY) {
            return matrixX >= 0 && matrixY >= 0 && matrixX < rowSize && matrixY < colSize;
        }
        
        public void reportToChunkActive(Element element) { reportToChunkActive(element.matrixX, element.matrixY); }
        public void reportToChunkActive(int x, int y) {
            if (useChunks && isWithinBounds(x, y)) {
                if (x % Chunk.size == 0) {
                    Chunk chunk = getChunkForCoordinates(x - 1, y);
                    if (chunk != null) chunk.shouldStepNextFrame = true;
                }
                if (x % Chunk.size == Chunk.size - 1) {
                    Chunk chunk = getChunkForCoordinates(x + 1, y);
                    if (chunk != null) chunk.shouldStepNextFrame = true;
                }
                if (y % Chunk.size == 0) {
                    Chunk chunk = getChunkForCoordinates(x, y - 1);
                    if (chunk != null) chunk.shouldStepNextFrame = true;
                }
                if (y % Chunk.size == Chunk.size - 1) {
                    Chunk chunk = getChunkForCoordinates(x, y + 1);
                    if (chunk != null) chunk.shouldStepNextFrame = true;
                }
                getChunkForCoordinates(x, y).shouldStepNextFrame = true;
            }
        }

        public Chunk getChunkForCoordinates(int x, int y) {
            if (isWithinBounds(x, y)) {
                int chunkY = y / Chunk.size;
                int chunkX = x / Chunk.size;
                return chunks[chunkY][chunkX];
            }
            return null;
        }

        private List<int> generateShuffledIndexes(int size) {
            List<int> list = new List<int>();
            for (int i = 0; i < size; i++) { list.Add(i); }
            return list;
        }
    }
}