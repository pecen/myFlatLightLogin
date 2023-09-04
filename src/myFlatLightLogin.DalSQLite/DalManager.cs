using myFlatLightLogin.Dal;
using System;

namespace myFlatLightLogin.DalSQLite
{
    public class DalManager : IDalManager
    {
        private static string _typeMask = typeof(DalManager).FullName.Replace("DalManager", @"{0}");

        public T GetProvider<T>() where T : class
        {
            var typeName = string.Format(_typeMask, typeof(T).Name.Substring(1));
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return Activator.CreateInstance(type) as T;
            }
            else
            {
                throw new NotImplementedException(typeName);
            }
        }

        public void Dispose() { }
    }
}
