using System;
using System.Linq;
using System.Text;

using Microsoft.Win32;

namespace IE11CompatibilityView
{
  public class RegistryReaderWriter
  {
    public static RegistryKey Read(string key, bool discardLastLevel, bool writable = false)
    {
      RegistryKey hive = GetBase(key, GetBitBase());
      string subkey = GetNextLevel(key);
      if (discardLastLevel)
      {
        subkey = GetKey(subkey);
      }
      return hive.OpenSubKey(subkey, writable);
    }

    public static object ReadValue(string key)
    {
      RegistryKey subKey = Read(key, true);
      string value = GetLastLevel(key);
      object valueContent = null;
      if (subKey != null)
      {
        valueContent = subKey.GetValue(value, null);
        subKey.Close();
      }
      return valueContent; 
    }

    public static bool WriteValue(string key, string content, bool append, RegistryValueKind valueType, string encoding)
    {
      try
      {
        object newContent;
        switch(valueType)
        {
          case RegistryValueKind.Binary:
            newContent = Encoding.GetEncoding(encoding).GetBytes(content);
            if (append)
            {
              newContent = Encoding.GetEncoding(encoding).GetBytes(ReadValue(key) + content);
            }
            break;
          default:
            newContent = content;
            if (append)
            {
              newContent = ReadValue(key) + content;
            }
          break;
        }

        RegistryKey subKey = Read(key, true, true);
        string value = GetLastLevel(key);
        subKey.SetValue(value, newContent, valueType);
        subKey.Close();
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public static bool RemoveValue(string key)
    {
      try
      {
        RegistryKey subKey = Read(key, true, true);
        subKey.DeleteValue(GetLastLevel(key));
        subKey.Close();
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public static bool RemoveSubKey(string key)
    {
      try
      {
        RegistryKey subKey = Read(key, true, true);
        if (subKey.GetSubKeyNames().Length != 0)
        {
          subKey.DeleteSubKeyTree(GetLastLevel(key));
          subKey.Close();
          return true;
        }
        return false;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public static bool CreateSubKey(string key)
    {
      RegistryKey subKey = Read(key, true, true);
      string subKeyName = GetLastLevel(key);
      if (subKey.OpenSubKey(subKeyName) != null)
      {
        subKey.Close();
        return false;
      }
      else
      {
        subKey.CreateSubKey(subKeyName);
        subKey.Close();
        return true;
      }
    }

    public static bool ReadSubKey(string key)
    {
      RegistryKey subKey = Read(key, true, true);
      if (subKey != null)
      {
        subKey.Close();
        return true;
      }
      return false;
    }

    private static RegistryKey GetBase(string key, RegistryView view)
    {
      RegistryHive hive;
      switch (GetTopLevel(key))
      { 
        case "HKEY_CURRENT_USER":
          hive = RegistryHive.CurrentUser;
          break;
        case "HKEY_CLASSES_ROOT":
          hive = RegistryHive.ClassesRoot;
          break;
        case "HKEY_CURRENT_CONFIG":
          hive = RegistryHive.CurrentConfig;
          break;
        case "HKEY_DYN_DATA":
          hive = RegistryHive.DynData;
          break;
        case "HKEY_PERFORMANCE_DATA":
          hive = RegistryHive.PerformanceData;
          break;
        case "HKEY_USERS":
          hive = RegistryHive.Users;
          break;
        default:
          hive = RegistryHive.LocalMachine;
          break;
      }
      return RegistryKey.OpenBaseKey(hive, view);
    }

    private static RegistryView GetBitBase()
    {
      if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
      {
        return RegistryView.Registry64;
      }
      return RegistryView.Default;
    }

    private static string GetNextLevel(string key)
    {
      string formattedKey = key.Replace('/', '\\');
      return formattedKey.Substring(formattedKey.IndexOf('\\') + 1);
    }

    private static string GetTopLevel(string key)
    {
      string formattedKey = key.Replace('/', '\\');
      return formattedKey.Substring(0, formattedKey.IndexOf('\\'));
    }

    private static string GetKey(string value)
    {
      string formattedValue = value.Replace('/', '\\');
      int limit = formattedValue.LastIndexOf('\\');
      if (limit == -1)
      {
        limit = 0;
      }
      return formattedValue.Substring(0, limit);
    }

    private static string GetLastLevel(string key)
    {
      string formattedKey = key.Replace('/', '\\');
      return formattedKey.Substring(formattedKey.LastIndexOf('\\') + 1);
    }
  }
}
