﻿#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;


namespace Devarc
{
    public static class EncryptUtil
    {
        public static int GetCRC(int _value)
        {
            byte[] data = BitConverter.GetBytes(_value);
            int value = 0;
            for (int i = 0; i < data.Length; i++)
            {
                value += (i + 3) * data[i];
            }
            return value;
        }

        public static int GetCRC(float _value)
        {
            byte[] data = BitConverter.GetBytes(_value);
            int value = 0;
            for (int i = 0; i < data.Length; i++)
            {
                value = (i + 2) * data[i];
            }
            return value;
        }

#if UNITY_EDITOR
        public static int GetCRC(SerializedProperty property)
        {
            if (property == null)
            {
                return 0;
            }
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    return GetCRC(property.intValue);
                case SerializedPropertyType.Float:
                    return GetCRC(property.floatValue);
                case SerializedPropertyType.String:
                    return GetCRC(property.stringValue);
                case SerializedPropertyType.ObjectReference:
                    return GetCRC(property.objectReferenceValue.name);
                default:
                    return 0;
            }
        }
#endif

        public static int GetCRC(string _value)
        {
            byte[] data = Encoding.UTF8.GetBytes(_value);
            int value = 0;
            for (int i = 0; i < data.Length; i++)
            {
                value = (i + 1) * data[i];
            }
            return value;
        }

        public static int GetCRC(byte[] _value, int _length)
        {
            int crc = 0;
            int lenDiv4 = _length / 4;
            for (int i = 0; i < lenDiv4; i++)
            {
                crc += 2 * (int)_value[4 * i + 0];
                crc += 3 * (int)_value[4 * i + 1];
                crc += 4 * (int)_value[4 * i + 2];
                crc += 5 * (int)_value[4 * i + 3];
            }
            for (int i = 4 * lenDiv4; i < _length; i++)
            {
                crc += (int)_value[i];
            }
            return crc;
        }


