using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DiscordTimeLauncher
{
    class RegFinder
    {
        public static RegFindValue RegFind(RegistryKey key, string find)
        {
            if (key == null || string.IsNullOrEmpty(find))
                return null;

            string[] props = key.GetValueNames();
            object value = null;

            if (props.Length != 0)
                foreach (string property in props)
                {
                    if (property.IndexOf(find, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return new RegFindValue(key, property, null, RegFindIn.Property);
                    }

                    value = key.GetValue(property, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (value is string && ((string)value).IndexOf(find, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return new RegFindValue(key, property, (string)value, RegFindIn.Value);
                    }
                }

            string[] subkeys = key.GetSubKeyNames();
            RegFindValue retVal = null;

            if (subkeys.Length != 0)
            {
                foreach (string subkey in subkeys)
                {
                    try
                    {
                        retVal = RegFind(key.OpenSubKey(subkey, RegistryKeyPermissionCheck.ReadSubTree), find);
                    }
                    catch (Exception ex)
                    {
                        // err msg, if need
                    }
                    if (retVal != null)
                    {
                        return retVal;
                    }
                }
            }
            key.Close();
            return null;
        }

        public class RegFindValue
        {
            RegistryKey regKey;
            string mProps;
            string mVal;
            RegFindIn mWhereFound;

            public RegistryKey Key
            { get { return regKey; } }

            public string Property
            { get { return mProps; } }

            public string Value
            { get { return mVal; } }

            RegFindIn WhereFound
            { get { return mWhereFound; } }

            public RegFindValue(RegistryKey key, string props, string val, RegFindIn where)
            {
                regKey = key;
                mProps = props;
                mVal = val;
                mWhereFound = where;
            }
        }

        public enum RegFindIn
        {
            Property,
            Value
        }
    }
}
