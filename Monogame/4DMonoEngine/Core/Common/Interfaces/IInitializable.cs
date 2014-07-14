using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4DMonoEngine.Core.Common.Interfaces
{
    interface IInitializable
    {
        IEnumerable<Type> Dependencies();
        bool IsInitialized();
        void Initialize();
    }
}