        public static bool EncryptFile(string _readFilePath, string _writeFilePath)
        {
            if (File.Exists(_readFilePath) == false)
            {
                Debug.LogError("Cannot find file: " + _readFilePath);
                return false;
            }

            try
            {
                FileStream rfs = File.OpenRead(_readFilePath);
                FileStream wfs = File.OpenWrite(_writeFilePath);
                byte[] bufRead = new byte[1024];
                byte[] bufWrite = new byte[1024];
                int read = 0;
                int crc = 0;

                byte[] header = new byte[4] { 0, 0, 0, 0 };
                wfs.Write(header, 0, 4); // 파일 SIZE (일단 아무 값이나 쓴다)
                wfs.Write(header, 0, 4); // CRC
                do
                {
                    read = rfs.Read(bufRead, 0, bufRead.Length);
                    if (read > 0)
                    {
                        Encrypt(bufRead, ref bufWrite, read);
                        wfs.Write(bufWrite, 0, read);
                        crc += GetCRC(bufRead, read);
                    }
                } while (read > 0);

                wfs.Position = 0;
                wfs.Write(BitConverter.GetBytes(read), 0, 4); // 파일 SIZE
                wfs.Write(BitConverter.GetBytes(crc), 0, 4); // CRC
                wfs.Close();
                wfs = null;
                rfs.Close();
                rfs = null;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
            return true;
        }

        public static bool DecryptFile(string _readFilePath, string _writeFilePath)
        {
            if (File.Exists(_readFilePath) == false)
            {
                Debug.LogError("Cannot find file: " + _readFilePath);
                return false;
            }

            try
            {
                FileStream rfs = File.OpenRead(_readFilePath);
                FileStream wfs = File.OpenWrite(_writeFilePath);
                byte[] bufRead = new byte[1024];
                byte[] bufWrite = new byte[1024];
                int read = 0;
                int fileCRC = 0;
                int crc = 0;

                rfs.Read(bufRead, 0, 4); // 파일 SIZE (일단 아무 값이나 쓴다)
                rfs.Read(bufRead, 0, 4); // CRC
                fileCRC = BitConverter.ToInt32(bufRead, 0);
                do
                {
                    read = rfs.Read(bufRead, 0, bufRead.Length);
                    if (read > 0)
                    {
                        Decrypt(bufRead, ref bufWrite, read);
                        wfs.Write(bufWrite, 0, read);
                        crc += GetCRC(bufWrite, read);
                    }
                } while (read > 0);

                wfs.Close();
                wfs = null;
                rfs.Close();
                rfs = null;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
            return true;
        }

        public static string Base64Encode(byte[] data)
        {
            byte[] dest = new byte[data.Length];
            Encrypt(data, ref dest, data.Length);
            return System.Convert.ToBase64String(dest);
        }

        public static byte[] Base64Decode(string base64)
        {
            byte[] encrypted = System.Convert.FromBase64String(base64);
            byte[] data = new byte[encrypted.Length];
            Decrypt(encrypted, ref data, encrypted.Length);
            return data;
        }

        static byte[] encryptData = new byte[]
        {
        0xC0, 0xB9, 0x54, 0x95, 0x56, 0x77, 0xEB, 0x27, 0x89, 0xC9,
        0x80, 0xDE, 0xD3, 0x81, 0xB1, 0xE4, 0x8C, 0xB5, 0x03, 0x4D,
        0xFF, 0xF2, 0x9A, 0x1A, 0x72, 0xE3, 0x4F, 0xFE, 0x1E, 0x30,
        0x21, 0xF1, 0x10, 0x4C, 0x4E, 0xDC, 0x0A, 0xF7, 0x14, 0x6A,
        0xD2, 0xBA, 0xE9, 0x02, 0xEF, 0xAE, 0xE8, 0x0B, 0xA9, 0x85,
        0x92, 0x08, 0xB3, 0x2B, 0x2F, 0xE5, 0x18, 0x23, 0x61, 0xCA,
        0x58, 0xB8, 0x50, 0xAC, 0xA4, 0xBD, 0xE0, 0x31, 0x49, 0x88,
        0xBE, 0xA2, 0x67, 0x15, 0xAB, 0xF6, 0xC6, 0x6E, 0x94, 0x26,
        0x0C, 0xA7, 0x5B, 0x47, 0x97, 0xDB, 0x65, 0x62, 0x7A, 0xD9,
        0x33, 0xC7, 0x84, 0xE7, 0x3E, 0xFB, 0x7F, 0x0D, 0x40, 0xC3,
        0x6D, 0x11, 0x8A, 0xEE, 0xBB, 0x86, 0xF0, 0x2D, 0x24, 0x1D,
        0x5A, 0xE2, 0xE1, 0x7E, 0x9D, 0xB2, 0x43, 0x83, 0xD8, 0x48,
        0x9E, 0x53, 0x2E, 0x06, 0x07, 0x01, 0x00, 0xDD, 0xCC, 0x2C,
        0xEA, 0x3D, 0x44, 0xFA, 0x51, 0x90, 0x3B, 0xEC, 0x09, 0x9B,
        0x4A, 0x70, 0x29, 0x63, 0x36, 0xF9, 0x52, 0x19, 0x2A, 0x41,
        0xCF, 0x3C, 0x37, 0x05, 0x1F, 0xA0, 0xD4, 0xC1, 0x8D, 0x57,
        0xB0, 0x3A, 0xC4, 0x32, 0xA5, 0x79, 0x5D, 0x7B, 0x38, 0x28,
        0xA6, 0xED, 0xB4, 0x66, 0x25, 0xD0, 0xC5, 0xAA, 0xAD, 0xCB,
        0xE6, 0x68, 0x60, 0x35, 0xA8, 0x13, 0xCE, 0xD7, 0xAF, 0x99,
        0xD5, 0x22, 0x8E, 0x0E, 0x59, 0xD1, 0x04, 0x4B, 0xDA, 0xF3,
        0xDF, 0xBC, 0x0F, 0x3F, 0x7D, 0xF4, 0xA3, 0x71, 0x69, 0xD6,
        0x39, 0xFD, 0x17, 0x75, 0x91, 0x1C, 0x96, 0x64, 0x78, 0x55,
        0x98, 0x6C, 0x45, 0x9C, 0x8F, 0xBF, 0xFC, 0x76, 0xF8, 0x5E,
        0x12, 0x16, 0x82, 0xC2, 0xB6, 0x5C, 0xB7, 0x9F, 0x6F, 0x5F,
        0xF5, 0xC8, 0x20, 0x6B, 0x87, 0xCD, 0x1B, 0x42, 0x73, 0x8B,
        0xA1, 0x74, 0x7C, 0x34, 0x93, 0x46
        };
        static byte[] decryptData = new byte[]
        {
        0x7E, 0x7D, 0x2B, 0x12, 0xC4, 0x99, 0x7B, 0x7C, 0x33, 0x8A,
        0x24, 0x2F, 0x50, 0x61, 0xC1, 0xCA, 0x20, 0x65, 0xE6, 0xB9,
        0x26, 0x49, 0xE7, 0xD4, 0x38, 0x93, 0x17, 0xF6, 0xD7, 0x6D,
        0x1C, 0x9A, 0xF2, 0x1E, 0xBF, 0x39, 0x6C, 0xAE, 0x4F, 0x07,
        0xA9, 0x8E, 0x94, 0x35, 0x81, 0x6B, 0x7A, 0x36, 0x1D, 0x43,
        0xA3, 0x5A, 0xFD, 0xB7, 0x90, 0x98, 0xA8, 0xD2, 0xA1, 0x88,
        0x97, 0x83, 0x5E, 0xCB, 0x62, 0x95, 0xF7, 0x74, 0x84, 0xDE,
        0xFF, 0x53, 0x77, 0x44, 0x8C, 0xC5, 0x21, 0x13, 0x22, 0x1A,
        0x3E, 0x86, 0x92, 0x79, 0x02, 0xDB, 0x04, 0x9F, 0x3C, 0xC2,
        0x6E, 0x52, 0xEB, 0xA6, 0xE5, 0xEF, 0xB6, 0x3A, 0x57, 0x8F,
        0xD9, 0x56, 0xAD, 0x48, 0xB5, 0xD0, 0x27, 0xF3, 0xDD, 0x64,
        0x4D, 0xEE, 0x8D, 0xCF, 0x18, 0xF8, 0xFB, 0xD5, 0xE3, 0x05,
        0xDA, 0xA5, 0x58, 0xA7, 0xFC, 0xCC, 0x71, 0x60, 0x0A, 0x0D,
        0xE8, 0x75, 0x5C, 0x31, 0x69, 0xF4, 0x45, 0x08, 0x66, 0xF9,
        0x10, 0x9E, 0xC0, 0xE0, 0x87, 0xD6, 0x32, 0xFE, 0x4E, 0x03,
        0xD8, 0x54, 0xDC, 0xBD, 0x16, 0x8B, 0xDF, 0x72, 0x78, 0xED,
        0x9B, 0xFA, 0x47, 0xCE, 0x40, 0xA4, 0xAA, 0x51, 0xB8, 0x30,
        0xB1, 0x4A, 0x3F, 0xB2, 0x2D, 0xBC, 0xA0, 0x0E, 0x73, 0x34,
        0xAC, 0x11, 0xEA, 0xEC, 0x3D, 0x01, 0x29, 0x68, 0xC9, 0x41,
        0x46, 0xE1, 0x00, 0x9D, 0xE9, 0x63, 0xA2, 0xB0, 0x4C, 0x5B,
        0xF1, 0x09, 0x3B, 0xB3, 0x80, 0xF5, 0xBA, 0x96, 0xAF, 0xC3,
        0x28, 0x0C, 0x9C, 0xBE, 0xD1, 0xBB, 0x76, 0x59, 0xC6, 0x55,
        0x23, 0x7F, 0x0B, 0xC8, 0x42, 0x70, 0x6F, 0x19, 0x0F, 0x37,
        0xB4, 0x5D, 0x2E, 0x2A, 0x82, 0x06, 0x89, 0xAB, 0x67, 0x2C,
        0x6A, 0x1F, 0x15, 0xC7, 0xCD, 0xF0, 0x4B, 0x25, 0xE4, 0x91,
        0x85, 0x5F, 0xE2, 0xD3, 0x1B, 0x14
        };

        public static byte[] Encrypt(string _source)
        {
            byte[] source = UTF8Encoding.UTF8.GetBytes(_source);
            byte[] dest = new byte[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                dest[i] = encryptData[_source[i]];
            }
            return dest;
        }

        public static string EncryptBase64(string _source)
        {
            var data = Encrypt(_source);
            return Base64Encode(data);
        }

        public static string DecryptBase64(string _encrypted)
        {
            var data = Base64Decode(_encrypted);
            return Decrypt(data);
        }

        public static string Decrypt(byte[] _data)
        {
            byte[] source = new byte[_data.Length];
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = decryptData[_data[i]];
            }
            return UTF8Encoding.UTF8.GetString(source);
        }

        public static int Encrypt(byte[] _from, ref byte[] _dest, int _length)
        {
            for (int i = 0; i < _from.Length; i++)
            {
                if (i >= _dest.Length)
                    break;
                _dest[i] = encryptData[_from[i]];
            }
            return UnityEngine.Mathf.Min(_from.Length, _dest.Length);
        }

        public static int Decrypt(byte[] _from, ref byte[] _dest, int _length)
        {
            for (int i = 0; i < _from.Length; i++)
            {
                if (i >= _dest.Length)
                    break;
                _dest[i] = decryptData[_from[i]];
            }
            return Mathf.Min(_from.Length, _dest.Length);
        }
    }
}
