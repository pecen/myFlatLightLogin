using System;
using System.Configuration;

namespace myFlatLightLogin.Dal
{
    public class DalFactory
    {
        private static readonly string _dalManager = "DalManager";
        private static Type? _dalType;

        public static IDalManager GetManager()
        {
            string? dalTypeName = ConfigurationManager.AppSettings[_dalManager];

            if (string.IsNullOrEmpty(dalTypeName))
                throw new NullReferenceException(_dalManager);

            if (_dalType == null || _dalType.FullName != dalTypeName.Split(',')[0])
            {
                _dalType = Type.GetType(dalTypeName);
                if (_dalType == null)
                    throw new ArgumentException(string.Format("Type {0} could not be found", dalTypeName));
            }

            object? instance = Activator.CreateInstance(_dalType);

            if (instance is not IDalManager dalManager)
                throw new InvalidCastException($"Instance of type {_dalType.FullName} could not be cast to IDalManager.");

            return dalManager;
        }
    }
}
