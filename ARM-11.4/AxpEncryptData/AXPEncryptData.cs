using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public class AXPEncryptData
{
    public static string CallEncryptDataJson(string json)
    {
        string keyString = GetAppSetting("key");
        string ivString = GetAppSetting("iv");

        byte[] Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(keyString));
        byte[] IV = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ivString)).Take(16).ToArray();
        var jsonData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);

        foreach (var item in jsonData)
        {
            if (item.ContainsKey("v") && item["v"] != "")
            {
                item["v"] = EncryptValue(item["v"], Key, IV);
            }
        }
        return JsonConvert.SerializeObject(jsonData, Newtonsoft.Json.Formatting.Indented);
    }
    public static string CallEncryptData(string plainText)
    {
        if (plainText == "")
            return plainText;
        string keyString = GetAppSetting("key");
        string ivString = GetAppSetting("iv");

        byte[] Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(keyString));
        byte[] IV = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ivString)).Take(16).ToArray();
        string _eVal = string.Empty;
        try
        {
            _eVal = EncryptValue(plainText, Key, IV);
        }
        catch (Exception ex) { }
        return _eVal;
    }
    private static string EncryptValue(string plainText, byte[] Key, byte[] IV)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
                sw.Close();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    public static string CallDecryptDataJson(string json)
    {
        string keyString = GetAppSetting("key");
        string ivString = GetAppSetting("iv");

        byte[] Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(keyString));
        byte[] IV = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ivString)).Take(16).ToArray();

        var jsonData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
        foreach (var item in jsonData)
        {
            if (item.ContainsKey("v"))
            {
                try
                {
                    item["v"] = DecryptValue(item["v"], Key, IV);
                }
                catch (Exception ex)
                {
                }
            }
        }
        return JsonConvert.SerializeObject(jsonData, Newtonsoft.Json.Formatting.Indented);
    }

    public static string CallDecryptData(string plainText)
    {
        string keyString = GetAppSetting("key");
        string ivString = GetAppSetting("iv");

        byte[] Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(keyString));
        byte[] IV = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ivString)).Take(16).ToArray();
        string _dVal = string.Empty;
        try
        {
            _dVal = DecryptValue(plainText, Key, IV);
        }
        catch (Exception ex) { }
        return _dVal;
    }
    private static string DecryptValue(string cipherText, byte[] Key, byte[] IV)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (MemoryStream ms = new MemoryStream(cipherBytes))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
    public static string GetAppSetting(string key)
    {
        if (key == "key")
            return "AgileEncryptTransactionData";
        else
            return "AgileEncryptAxpertTransactionData";
    }

    public static string RandomNumber(int min, int max)
    {
        Random random = new Random();
        return random.Next(min, max).ToString();
    }


    private static String EncryptData(String input)

    {
        return "Enc_" + input;
    }

    private static String DecryptData(String input)

    {
        return "Dec_" + input;
    }


    //[DllExport]
    public static string dummy(string msg)
    {
        //Call paytm generateCheckSumByJson to generate checksum
        try
        {
            if (msg == "true")
            {
                return "true";
            }
            else
            {
                return "false";
            }
        }
        catch
        {
            return "error";
        }
    }


    //[DllExport]
    public static string Echo(string msg)
    {
        try
        {

            return msg;
        }
        catch
        {
            return "error";
        }
    }

    //[DllExport]
    public static string Encrypt([MarshalAs(UnmanagedType.BStr)] string input)
    {
        try
        {
            string Enc_Str = CallEncryptData(input);
            return Enc_Str;
        }
        catch (Exception e)
        {
            return "Error : " + e.Message;
        }
    }

    //[DllExport]
    public static string Decrypt([MarshalAs(UnmanagedType.BStr)] string input)
    {
        try
        {
            string Dec_Str = CallDecryptData(input);
            return Dec_Str;
        }
        catch (Exception e)
        {
            return "Error : " + e.Message;
        }
    }

    public static DataTable CallDecryptDataTable(DataTable dataDt, List<string> encryptFields)
    {
        string keyString = GetAppSetting("key");
        string ivString = GetAppSetting("iv");

        byte[] Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(keyString));
        byte[] IV = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ivString)).Take(16).ToArray();
        try
        {
            DataTable decDT = dataDt;
            foreach (DataRow row in decDT.Rows)
            {
                foreach (var enFld in encryptFields)
                {
                    if (!decDT.Columns.Contains(enFld))
                        continue;
                    var encryptedValue = row[enFld]?.ToString();
                    if (string.IsNullOrEmpty(encryptedValue))
                        continue;
                    try
                    {
                        row[enFld] = DecryptValue(encryptedValue, Key, IV);
                    }
                    catch (Exception ex) { }
                }
            }
            return decDT;
        }
        catch (Exception e)
        {
            return dataDt;
        }
    }
}
