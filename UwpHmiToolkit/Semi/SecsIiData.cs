using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using static UwpHmiToolkit.DataTools.DataTool;

namespace UwpHmiToolkit.Semi
{
    public partial class SecsII
    {
        public enum DataItemType : byte
        {
            List = 0b0000_00,
            Binary = 0b0010_00,
            Boolean = 0b0010_01,
            ASCII = 0b0100_00,
            JIS8 = 0b0100_01,
            I8 = 0b0110_00,
            I1 = 0b0110_01,
            I2 = 0b0110_10,
            I4 = 0b0111_00,
            F8 = 0b1000_00,
            F4 = 0b1001_00,
            U8 = 0b1010_00,
            U1 = 0b1010_01,
            U2 = 0b1010_10,
            U4 = 0b1011_00,
        }

        public static byte[] EncodeSecsII(SecsDataBase secsData) => secsData is null ? null : secsData.Encode.ToArray();

        private static SecsDataBase DecodeSecsII(byte[] bytes, ref int index)
        {
            var b = bytes[index];
            //Get length
            var lbl = b & 0b_11;
            var ls = new byte[4];
            for (int j = 0; j < lbl; j++)
                ls[j] = bytes[index + lbl - j];
            var length = BitConverter.ToInt32(ls, 0);
            index += 1 + lbl;
            try
            {
                switch (b >> 2)
                {
                    default:
                        return null;
                    case (byte)DataItemType.List:
                        {
                            L list = new L();
                            var handledItemCount = 0;
                            while (handledItemCount < length)
                            {
                                list.Items.Add(DecodeSecsII(bytes, ref index));
                                handledItemCount++;
                            }
                            return list;
                        }
                    case (byte)DataItemType.Binary:
                        {
                            B binarys = new B();
                            for (int i = 0; i < length; i++)
                            {
                                binarys.Items.Add(bytes[index]);
                                index += 1;
                            }
                            return binarys;
                        }
                    case (byte)DataItemType.Boolean:
                        {
                            TF bools = new TF();
                            for (int i = 0; i < length; i++)
                            {
                                bools.Items.Add(bytes[index]);
                                index += 1;
                            }
                            return bools;
                        }
                    case (byte)DataItemType.ASCII:
                        {
                            var bs = Encoding.ASCII.GetString(bytes, index, length);
                            A asc = new A(bs);
                            index += length;
                            return asc;
                        }
                    case (byte)DataItemType.JIS8:
                        {
                            //TODO: Understand jis8
                            var bs = Encoding.ASCII.GetString(bytes, index, length);
                            J asc = new J(bs);
                            index += length;
                            return asc;
                        }
                    case (byte)DataItemType.I1:
                        {
                            I1 i1 = new I1();
                            for (int i = 0; i < length; i++)
                            {
                                i1.Items.Add((sbyte)bytes[index]);
                                index += 1;
                            }
                            return i1;
                        }
                    case (byte)DataItemType.I2:
                        {
                            const int size = 2;
                            I2 i2 = new I2();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1]
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToInt16(bs, 0);
                                i2.Items.Add(value);
                                index += size;
                            }
                            return i2;
                        }
                    case (byte)DataItemType.I4:
                        {
                            const int size = 4;
                            I4 i4 = new I4();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToInt32(bs, 0);
                                i4.Items.Add(value);
                                index += size;
                            }
                            return i4;
                        }
                    case (byte)DataItemType.I8:
                        {
                            const int size = 8;
                            I8 i8 = new I8();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                bytes[index + 4],
                                bytes[index + 5],
                                bytes[index + 6],
                                bytes[index + 7],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToInt64(bs, 0);
                                i8.Items.Add(value);
                                index += size;
                            }
                            return i8;
                        }
                    case (byte)DataItemType.U1:
                        {
                            U1 u1 = new U1();
                            for (int i = 0; i < length; i++)
                            {
                                u1.Items.Add(bytes[index]);
                                index += 1;
                            }
                            return u1;
                        }
                    case (byte)DataItemType.U2:
                        {
                            const int size = 2;
                            U2 u2 = new U2();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1]
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToUInt16(bs, 0);
                                u2.Items.Add(value);
                                index += size;
                            }
                            return u2;
                        }
                    case (byte)DataItemType.U4:
                        {
                            const int size = 4;
                            U4 u4 = new U4();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToUInt32(bs, 0);
                                u4.Items.Add(value);
                                index += size;
                            }
                            return u4;
                        }
                    case (byte)DataItemType.U8:
                        {
                            const int size = 8;
                            U8 u8 = new U8();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                bytes[index + 4],
                                bytes[index + 5],
                                bytes[index + 6],
                                bytes[index + 7],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToUInt64(bs, 0);
                                u8.Items.Add(value);
                                index += size;
                            }
                            return u8;
                        }
                    case (byte)DataItemType.F4:
                        {
                            const int size = 4;
                            F4 f4 = new F4();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToSingle(bs, 0);
                                f4.Items.Add(value);
                                index += size;
                            }
                            return f4;
                        }
                    case (byte)DataItemType.F8:
                        {
                            const int size = 8;
                            F8 f8 = new F8();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                bytes[index + 4],
                                bytes[index + 5],
                                bytes[index + 6],
                                bytes[index + 7],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToDouble(bs, 0);
                                f8.Items.Add(value);
                                index += size;
                            }
                            return f8;
                        }

                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return null;
            }
        }

        public static SecsDataBase DecodeSecsII(byte[] bytes)
        {
            var index = 0;
            var b = bytes[index];
            //Get length
            var lbl = b & 0b_11;
            var ls = new byte[4];
            for (int j = 0; j < lbl; j++)
                ls[j] = bytes[index + lbl - j];
            var length = BitConverter.ToInt32(ls, 0);
            index += 1 + lbl;
            try
            {
                switch (b >> 2)
                {
                    default:
                        return null;
                    case (byte)DataItemType.List:
                        {
                            L list = new L();
                            var handledItemCount = 0;
                            while (handledItemCount < length)
                            {
                                list.Items.Add(DecodeSecsII(bytes, ref index));
                                handledItemCount++;
                            }
                            return list;
                        }
                    case (byte)DataItemType.Binary:
                        {
                            B binarys = new B();
                            for (int i = 0; i < length; i++)
                            {
                                binarys.Items.Add(bytes[index]);
                                index += 1;
                            }
                            return binarys;
                        }
                    case (byte)DataItemType.Boolean:
                        {
                            TF bools = new TF();
                            for (int i = 0; i < length; i++)
                            {
                                bools.Items.Add(bytes[index]);
                                index += 1;
                            }
                            return bools;
                        }
                    case (byte)DataItemType.ASCII:
                        {
                            var bs = Encoding.ASCII.GetString(bytes, index, length);
                            A asc = new A(bs);
                            index += length;
                            return asc;
                        }
                    case (byte)DataItemType.JIS8:
                        {
                            var bs = Encoding.ASCII.GetString(bytes, index, length);
                            J asc = new J(bs);
                            index += length;
                            return asc;
                        }
                    case (byte)DataItemType.I1:
                        {
                            I1 i1 = new I1();
                            for (int i = 0; i < length; i++)
                            {
                                i1.Items.Add((sbyte)bytes[index]);
                                index += 1;
                            }
                            return i1;
                        }
                    case (byte)DataItemType.I2:
                        {
                            const int size = 2;
                            I2 i2 = new I2();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1]
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToInt16(bs, 0);
                                i2.Items.Add(value);
                                index += size;
                            }
                            return i2;
                        }
                    case (byte)DataItemType.I4:
                        {
                            const int size = 4;
                            I4 i4 = new I4();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToInt32(bs, 0);
                                i4.Items.Add(value);
                                index += size;
                            }
                            return i4;
                        }
                    case (byte)DataItemType.I8:
                        {
                            const int size = 8;
                            I8 i8 = new I8();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                bytes[index + 4],
                                bytes[index + 5],
                                bytes[index + 6],
                                bytes[index + 7],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToInt64(bs, 0);
                                i8.Items.Add(value);
                                index += size;
                            }
                            return i8;
                        }
                    case (byte)DataItemType.U1:
                        {
                            U1 u1 = new U1();
                            for (int i = 0; i < length; i++)
                            {
                                u1.Items.Add(bytes[index]);
                                index += 1;
                            }
                            return u1;
                        }
                    case (byte)DataItemType.U2:
                        {
                            const int size = 2;
                            U2 u2 = new U2();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1]
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToUInt16(bs, 0);
                                u2.Items.Add(value);
                                index += size;
                            }
                            return u2;
                        }
                    case (byte)DataItemType.U4:
                        {
                            const int size = 4;
                            U4 u4 = new U4();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToUInt32(bs, 0);
                                u4.Items.Add(value);
                                index += size;
                            }
                            return u4;
                        }
                    case (byte)DataItemType.U8:
                        {
                            const int size = 8;
                            U8 u8 = new U8();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                bytes[index + 4],
                                bytes[index + 5],
                                bytes[index + 6],
                                bytes[index + 7],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToUInt64(bs, 0);
                                u8.Items.Add(value);
                                index += size;
                            }
                            return u8;
                        }
                    case (byte)DataItemType.F4:
                        {
                            const int size = 4;
                            F4 f4 = new F4();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToSingle(bs, 0);
                                f4.Items.Add(value);
                                index += size;
                            }
                            return f4;
                        }
                    case (byte)DataItemType.F8:
                        {
                            const int size = 8;
                            F8 f8 = new F8();
                            for (int i = 0; i < length; i += size)
                            {
                                var bs = new byte[size]
                                {
                                bytes[index],
                                bytes[index + 1],
                                bytes[index + 2],
                                bytes[index + 3],
                                bytes[index + 4],
                                bytes[index + 5],
                                bytes[index + 6],
                                bytes[index + 7],
                                };
                                ReverseIfLittleEndian(bs);
                                var value = BitConverter.ToDouble(bs, 0);
                                f8.Items.Add(value);
                                index += size;
                            }
                            return f8;
                        }

                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return null;
            }
        }


        public abstract class SecsDataBase
        {
            public abstract DataItemType Type { get; }

            public virtual int Length => ValueInBytes.Count;

            public bool IsEmpty => ValueInBytes.Count == 0;

            private byte lengthOfLengthBytes => (byte)(Length > 0xff ? (Length > 0xffff ? 3 : 2) : 1);

            public byte[] LengthInBytes
            {
                get
                {
                    var bs = BitConverter.GetBytes(Length);
                    var result = new byte[lengthOfLengthBytes];
                    Array.Copy(bs, result, lengthOfLengthBytes);
                    ReverseIfLittleEndian(result);
                    return result;
                }
            }

            public byte TypeCode => (byte)(((byte)Type << 2) + lengthOfLengthBytes);

            public abstract List<byte> ValueInBytes { get; }

            public List<byte> Encode
            {
                get
                {
                    List<byte> lbs = new List<byte>();
                    lbs.Add(TypeCode);
                    lbs.AddRange(LengthInBytes);
                    lbs.AddRange(ValueInBytes);
                    return lbs;
                }
            }

            /// <summary>
            /// Get SECS Message Language format string. (Easier to read)
            /// </summary>
            /// <returns></returns>
            public abstract string ToSML(int indentLevel);

            protected const string space = " ";

            protected string AddBracket(string str, int indentLevel)
            {
                var sml = "";
                sml += RepeatString(space, indentLevel) + "<";
                sml += str;
                if (sml.Last() == '\n')
                    sml += RepeatString(space, indentLevel);
                sml += ">";
                if (indentLevel == 0)
                    sml += ".";
                return sml;
            }

        }
        public abstract class SecsData<T> : SecsDataBase
        {
            public abstract List<T> Items { get; set; }

            public virtual object GetItem(int index)
            {
                if (Items.ElementAtOrDefault(index) != null)
                    return Items[index];
                else
                    return null;
            }

            public virtual int Count => Items.Count;
        }

        public class L : SecsData<SecsDataBase>
        {
            public override DataItemType Type { get; } = DataItemType.List;

            public override int Length => Items.Count;

            public override List<SecsDataBase> Items { get; set; } = new List<SecsDataBase>();

            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        rs.AddRange(item.Encode);
                    }
                    return rs;
                }
            }

            public L(IEnumerable<SecsDataBase> items)
            {
                Items.AddRange(items);
            }

            public L(params SecsDataBase[] secsDatas)
            {
                Items.AddRange(secsDatas);
            }

            public L() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"L [{Items.Count}]";
                sml += "\n";
                foreach (var item in Items)
                {
                    sml += item.ToSML(indentLevel + 2);
                    sml += "\n";
                }
                var result = AddBracket(sml, indentLevel);
                return result;
            }

        }

        public class A : SecsData<char>
        {
            public override DataItemType Type { get; } = DataItemType.ASCII;

            public override List<char> Items { get; set; } = new List<char>();

            public override List<byte> ValueInBytes
                => Encoding.ASCII.GetBytes(new string(Items.ToArray())).ToList();

            public string GetString => !IsEmpty ? new string(Items.ToArray()) : null;

            public A(string str)
            {
                if (str != null)
                    Items = str.ToList();
            }

            public A(params char[] chars)
            {
                Items.AddRange(chars);
            }

            public A() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"A [{Items.Count}]";
                sml += " \"";
                sml += Encoding.ASCII.GetString(ValueInBytes.ToArray());
                sml += "\"";

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class B : SecsData<byte>
        {
            public override DataItemType Type { get; } = DataItemType.Binary;

            public override List<byte> Items { get; set; } = new List<byte>();

            public override List<byte> ValueInBytes => Items;

            public B(IEnumerable<byte> binarys)
            {
                Items.AddRange(binarys);
            }

            public B(params byte[] bytes)
            {
                Items.AddRange(bytes);
            }

            public B() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"B [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class TF : SecsData<byte>
        {
            public override DataItemType Type { get; } = DataItemType.Boolean;

            public override List<byte> Items { get; set; } = new List<byte>();

            public override List<byte> ValueInBytes => Items;

            public TF(IEnumerable<byte> binarys)
            {
                Items.AddRange(binarys);
            }

            public TF(params bool[] bools)
            {
                foreach (var b in bools)
                    Items.Add((byte)(b ? 1 : 0));
            }
            public TF() { }
            public override string ToSML(int indentLevel)
            {
                var sml = $"Boolean [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + (item == 0 ? "0" : "1");
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class J : SecsData<char>
        {
            public override DataItemType Type { get; } = DataItemType.JIS8;
            public override List<char> Items { get; set; } = new List<char>();

            public override List<byte> ValueInBytes
                => Encoding.ASCII.GetBytes(new string(Items.ToArray())).ToList();

            public J(string str)
            {
                if (str != null)
                    Items = str.ToList();
            }
            public J(params char[] chars)
            {
                Items.AddRange(chars);
            }

            public J() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"J [{Items.Count}]";
                sml += " \"";
                sml += Encoding.ASCII.GetString(ValueInBytes.ToArray());
                sml += RepeatString(space, indentLevel) + "\"";

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class I1 : SecsData<sbyte>
        {
            public override DataItemType Type { get; } = DataItemType.I1;

            public override List<sbyte> Items { get; set; } = new List<sbyte>();

            public override List<byte> ValueInBytes
                => Array.ConvertAll(Items.ToArray(), i => unchecked((byte)i)).ToList();

            public I1(IEnumerable<sbyte> value)
            {
                Items.AddRange(value);
            }

            public I1(params sbyte[] sbytes)
            {
                Items.AddRange(sbytes);
            }

            public I1() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"I1 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class I2 : SecsData<short>
        {
            public override DataItemType Type { get; } = DataItemType.I2;

            public override List<short> Items { get; set; } = new List<short>();

            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public I2(IEnumerable<short> value)
            {
                Items.AddRange(value);
            }

            public I2(params short[] shorts)
            {
                Items.AddRange(shorts);
            }

            public I2() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"I2 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class I4 : SecsData<int>
        {
            public override DataItemType Type { get; } = DataItemType.I4;
            public override List<int> Items { get; set; } = new List<int>();
            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public I4(IEnumerable<int> value)
            {
                Items.AddRange(value);
            }

            public I4(params int[] values)
            {
                Items.AddRange(values);
            }

            public I4() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"I4 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class I8 : SecsData<long>
        {
            public override DataItemType Type { get; } = DataItemType.I8;
            public override List<long> Items { get; set; } = new List<long>();
            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public I8(IEnumerable<long> value)
            {
                Items.AddRange(value);
            }

            public I8(params long[] values)
            {
                Items.AddRange(values);
            }

            public I8() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"I8 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }


        public class U1 : SecsData<byte>
        {
            public override DataItemType Type { get; } = DataItemType.U1;

            public override List<byte> Items { get; set; } = new List<byte>();

            public override List<byte> ValueInBytes => Items;

            public U1(IEnumerable<byte> value)
            {
                Items.AddRange(value);
            }

            public U1(params byte[] values)
            {
                Items.AddRange(values);
            }

            public U1() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"U1 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class U2 : SecsData<ushort>
        {
            public override DataItemType Type { get; } = DataItemType.U2;

            public override List<ushort> Items { get; set; } = new List<ushort>();

            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public U2(IEnumerable<ushort> value)
            {
                Items.AddRange(value);
            }
            public U2(params ushort[] values)
            {
                Items.AddRange(values);
            }

            public U2() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"U2 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class U4 : SecsData<uint>
        {
            public override DataItemType Type { get; } = DataItemType.U4;
            public override List<uint> Items { get; set; } = new List<uint>();
            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public U4(IEnumerable<uint> value)
            {
                Items.AddRange(value);
            }

            public U4(params uint[] values)
            {
                Items.AddRange(values);
            }

            public U4() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"U4 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class U8 : SecsData<ulong>
        {
            public override DataItemType Type { get; } = DataItemType.U8;
            public override List<ulong> Items { get; set; } = new List<ulong>();
            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public U8(IEnumerable<ulong> value)
            {
                Items.AddRange(value);
            }
            public U8(params ulong[] values)
            {
                Items.AddRange(values);
            }

            public U8() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"U8 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class F4 : SecsData<float>
        {
            public override DataItemType Type { get; } = DataItemType.F4;
            public override List<float> Items { get; set; } = new List<float>();
            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public F4(IEnumerable<float> value)
            {
                Items.AddRange(value);
            }
            public F4(params float[] values)
            {
                Items.AddRange(values);
            }

            public F4() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"F4 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

        public class F8 : SecsData<double>
        {
            public override DataItemType Type { get; } = DataItemType.F8;
            public override List<double> Items { get; set; } = new List<double>();
            public override List<byte> ValueInBytes
            {
                get
                {
                    var rs = new List<byte>();
                    foreach (var item in Items)
                    {
                        var bs = BitConverter.GetBytes(item);
                        ReverseIfLittleEndian(bs);
                        rs.AddRange(bs);
                    }
                    return rs;
                }
            }

            public F8(IEnumerable<double> value)
            {
                Items.AddRange(value);
            }
            public F8(params double[] values)
            {
                Items.AddRange(values);
            }

            public F8() { }

            public override string ToSML(int indentLevel)
            {
                var sml = $"F8 [{Items.Count}]";
                foreach (var item in Items)
                {
                    sml += space + item.ToString();
                }

                var result = AddBracket(sml, indentLevel);
                return result;
            }
        }

    }
}
