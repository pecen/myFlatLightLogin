using System;

namespace myFlatLightLogin.Dal
{
    public interface IDalManager : IDisposable
    {
        T GetProvider<T>() where T : class;
    }
}
