using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Queries
{
    public class GetDesiredVelocityQuery : AbstractQuery<Vector2>
    {
        protected override Vector2 OnDo()
        {
            var model = this.GetModel<IPlayerCharacter2DModel>();
            return model.Velocity.Value;
        }
    }
}
