using System;
using System.ComponentModel;
using System.Reflection;

namespace myFlatLightLogin.Core.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets value stored in the Description attribute of the given enumerator value.
        /// </summary>
        /// <param name="value">Enumerator value.</param>
        /// <returns>Description stored in attribute.</returns>
        public static string GetDescription(this Enum value)
        {
            if (value != null)
            {
                DescriptionAttribute attr = GetAttribute<DescriptionAttribute>(value);
                if (attr != null)
                {
                    return attr.Description;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Generic method getting attribute object of the given type from enumerated value.
        /// </summary>
        /// <typeparam name="TAttributeType">Attribute type.</typeparam>
        /// <param name="enumValue">Enumerated value.</param>
        /// <returns>Attribute object.</returns>
        public static TAttributeType GetAttribute<TAttributeType>(this Enum enumValue) where TAttributeType : Attribute
        {
            return (TAttributeType)GetEnumAttribute(enumValue, typeof(TAttributeType));
        }

        /// <summary>
        /// Basic method getting attribute object of the given type from enumerator type and enumerated value.
        /// </summary>
        /// <param name="enumValue">Enumerator type.</param>
        /// <param name="attributeType">Enumerator value.</param>
        /// <returns>Attribute object.</returns>
        public static Attribute GetEnumAttribute(Enum enumValue, Type attributeType)
        {
            Attribute[] atts = GetEnumAttributes(enumValue, attributeType);
            if (atts == null || atts.Length == 0)
            {
                return null;
            }
            else
            {
                return atts[0];
            }
        }

        /// <summary>
        /// Basic method getting attribute objects of the given type from an enumerated value.
        /// </summary>
        /// <param name="enumValue">Enumerator value.</param>
        /// <param name="attributeType">Attribute type.</param>
        /// <returns>Attribute object.</returns>
        public static Attribute[] GetEnumAttributes(Enum enumValue, Type attributeType)
        {
            if (Enum.IsDefined(enumValue.GetType(), enumValue))
            {
                FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());
                return (Attribute[])field.GetCustomAttributes(attributeType, true);
            }
            else
            {
                return null;
            }
        }
    }
}
