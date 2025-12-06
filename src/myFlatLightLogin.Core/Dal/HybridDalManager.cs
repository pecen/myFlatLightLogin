using myFlatLightLogin.Core.Infrastructure;
using myFlatLightLogin.Dal;
using System;

namespace myFlatLightLogin.Core.Dal
{
    /// <summary>
    /// DAL Manager for hybrid online/offline data access.
    /// Provides HybridUserDal instances through CSLA's DalFactory pattern.
    ///
    /// This manager integrates with CSLA's data portal by implementing IDalManager,
    /// allowing BLL classes to use hybrid DAL without knowing about infrastructure.
    /// </summary>
    public class HybridDalManager : IDalManager
    {
        /// <summary>
        /// Gets a provider instance for the specified DAL interface.
        /// Supports IUserDal (returns HybridUserDal with infrastructure dependencies resolved).
        /// </summary>
        public T GetProvider<T>() where T : class
        {
            var interfaceType = typeof(T);

            // Return HybridUserDal for IUserDal requests
            if (interfaceType == typeof(IUserDal))
            {
                // Resolve HybridUserDal from ServiceLocator (already configured with dependencies)
                return ServiceLocator.HybridUserDal as T;
            }

            // For IRoleDal, we could return a hybrid role DAL if needed
            // For now, fall back to Firebase or SQLite RoleDal
            if (interfaceType == typeof(IRoleDal))
            {
                // Use Firebase RoleDal for now (could be made hybrid in the future)
                return new DalFirebase.RoleDal() as T;
            }

            throw new NotImplementedException($"No provider registered for {interfaceType.Name}");
        }

        /// <summary>
        /// Disposes resources (none to dispose in this implementation).
        /// </summary>
        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
