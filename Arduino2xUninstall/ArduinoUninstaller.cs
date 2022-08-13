using Microsoft.Win32;
using System.Resources;
using Arduino2xUninstaller.Properties;
using System.Diagnostics;
using Addio.Windows;

namespace Arduino2xUninstaller
{

    [Flags]
    public enum ErrorCodes
    {
        None = 0,
        /// <summary>
        /// Arduino IDE must be closed.
        /// </summary>
        ArduinoOpen = 1,
        /// <summary>
        /// Arduino IDE is installed, but the GUID could not be found in the registry.
        /// </summary>
        GuidNotFound = 2,
        /// <summary>
        /// The install path could not be found.
        /// </summary>
        PathNotFound = 4,
        /// <summary>
        /// The install paths could not be deleted.
        /// </summary>
        FilesNotDeleted = 8,
        /// <summary>
        /// Atleast 1 registry key could not be deleted.
        /// </summary>
        CantDeleteKey = 16
    }


    public static class ArduinoUninstaller
    {

        public delegate void ProgressHandler(int progress, string message);
        public static event ProgressHandler ProgressUpdate;


        /// <summary>
        /// List of GUID's installs are known to have.
        /// </summary>
        public static List<Guid> knownGUIDs = new List<Guid>()
        {
            new Guid("459fc68c-eb53-59f8-8957-9913bc627af3"),
            new Guid("80a6dee0-faee-5422-9648-6b7ee9b05f5a")
        };

        /// <summary>
        /// GUID for the "All Users" installation.
        /// </summary>
        /// <remarks><see cref="Guid.Empty"/> if not installed for all users.</remarks>
        public static Guid allUsersGuid;

        /// <summary>
        /// GUID for the "Current User" installation.
        /// </summary>
        /// <remarks><see cref="Guid.Empty"/> if not installed for current user.</remarks>
        public static Guid currentUserGuid;

        static ArduinoUninstaller()
        {
            string[] guids = Resources.KnownGUIDs.Split(Environment.NewLine);

            foreach(string guid in guids)
            {
                knownGUIDs.Add(new Guid(guid));
            }

            ConfirmGuids();

        }

        /// <summary>
        /// Is the Arduino IDE open?
        /// </summary>
        /// <returns></returns>
        public static bool IsArduinoIdeOpen()
        {
            string[] processes = Process.GetProcesses().Select(x => x.ProcessName).ToArray();
            return processes.Any(x => x == "Arduino IDE");
            //Process[] processes = Process.GetProcessesByName("Arduino IDE");
            //return processes.Length > 0;
        }

        /// <summary>
        /// Close all instances of the Arduino IDE.
        /// </summary>
        /// <returns>True if all processes were killed, false if there are still Open Arduino IDE processes.</returns>
        public static bool CloseArduinoIde()
        {
            if (!IsArduinoIdeOpen())
                return true;

            Process[] processes = Process.GetProcessesByName("Arduino IDE");

            int errors = 0;

            foreach(Process process in processes)
            {
                try
                {
                    process.CloseMainWindow();
                    process.Kill();
                    process.Dispose();
                }
                catch { errors += 1; }
            }

            return !IsArduinoIdeOpen();
        }

        /// <summary>
        /// Is Arduino IDE installed on the system?
        /// </summary>
        /// <returns>True if an installation was found, false if it was not.</returns>
        public static bool IsInstalled()
        {
            return Strings.CurrentUserInstallLocation != null || Strings.AllUsersInstallLocation != null || GuidsFound();
        }

        /// <summary>
        /// Was a GUID able to be confirmed?
        /// </summary>
        /// <returns>True if atleast 1 GUID was confirmed, false if neither were.</returns>
        /// <remarks>If neither were found, that could be that Ardunio IDE is not installed.</remarks>
        public static bool GuidsFound()
        {
            return allUsersGuid != Guid.Empty || currentUserGuid != Guid.Empty;
        }

