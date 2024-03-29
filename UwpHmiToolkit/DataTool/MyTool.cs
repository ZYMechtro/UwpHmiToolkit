﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UwpHmiToolkit.DataTools
{
    internal static class DataTool
    {
        public static void ConcateBytes(ref byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            first = ret;
        }

        public static byte[] CombineBytes(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public static byte[] CombineBytes(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Where(x => x != null).Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                if (data != null)
                {
                    Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                    offset += data.Length;
                }
            }
            return ret;
        }

        public static byte[] ReverseIfLittleEndian(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return array;
        }

        public static string RepeatString(string str, int times)
        {
            var result = "";
            for (int i = 0; i < times; i++)
                result += str;
            return result;
        }
    }
}
