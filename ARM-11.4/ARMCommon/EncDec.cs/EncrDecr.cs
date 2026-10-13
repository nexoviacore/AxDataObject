using System.Collections;
using System.Security.Cryptography;
using System.Text;
namespace AgileConnect.EncrDecr.cs
{
    public class EncrDecr
    {
        //private  readonly static string key = "asdfewrewqrss323";

        public static string Encrypt(string text, string key)
        {
            byte[] iv = new byte[16];
            byte[] array;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(text);

                        }
                        array = ms.ToArray();
                    }
                }

            }
            return Convert.ToBase64String(array);
        }

        public static string Decrypt(string text, string key)
        {

            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(text);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cryptoStream))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static string RefreshKey(int size)
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

            byte[] data = new byte[4 * size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }
        
        public static string AxpertEncryptString(string str)
        {
            string enstr = string.Empty;
            string insid = str;
            string dtid = GetTimeId();

            int i = dtid.Length;
            string s = dtid.Substring(0, dtid.Length - 4);

            insid = AxpertEncodeString(s, insid);
            enstr = insid + dtid;
            return enstr;
        }

        private static string GetTimeId()
        {
            string res = string.Empty;
            string s = string.Empty, s1 = string.Empty;
            int i;
            string dtime = "01020345060708";
            i = int.Parse(dtime.Substring(0, 2));
            s = (i + 31).ToString();
            s = s + (int.Parse(dtime.Substring(2, 2)) + i + 13);
            s = s + (int.Parse(dtime.Substring(4, 4)) * i);
            s = s + dtime.Substring(8, 2) + dtime.Substring(10, 2) + dtime.Substring(12, 2);
            i = s.Length;
            s1 = "00" + i;
            res = s + s1;
            return res;
        }

        private static  string AxpertEncodeString(string dtid, string dbid)
        {
            string Result = string.Empty;
            int len = dbid.Length;
            int len1 = dtid.Length;
            string s = string.Empty, s1 = string.Empty;
            if (len1 < len)
            {
                for (int i = len1; i < len; i++)
                {
                    dtid = dtid + '0';
                }
            }
            ArrayList arr = new ArrayList();
            for (int i = 0; i < len; i++)
            {
                arr.Add((Encoding.ASCII.GetBytes(dbid[i].ToString())[0] + dtid[i]));
            }

            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i].ToString().Length == 4)
                    s1 = s1 + arr[i];
                else if (arr[i].ToString().Length == 3)
                    s1 = s1 + ("0" + arr[i]);
                else if (arr[i].ToString().Length == 2)
                    s1 = s1 + ("00" + arr[i]);
            }
            int i1 = dbid.Length;
            if (i1.ToString().Length == 1)
                s = "000" + i1;
            else if (i1.ToString().Length == 2)
                s = "00" + i1;
            else if (i1.ToString().Length == 3)
                s = "0" + i1;
            else if (i1.ToString().Length == 4)
                s = i1.ToString();
            Result = s + s1;
            return Result;
        }

    }
}
