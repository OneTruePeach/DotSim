using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DotSim
{
    public abstract class Element
    {
        protected Random rng = new Random();
        private static readonly int REACTION_FRAME = 3;
        public static readonly int EFFECTS_FRAME = 1;
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
        public int resetFlammabilityResistance = 50;
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
            SetCoordinatesByMatrix(x, y);
            defaultColor = GetColorForThisElement(elementName);
            color = defaultColor; 
            stepped.Set(0, false);
        }

        public virtual void CustomElementFunctions(WorldMatrix matrix) { }
        public abstract void Step(WorldMatrix matrix);
        public virtual bool ActOnOther(Element other, WorldMatrix matrix) { return false; }
        protected abstract bool ActOnNeighboringElement(Element neighbor, int modifiedMatrixX, int modifiedMatrixY, WorldMatrix matrix, bool isFinal, bool isFirst, Vector3 lastValidLocation, int depth);

        public void SwapPositions(WorldMatrix matrix, Element toSwap) { SwapPositions(matrix, toSwap, toSwap.matrixX, toSwap.matrixY); }
        public void SwapPositions(WorldMatrix matrix, Element toSwap, int toSwapX, int toSwapY) {
            if (matrixX == toSwapX && matrixY == toSwapY) { return; }
            matrix.SetElementAtIndex(matrixX, matrixY, toSwap);
            matrix.SetElementAtIndex(toSwapX, toSwapY, this);
        }
       
        public void MoveToLastValid(WorldMatrix matrix, Vector3 moveToLocation) {
            if ((int)(moveToLocation.X) == matrixX && (int)(moveToLocation.Y) == matrixY) return;
            Element toSwap = matrix.Get(moveToLocation);
            SwapPositions(matrix, toSwap, (int)moveToLocation.X, (int)moveToLocation.Y);
        }

        public void MoveToLastValidAndSwap(WorldMatrix matrix, Element toSwap, int toSwapX, int toSwapY, Vector3 moveToLocation) {
            Element thirdNeighbor = matrix.Get(moveToLocation);
            if (this == thirdNeighbor || toSwap == thirdNeighbor) {
                SwapPositions(matrix, toSwap, toSwapX, toSwapY);
                return;
            }

            if (this == toSwap) {
                SwapPositions(matrix, thirdNeighbor, (int)moveToLocation.X, (int)moveToLocation.Y);
                return;
            }

            matrix.SetElementAtIndex(matrixX, matrixY, thirdNeighbor);
            matrix.SetElementAtIndex(toSwapX, toSwapY, this);
            matrix.SetElementAtIndex((int)moveToLocation.X, (int)moveToLocation.Y, toSwap);
        }

        public void SetOwningBodyCoords(Vector2 coords) { SetOwningBodyCoords((int)coords.Y, (int)coords.Y); }
        public void SetOwningBodyCoords(int x, int y) { owningBodyCoords = new Vector2(x, y); }


        public void SetCoordinatesByMatrix(int x, int y) {
            matrixX = x;
            matrixY = y;
            pixelX = x * WorldMatrix.pixelSizeMultiplier;
            pixelY = y * WorldMatrix.pixelSizeMultiplier;
        }

        public bool IsReactionFrame() {
            return true; //game.framecount % 3 == REACTION_FRAME;
        }
        public bool IsEffectsFrame() {
            return true; //game.framecount % 3 == EFFECTS_FRAME;
        }

        public virtual bool Corrode(WorldMatrix matrix) {
            health -= 170;
            CheckIfDead(matrix);
            return true;
        }

        public bool ApplyHeatToNeighborsIfIgnited(WorldMatrix matrix) {
            if (!IsEffectsFrame() || !ShouldApplyHeat()) return false;
            for (int x = matrixX - 1; x <= matrixX + 1; x++) {
                for (int y = matrixY - 1; y <= matrixY + 1; y++) {
                    if (!(x == 0 && y == 0)) {
                        Element neighbor = matrix.Get(x, y);
                        if (neighbor != null) { neighbor.ReceiveHeat(matrix, heatFactor); }
                    }
                }
            }
            return true;
        }

        public virtual bool ShouldApplyHeat() { return isIgnited || heated; }

        public virtual bool ReceiveHeat(WorldMatrix matrix, int heat) {
            if (isIgnited) { return false; }
            flammabilityResistance -= (int)(rng.NextDouble() * heat);
            CheckIfIgnited();
            return true;
        }

        public bool ReceiveCooling(WorldMatrix matrix, int cooling) {
            if (isIgnited) {
                flammabilityResistance += cooling;
                CheckIfIgnited();
                return true;
            }
            return false;
        }

        public void CheckIfIgnited() {
            if (flammabilityResistance <= 0) {
                isIgnited = true;
                ModifyColor();
            } else {
                isIgnited = false;
                color = defaultColor;
            }
        }

        public virtual void CheckIfDead(WorldMatrix matrix) {
            if (health <= 0) { Die(matrix); }
        }

        public virtual void DieAndReplace(WorldMatrix matrix, string element) { Die(matrix, element); }
        public void Die(WorldMatrix matrix) { Die(matrix, "EmptyCell"); }
        public void Die(WorldMatrix matrix, string element) {
            isDead = true;
            Element newElement = CreateElementByMatrix(matrixX, matrixY, element);
            matrix.SetElementAtIndex(matrixX, matrixY, newElement);
           matrix.ReportToChunkActive(matrixX, matrixY);
            if (owningBody != null) {
                owningBody.elementDeath(this, newElement);
                foreach(Vector2 vector in secondaryMatrixCoords) {
                    matrix.SetElementAtIndex((int)vector.X, (int)vector.Y, CreateElementByMatrix(0, 0, element));
                }
            }
        }

        public void DieAndReplaceWithParticle(WorldMatrix matrix, Vector3 velocity) {
            matrix.SetElementAtIndex(matrixX, matrixY, CreateParticleByMatrix(matrix, matrixX, matrixY, velocity, this, color, isIgnited));
            matrix.ReportToChunkActive(matrixX, matrixY);
        }

        public bool DidNotMove(Vector3 formerLocation) { return formerLocation.X == matrixX && formerLocation.Y == matrixY; }
        public bool HasNotMovedBeyondThreshold() { return stoppedMovingCount >= stoppedMovingThreshold; }

        public void TakeEffectsDamage(WorldMatrix matrix) {
            if (!IsEffectsFrame()) { return; }
            if (isIgnited) { TakeFireDamage(matrix); }
            CheckIfDead(matrix);
        }

        public virtual void TakeFireDamage(WorldMatrix matrix) {
            health -= fireDamage;
            if (IsSurrounded(matrix)) {
                flammabilityResistance /= 2;
            }
            CheckIfIgnited();
        }

        public virtual bool Stain(Color color) {
            if (rng.NextDouble() > 0.2f || isIgnited) return false;
            discolored = true;
            return true;
        }

        public virtual bool Stain(float r, float g, float b, float a) {
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
        public static Element CreateElementByMatrix(int x, int y, Element element) {
            if (element is EmptyCell) { return EmptyCell.GetInstance(); }
            if (element is Sand) { return new Sand(x, y); }
            if (element is Stone) { return new Stone(x, y); }
            if (element is Water) { return new Water(x, y); }
            return null;
        }

        /// <summary>
        /// Spawns an element at a location. TODO: make this abstract and have individual materials override it(?)
        /// </summary>
        /// <returns>An instance of the specified element</returns>
        public static Element CreateElementByMatrix(int x, int y, string element) {
            if (element == "EmptyCell") { return EmptyCell.GetInstance(); } //not materials
            if (element == "Particle") { return null; }

            if (element == "Ground") { return new Ground(x, y); } //immovable solids
            if (element == "Stone") { return new Stone(x, y); }
            if (element == "Brick") { return new Brick(x, y); }
            if (element == "Wood") { return new Wood(x, y); }
            if (element == "Titanium") { return new Titanium(x, y); }
            if (element == "Bacteria") { return new Bacteria(x, y); }

            if (element == "Sand") { return new Sand(x, y); } //movable solids
            if (element == "Dirt") { return new Dirt(x, y); }
            if (element == "Snow") { return new Snow(x, y); }
            if (element == "Coal") { return new Coal(x, y); }
            if (element == "Ember") { return new Ember(x, y); }
            if (element == "Gunpowder") { return new Gunpowder(x, y); }

            if (element == "Water") { return new Water(x, y); } //liquids
            if (element == "Oil") { return new  Oil(x, y); }
            if (element == "Acid") { return new Acid(x, y); }
            if (element == "Blood") { return new Blood(x, y); }
            if (element == "Lava") { return new Lava(x, y); }
            if (element == "Cement") { return new Cement(x, y); }

            if (element == "Smoke") { return new Smoke(x, y); } //gasses
            if (element == "Steam") { return new Steam(x, y); }
            if (element == "Spark") { return new Spark(x, y); }
            if (element == "FlammableGas") { return new FlammableGas(x, y); }
            if (element == "ExplosionSpark") { return new ExplosionSpark(x, y); }

            return null;
        }

        public static Element CreateParticleByMatrix(WorldMatrix matrix, int x, int y, Vector3 vector3, Element element, Color color, bool isIgnited) {
            if (matrix.IsWithinBounds(x, y)) {
                Element newElement = new Particle(x, y, vector3, element, color, isIgnited);
                matrix.SetElementAtIndex(x, y, newElement);
                return newElement;
            }
            return null;
        }

        public List<Color> GetColorsByElementName(string element) {
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

        public Color GetColorForThisElement(string element) {
            List<Color> colors = GetColorsByElementName(element);
            int random = rng.Next(0, colors.Count);
            Color color = colors[random];
            return color;
        }

        public virtual bool Explode(WorldMatrix matrix, int strength) {
            if (explosionResistance < strength) {
                if (rng.NextDouble() > 0.3) {
                    DieAndReplace(matrix, "ExplosionSpark");
                } else {
                    Die(matrix);
                }
                return true;
            } else {
                DarkenColor();
                return false;
            }
        }

        public virtual void DarkenColor() {
            color = new Color(color.R * .85f, color.G * .85f, color.B * .85f, color.A);
            discolored = true;
        }

        public virtual void DarkenColor(float factor) {
            color = new Color(color.R * factor, color.G * factor, color.B * factor, color.A);
            discolored = true;
        }

        private bool IsSurrounded(WorldMatrix matrix) {
            if (matrix.Get(matrixX, matrixY + 1) is EmptyCell) { return false; }
            if (matrix.Get(matrixX, matrixY - 1) is EmptyCell) { return false; }
            if (matrix.Get(matrixX + 1, matrixY) is EmptyCell) { return false; }
            if (matrix.Get(matrixX - 1, matrixY) is EmptyCell) { return false; }
            return true;
        }

        public virtual void SpawnSparkIfIgnited(WorldMatrix matrix) {
            if (!IsEffectsFrame() || !isIgnited) return;
            Element upNeighbor = matrix.Get(matrixX, matrixY + 1);
            if (upNeighbor != null) {
                if (upNeighbor is EmptyCell) {
                    string elementToSpawn = rng.NextDouble() > .1 ? "Spark" : "Smoke";
                    matrix.SpawnElementByMatrix(matrixX, matrixY + 1, elementToSpawn);
                }
            }
        }

        public void CheckLifeSpan(WorldMatrix matrix) {
            if (lifeSpan != null) {
                lifeSpan--;
                if (lifeSpan <= 0) {
                    Die(matrix);
                }
            }
        }

        public void Magmatize(WorldMatrix matrix, int damage) {
            health -= damage;
            CheckIfDead(matrix);
        }

        public virtual bool Infect(WorldMatrix matrix) {
            if (rng.NextDouble() > 0.95f) {
                DieAndReplace(matrix, "Bacteria");
                return true;
            }
            return false;
        }

        public virtual void ModifyColor() { if (isIgnited) { color = GetColorForThisElement("Fire"); } }

        public Color GetRandomFireColor() { //this might look ok? idk
            List<Color> fireColors = new List<Color>();
            fireColors.AddRange(new List<Color> {
                    new Color(255, 069, 000, 255),
                    new Color(255, 255, 000, 255),
                    new Color(150, 000, 000, 255)});
            return fireColors[(int)(rng.NextDouble() * fireColors.Count)];
        }

        public bool CleanColor() {
            if (!discolored || rng.NextDouble() > .2f) return false;
            color = defaultColor;
            discolored = false;
            return true;
        }
    }
}