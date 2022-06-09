using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nyerguds.Util
{
    public static class ArrayUtils
    {

        public static T[][] SwapDimensions<T>(T[][] original)
        {
            var origHeight = original.Length;
            if (origHeight == 0)
                return Array.Empty<T[]>();
            // Since this is for images, it is assumed that the array is a perfectly rectangular matrix
            var origWidth = original[0].Length;

            var swapped = new T[origWidth][];
            for (var newHeight = 0; newHeight < origWidth; newHeight++)
            {
                swapped[newHeight] = new T[origHeight];
                for (var newWidth = 0; newWidth < origHeight; newWidth++)
                    swapped[newHeight][newWidth] = original[newWidth][newHeight];
            }
            return swapped;
        }

        public static bool ArraysAreEqual<T>(T[] row1, T[] row2) where T : IEquatable<T>
        {
            // There's probably a Linq version of this though... Probably .All() or something.
            // But this is with simple arrays.
            if (row1 == null && row2 == null)
                return true;
            if (row1 == null || row2 == null)
                return false;
            if (row1.Length != row2.Length)
                return false;
            for (var i = 0; i < row1.Length; i++)
                if (row1[i].Equals(row2[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Creates and returns a new array, containing the contents of all the given arrays, in the given order.
        /// </summary>
        /// <typeparam name="T">Type of the arrays</typeparam>
        /// <param name="arrays">Arrays to join together.</param>
        /// <returns>A new array containing the contents of all given arrays, joined together.</returns>
        public static T[] MergeArrays<T>(params T[][] arrays)
        {
            var length = 0;
            foreach (var array in arrays)
            {
                if (array != null)
                    length += array.Length;
            }
            var result = new T[length];
            var copyIndex = 0;
            foreach (var array in arrays)
            {
                if (array != null)
                {
                    array.CopyTo(result, copyIndex);
                    copyIndex += array.Length;
                }
            }
            return result;
        }

        public static void WriteIntToByteArray(byte[] data, int startIndex, int bytes, bool littleEndian, ulong value)
        {
            var lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (var index = 0; index < bytes; index++)
            {
                var offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (byte)(value >> (8 * index) & 0xFF);
            }
        }

        public static ulong ReadIntFromByteArray(ReadOnlySpan<byte> data, bool littleEndian)
        {
            var lastByte = data.Length - 1;
            if (data.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "Data array is too small.");
            ulong value = 0;
            for (var index = 0; index < data.Length; ++index)
            {
                var offs = littleEndian ? index : lastByte - index;
                value += (ulong)(data[offs] << (8 * index));
            }
            return value;
        }

        public static int ReadBitsFromByteArray(ReadOnlySpan<byte> dataArr, ref int bitIndex, int codeLen, int bufferInEnd)
        {
            var intCode = 0;
            var byteIndex = bitIndex / 8;
            var ignoreBitsAtIndex = bitIndex % 8;
            var bitsToReadAtIndex = Math.Min(codeLen, 8 - ignoreBitsAtIndex);
            var totalUsedBits = 0;
            while (codeLen > 0)
            {
                if (byteIndex >= bufferInEnd)
                    return -1;

                var toAdd = (dataArr[byteIndex] >> ignoreBitsAtIndex) & ((1 << bitsToReadAtIndex) - 1);
                intCode |= (toAdd << totalUsedBits);
                totalUsedBits += bitsToReadAtIndex;
                codeLen -= bitsToReadAtIndex;
                bitsToReadAtIndex = Math.Min(codeLen, 8);
                ignoreBitsAtIndex = 0;
                byteIndex++;
            }
            bitIndex += totalUsedBits;
            return intCode;
        }

        public static void WriteBitsToByteArray(Span<byte> dataArr, int bitIndex, int codeLen, int intCode)
        {
            var byteIndex = bitIndex / 8;
            var usedBitsAtIndex = bitIndex % 8;
            var bitsToWriteAtIndex = Math.Min(codeLen, 8 - usedBitsAtIndex);
            while (codeLen > 0)
            {
                var codeToWrite = (intCode & ((1 << bitsToWriteAtIndex) - 1)) << usedBitsAtIndex;
                intCode >>= bitsToWriteAtIndex;
                dataArr[byteIndex] |= (byte)codeToWrite;
                codeLen -= bitsToWriteAtIndex;
                bitsToWriteAtIndex = Math.Min(codeLen, 8);
                usedBitsAtIndex = 0;
                byteIndex++;
            }
        }

        public static T CloneStruct<T>(T obj) where T : struct
        {
            return StructFromByteArray<T>(StructToByteArray(obj));
        }

        public static T StructFromByteArray<T>(byte[] bytes) where T : struct
        {
            return ReadStructFromByteArray<T>(bytes, 0);
        }

        public static byte[] StructToByteArray<T>(T obj) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var target = new byte[size];
            WriteStructToByteArray(obj, target, 0);
            return target;
        }

        public static T ReadStructFromByteArray<T>(byte[] bytes, int offset) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            if (size + offset > bytes.Length)
                throw new InvalidEnumArgumentException("Array is too small to get the requested struct!");
            var ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, offset, ptr, size);
                var obj = Marshal.PtrToStructure(ptr, typeof(T));
                return (T)obj;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public static void WriteStructToByteArray<T>(T obj, byte[] target, int index) where T : struct
        {
            var tType = typeof(T);
            var size = Marshal.SizeOf(tType);
            if (!BitConverter.IsLittleEndian)
            {
                var arr = GetStructBytes(obj, true);
                Array.Copy(arr, 0, target, index, arr.Length);

            }
            var ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, target, index, size);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        static byte[] GetStructBytes<T>(T obj, bool littleEndian)
        {
            var tType = typeof(T);
            if (!tType.IsValueType)
                return Array.Empty<byte>();
            if (tType.IsPrimitive)
                return GetValueTypeBytes((IConvertible)obj, littleEndian);

            var pi = tType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic).OrderBy(f => f.MetadataToken).ToArray();
            var allValuesDict = new Dictionary<int, byte[]>();
            foreach (var info in pi)
            {
                byte[] b = null;
                var propertyType = info.PropertyType;
                if (tType.IsPrimitive)
                    b = GetValueTypeBytes((IConvertible)info.GetValue(obj, null), littleEndian);
                else if (propertyType.IsValueType)
                    b = GetStructBytes(info.GetValue(obj, null), littleEndian);
                allValuesDict.Add(info.MetadataToken, b);
            }
            var fi = tType.GetFields(BindingFlags.Public | BindingFlags.NonPublic).OrderBy(f => f.MetadataToken).ToArray();
            foreach (var info in fi)
            {
                byte[] b = null;
                var propertyType = info.FieldType;
                if (tType.IsPrimitive)
                    b = GetValueTypeBytes((IConvertible)info.GetValue(obj), littleEndian);
                else if (propertyType.IsValueType)
                    b = GetStructBytes(info.GetValue(obj), littleEndian);
                allValuesDict.Add(info.MetadataToken, b);
            }
            var allValues = new byte[allValuesDict.Count][];
            for (var i = 0; i < fi.Length; i++)
            {
                var info = fi[i];
                var fieldType = info.FieldType;
                if (fieldType.IsValueType)
                    allValues[i] = GetValueTypeBytes((IConvertible)info.GetValue(obj), littleEndian);
                else
                    allValues[i] = GetStructBytes(info.GetValue(obj), littleEndian);
            }
            return MergeArrays(allValues);
        }

        static byte[] GetValueTypeBytes<T>(T obj, bool littleEndian) where T : IConvertible
        {
            var tType = typeof(T);
            if (tType == typeof(sbyte)
                || tType == typeof(ushort)
                || tType == typeof(short)
                || tType == typeof(uint)
                || tType == typeof(int)
                || tType == typeof(ulong)
                || tType == typeof(long))
            {
                var len = Marshal.SizeOf(tType);
                var ret = new byte[len];
                WriteIntToByteArray(ret, 0, len, littleEndian, obj.ToUInt64(null));
                return ret;
            }
            var le = BitConverter.IsLittleEndian;
            if (tType == typeof(float))
            {
                var sBytes = BitConverter.GetBytes(obj.ToSingle(null));
                if (!le && littleEndian || !littleEndian)
                    Array.Reverse(sBytes);
                return sBytes;
            }
            if (tType == typeof(double))
            {
                var dBytes = BitConverter.GetBytes(obj.ToDouble(null));
                if (!le && littleEndian || !littleEndian)
                    Array.Reverse(dBytes);
                return dBytes;
            }
            if (tType == typeof(bool))
                return new[] { (byte)((obj as bool?).GetValueOrDefault(false) ? 1 : 0) };
            return Array.Empty<byte>();
        }
    }
}
