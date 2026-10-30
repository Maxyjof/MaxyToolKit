using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MaxyToolKit.Editor
{
    /// <summary>
    /// PSB 导入后记录的单个图层精灵信息，来自 .psb.meta 里的 rigSpriteImportData
    /// </summary>
    public class PSBMetaSprite
    {
        /// <summary>精灵名字，一般等于图层名</summary>
        public string name = string.Empty;
        /// <summary>精灵在文档坐标里的左下角位置，y 轴向上，单位像素</summary>
        public Vector2 documentPosition;
        /// <summary>精灵尺寸，单位像素，已裁掉透明边</summary>
        public Vector2 size;
        /// <summary>精灵轴心，0~1</summary>
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        /// <summary>精灵 ID，导入器内部使用</summary>
        public string spriteID = string.Empty;
    }

    /// <summary>
    /// 读取 PSB 导入后生成的 .meta，取出每个图层的精灵名、尺寸与文档位置
    /// 直接读文本而不是走序列化接口，避免依赖不同版本 PSD Importer 的内部 API
    /// </summary>
    public static class PSBMetaReader
    {
        /// <summary>
        /// 读取指定 PSB 的 .meta 图层精灵列表
        /// </summary>
        /// <param name="psbPath">PSB 资产路径</param>
        /// <returns>按导入顺序排列的精灵信息，文件不存在或没有数据时返回空列表</returns>
        public static List<PSBMetaSprite> Read(string psbPath)
        {
            var result = new List<PSBMetaSprite>();
            if (string.IsNullOrEmpty(psbPath)) return result;
            string metaPath = psbPath + ".meta";
            if (!File.Exists(metaPath)) return result;
            string[] lines = File.ReadAllLines(metaPath, Encoding.UTF8);
            int start = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimEnd().TrimStart().StartsWith("rigSpriteImportData:", StringComparison.Ordinal))
                {
                    start = i + 1;
                    break;
                }
            }
            if (start < 0) return result;
            PSBMetaSprite current = null;
            bool inRect = false;
            for (int i = start; i < lines.Length; i++)
            {
                string raw = lines[i];
                string line = raw.Trim();
                //遇到下一个二级字段（缩进两个空格且不是列表项）就说明本段结束
                if (line.Length == 0) continue;
                if (!raw.StartsWith("    ", StringComparison.Ordinal) && !line.StartsWith("-", StringComparison.Ordinal))
                    break;
                if (line.StartsWith("- name:", StringComparison.Ordinal))
                {
                    current = new PSBMetaSprite { name = ParseName(line.Substring("- name:".Length).Trim()) };
                    result.Add(current);
                    inRect = false;
                    continue;
                }
                if (current == null) continue;
                if (line.StartsWith("pivot:", StringComparison.Ordinal))
                {
                    current.pivot = ParseVector(line.Substring("pivot:".Length).Trim(), current.pivot);
                }
                else if (line.StartsWith("spriteID:", StringComparison.Ordinal))
                {
                    current.spriteID = line.Substring("spriteID:".Length).Trim();
                }
                else if (line.StartsWith("spritePosition:", StringComparison.Ordinal))
                {
                    current.documentPosition = ParseVector(line.Substring("spritePosition:".Length).Trim(), Vector2.zero);
                }
                else if (line.StartsWith("rect:", StringComparison.Ordinal))
                {
                    inRect = true;
                }
                else if (inRect && line.StartsWith("x:", StringComparison.Ordinal))
                {
                    current.size.x = ParseFloat(line.Substring(2).Trim(), current.size.x);
                }
                else if (inRect && line.StartsWith("y:", StringComparison.Ordinal))
                {
                    current.size.y = ParseFloat(line.Substring(2).Trim(), current.size.y);
                }
                else if (inRect && line.StartsWith("width:", StringComparison.Ordinal))
                {
                    current.size.x = ParseFloat(line.Substring("width:".Length).Trim(), current.size.x);
                }
                else if (inRect && line.StartsWith("height:", StringComparison.Ordinal))
                {
                    current.size.y = ParseFloat(line.Substring("height:".Length).Trim(), current.size.y);
                }
                else if (line.StartsWith("spriteBone:", StringComparison.Ordinal))
                {
                    inRect = false;
                }
            }
            return result;
        }

        /// <summary>
        /// 解析 Unity 写出的名字字段，可能是带转义的引号字符串
        /// </summary>
        /// <param name="text">等号后面的原始文本</param>
        /// <returns>还原后的名字</returns>
        static string ParseName(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = text.Trim();
            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
            {
                string inner = text.Substring(1, text.Length - 2);
                var builder = new StringBuilder(inner.Length);
                for (int i = 0; i < inner.Length; i++)
                {
                    char c = inner[i];
                    if (c != '\\' || i + 1 >= inner.Length)
                    {
                        builder.Append(c);
                        continue;
                    }
                    char next = inner[i + 1];
                    if (next == 'u' && i + 5 < inner.Length)
                    {
                        string hex = inner.Substring(i + 2, 4);
                        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                        {
                            builder.Append((char)code);
                            i += 5;
                            continue;
                        }
                    }
                    //常见转义还原，其余字符原样保留
                    switch (next)
                    {
                        case 'n': builder.Append('\n'); break;
                        case 't': builder.Append('\t'); break;
                        case 'r': builder.Append('\r'); break;
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        default: builder.Append(next); break;
                    }
                    i++;
                }
                return builder.ToString();
            }
            return text;
        }

        /// <summary>
        /// 解析形如 {x: 1, y: 2} 的向量
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <param name="fallback">解析失败时的兜底值</param>
        /// <returns>解析出的向量</returns>
        static Vector2 ParseVector(string text, Vector2 fallback)
        {
            if (string.IsNullOrEmpty(text)) return fallback;
            string body = text.Trim();
            if (body.StartsWith("{", StringComparison.Ordinal)) body = body.Substring(1);
            if (body.EndsWith("}", StringComparison.Ordinal)) body = body.Substring(0, body.Length - 1);
            var parts = body.Split(',');
            if (parts.Length < 2) return fallback;
            return new Vector2(
                ParseFloat(parts[0].Replace("x:", string.Empty).Trim(), fallback.x),
                ParseFloat(parts[1].Replace("y:", string.Empty).Trim(), fallback.y));
        }

        /// <summary>
        /// 解析浮点数，兼容 Unity 写出的本地化数字
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <param name="fallback">解析失败时的兜底值</param>
        /// <returns>解析出的数值</returns>
        static float ParseFloat(string text, float fallback)
        {
            if (string.IsNullOrEmpty(text)) return fallback;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value
                : fallback;
        }
    }
}
