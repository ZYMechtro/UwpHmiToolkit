using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public abstract class SecsDataBase
        {
            public abstract DataItemType Type { get; }

            public virtual int Length => ValueInBytes.Count;

            private byte lengthOfLengthBytes => (byte)(Length > 0xff ? (Length > 0xffff ? 3 : 2) : 1);

            public byte[] LengthInBytes
            {
                get
                {
                    var bs = BitConverter.GetBytes(Length);
                    var result = new byte[lengthOfLengthBytes];
                    Array.Copy(bs, result, lengthOfLengthBytes);
                    ToBigEndian(result);
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

            public SecsDataBase Decode(byte[] bytes)
            {
                //TODO: Decode
                return null;
            }

        }
        public abstract class SecsData<T> : SecsDataBase
        {
            public abstract List<T> Items { get; set; }

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

        }

        public class A : SecsData<char>
        {
            public override DataItemType Type { get; } = DataItemType.ASCII;

            public override List<char> Items { get; set; } = new List<char>();

            public override List<byte> ValueInBytes
                => Encoding.ASCII.GetBytes(new string(Items.ToArray())).ToList();

            public A(string str)
            {
                Items = str.ToList();
            }

            public A(params char[] chars)
            {
                Items.AddRange(chars);
            }

            public A() { }
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
        }
        public class J : SecsData<char>
        {
            public override DataItemType Type { get; } = DataItemType.JIS8;
            public override List<char> Items { get; set; } = new List<char>();

            public override List<byte> ValueInBytes
                => Encoding.ASCII.GetBytes(new string(Items.ToArray())).ToList();

            public J(string str)
            {
                Items = str.ToList();
            }
            public J(params char[] chars)
            {
                Items.AddRange(chars);
            }

            public J() { }
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
                        ToBigEndian(bs);
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
                        ToBigEndian(bs);
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
                        ToBigEndian(bs);
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
                        ToBigEndian(bs);
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
                        ToBigEndian(bs);
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
                        ToBigEndian(bs);
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
                        ToBigEndian(bs);
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
                        ToBigEndian(bs);
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
        }

    }
}
