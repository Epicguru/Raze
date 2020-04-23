using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Raze.World;

namespace Raze.Entities.Instances
{
    /// <summary>
    /// An entity that can move around by hopping, and will not move into the same place as another MovingEntity.
    /// </summary>
    public class MovingEntity : BasicEntity
    {
        public Point3D WorldPosition
        {
            get;
            private set;
        }
        public float LerpTime { get; set; }
        public float LerpHopHeight { get; set; }

        private float lerpTimer;
        private Vector3 lerpStart;
        private Vector3 realPos;

        public override Vector3 Position
        {
            get
            {
                return realPos;
            }
            set
            {
                Debug.Warn("Should not move a MovingEntity using Position: Use TeleportTo and MoveTo instead.");
            }
        }

        protected MovingEntity(EntityDef def) : base(def)
        {

        }

        public override void ApplyDef(EntityDef def)
        {
            var d = def as MovingEntityDef;
            LerpTime = d.LerpTime;
            lerpTimer = LerpTime + 1f;
            LerpHopHeight = d.LerpHopHeight;

            base.ApplyDef(def);
        }

        public void TeleportTo(Point3D position)
        {
            if (position == this.WorldPosition)
                return;
            if (!Map.IsPointInRange(position))
                return;

            lerpTimer = LerpTime + 1f;
            lerpStart = this.Position;
            WorldPosition = position;
        }

        public void MoveTo(Point3D position)
        {
            if (position == this.WorldPosition)
                return;
            if (!Map.IsPointInRange(position))
                return;

            Point3D relative = position - this.WorldPosition;

            CardinalDirection dir = this.Direction;
            if (relative.X > 0)
                dir = CardinalDirection.West;
            if (relative.X < 0)
                dir = CardinalDirection.East;
            if (relative.Y > 0)
                dir = CardinalDirection.South;
            if (relative.Y < 0)
                dir = CardinalDirection.North;
            this.Direction = dir;

            lerpTimer = 0f;
            lerpStart = this.Position;
            WorldPosition = position;
        }

        protected override void Update()
        {
            if (Input.IsKeyJustDown(Keys.J))
                MoveTo(WorldPosition + new Point3D(Rand.Range(-1, 2), Rand.Range(-1, 2), 0));

            if(lerpTimer < LerpTime)
                lerpTimer += Time.deltaTime;
            float t = MathHelper.Clamp(lerpTimer / LerpTime, 0f, 1f);
            realPos = Vector3.Lerp(lerpStart, WorldPosition, t);
            realPos += new Vector3(0f, 0f, LerpHopHeight * MathF.Sin(MathF.PI * 0.5f * (t > 0.5f ? 1f - t : t * 2)));

            base.Update();
        }
    }
}
