namespace Supercell.Laser.Logic.Battle.Level
{
    public class RenderSystem
    {
        protected internal int Height { get; set; }
        protected internal int Width { get; set; }
        protected internal TileMap LogicTileMap { get; set; } = null!;

        public int GetTilemapWidth()
        {
            return Width;
        }

        public bool GetWaterTile(int x, int y)
        {
            return true; // todo
        }

        public int GetTilemapHeight()
        {
            return Height;
        }
    }
}
