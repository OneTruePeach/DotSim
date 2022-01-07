using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DotSim
{
    public abstract class Element
    {
        protected Random rng = new Random();
        private static int REACTION_FRAME = 3;
        public static int EFFECTS_FRAME = 1;
        public int pixelX;
        public int pixelY;
        public int matrixX;
        public int matrixY;
        public float xThreshold = 0;
        public float yThreshold = 0;
        public Vector3 vel = new Vector3();
        public int stoppedMovingCount = 0;
        public int stoppedMovingThreshold = 1;
        public bool isFreeFalling = true;
        public float frictionFactor;
        public float inertialResistance;

        public int mass;
        public int health = 500;
        public int? lifeSpan = null;
        public bool isDead = false;

        public int temperature = 0;
        public int flammabilityResistance = 100;
        public bool isIgnited;
        public int heatFactor = 10;
        public int coolingFactor = 5;
        public int fireDamage = 3;
        public bool heated = false;

        public int explosionResistance = 1;
        public int explosionRadius = 0;
        public bool discolored = false;

        public List<Color> colorList = new List<Color>();
        public Color color = new Color();
        public Color defaultColor = new Color();
        public string elementName;
        public PhysicsElementActor owningBody = null;
        public Vector2? owningBodyCoords = null;
        public List<Vector2> secondaryMatrixCoords = new List<Vector2>();

        public BitArray stepped = new BitArray(1);

        public Element(int x, int y) { 
            setCoordinatesByMatrix(x, y);
            defaultColor = getColorForThisElement(elementName);
            color = defaultColor; 
            stepped.Set(0, false);
        }

        public abstract void step(WorldMatrix matrix);
        public abstract bool actOnOther(Element other, WorldMatrix matrix);
        protected abstract bool actOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth);

        public void swapPositions(WorldMatrix matrix, Element toSwap) { swapPositions(matrix, toSwap, toSwap.matrixX, toSwap.matrixY); }
        public void swapPositions(WorldMatrix matrix, Element toSwap, int toSwapX, int toSwapY) {
            if (matrixX == toSwapX && matrixY == toSwapY) { return; }
            matrix.setElementAtIndex(matrixX, matrixY, toSwap);
            matrix.setElementAtIndex(toSwapX, toSwapY, this);
        }
       
        public void moveToLastValid(WorldMatrix matrix, Vector3 moveToLocation) {
            if ((int)(moveToLocation.X) == matrixX && (int)(moveToLocation.Y) == matrixY) return;
            Element toSwap = matrix.get(moveToLocation);
            swapPositions(matrix, toSwap, (int)moveToLocation.X, (int)moveToLocation.Y);
        }

        public void moveToLastValidAndSwap(WorldMatrix matrix, Element toSwap, int toSwapX, int toSwapY, Vector3 moveToLocation) {
            Element thirdNeighbor = matrix.get(moveToLocation);
            if (this == thirdNeighbor || toSwap == thirdNeighbor) {
                swapPositions(matrix, toSwap, toSwapX, toSwapY);
                return;
            }

            if (this == toSwap) {
                swapPositions(matrix, thirdNeighbor, (int)moveToLocation.X, (int)moveToLocation.Y);
                return;
            }

            matrix.setElementAtIndex(matrixX, matrixY, thirdNeighbor);
            matrix.setElementAtIndex(toSwapX, toSwapY, this);
            matrix.setElementAtIndex((int)moveToLocation.X, (int)moveToLocation.Y, toSwap);
        }

        public void setOwningBodyCoords(Vector2 coords) { setOwningBodyCoords((int)coords.Y, (int)coords.Y); }
        public void setOwningBodyCoords(int x, int y) { owningBodyCoords = new Vector2(x, y); }


        public void setCoordinatesByMatrix(int x, int y) {
            matrixX = x;
            matrixY = y;
            pixelX = x * WorldMatrix.pixelSizeMultiplier;
            pixelY = y * WorldMatrix.pixelSizeMultiplier;
        }

        public bool isReactionFrame() {
            return true; //game.framecount % 3 == REACTION_FRAME;
        }
        public bool isEffectsFrame() {
            return true; //game.framecount % 3 == EFFECTS_FRAME;
        }

        public virtual bool corrode(WorldMatrix matrix) {
            health -= 170;
            checkIfDead(matrix);
            return true;
        }

        public bool applyHeatToNeighborsIfIgnited(WorldMatrix matrix) {
            if (!isEffectsFrame() || !shouldApplyHeat()) return false;
            for (int x = matrixX - 1; x <= matrixX + 1; x++) {
                for (int y = matrixY - 1; y <= matrixY + 1; y++) {
                    if (!(x == 0 && y == 0)) {
                        Element neighbor = matrix.get(x, y);
                        if (neighbor != null) { neighbor.receiveHeat(matrix, heatFactor); }
                    }
                }
            }
            return true;
        }

        public virtual bool shouldApplyHeat() { return isIgnited || heated; }

        public virtual bool receiveHeat(WorldMatrix matrix, int heat) {
            if (isIgnited) { return false; }
            flammabilityResistance -= (int)(rng.NextDouble() * heat);
            checkIfIgnited();
            return true;
        }

        public bool receiveCooling(WorldMatrix matrix, int cooling) {
            if (isIgnited) {
                flammabilityResistance += cooling;
                checkIfIgnited();
                return true;
            }
            return false;
        }

        public void checkIfIgnited() {
            if (flammabilityResistance <= 0) {
                isIgnited = true;
                modifyColor();
            } else {
                isIgnited = false;
                color = defaultColor;
            }
        }

        public void checkIfDead(WorldMatrix matrix) {
            if (health <= 0) { die(matrix); }
        }

        public virtual void dieAndReplace(WorldMatrix matrix, string element) { die(matrix, element); }
        public void die(WorldMatrix matrix) { die(matrix, "EmptyCell"); }
        public void die(WorldMatrix matrix, string element) {
            isDead = true;
            Element newElement = createElementByMatrix(matrixX, matrixY, element);
            matrix.setElementAtIndex(matrixX, matrixY, newElement);
           matrix.reportToChunkActive(matrixX, matrixY);
            if (owningBody != null) {
                owningBody.elementDeath(this, newElement);
                foreach(Vector2 vector in secondaryMatrixCoords) {
                    matrix.setElementAtIndex((int)vector.X, (int)vector.Y, createElementByMatrix(0, 0, element));
                }
            }
        }

        public void dieAndReplaceWithParticle(WorldMatrix matrix, Vector3 velocity) {
            matrix.setElementAtIndex(matrixX, matrixY, createParticleByMatrix(matrix, matrixX, matrixY, velocity, this, color, isIgnited));
            matrix.reportToChunkActive(matrixX, matrixY);
        }

        public bool didNotMove(Vector3 formerLocation) { return formerLocation.X == matrixX && formerLocation.Y == matrixY; }
        public bool hasNotMovedBeyondThreshold() { return stoppedMovingCount >= stoppedMovingThreshold; }

        public void takeEffectsDamage(WorldMatrix matrix) {
            if (!isEffectsFrame()) { return; }
            if (isIgnited) { takeFireDamage(matrix); }
            checkIfDead(matrix);
        }

        public void takeFireDamage(WorldMatrix matrix) {
            health -= fireDamage;
            if (isSurrounded(matrix)) {
                flammabilityResistance /= 2;
            }
            checkIfIgnited();
        }

        public virtual bool stain(Color color) {
            if (rng.NextDouble() > 0.2f || isIgnited) return false;
            discolored = true;
            return true;
        }

        public virtual bool stain(float r, float g, float b, float a) {
            if (rng.NextDouble() > 0.2 || isIgnited) { return false; }
            color.R += (byte)r;
            color.G += (byte)g;
            color.B += (byte)b;
            color.A += (byte)a;
            if (color.R > 255) { color.R = 255; }
            if (color.G > 255) { color.G = 255; }
            if (color.B > 255) { color.B = 255; }
            if (color.A > 255) { color.A = 255; }
            if (color.R < 0) { color.R = 0; }
            if (color.G < 0) { color.G = 0; }
            if (color.B < 0) { color.B = 0; }
            if (color.A < 0) { color.A = 0; }
            discolored = true;
            return true;
        }

        /// <summary>
        /// Spawns an element at a location. TODO: make this abstract and have individual materials override it(?)
        /// </summary>
        /// <returns>An instance of the specified element</returns>
        public static Element createElementByMatrix(int x, int y, Element element) {
            if (element is EmptyCell) { return EmptyCell.getInstance(); }
            if (element is Sand) { return new Sand(x, y); }
            if (element is Stone) { return new Stone(x, y); }
            if (element is Water) { return new Water(x, y); }
            return null;
        }

        /// <summary>
        /// Spawns an element at a location. TODO: make this abstract and have individual materials override it(?)
        /// </summary>
        /// <returns>An instance of the specified element</returns>
        public static Element createElementByMatrix(int x, int y, string element) {
            if (element == "EmptyCell") { return EmptyCell.getInstance(); }
            if (element == "Sand") { return new Sand(x, y); }
            if (element == "Stone") { return new Stone(x, y); }
            if (element == "Water") { return new Water(x, y); }
            return null;
        }

        public static Element createParticleByMatrix(WorldMatrix matrix, int x, int y, Vector3 vector3, Element element, Color color, bool isIgnited) {
            if (matrix.isWithinBounds(x, y)) {
                Element newElement = new Particle(x, y, vector3, element, color, isIgnited);
                matrix.setElementAtIndex(x, y, newElement);
                return newElement;
            }
            return null;
        }

        public List<Color> getColorsByElementName(string element) {
            List<Color> colors = new List<Color>();
            if (element == "EmptyCell") { colors.Add(new Color(0, 0, 0, 0)); }
            if (element == "Sand") {
                colors.AddRange(new List<Color> {
                    new Color(255, 255, 000, 255),
                    new Color(178, 201, 006, 255),
                    new Color(233, 252, 090, 255)
                });
            }
            if (element == "Stone") {
                colors.AddRange(new List<Color> {
                    new Color(150, 150, 150, 255)
                });
            }
            if (element == "Water") {
                colors.AddRange(new List<Color> {
                    new Color(028, 086, 234, 204),
                    new Color(178, 201, 006, 255),
                    new Color(233, 252, 090, 255)
                });
            }
            if (element == "Steam") {
                colors.AddRange(new List<Color> {
                    new Color(204, 204, 204, 026),
                    new Color(204, 204, 204, 115),
                    new Color(204, 204, 204, 204)
                });
            }
            return colors;
        }

        public Color getColorForThisElement(string element) {
            List<Color> colors = new List<Color>();
            colors = getColorsByElementName(element);
            int random = rng.Next(0, colors.Count);
            Color color = colors[random];
            return color;
        }

        public virtual bool explode(WorldMatrix matrix, int strength) {
            if (explosionResistance < strength) {
                if (rng.NextDouble() > 0.3) {
                    dieAndReplace(matrix, "ExplosionSpark");
                } else {
                    die(matrix);
                }
                return true;
            } else {
                darkenColor();
                return false;
            }
        }

        public virtual void darkenColor() {
            color = new Color(color.R * .85f, color.G * .85f, color.B * .85f, color.A);
            discolored = true;
        }

        public virtual void darkenColor(float factor) {
            color = new Color(color.R * factor, color.G * factor, color.B * factor, color.A);
            discolored = true;
        }

        private bool isSurrounded(WorldMatrix matrix) {
            if (matrix.get(matrixX, matrixY + 1) is EmptyCell) { return false; }
            if (matrix.get(matrixX, matrixY - 1) is EmptyCell) { return false; }
            if (matrix.get(matrixX + 1, matrixY) is EmptyCell) { return false; }
            if (matrix.get(matrixX - 1, matrixY) is EmptyCell) { return false; }
            return true;
        }

        public virtual void spawnSparkIfIgnited(WorldMatrix matrix) {
            if (!isEffectsFrame() || !isIgnited) return;
            Element upNeighbor = matrix.get(matrixX, matrixY + 1);
            if (upNeighbor != null) {
                if (upNeighbor is EmptyCell) {
                    string elementToSpawn = rng.NextDouble() > .1 ? "Spark" : "Smoke";
                    matrix.spawnElementByMatrix(matrixX, matrixY + 1, elementToSpawn);
                }
            }
        }

        public void checkLifeSpan(WorldMatrix matrix) {
            if (lifeSpan != null) {
                lifeSpan--;
                if (lifeSpan <= 0) {
                    die(matrix);
                }
            }
        }

        public void magmatize(WorldMatrix matrix, int damage) {
            health -= damage;
            checkIfDead(matrix);
        }

        public virtual bool infect(WorldMatrix matrix) {
            if (rng.NextDouble() > 0.95f) {
                dieAndReplace(matrix, "SlimeMold");
                return true;
            }
            return false;
        }

        public void modifyColor() { if (isIgnited) { color = getColorForThisElement("Fire"); } }

        public bool cleanColor() {
            if (!discolored || rng.NextDouble() > .2f) return false;
            color = defaultColor;
            discolored = false;
            return true;
        }
    }
}