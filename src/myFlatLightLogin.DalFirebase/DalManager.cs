using myFlatLightLogin.Dal;
using System;

namespace myFlatLightLogin.DalFirebase
{
    /// <summary>
    /// Firebase DAL Manager - Provides Firebase-specific DAL implementations.
    /// </summary>
    public class DalManager : IDalManager
    {
        private static string _typeMask = typeof(DalManager).FullName.Replace("DalManager", @"{0}");

        /// <summary>
        /// Gets a provider instance for the specified DAL interface.
        /// Uses reflection to create the concrete implementation.
        /// </summary>
        /// <typeparam name="T">DAL interface type (e.g., IUserDal)</typeparam>
        /// <returns>Concrete DAL implementation</returns>
        public T GetProvider<T>() where T : class
        {
            // Convert interface name (e.g., "IUserDal") to implementation name (e.g., "UserDal")
            var typeName = string.Format(_typeMask, typeof(T).Name.Substring(1));
            var type = Type.GetType(typeName);

            if (type != null)
            {
                return Activator.CreateInstance(type) as T;
            }
            else
            {
                throw new NotImplementedException($"Firebase DAL implementation not found for {typeName}");
            }
        }

        public void Dispose()
        {
            // Cleanup resources if needed
        }
    }
}
