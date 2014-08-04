using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IE11CompatibilityView
{
  class IECompatibilityViewMgr
  {
    // Estos 3 valores, hasta lo que he visto son fijos
    private static byte[] header = new byte[] { 0x41, 0x1F, 0x00, 0x00, 0x53, 0x08, 0xAD, 0xBA };
    private static byte[] delim_a = new byte[] { 0x01, 0x00, 0x00, 0x00 };
    private static byte[] delim_b = new byte[] { 0x0C, 0x00, 0x00, 0x00 };

    // Estos 2 valores son cambiantes, pero poniendo estos fijos no debe haber conflictos
    private static byte[] checksum = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
    private static byte[] filler = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

    static void Main(string[] args)
    {
      try
      {
        string Operation = args[0];
        string Domain = args[1];

        // A partir de Internet Explorer 11 los dominios que se ven en modo de compatibilidad se establecen en este registro binario
        string Registry = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\BrowserEmulation\ClearableListData\UserFilter";
        createSubKey(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\BrowserEmulation\ClearableListData");
        operate(Operation, Domain, Registry);
      }
      catch (Exception)
      {
        Console.WriteLine("Argument Error, usage: (instalar|desinstalar) Domain");
      }
    }

    private static void createSubKey(string subKey)
    {
      RegistryReaderWriter.CreateSubKey(subKey);
    }

    private static void operate(string Operation, string Domain, string Registry)
    {
      if ((Operation == "instalar" || Operation == "desinstalar") && Domain != "")
      {
        bool install = Operation == "instalar";

        byte[] userFilterCurrentEntries = null, regbinary = null;
        
        int counter = 0;
        bool includeOldValue = false;
        byte[] userFilter = (byte[])RegistryReaderWriter.ReadValue(Registry);
        byte[] entry = getEntry(Domain);

        if (userFilter != null)
        {
          userFilterCurrentEntries = userFilter.Skip(24).ToArray();
          if (install && entryIncludedIndex(Domain, userFilterCurrentEntries) != -1)
          {
            Console.WriteLine("Domain entry already included.");
            return;
          }

          counter = BitConverter.ToInt32(userFilter.Skip(8).Take(4).ToArray(), 0);
          includeOldValue = true;
        }

        if (install)
        {
          byte[] counterBytes = BitConverter.GetBytes(counter + 1);
          List<byte[]> regParts = new List<byte[]>() { header, counterBytes, checksum, delim_a, counterBytes };

          if (includeOldValue)
          {
            regParts.Add(userFilterCurrentEntries);
          }
          regParts.Add(entry);
          regbinary = joinArrays(regParts);

          RegistryReaderWriter.WriteValue(Registry, Encoding.Unicode.GetString(regbinary), false, Microsoft.Win32.RegistryValueKind.Binary, "UNICODE");
        }
        else
        {
          if (userFilter == null)
          {
            Console.WriteLine("Can't delete entry because there isn't any.");
            return;
          }

          int index = entryIncludedIndex(Domain, userFilterCurrentEntries);
          if (index != -1)
          {
            if (counter == 1)
            {
              RegistryReaderWriter.RemoveValue(Registry);
            }
            else
            {
              byte[] counterBytes = BitConverter.GetBytes(counter - 1), pre = null, post = null; ;
              List<byte[]> regParts = new List<byte[]>() { header, counterBytes, checksum, delim_a, counterBytes };

              index -= 16;
              pre = userFilterCurrentEntries.Take(index + 1).ToArray();
              if (pre.Length > 1)
              {
                pre = userFilterCurrentEntries.Take(index).ToArray();
                regParts.Add(pre);
              }

              index += 16 + 2 + 2 * Domain.Length;
              post = userFilterCurrentEntries.Skip(index).ToArray();

              if (post.Length > 1)
              {
                regParts.Add(post);
              }

              regbinary = joinArrays(regParts);
              RegistryReaderWriter.WriteValue(Registry, Encoding.Unicode.GetString(regbinary), false, Microsoft.Win32.RegistryValueKind.Binary, "UNICODE");
            }
          }
        }
      }
    }

    private static int entryIncludedIndex(string Domain, byte[] userFilterEntries)
    {
      byte[] entryLength = BitConverter.GetBytes(Convert.ToInt16(Domain.Length));
      byte[] entryData = Encoding.Unicode.GetBytes(Domain);
      return subArrayIndex(joinArrays(new List<byte[]>() { entryLength, entryData }), userFilterEntries);
    }

    private static byte[] joinArrays(List<byte[]> listOfArraysToJoin)
    {
      int size = 0;
      foreach (byte[] array in listOfArraysToJoin)
      {
        size += array.Length;
      }
      byte[] joint = new byte[size];
      int index = 0;
      foreach (byte[] array in listOfArraysToJoin)
      {
        array.CopyTo(joint, index);
        index += array.Length;
      }
      return joint;
    }

    private static byte[] getEntry(string Domain)
    {
      byte[] length = BitConverter.GetBytes(Convert.ToInt16(Domain.Length));
      byte[] data = Encoding.Unicode.GetBytes(Domain);
      return joinArrays(new List<byte[]>() { delim_b, filler, delim_a, length, data });
    }

    private static int subArrayIndex(byte[] subArray, byte[] array)
    {
      if (subArray.Length <= array.Length)
      {
        for (int i = 0; i <= (array.Length - subArray.Length); i++)
        {
          for (int j = 0; j < subArray.Length; j++)
          {
            if (array[i + j] != subArray[j])
            {
              break;
            }
            if (j == (subArray.Length - 1))
            {
              return i;
            }
          }
        }
      }
      return -1;
    }
  }
}
