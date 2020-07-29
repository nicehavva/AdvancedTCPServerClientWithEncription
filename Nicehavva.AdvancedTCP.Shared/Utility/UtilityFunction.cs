using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Nicehavva.AdvancedTCP.Shared.Enums;
using System.Security.Cryptography;

namespace Nicehavva.AdvancedTCP.Shared.Utility
{
    public class UtilityFunction
    {
        public UtilityFunction()
        {
        }
        public static byte[] EncryptByte(byte[] in_Byte, double First_X, double U, ChoasEnum choas_select)
        {
            try
            {
                Encryption t_encription = new Encryption(128, First_X, U, choas_select);
                byte[] t_Output = new byte[in_Byte.Length];
                int i = 0;
                byte[] file_value = new byte[128];
                int in_c = 0;
                int out_c = 0;
                while (in_c < in_Byte.Length)
                {
                    file_value[i] = in_Byte[in_c++];
                    i++;
                    if (i == 128)
                    {
                        t_encription.encrypt(file_value);
                        byte[] encrypt_data = t_encription.encrypt_information;
                        for (int i_f = 0; i_f < encrypt_data.Length; i_f++)
                        {
                            t_Output[out_c++] = encrypt_data[i_f];
                        }
                        i = 0;
                    }
                }
                if (i != 0)
                {
                    if (i % 2 == 0)
                    {
                        byte[] file_value2 = new byte[i];
                        for (int j = 0; j < i; j++)
                        {
                            file_value2[j] = file_value[j];
                        }
                        t_encription.encrypt(file_value2);
                        byte[] encrypt_data = t_encription.encrypt_information;
                        for (int i_f = 0; i_f < encrypt_data.Length; i_f++)
                        {
                            t_Output[out_c++] = encrypt_data[i_f];
                        }
                    }
                    else
                    {
                        byte[] file_value2 = new byte[i - 1];
                        for (int j = 0; j < i - 1; j++)
                        {
                            file_value2[j] = file_value[j];
                        }
                        if (i > 2)
                        {
                            t_encription.encrypt(file_value2);
                            byte[] encrypt_data = t_encription.encrypt_information;
                            for (int i_f = 0; i_f < encrypt_data.Length; i_f++)
                            {
                                t_Output[out_c++] = encrypt_data[i_f];
                            }
                        }
                        t_Output[out_c++] = file_value[i - 1];
                    }
                }
                return t_Output;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public static string EncryptString(string inputText, double First_X, double U, ChoasEnum choas_select)
        {
            try
            {
                UnicodeEncoding byteConverter = new UnicodeEncoding();
                byte[] dataToEncrypt = byteConverter.GetBytes(inputText);

                return Convert.ToBase64String(UtilityFunction.EncryptByte(dataToEncrypt, First_X, U, choas_select));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static MemoryStream EncryptStream(MemoryStream inputStream, double First_X, double U, ChoasEnum choas_select)
        {
            try
            {
                byte[] dataToEncrypt = inputStream.ToArray();
                var encryptData=UtilityFunction.EncryptByte(dataToEncrypt, First_X, U, choas_select);
                MemoryStream ms = new MemoryStream();
                ms.Write(encryptData, 0, encryptData.Length);
                return ms;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static string DecryptString(string inputText, double First_X, double U, ChoasEnum choas_select)
        {
            try
            {
                byte[] decryptedData = UtilityFunction.EncryptByte(Convert.FromBase64String(inputText), First_X, U, choas_select);
                UnicodeEncoding byteConverter = new UnicodeEncoding();
                return byteConverter.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static byte[] EncryptByte(string publicKey, byte[] data)
        {
            // Create a byte array to store the encrypted data in it   
            byte[] encryptedData;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                // Set the rsa pulic key   
                rsa.FromXmlString(publicKey);

                // Encrypt the data and store it in the encyptedData Array   
                encryptedData = rsa.Encrypt(data, false);
            }
            // Save the encypted data array into a file   
            return encryptedData;
        }
        
        public static byte[] DecryptByte(string privateKey, byte[] data)
        {
            try
            {
                // Create an array to store the decrypted data in it   
                byte[] decryptedData;
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    // Set the private key of the algorithm   
                    rsa.FromXmlString(privateKey);
                    decryptedData = rsa.Decrypt(data, false);
                }
                return decryptedData;
            }
            catch (Exception)
            {

                return null;
            }

        }
        
        public static string EncryptString(string publicKey, string text)
        {
            // Convert the text to an array of bytes   
            UnicodeEncoding byteConverter = new UnicodeEncoding();
            byte[] dataToEncrypt = byteConverter.GetBytes(text);

            // Create a byte array to store the encrypted data in it   
            byte[] encryptedData;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                // Set the rsa pulic key   
                rsa.FromXmlString(publicKey);

                // Encrypt the data and store it in the encyptedData Array   
                encryptedData = rsa.Encrypt(dataToEncrypt, false);
            }
            // Save the encypted data array into a file   
            return Convert.ToBase64String(encryptedData);
        }
        
        public static string DecryptString(string privateKey, string data)
        {
            // read the encrypted bytes from the file   
            byte[] dataToDecrypt = Convert.FromBase64String(data);

            // Create an array to store the decrypted data in it   
            byte[] decryptedData;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                // Set the private key of the algorithm   
                rsa.FromXmlString(privateKey);
                decryptedData = rsa.Decrypt(dataToDecrypt, false);
            }

            // Get the string value from the decryptedData byte array   
            UnicodeEncoding byteConverter = new UnicodeEncoding();
            return byteConverter.GetString(decryptedData);
        }
        
        public static byte[] StringToBytes(string in_string)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                byte[] t_bytes;
                MemoryStream ms = new MemoryStream();

                bf.Serialize(ms, in_string);
                ms.Seek(0, 0);
                t_bytes = ms.ToArray();
                return t_bytes;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public static string StringFromBytes(byte[] in_array)
        {
            try
            {
                BinaryFormatter bfx = new BinaryFormatter();
                MemoryStream msx = new MemoryStream();
                msx.Write(in_array, 0, in_array.Length);
                msx.Seek(0, 0);
                string sx = (string)bfx.Deserialize(msx);
                return sx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
