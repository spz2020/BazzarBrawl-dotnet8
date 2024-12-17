namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Math;

    public class LogicItemServer : LogicGameObjectServer
    {
        public ItemData ItemData => DataTables.Get(18).GetDataByGlobalId<ItemData>(DataId);

        private bool m_pickedUp;
        private bool m_shouldPlayAppearAnimation;
        private int m_angle;
        private int m_ticksGone;

        private LogicCharacterServer m_picker;
        private int m_ticksSincePickUp;

        public LogicItemServer(int classId, int instanceId) : base(classId, instanceId)
        {
            m_shouldPlayAppearAnimation = true;
        }

        public override void Tick()
        {
            m_ticksGone++;

            if (ItemData.CanBePickedUp && m_shouldPlayAppearAnimation)
            {
                if (m_ticksGone <= 10)
                {
                    int dx = LogicMath.Cos(m_angle) / 20;
                    int dy = LogicMath.Sin(m_angle) / 20;

                    int newX = GetX() + dx;
                    int newY = GetY() + dy;

                    int tx = TileMap.LogicToTile(newX);
                    int ty = TileMap.LogicToTile(newY);

                    TileMap tileMap = GameObjectManager.GetBattle().GetTileMap();

                    if (!(tx >= tileMap.Width || tx <= 0))
                    {
                        Position.X = newX;
                    }

                    if (!(ty >= tileMap.Height || ty <= 0))
                    {
                        Position.Y = newY;
                    }

                    if (m_ticksGone <= 10 / 2)
                    {
                        Z += 120;
                    }
                    else
                    {
                        Z -= 120;
                    }
                }
                else if (m_ticksGone <= 14)
                {
                    int v1 = m_ticksGone - 10;
                    if (v1 <= 2)
                    {
                        Z += 120 / v1;
                    }
                    else
                    {
                        Z -= 120 / (v1 - 2);
                    }
                }
            }

            if (m_pickedUp)
            {
                m_ticksSincePickUp++;

                int deltaX = (int)(0.05f * (float)LogicMath.Cos((int)(m_angle - (float)(System.Math.PI * 90 / 180.0))));
                int deltaY = (int)(0.05f * (float)LogicMath.Sin((int)(m_angle - (float)(System.Math.PI * 90 / 180.0))));

                Position.X += deltaX;
                Position.Y += deltaY;
                Z += 120;

                if (m_ticksSincePickUp >= 4)
                {
                    m_picker.ApplyItem(this);
                    GameObjectManager.RemoveGameObject(this);
                }
            }
        }

        public void PickUp(LogicCharacterServer character)
        {
            m_shouldPlayAppearAnimation = false;
            m_pickedUp = true;
            m_picker = character;
            m_ticksSincePickUp = 0;

            int dx = character.GetX() - Position.X;
            int dy = character.GetY() - Position.Y;
            m_angle = LogicMath.GetAngle(dx, dy);
        }

        public void SetAngle(int angle)
        {
            m_angle = angle;
        }

        public void DisableAppearAnimation()
        {
            m_shouldPlayAppearAnimation = false;
        }

        public bool CanBePickedUp()
        {
            return ItemData.CanBePickedUp && !m_pickedUp;
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);
            bitStream.WritePositiveInt(10, 4);


            if (ItemData.GetInstanceId() == 5 || ItemData.GetInstanceId() == 24)
            {
                bitStream.WritePositiveIntMax16383(170);
                bitStream.WritePositiveIntMax16383(30);

                if (ItemData.GetInstanceId() == 24) bitStream.WritePositiveIntMax16383(0);
            }
            else
            {
                var v7 = ItemData.GetInstanceId();

                if (v7 <= 0x19 && ((1 << v7) & 0x2005040) != 0)
                {
                    bitStream.WritePositiveIntMax63(0);
                    bitStream.WritePositiveIntMax63(0);
                }
                else
                {
                    if ((v7 & 0xFFFFFFF8) != 16) return;
                    bitStream.WritePositiveIntMax3(0);
                    bitStream.WritePositiveIntMax63(0);
                }
            }
        }

        public override int GetObjectType()
        {
            return 4;
        }
    }
}
