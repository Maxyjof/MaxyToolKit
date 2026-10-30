using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// PSB 里的一个图层节点，组节点没有像素，普通叶子节点对应一张图层图
    /// </summary>
    public class PSBLayerNode
    {
        /// <summary>图层名字，优先取 unicode 名称，其次取旧版名称</summary>
        public string name = string.Empty;
        /// <summary>是否是图层组</summary>
        public bool isGroup;
        /// <summary>是否可见，组的不可见会传递给子节点</summary>
        public bool visible = true;
        /// <summary>图层不透明度，0~255</summary>
        public int opacity = 255;
        /// <summary>文档坐标下的图层矩形，x/y 为左下角，y 轴向上，单位像素</summary>
        public Rect rect;
        /// <summary>子节点，按 PSB 自下而上的顺序保存</summary>
        public readonly List<PSBLayerNode> children = new List<PSBLayerNode>();
        /// <summary>父节点，根节点为 null</summary>
        public PSBLayerNode parent;
    }

    /// <summary>
    /// PSB 文件结构读取结果
    /// </summary>
    public class PSBDocument
    {
        /// <summary>PSB 文件路径</summary>
        public string path = string.Empty;
        /// <summary>文档宽度，单位像素</summary>
        public int width;
        /// <summary>文档高度，单位像素</summary>
        public int height;
        /// <summary>根图层，按 PSB 自下而上的顺序保存</summary>
        public readonly List<PSBLayerNode> roots = new List<PSBLayerNode>();
        /// <summary>所有叶子图层，按 PSB 自下而上的顺序拍平</summary>
        public readonly List<PSBLayerNode> leafLayers = new List<PSBLayerNode>();
    }

    /// <summary>
    /// 直接读取 PSB/PSD 二进制里的图层结构，不依赖 PSD Importer 的导入结果
    /// 只解析图层记录，不解析像素数据，因此速度很快
    /// </summary>
    public static class PSBFileParser
    {
        /// <summary>PSB 里用 8 字节长度字段的附加信息块</summary>
        static readonly HashSet<string> LargeBlockKeys = new HashSet<string>
        {
            "LMsk", "Lr16", "Lr32", "Layr", "Mt16", "Mt32", "Mtrn",
            "Alph", "FMsk", "lnk2", "Feid", "FXid", "PxSD", "lnkD", "lnk3"
        };

        /// <summary>
        /// 解析 PSB 文件结构
        /// </summary>
        /// <param name="path">PSB 文件在磁盘上的路径</param>
        /// <returns>图层结构，解析失败时抛出带中文说明的异常</returns>
        public static PSBDocument Parse(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("PSB 路径为空", "path");
            if (!File.Exists(path)) throw new FileNotFoundException("找不到 PSB 文件：" + path);
            byte[] _data = File.ReadAllBytes(path);
            var reader = new BigEndianReader(_data, path);
            //文件头：签名、版本、保留位、通道数、高度、宽度、位深、颜色模式
            string signature = reader.ReadFixedString(4);
            if (signature != "8BPS") throw new InvalidDataException("不是 PSD/PSB 文件：" + path);
            int version = reader.ReadUInt16();
            if (version != 1 && version != 2) throw new InvalidDataException("不支持的 PSD 版本：" + version);
            bool psb = version == 2;
            reader.Skip(6);
            reader.ReadUInt16();
            int height = (int)reader.ReadUInt32();
            int width = (int)reader.ReadUInt32();
            reader.ReadUInt16();
            reader.ReadUInt16();
            //跳过颜色模式数据与图像资源
            reader.Skip(4 + (int)reader.ReadUInt32At(reader.position));
            reader.Skip(4 + (int)reader.ReadUInt32At(reader.position));
            //图层与蒙版信息段：PSB 的长度字段是 8 字节
            if (psb) reader.ReadUInt64();
            else reader.ReadUInt32();
            //图层信息段长度
            long layerInfoLength = psb ? (long)reader.ReadUInt64() : reader.ReadUInt32();
            long layerInfoEnd = reader.position + layerInfoLength;
            int recordCount = Math.Abs(reader.ReadInt16());
            var document = new PSBDocument { path = path, width = width, height = height };
            var groupStack = new List<PSBLayerNode>();
            var rootList = new List<PSBLayerNode>();
            for (int index = 0; index < recordCount; index++)
            {
                if (reader.position + 16 > _data.Length) throw new InvalidDataException("PSB 图层记录被截断，文件可能损坏");
                int top = (int)reader.ReadInt32();
                int left = (int)reader.ReadInt32();
                int bottom = (int)reader.ReadInt32();
                int right = (int)reader.ReadInt32();
                int channelCount = reader.ReadUInt16();
                for (int channel = 0; channel < channelCount; channel++)
                {
                    reader.ReadInt16();
                    if (psb) reader.ReadUInt64();
                    else reader.ReadUInt32();
                }
                reader.ReadFixedString(4);
                reader.ReadFixedString(4);
                int opacity = reader.ReadByte();
                reader.ReadByte();
                int flags = reader.ReadByte();
                reader.ReadByte();
                int extraLength = (int)reader.ReadUInt32();
                var extra = new LayerExtraReader(reader.ReadBytes(extraLength), LargeBlockKeys);
                var layer = extra.ReadLayer(opacity, flags, left, top, right, bottom, height);
                if (layer.groupDivider == 3)
                {
                    //分隔块结束：弹出当前组
                    if (groupStack.Count > 0) groupStack.RemoveAt(groupStack.Count - 1);
                    continue;
                }
                if (layer.groupDivider == 1 || layer.groupDivider == 2)
                {
                    //分隔块开始：创建组并压栈，组自身也占一个层级位置
                    var group = new PSBLayerNode
                    {
                        name = layer.name,
                        isGroup = true,
                        visible = layer.visible,
                        opacity = layer.opacity,
                        rect = new Rect(left, height - bottom, right - left, bottom - top)
                    };
                    AddChild(groupStack, rootList, group);
                    groupStack.Add(group);
                    continue;
                }
                var node = new PSBLayerNode
                {
                    name = layer.name,
                    isGroup = false,
                    visible = layer.visible,
                    opacity = layer.opacity,
                    rect = new Rect(left, height - bottom, right - left, bottom - top)
                };
                AddChild(groupStack, rootList, node);
                document.leafLayers.Add(node);
            }
            PrependGroupFlagFix(document, groupStack);
            document.roots.AddRange(rootList);
            //父节点回填，方便后续计算组包围盒与继承属性
            foreach (var root in document.roots) AssignParent(root, null);
            if (reader.position > layerInfoEnd + 1)
                throw new InvalidDataException("PSB 图层信息解析越界，可能是未知的图层附加块格式");
            return document;
        }

        /// <summary>
        /// 把节点挂到当前所在的组或根列表上
        /// </summary>
        /// <param name="groupStack">当前打开的组栈</param>
        /// <param name="rootList">根节点列表</param>
        /// <param name="node">要挂载的节点</param>
        static void AddChild(List<PSBLayerNode> groupStack, List<PSBLayerNode> rootList, PSBLayerNode node)
        {
            if (groupStack.Count == 0) rootList.Add(node);
            else groupStack[groupStack.Count - 1].children.Add(node);
        }

        /// <summary>
        /// 未闭合的组补一个父级关联，避免后续遍历出现空父节点
        /// </summary>
        /// <param name="document">解析结果</param>
        /// <param name="groupStack">残留的组栈</param>
        static void PrependGroupFlagFix(PSBDocument document, List<PSBLayerNode> groupStack)
        {
            //正常情况下 PSB 的组都会闭合；残留说明文件结构异常，这里只清空栈避免后续误用
            groupStack.Clear();
        }

        /// <summary>
        /// 递归回填父节点引用
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="parent">父节点</param>
        static void AssignParent(PSBLayerNode node, PSBLayerNode parent)
        {
            node.parent = parent;
            foreach (var child in node.children) AssignParent(child, node);
        }

        /// <summary>
        /// 图层附加信息读取器，负责取名称、可见性与组分隔类型
        /// </summary>
        class LayerExtraReader
        {
            readonly byte[] _data;
            readonly HashSet<string> _largeKeys;
            int _position;

            /// <summary>创建一个附加信息读取器</summary>
            /// <param name="buffer">附加信息原始字节</param>
            /// <param name="largeKeys">使用 8 字节长度的块键名</param>
            public LayerExtraReader(byte[] buffer, HashSet<string> _largeKeys)
            {
                _data = buffer ?? Array.Empty<byte>();
                this._largeKeys = _largeKeys;
            }

            /// <summary>
            /// 读取一条图层的附加信息
            /// </summary>
            /// <param name="opacity">不透明度</param>
            /// <param name="flags">标志位</param>
            /// <param name="left">左边界</param>
            /// <param name="top">上边界</param>
            /// <param name="right">右边界</param>
            /// <param name="bottom">下边界</param>
            /// <param name="documentHeight">文档高度</param>
            /// <returns>图层附加信息</returns>
            public LayerExtra ReadLayer(int opacity, int flags, int left, int top, int right, int bottom, int documentHeight)
            {
                var result = new LayerExtra
                {
                    opacity = opacity,
                    visible = (flags & 0x02) == 0
                };
                if (_data.Length < 8) return result;
                //图层蒙版数据与混合范围先跳过
                int maskLength = ReadInt32();
                _position += maskLength;
                int blendLength = ReadInt32();
                _position += blendLength;
                //旧版 Pascal 名称，作为 unicode 名称缺失时的兜底
                if (_position < _data.Length)
                {
                    int nameLength = _data[_position];
                    _position++;
                    int clamped = Mathf.Clamp(nameLength, 0, _data.Length - _position);
                    result.name = Encoding.UTF8.GetString(_data, _position, clamped);
                    _position += clamped;
                    //名称后面按 4 字节对齐
                    int padding = ((nameLength + 1 + 3) / 4) * 4 - (nameLength + 1);
                    _position += Mathf.Clamp(padding, 0, _data.Length - _position);
                }
                //附加信息块：lsct/lsdk 表示组，luni 是 unicode 图层名
                while (_position + 12 <= _data.Length)
                {
                    string signature = ReadFixedString(4);
                    if (signature != "8BIM" && signature != "8B64") break;
                    string key = ReadFixedString(4);
                    long blockLength = _largeKeys.Contains(key) ? (long)ReadUInt64() : (uint)ReadInt32();
                    int blockEnd = (int)Math.Min(_data.Length, _position + blockLength);
                    if (key == "lsct" || key == "lsdk")
                    {
                        if (_position + 4 <= _data.Length) result.groupDivider = ReadInt32();
                    }
                    else if (key == "luni")
                    {
                        if (_position + 4 <= _data.Length)
                        {
                            int charCount = ReadInt32();
                            int byteCount = Mathf.Clamp(charCount * 2, 0, _data.Length - _position);
                            if (byteCount > 0) result.name = Encoding.BigEndianUnicode.GetString(_data, _position, byteCount);
                        }
                    }
                    _position = blockEnd;
                }
                return result;
            }

            /// <summary>读取一个大端整数</summary>
            /// <returns>读取到的值</returns>
            int ReadInt32()
            {
                if (_position + 4 > _data.Length) return 0;
                int value = (_data[_position] << 24) | (_data[_position + 1] << 16) | (_data[_position + 2] << 8) | _data[_position + 3];
                _position += 4;
                return value;
            }

            /// <summary>读取一个大端无符号 64 位整数</summary>
            /// <returns>读取到的值</returns>
            ulong ReadUInt64()
            {
                ulong value = ((ulong)ReadInt32() << 32) | (uint)ReadInt32();
                return value;
            }

            /// <summary>读取固定长度字符串</summary>
            /// <param name="length">字节数</param>
            /// <returns>ASCII 字符串</returns>
            string ReadFixedString(int length)
            {
                int clamped = Mathf.Clamp(length, 0, _data.Length - _position);
                string value = Encoding.ASCII.GetString(_data, _position, clamped);
                _position += clamped;
                return value;
            }
        }

        /// <summary>单条图层的附加信息</summary>
        class LayerExtra
        {
            /// <summary>图层名字</summary>
            public string name = string.Empty;
            /// <summary>是否可见</summary>
            public bool visible = true;
            /// <summary>不透明度</summary>
            public int opacity = 255;
            /// <summary>组分隔类型，1/2 为组开始，3 为组结束，0 表示普通图层</summary>
            public int groupDivider;
        }
    }

    /// <summary>
    /// 大端字节读取器，PSD/PSB 全部使用大端字节序
    /// </summary>
    class BigEndianReader
    {
        readonly byte[] _data;
        readonly string _source;
        int _offset;

        /// <summary>创建一个读取器</summary>
        /// <param name="buffer">文件字节</param>
        /// <param name="sourceName">来源路径，仅用于报错</param>
        public BigEndianReader(byte[] buffer, string sourceName)
        {
            _data = buffer;
            _source = sourceName;
        }

        /// <summary>当前读取位置</summary>
        public int position => _offset;

        /// <summary>跳过指定字节</summary>
        /// <param name="count">字节数</param>
        public void Skip(int count) => _offset += count;

        /// <summary>读取指定位置的 32 位无符号整数，不移动读取位置</summary>
        /// <param name="at">目标位置</param>
        /// <returns>读取到的值</returns>
        public uint ReadUInt32At(int at)
        {
            Ensure(at, 4);
            return ((uint)_data[at] << 24) | ((uint)_data[at + 1] << 16) | ((uint)_data[at + 2] << 8) | _data[at + 3];
        }

        /// <summary>读取 16 位无符号整数</summary>
        /// <returns>读取到的值</returns>
        public ushort ReadUInt16()
        {
            Ensure(_offset, 2);
            int value = (_data[_offset] << 8) | _data[_offset + 1];
            _offset += 2;
            return (ushort)value;
        }

        /// <summary>读取 16 位有符号整数</summary>
        /// <returns>读取到的值</returns>
        public short ReadInt16() => (short)ReadUInt16();

        /// <summary>读取 32 位无符号整数</summary>
        /// <returns>读取到的值</returns>
        public uint ReadUInt32()
        {
            Ensure(_offset, 4);
            uint value = ((uint)_data[_offset] << 24) | ((uint)_data[_offset + 1] << 16) | ((uint)_data[_offset + 2] << 8) | _data[_offset + 3];
            _offset += 4;
            return value;
        }

        /// <summary>读取 32 位有符号整数</summary>
        /// <returns>读取到的值</returns>
        public int ReadInt32() => (int)ReadUInt32();

        /// <summary>读取 64 位无符号整数</summary>
        /// <returns>读取到的值</returns>
        public ulong ReadUInt64()
        {
            ulong high = ReadUInt32();
            ulong low = ReadUInt32();
            return (high << 32) | low;
        }

        /// <summary>读取一个字节</summary>
        /// <returns>读取到的值</returns>
        public byte ReadByte()
        {
            Ensure(_offset, 1);
            return _data[_offset++];
        }

        /// <summary>读取指定长度字节</summary>
        /// <param name="count">字节数</param>
        /// <returns>字节数组</returns>
        public byte[] ReadBytes(int count)
        {
            Ensure(_offset, count);
            var result = new byte[count];
            Array.Copy(_data, _offset, result, 0, count);
            _offset += count;
            return result;
        }

        /// <summary>读取固定长度 ASCII 字符串</summary>
        /// <param name="count">字节数</param>
        /// <returns>字符串</returns>
        public string ReadFixedString(int count) => Encoding.ASCII.GetString(ReadBytes(count));

        /// <summary>校验剩余长度，避免越界读取</summary>
        /// <param name="at">起始位置</param>
        /// <param name="count">需要的字节数</param>
        void Ensure(int at, int count)
        {
            if (at < 0 || count < 0 || at + count > _data.Length)
                throw new InvalidDataException("PSB 读取越界：" + _source + "（位置 " + at + "，长度 " + count + "）");
        }
    }
}
