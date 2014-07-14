using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Common.Interfaces
{
    public interface IAction
    {
        IEnumerable<BoundingBox> ApplicationZones { get; }
        void SetTargets(IEnumerable<IEntity> targets);
        void Start();
        bool Update(GameTime gameTime);
        void Cancel();
        bool IsComplete();
    }
}