        /// <summary>
        /// Uninstall Arduino IDE from the system.
        /// Deletes registry keys, and files for both the Current User and the Local Machine.
        /// </summary>
        /// <returns>Error flags</returns>
        public static ErrorCodes Uninstall()
        {
            ErrorCodes errorCodes = ErrorCodes.None;

            //Must call before removing RegistryKeys,
            //install paths are retrieved with Property.
            if (!IsInstalled())
                return 0;

            if (IsArduinoIdeOpen())
                return ErrorCodes.ArduinoOpen;

            if (!GuidsFound())
                return ErrorCodes.GuidNotFound;

            if (DeleteRegistryKeys() > 0)
                errorCodes |= ErrorCodes.CantDeleteKey;

            errorCodes |= DeleteFiles();

            ProgressUpdate?.Invoke(100, errorCodes == ErrorCodes.None ? "Uninstall Complete!" : String.Format("Uninstall finished with errors! 0x{0}", ((int)errorCodes).ToString("X8")));

            return 0;
        }

        private static void ConfirmGuids()
        {
            //Attempt to confirm GUIDs using known GUIDs.
            RegistryKey machineSoftwareKey = AttemptOpenKey(Registry.LocalMachine, "SOFTWARE\\{0}", knownGUIDs, out allUsersGuid);
            RegistryKey userSoftwareKey = AttemptOpenKey(Registry.CurrentUser, "SOFTWARE\\{0}", knownGUIDs, out currentUserGuid);

            if(machineSoftwareKey != null && userSoftwareKey != null)
            {
                //There is a confirmed installation on both the current user and local machine.
                machineSoftwareKey.Close();
                userSoftwareKey.Close();
                return;
            }

            //All users has not been confirmed, search for a registry key in LocalMachine.
            if (allUsersGuid != Guid.Empty)
                using (machineSoftwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE"))
                {
                    if (machineSoftwareKey != null)
                    {
                        Guid guid = Search(machineSoftwareKey);

                        if (guid != null)
                            allUsersGuid = guid;
                    }
                }

            //Current user has not been confirmed, search for a registry key in CurrentUser.
            if (currentUserGuid != Guid.Empty)
                using (userSoftwareKey = Registry.CurrentUser.OpenSubKey("SOFTWARE"))
                {
                    if (userSoftwareKey != null)
                    {
                        Guid guid = Search(userSoftwareKey);

                        if (guid != null)
                            currentUserGuid = guid;
                    }
                }

            //Enumerates through a registry key's children,
            //and checks all keys with a GUID for a name,
            //to see if the key is for the Arduino IDE
            Guid Search(RegistryKey root)
            {
                string[] childKeys = root.GetSubKeyNames();

                foreach (string key in childKeys)
                {
                    Guid guid;

                    try
                    {
                        guid = new Guid(key);
                    }
                    catch
                    {
                        //Key is not a GUID, try next.
                        continue;
                    }

                    //Key is a GUID, check if its for Arduino IDE
                    using (RegistryKey subKey = root.OpenSubKey(key))
                    {
                        object value = subKey.GetValue("ShortcutName");

                        if (value == null)
                            continue;

                        if ((string)value == "Arduino IDE")
                            return guid;
                    }
                }

                return Guid.Empty;
            }
        }

        /// <summary>
        /// Deletes all registry keys.
        /// </summary>
        /// <returns>A counter for how many keys could not be deleted.</returns>
        private static int DeleteRegistryKeys()
        {
            ProgressUpdate?.Invoke(1, "Removing Registry Keys");

            int errors = 0;

            foreach(string format in Strings.registryKeyFormats)
            {
               DeleteRegistryKey(format);
            }

            ProgressUpdate?.Invoke(25, "Removing Stray Registry Values");

            foreach (string format in Strings.registryValueFormats)
            {
                DeleteRegistryValue(format);
            }

            //if (allUsersGuid != Guid.Empty)
            //{
            //    ProgressUpdate?.Invoke(1, "Removing Registry Keys for All Users.");

            //    foreach(string format in Strings.localMachineKeyFormats)
            //    {
            //        string name = String.Format(format, allUsersGuid.ToString("D"));

            //        try
            //        {
            //            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(name))
            //            {
            //                if (key != null)
            //                    Registry.LocalMachine.DeleteSubKey(name);
            //            }
            //        }
            //        catch { errors++; }
            //    }
            //}

            //if(currentUserGuid != Guid.Empty)
            //{
            //    ProgressUpdate?.Invoke(25, "Removing Registry Keys for Current User.");

            //    foreach (string format in Strings.currentUserKeyFormats)
            //    {
            //        string name = String.Format(format, currentUserGuid.ToString("D"));

            //        try
            //        {
            //            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(name))
            //            {
            //                if (key != null)
            //                    Registry.CurrentUser.DeleteSubKey(name);
            //            }
            //        }
            //        catch { errors++; }
            //    }
            //}

            //ProgressUpdate?.Invoke(40, "Removing shared Registry Keys.");

            //foreach (string format in Strings.classesRootFormats)
            //{
            //    string name = String.Format(format, allUsersGuid.ToString("D"));
            //    try
            //    {
            //        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(name))
            //        {
            //            if (key != null)
            //                Registry.LocalMachine.DeleteSubKey(name);
            //        }
            //    }
            //    catch { errors++; }
            //}

            //foreach (string format in Strings.usersKeyFormats)
            //{
            //    string owner = System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString().Trim('{', '}');

            //    string current = String.Format(format, allUsersGuid.ToString("D;"), owner);
            //    try
            //    {
            //        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(current))
            //        {
            //            if (key != null)
            //                Registry.LocalMachine.DeleteSubKey(current);
            //        }
            //    }
            //    catch { errors++; }

            //    //Try both GUIDs as an older install may use a different GUID.
            //    string all = String.Format(format, allUsersGuid.ToString("D"), owner);
            //    try
            //    {
            //        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(all))
            //        {
            //            if (key != null)
            //                Registry.LocalMachine.DeleteSubKey(all);
            //        }
            //    }
            //    catch { errors++; }

            //}

            return 0;
        }

        private static ErrorCodes DeleteFiles()
        {
            string allUsersPath = Strings.AllUsersInstallLocation;
            string currentUserPath = Strings.CurrentUserInstallLocation;

            if (allUsersPath == null && currentUserPath == null)
                return ErrorCodes.None;//ErrorCodes.PathNotFound;

            if (allUsersGuid != Guid.Empty)
            {
                ProgressUpdate?.Invoke(50, "Deleting files for All Users.");

                if (allUsersPath != null)
                    Directory.Delete(allUsersPath, true);

            }

            if (currentUserGuid != Guid.Empty)
            {
                ProgressUpdate?.Invoke(75, "Deleting files for Current User.");

                if (currentUserPath != null)
                    Directory.Delete(currentUserPath, true);
            }

            if (Directory.Exists(currentUserPath) || Directory.Exists(allUsersPath))
                return ErrorCodes.FilesNotDeleted;


            return 0;
        }




        private static RegistryKey AttemptOpenKey(RegistryKey baseKey, string format, List<Guid> guids, out Guid working)
        {
            foreach(Guid guid in guids)
            {
                RegistryKey key = baseKey.OpenSubKey(String.Format(format, guid.ToString("D")));

                if (key != null)
                {
                    working = guid;
                    return key;
                }
            }

            working = Guid.Empty;
            return null;
        }

        private static Guid AttemptGuidSearch()
        {
            Guid Search(RegistryKey root)
            {
                string[] childKeys = root.GetSubKeyNames();

                foreach(string key in childKeys)
                {
                    Guid guid;

                    try
                    {
                        guid = new Guid(key);
                    }
                    catch
                    {
                        //Key is not a GUID, try next.
                        continue;
                    }

                    //Key is a GUID, check if its for Arduino IDE
                    using(RegistryKey subKey = root.OpenSubKey(key))
                    {
                        object value = subKey.GetValue("ShortcutName");

                        if (value == null)
                            continue;

                        if ((string)value == "Arduino IDE")
                            return guid;
                    }
                }

                return Guid.Empty;
            }

            using (RegistryKey machineSoftwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE"))
            {
                if (machineSoftwareKey != null)
                {
                    Guid guid = Search(machineSoftwareKey);

                    if (guid != null)
                        return guid;
                }
            }

            using (RegistryKey userSoftwareKey = Registry.CurrentUser.OpenSubKey("SOFTWARE"))
            {
                if (userSoftwareKey != null)
                {
                    Guid guid = Search(userSoftwareKey);

                    if (guid != null)
                        return guid;
                }
            }

            return Guid.Empty;
        }

        private static RegistryHive? GetBaseKeyHive(string path)
        {
            string[] names = path.Split('\\');

            int index = names[0] == "Computer" ? 1 : 0;

            switch (names[index])
            {
                case Strings.BaseKeys.classes_root:
                    return RegistryHive.ClassesRoot;
                case Strings.BaseKeys.current_user:
                    return RegistryHive.CurrentUser;
                case Strings.BaseKeys.local_machine:
                    return RegistryHive.LocalMachine;
                case Strings.BaseKeys.users:
                    return RegistryHive.Users;
                case Strings.BaseKeys.current_config:
                    return RegistryHive.CurrentConfig;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if the key was deleted, or it did not exist, and false if the key was unable to be deleted.</returns>
        private static bool DeleteRegistryKey(string format)
        {
            //Use both GUIDs for the event an old install did not have its GUID removed.
            string allUsers = allUsersGuid != Guid.Empty ? String.Format(format, allUsersGuid.ToString("D"), Strings.OwnerString, Strings.AllUsersInstallLocation, Strings.CurrentUserInstallLocation) : null;
            string currentUser = currentUserGuid != Guid.Empty ? String.Format(format, currentUserGuid.ToString("D"), Strings.OwnerString, Strings.AllUsersInstallLocation, Strings.CurrentUserInstallLocation) : null;

            bool failed = false;

            if (allUsers != null)
                if(!RegistryHelper.DeleteKey(allUsers))
                    failed = true;

            if (currentUser != null)
                if(!RegistryHelper.DeleteKey(currentUser))
                    failed = true;

            return  !failed;

            //RegistryHive? hive = RegistryHelper.GetBaseKeyHive(format, out _);

            //if (!hive.HasValue)
            //    return false;

            //Guid guid;

            //switch(hive)
            //{
            //    case RegistryHive.LocalMachine:
            //        guid = allUsersGuid;
            //        break;
            //    case RegistryHive.CurrentUser:
            //        guid = currentUserGuid;
            //        break;
            //    default:
            //        if (allUsersGuid != Guid.Empty)
            //            guid = allUsersGuid;
            //        else
            //            guid = currentUserGuid;
            //        break;

            //}

            //string name = String.Format(format, guid.ToString("D;"), Strings.OwnerString, Strings.AllUsersInstallLocation, Strings.CurrentUserInstallLocation);
            //RegistryHelper.DeleteKey(name);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if the key was deleted, or it did not exist, and false if the key was unable to be deleted.</returns>
        private static bool DeleteRegistryValue(string format)
        {
            //Use both GUIDs for the event an old install did not have its GUID removed.
            string allUsers = allUsersGuid != Guid.Empty ? String.Format(format, allUsersGuid.ToString("D"), Strings.OwnerString, Strings.AllUsersInstallLocation, Strings.CurrentUserInstallLocation) : null;
            string currentUser = currentUserGuid != Guid.Empty ? String.Format(format, currentUserGuid.ToString("D"), Strings.OwnerString, Strings.AllUsersInstallLocation, Strings.CurrentUserInstallLocation) : null;

            bool failed = false;

            if (allUsers != null)
                if (!RegistryHelper.DeleteValue(allUsers))
                    failed = true;

            if (currentUser != null)
                if (!RegistryHelper.DeleteValue(currentUser))
                    failed = true;

            return !failed;
        }

        internal static class Strings
        {

            internal static string OwnerString { get; } = System.Security.Principal.WindowsIdentity.GetCurrent().Owner.ToString().Trim('{', '}');

          

            /// <summary>
            /// Formats used to generate the registry key names for removal.
            /// </summary>
            /// <remarks>{0} = GUID", {1} = <see cref="System.Security.Principal.WindowsIdentity.GetCurrent()"/>.Owner, {2} = All Users install path, {3} = Current User install path</remarks>
            internal static readonly string[] registryKeyFormats =
            {
                @"HKEY_CURRENT_USER\SOFTWARE\{0}",
                @"HKEY_CURRENT_USER\SOFTWARE\Classes\ino",
                @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\{0}",
                @"HKEY_LOCAL_MACHINE\Classes\ino",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}",
                @"HKEY_USERS\{1}\SOFTWARE\Classes\ino",
                @"HKEY_USERS\{1}\SOFTWARE\{0}",
                @"HKEY_USERS\{1}\SOFTWARE\Classes\ino",
                @"HKEY_USERS\{1}\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}",
                @"HKEY_USERS\{1}_Classes\ino",
                @"HKEY_CLASSES_ROOT\ino",
            };

            /// <summary>
            /// Format used to generate the key name which holds the install path for the local machine.
            /// </summary>
            private const string allusersInstallLocationFormat = @"SOFTWARE\{0}";

            /// <summary>
            /// Format used to generate the key name which holds the install path for the current user.
            /// </summary>
            private const string currentUserInstallLocationFormat = @"{1}\SOFTWARE\{0}";


            /// <summary>
            /// Format strings used to generate paths to stray registry values for deletion.
            /// </summary>
            /// <remarks>{0} = GUID", {1} = <see cref="System.Security.Principal.WindowsIdentity.GetCurrent()"/>.Owner, {2} = All Users install path, {3} = Current User install path</remarks>
            internal static readonly string[] registryValueFormats =
            {
                //C:\Users\Addio\AppData\Local\Programs\Arduino IDE\Arduino IDE.exe
                @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store\{2}",
                @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store\{3}"
            };

            ///// <summary>
            ///// Format strings used to generate RegistryKeys for HKEY_CURRENT_USER.
            ///// </summary>
            ///// <remarks>{0} = GUID"/></remarks>
            //internal static string[] currentUserKeyFormats =
            //{
            //    @"SOFTWARE\{0}",
            //    @"SOFTWARE\Classes\ino",
            //    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}"
            //};



            //internal static string[] localMachineKeyFormats =
            //{
            //    @"SOFTWARE\{0}",
            //    @"Classes\ino",
            //    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}",
            //};

            //internal static string[] classesRootFormats =
            //{
            //    @"ino"
            //};

            ///// <summary>
            ///// 
            ///// </summary>
            ///// <remarks>{0} = GUID, {1} = <see cref="System.Security.Principal.WindowsIdentity.GetCurrent()"/>.Owner</remarks>
            //internal static string[] usersKeyFormats =
            //{
            //    @"{1}\SOFTWARE\{0}",
            //    @"{1}\SOFTWARE\Classes\ino",
            //    @"{1}\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}",
            //    @"{1}_Classes\ino",
            //};

            /// <inheritdoc cref="CurrentUserInstallLocation"/>
            private static string _currentUserInstallLocation;

            /// <summary>
            /// Install location for the current user.
            /// </summary>
            internal static string CurrentUserInstallLocation
            {
                get
                {
                    if (_currentUserInstallLocation != null)
                        return _currentUserInstallLocation;

                    var current = System.Security.Principal.WindowsIdentity.GetCurrent();
                    //string user = current.Name;

                    //Check if installed to default location.
                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string path = Path.Combine(local, "Programs\\Arduino IDE");
                    //string path = Path.Combine(String.Format(@"C:\Users\{0}\AppData\Local\Programs\Arduino IDE", user));

                    if (Directory.Exists(path))
                        return _currentUserInstallLocation = path;

                    //Installed to custom location, get path from registry.
                    string keyname = String.Format(currentUserInstallLocationFormat, currentUserGuid.ToString("D"), Strings.OwnerString);
                    using (RegistryKey key = Registry.Users.OpenSubKey(keyname))
                    {
                        if (key == null)
                            return null;

                        object pathObj = key.GetValue("InstallLocation");

                        if (pathObj != null)
                            return _currentUserInstallLocation = pathObj as string;
                    }

                    return null;
                }
            }

            /// <inheritdoc cref="AllUsersInstallLocation"/>
            private static string _allUsersInstallLocation;

            /// <summary>
            /// Install location for all users.
            /// </summary>
            internal static string AllUsersInstallLocation
            {
                get
                {
                    if (_allUsersInstallLocation != null)
                        return _allUsersInstallLocation;

                    //Check if installed to default location.
                    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string path = Path.Combine(programFiles, "Arduino IDE");

                    if (Directory.Exists(path))
                        return _allUsersInstallLocation = path;

                    //Installed to custom location, get path from registry.
                    string keyname = String.Format(allusersInstallLocationFormat, currentUserGuid.ToString("D"), Strings.OwnerString);
                    using (RegistryKey key = Registry.Users.OpenSubKey(keyname))
                    {
                        if (key == null)
                            return null;

                        object pathObj = key.GetValue("InstallLocation");

                        if (pathObj != null)
                            return _allUsersInstallLocation = pathObj as string;
                    }

                    return null;
                }
            }

            internal static class BaseKeys
            {
                public const string computer = "Computer";
                public const string classes_root = "HKEY_CLASSES_ROOT";
                public const string current_user = "HKEY_CURRENT_USER";
                public const string local_machine = "HKEY_LOCAL_MACHINE";
                public const string users = "HKEY_USERS";
                public const string current_config = "HKEY_CURRENT_CONFIG";
            }
        }

    }
}