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
        public static int pixelSizeMultiplier = 6;
        private List<int> ShuffledXIndexes { get; set; }
        public bool useChunks = true;
        //public int drawThreadCount = 8; //multithreading todo

        private List<List<Element>> matrix;
        private List<List<Chunk>> chunks;

        public WorldMatrix(int width, int height) {
            colSize = width / pixelSizeMultiplier;
            rowSize = height / pixelSizeMultiplier;
            matrix = GenerateMatrix();
            chunks = GenerateChunks();
            ShuffledXIndexes = GenerateShuffledIndexes(colSize);
        }

        /// <summary>
        /// Generates chunks for element matrix
        /// </summary>
        /// <returns>A 2D array of Chunk objects, just large enough to "cover" the world matrix.</returns>
        private List<List<Chunk>> GenerateChunks() {
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
                    newChunk.TopLeft = new Vector3(Math.Min(xPos, colSize), Math.Min(yPos, rowSize), 0);
                    newChunk.BottomRight = new Vector3(Math.Min(xPos + Chunk.size, colSize), Math.Min(yPos + Chunk.size, rowSize), 0);
                }
            }
            return chunks;
        }

        /// <summary>
        /// Fills the matrix with empty elements to initialize it
        /// </summary>
        /// <returns> a 2D array of "Element.EmptyCell"s</returns>
        private List<List<Element>> GenerateMatrix() {
            List<List<Element>> outerArray = new List<List<Element>>(rowSize);
            for (int y = 0; y < rowSize; y++) {
                List<Element> innerArray = new List<Element>(colSize);
                for ( int x = 0; x < colSize; x++) {
                    innerArray.Add(Element.CreateElementByMatrix(x, y, "EmptyCell"));
                }
                outerArray.Add(innerArray);
            }
            return outerArray;
        }

        public bool ShouldStepElementInChunk(Element element) {
            return GetChunkForElement(element).ShouldStep;
        }

        public Chunk GetChunkForElement(Element element) {
            return GetChunkForCoordinates(element.matrixX, element.matrixY);
        }

        public Chunk GetChunkForCoordinates(int x, int y) {
            if (IsWithinBounds(x, y)) {
                int chunkY = y / Chunk.size;
                int chunkX = x / Chunk.size;
                return chunks[chunkY][chunkX];
            }
            return null;
        }

        private void StepAll() {
            for (int y = 0; y < colSize; y++) {
                List<Element> row = GetRow(y);
                foreach (int x in ShuffledXIndexes) {
                    Element element = row[x];
                    if (element != null) {
                        element.Step(this);
                    }
                }
            }
        }

        public List<Element> GetRow(int index) { return matrix[index]; }
        public Element Get(Vector3 location) { return Get((int)location.X, (int)location.Y); }
        public Element Get(float x, float y) { return Get((int)x, (int)y); }
        public Element Get(int x, int y) {
            if (IsWithinBounds(x, y)) {
                return matrix[y][x];
            } else {
                return null;
            }
        }

        public int ToMatrix(float pixelVal) { return ToMatrix((int)pixelVal); }
        public int ToMatrix(int pixelVal) { return pixelVal / 6; }

        public bool SetElementAtIndex(int x, int y, Element element) {
            matrix[x][y] = element;
            element.SetCoordinatesByMatrix(x, y);
            return true;
        }


        public void SpawnElementByPixel(int pixelX, int pixelY, Element element) {
            int matrixX = ToMatrix(pixelX);
            int matrixY = ToMatrix(pixelY);
            SpawnElementByMatrix(matrixX, matrixY, element);
        }

        public Element SpawnElementByMatrix(int matrixX, int matrixY, Element element) {
            if (IsWithinBounds(matrixX, matrixY)) {
                Element currentElement = Get(matrixX, matrixY);
                currentElement.Die(this);
                Element newElement = Element.CreateElementByMatrix(matrixX, matrixY, element.elementName);
                SetElementAtIndex(matrixX, matrixY, newElement);
                ReportToChunkActive(newElement);
                return newElement;
            }
            return null;
        }

        public Element SpawnElementByMatrix(int matrixX, int matrixY, string elementName) {
            if (IsWithinBounds(matrixX, matrixY)) {
                Element currentElement = Get(matrixX, matrixY);
                currentElement.Die(this);
                Element newElement = Element.CreateElementByMatrix(matrixX, matrixY, elementName);
                SetElementAtIndex(matrixX, matrixY, newElement);
                ReportToChunkActive(newElement);
                return newElement;
            }
            return null;
        }

        public bool IsWithinBounds(Vector2 vec) { return IsWithinBounds((int)vec.X, (int)vec.Y); }
        public bool IsWithinBounds(int matrixX, int matrixY) {
            return matrixX >= 0 && matrixY >= 0 && matrixX < rowSize && matrixY < colSize;
        }
        
        public void ReportToChunkActive(Element element) { ReportToChunkActive(element.matrixX, element.matrixY); }
        public void ReportToChunkActive(int x, int y) {
            if (useChunks && IsWithinBounds(x, y)) {
                if (x % Chunk.size == 0) {
                    Chunk chunk = GetChunkForCoordinates(x - 1, y);
                    if (chunk != null) chunk.ShouldStepNextFrame = true;
                }
                if (x % Chunk.size == Chunk.size - 1) {
                    Chunk chunk = GetChunkForCoordinates(x + 1, y);
                    if (chunk != null) chunk.ShouldStepNextFrame = true;
                }
                if (y % Chunk.size == 0) {
                    Chunk chunk = GetChunkForCoordinates(x, y - 1);
                    if (chunk != null) chunk.ShouldStepNextFrame = true;
                }
                if (y % Chunk.size == Chunk.size - 1) {
                    Chunk chunk = GetChunkForCoordinates(x, y + 1);
                    if (chunk != null) chunk.ShouldStepNextFrame = true;
                }
                GetChunkForCoordinates(x, y).ShouldStepNextFrame = true;
            }
        }

        private List<int> GenerateShuffledIndexes(int size) {
            List<int> list = new List<int>();
            for (int i = 0; i < size; i++) { list.Add(i); }
            return list;
        }
    }
}