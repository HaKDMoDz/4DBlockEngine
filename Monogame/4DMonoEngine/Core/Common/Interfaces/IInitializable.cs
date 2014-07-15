using System;
using System.Collections.Generic;

namespace _4DMonoEngine.Core.Common.Interfaces
{
    public interface IInitializable
    {
        IEnumerable<Type> Dependencies();
        bool IsInitialized();
        void Initialize();
    }
}
