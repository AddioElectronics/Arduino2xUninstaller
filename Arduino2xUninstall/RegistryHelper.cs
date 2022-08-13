using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;


namespace Addio.Windows
{
    public static class RegistryHelper
    {
        /// <summary>
        /// Gets the base key hive from a path string.
        /// </summary>
        /// <param name="path">Registry path.</param>
        /// <param name="subPath">The path with the base key removed, or null if no subpath was included.</param>
        /// <returns>The <see cref="RegistryHive"/> the <paramref name="path"/> points to, or null if the <paramref name="path"/> is invalid.</returns>
        public static RegistryHive? GetBaseKeyHive(string path, out string subPath)
        {
            string[] names = path.Split('\\');

            int index = names[0] == "Computer" ? 1 : 0;

            int joinIndex = index + 1;

            if (index < names.Length)
                subPath = String.Join('\\', names, joinIndex, names.Length - joinIndex);
            else
                subPath = null;

            switch (names[index])
            {
                case Strings.classes_root:
                    return RegistryHive.ClassesRoot;
                case Strings.current_user:
                    return RegistryHive.CurrentUser;
                case Strings.local_machine:
                    return RegistryHive.LocalMachine;
                case Strings.users:
                    return RegistryHive.Users;
                case Strings.current_config:
                    return RegistryHive.CurrentConfig;
            }
            subPath = null;
            return null;
        }

        /// <summary>
        /// Get the base key from a path string.
        /// </summary>
        /// <param name="path">Registry path</param>
        /// <param name="view">The registry view.</param>
        /// <param name="subPath">The path with the base key removed, or null if no subpath was included.</param>
        /// <returns>The base registry key, CLASES_ROOT, CURRENT_USER, etc. or null if path does not contain "Computer" or a base key.</returns>
        public static RegistryKey GetBaseKey(string path, RegistryView view, out string subPath)
        {
            RegistryHive? hive = GetBaseKeyHive(path, out subPath);

            if (!hive.HasValue)
                return null;

            return RegistryKey.OpenBaseKey(hive.Value, view);
        }

        /// <summary>
        /// Open a <see cref="RegistryKey"/> from a full path containing the base key.
        /// </summary>
        /// <param name="path">Path to a registry key</param>
        /// <param name="writeable">Set true if you need access to write.</param>
        /// <param name="view">Specify which registry view to target on a 64-bit operating system.</param>
        /// <returns>The base or sub <see cref="RegistryKey"/> the <paramref name="path"/> points to, or null if it does not exist.</returns>
        public static RegistryKey OpenKey(string path, bool writeable = false, RegistryView view = RegistryView.Default)
        {
            string subpath;
            RegistryKey baseKey = GetBaseKey(path, view, out subpath);
            if (baseKey == null)
                return null;

            if (subpath == null)
                return baseKey;

            RegistryKey key = baseKey.OpenSubKey(subpath, writeable);
            baseKey.Dispose();
            return key;
        }

        /// <summary>
        /// Deletes a <see cref="RegistryKey"/> using a full path.
        /// </summary>
        /// <param name="path">Path to a registry key, with the string after the last '\' being the key name.</param>
        /// <returns>True if the key was deleted or does not exist, false if an exception happened in the process.</returns>
        public static bool DeleteKey(string path)
        {
            string[] split = path.Split('\\');
            string keyPath = String.Join('\\', split, 0, split.Length - 1);

            try
            {
                using (RegistryKey key = OpenKey(keyPath, true))
                {
                    if (key == null)
                        return true;

                    string keyName = split[split.Length - 1];

                    key.DeleteSubKeyTree(keyName);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deletes a value using a full path.
        /// </summary>
        /// <param name="path">Path to a registry value, with the string after the last '\' being the value name.</param>
        /// <returns>True if the value was deleted or does not exist, false if an exception happened in the process.</returns>
        public static bool DeleteValue(string path)
        {
            string[] split = path.Split('\\');
            string keyPath = String.Join('\\', split, 0, split.Length - 1);

            try
            {
                using (RegistryKey key = OpenKey(keyPath))
                {
                    if (key == null)
                        return true;

                    string valueName = split[split.Length - 1];

                    if (key.GetValue(valueName) != null)
                        key.DeleteValue(valueName);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// A class containing constants of heavily used strings.
        /// </summary>
        public static class Strings
        {
            public const char separator = '\\';
            public const string computer = "Computer";
            public const string classes_root = "HKEY_CLASSES_ROOT";
            public const string current_user = "HKEY_CURRENT_USER";
            public const string local_machine = "HKEY_LOCAL_MACHINE";
            public const string users = "HKEY_USERS";
            public const string current_config = "HKEY_CURRENT_CONFIG";

            public static readonly string[] root_strings = new string[]
            {
                classes_root,
                current_user,
                local_machine,
                users,
                current_config
            };
        }

    }
}
